/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Editor.PointCloud.Trees
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.MemoryMappedFiles;
    using Simulator.PointCloud;
    using Simulator.PointCloud.Trees;
    using Unity.Collections.LowLevel.Unsafe;

    /// <summary>
    /// Base class for processors used to build specific node trees.
    /// </summary>
    public abstract class TreeNodeProcessor : ParallelProcessor
    {
        /// <summary>
        /// List of new child nodes created during last task.
        /// </summary>
        protected readonly List<NodeRecord> ChildNodeRecords;

        /// <summary>
        /// Settings used during the tree building process.
        /// </summary>
        protected readonly TreeImportSettings Settings;

        private readonly PointCloudPoint[] inputBuffer;

        private readonly string dataPath;
        private readonly string tmpFolderPath;
        private readonly int stride;

        protected IOrganizedPointCollection PointCollection;
        protected IOrganizedPointCollection VolatilePointCollection;

        /// <summary>
        /// Buffers for storing points that should be passed to children.
        /// <para>Has to be initialized in derived class constructor.</para>
        /// </summary>
        protected PointCloudPoint[][] ChildBuffers;

        /// <summary>
        /// <para>Array for storing current count of valid points in <see cref="ChildBuffers"/>.</para>
        /// <para>Has to be initialized in derived class constructor.</para>
        /// </summary>
        protected int[] ChildCounts;

        /// <summary>
        /// <para>Array for storing current count of temporary children files.</para>
        /// <para>Has to be initialized in derived class constructor.</para>
        /// </summary>
        protected int[] ChildFileCounts;

        private int inputCount;
        private bool nodeDiscarded;

        /// <summary>
        /// Amount of points fully processed by this instance across all tasks up to this point.
        /// </summary>
        public int FinishedPoints { get; private set; }

        /// <summary>
        /// Amount of points discarded by this instance (because of density) across all tasks up to this point.
        /// </summary>
        public int DiscardedPoints { get; private set; }

        protected TreeNodeProcessor(string dataPath, TreeImportSettings settings, TreeImportData importData)
        {
            Settings = settings;
            this.dataPath = dataPath;

            tmpFolderPath = Path.Combine(dataPath, "tmp");

            stride = UnsafeUtility.SizeOf<PointCloudPoint>();
            ChildNodeRecords = new List<NodeRecord>();
            inputBuffer = new PointCloudPoint[settings.chunkSize];

            if (settings.sampling == TreeImportSettings.SamplingMethod.CellCenter)
            {
                PointCollection = new CellCenterPointCollection();
                VolatilePointCollection = new CellCenterPointCollection();
            }
            else
            {
                PointCollection = new PoissonDiskPointCollection();
                VolatilePointCollection = new CellCenterPointCollection();
            }

            PointCollection.Initialize(settings, importData);
            VolatilePointCollection.Initialize(settings, importData);
        }

        /// <summary>
        /// Pulls children created during last processing into passed list.
        /// </summary>
        /// <param name="resultsTarget">List that should be populated with created children records.</param>
        /// <param name="nodeCreated">True if node was actually created during last processing, false otherwise.</param>
        public void PullResults(List<NodeRecord> resultsTarget, out bool nodeCreated)
        {
            foreach (var childNodeRecord in ChildNodeRecords)
                resultsTarget.Add(childNodeRecord);

            nodeCreated = !nodeDiscarded;

            ClearState();
        }

        /// <summary>
        /// Loops over all points that should be processed and assigns them either to given node or one of the children.
        /// </summary>
        /// <param name="record">Record of the node that should be processed.</param>
        protected override void DoWorkInternal(NodeRecord record)
        {
            ClearState();

            PointCollection.UpdateForNode(record);
            if ((record.Identifier.Length > Settings.maxTreeDepth) || (PointCollection.MinDistance < Settings.minPointDistance))
                nodeDiscarded = true;

            if (nodeDiscarded)
            {
                // Node is discarded - remove all temporary files for it and update progress values
                var inputIndex = 0;
                while (true)
                {
                    var inputFilePath = Path.Combine(tmpFolderPath, $"{record.Identifier}_tmp{inputIndex.ToString()}");
                    if (!File.Exists(inputFilePath))
                        break;

                    var size = new FileInfo(inputFilePath).Length;
                    var pointCount = (int) (size / stride);

                    FinishedPoints += pointCount;
                    DiscardedPoints += pointCount;

                    File.Delete(inputFilePath);
                    inputIndex++;
                }
            }
            else
            {
                var inputIndex = 0;
                while (true)
                {
                    // Load previously saved temporary files. If there is no next file, processing is done.
                    var inputFilePath = Path.Combine(tmpFolderPath, $"{record.Identifier}_tmp{inputIndex.ToString()}");
                    if (!File.Exists(inputFilePath))
                        break;

                    LoadInputFile(inputFilePath);
                    File.Delete(inputFilePath);

                    inputIndex++;

                    for (var i = 0; i < inputCount; ++i)
                    {
                        if (cancelFlag)
                            return;

                        var point = inputBuffer[i];

                        if (PointCollection.TryAddPoint(point, out var replacedPoint))
                        {
                            if (replacedPoint != null)
                                PassPointToChild(replacedPoint.Value);
                            else
                                FinishedPoints++;
                        }
                        else
                            PassPointToChild(point);
                    }
                }

                if (Settings.generateMesh &&
                    Settings.meshDetailLevel == NodeRecord.Identifier.Length &&
                    PointCollection is CellCenterPointCollection collection)
                {
                    // var path = Path.Combine(dataPath, NodeRecord.Identifier + ".pcMesh");
                    collection.FlushMeshGenerationData(tmpFolderPath);
                }

                // Work is finished, make sure to flush all children data...
                for (var i = 0; i < ChildCounts.Length; ++i)
                {
                    if (ChildCounts[i] > 0)
                        FlushChildFile((byte) i);
                }

                // ...and data of this node
                SaveNodeToDisk();
            }
        }

        /// <summary>
        /// Clears internal state of this instance. Should be called after pulling results.
        /// </summary>
        private void ClearState()
        {
            nodeDiscarded = false;
            PointCollection.ClearState();
            VolatilePointCollection.ClearState();
            ChildNodeRecords.Clear();

            for (var i = 0; i < ChildCounts.Length; ++i)
                ChildCounts[i] = 0;

            for (var i = 0; i < ChildFileCounts.Length; ++i)
                ChildFileCounts[i] = 0;
        }

        /// <summary>
        /// Loads points from file under specified path to this instance's buffers.
        /// </summary>
        /// <param name="path"></param>
        private void LoadInputFile(string path)
        {
            var size = new FileInfo(path).Length;
            inputCount = (int) (size / stride);

            using (var mmf = MemoryMappedFile.CreateFromFile(path, FileMode.Open))
            {
                using (var accessor = mmf.CreateViewAccessor(0, size))
                {
                    unsafe
                    {
                        try
                        {
                            byte* sourcePtr = null;
                            accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref sourcePtr);

                            fixed (PointCloudPoint* targetPtr = inputBuffer)
                            {
                                UnsafeUtility.MemCpy(targetPtr, sourcePtr, size);
                            }
                        }
                        finally
                        {
                            accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// <para>For child with given index, flushes points stored in this instance's buffer to disk.</para>
        /// <para>This will create either temporary or final file, based on current state and settings.</para>
        /// </summary>
        protected void FlushChildFile(byte childIndex)
        {
            var nextLevelRequired =
                Settings.generateMesh &&
                (NodeRecord.Identifier.Length < Settings.meshDetailLevel);

            var final =
                (ChildFileCounts[childIndex] == 0) &&
                (ChildCounts[childIndex] < Settings.nodeBranchThreshold) &&
                !nextLevelRequired;

            if (final)
            {
                FilterAndFlushLeafNode(childIndex);
            }
            else
            {
                var fileName = $"{NodeRecord.Identifier}{childIndex.ToString()}_tmp{(ChildFileCounts[childIndex]++).ToString()}";
                var path = Path.Combine(tmpFolderPath, fileName);
                var size = (long) ChildCounts[childIndex] * stride;

                ChildCounts[childIndex] = 0;

                SaveToFile(path, fileName, size, ChildBuffers[childIndex]);
            }
        }

        private void FilterAndFlushLeafNode(byte childIndex)
        {
            var fileName = $"{NodeRecord.Identifier}{childIndex.ToString()}";
            NodeRecord childRecord = null;

            foreach (var record in ChildNodeRecords)
            {
                if (record.Identifier == fileName)
                {
                    childRecord = record;
                    break;
                }
            }

            if (childRecord == null)
                throw new Exception($"Unable to find record for node with ID {fileName}.");

            VolatilePointCollection.ClearState();
            VolatilePointCollection.UpdateForNode(childRecord, Settings.minPointDistance, true);

            for (var i = 0; i < ChildCounts[childIndex]; ++i)
            {
                // This can take a while, make sure to respond to cancel request
                if (cancelFlag)
                    return;

                var point = ChildBuffers[childIndex][i];
                VolatilePointCollection.TryAddPoint(point, out _);
            }

            var data = VolatilePointCollection.ToArray();
            childRecord.PointCount = data.Length;

            FinishedPoints += ChildCounts[childIndex];
            DiscardedPoints += ChildCounts[childIndex] - data.Length;
            ChildCounts[childIndex] = 0;

            var path = Path.Combine(dataPath, fileName + TreeUtility.NodeFileExtension);
            var size = (long) data.Length * stride;

            SaveToFile(path, fileName, size, data);
        }

        /// <summary>
        /// Saves this node to disk. Overwrites previous file if it exists.
        /// </summary>
        private void SaveNodeToDisk()
        {
            var nodeData = PointCollection.ToArray();
            var pointCount = nodeData.Length;
            NodeRecord.PointCount = pointCount;
            var nodePath = Path.Combine(dataPath, NodeRecord.Identifier + TreeUtility.NodeFileExtension);
            var size = pointCount * stride;

            if (File.Exists(nodePath))
                File.Delete(nodePath);

            SaveToFile(nodePath, NodeRecord.Identifier, size, nodeData);
        }

        private void SaveToFile(string path, string mapName, long size, PointCloudPoint[] data)
        {
            using (var mmf = MemoryMappedFile.CreateFromFile(path, FileMode.Create, mapName, size))
            {
                using (var accessor = mmf.CreateViewAccessor(0, size))
                {
                    unsafe
                    {
                        byte* targetPtr = null;
                        accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref targetPtr);

                        try
                        {
                            fixed (void* sourcePtr = data)
                                UnsafeUtility.MemCpy(targetPtr, sourcePtr, size);
                        }
                        finally
                        {
                            accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Passes given point to one of the children. Child is chosen based on point's position.
        /// </summary>
        /// <param name="point">Point to pass.</param>
        protected abstract void PassPointToChild(PointCloudPoint point);

        /// <summary>
        /// Creates and returns instance of <see cref="NodeRecord"/>-derived type specific for associated node tree structure. 
        /// </summary>
        /// <param name="index">Index of the child node.</param>
        protected abstract NodeRecord CreateChildRecord(byte index);
    }
}