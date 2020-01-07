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
    using System.Threading;
    using Simulator.PointCloud;
    using Simulator.PointCloud.Trees;
    using Unity.Collections.LowLevel.Unsafe;

    /// <summary>
    /// Base class for processors used to build specific node trees.
    /// </summary>
    public abstract class TreeNodeProcessor
    {
        /// <summary>
        /// Describes work status on a processor.
        /// </summary>
        public enum WorkStatus
        {
            /// No data is currently being processed.
            Idle,

            /// Work has been queued and processing will start soon.
            Queued,

            /// Data is currently being processed.
            Busy
        }

        /// <summary>
        /// List of new child nodes created during last task.
        /// </summary>
        protected readonly List<NodeRecord> ChildNodeRecords;

        /// <summary>
        /// Settings used during the tree building process.
        /// </summary>
        protected readonly TreeImportSettings Settings;
        
        private readonly PointCloudPoint[] inputBuffer;
        private readonly object scheduleLock = new object();

        private readonly string dataPath;
        private readonly string tmpFolderPath;
        private readonly int stride;
        
        protected IOrganizedPointCollection PointCollection;

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
        private bool cancelFlag;

        /// <summary>
        /// Record with metadata of the node.
        /// </summary>
        protected NodeRecord NodeRecord;

        private WorkStatus workStatus = WorkStatus.Idle;

        /// <summary>
        /// Describes current work status of this processor.
        /// </summary>
        public WorkStatus Status
        {
            get
            {
                lock (scheduleLock)
                {
                    return workStatus;
                }
            }
        }

        /// <summary>
        /// Amount of points fully processed by this instance across all tasks up to this point.
        /// </summary>
        public int FinishedPoints { get; private set; }

        protected TreeNodeProcessor(string dataPath, TreeImportSettings settings)
        {
            Settings = settings;
            this.dataPath = dataPath;

            tmpFolderPath = Path.Combine(dataPath, "tmp");

            stride = UnsafeUtility.SizeOf<PointCloudPoint>();
            ChildNodeRecords = new List<NodeRecord>();
            inputBuffer = new PointCloudPoint[settings.chunkSize];
            
        }

        /// <summary>
        /// Starts work loop. It will be running until <see cref="StopWork"/> is called.
        /// </summary>
        public void StartWork()
        {
            while (!cancelFlag)
            {
                if (workStatus == WorkStatus.Queued)
                {
                    lock (scheduleLock)
                        workStatus = WorkStatus.Busy;

                    ProcessNodeInternal(NodeRecord);

                    lock (scheduleLock)
                        workStatus = WorkStatus.Idle;
                }
                else
                    Thread.Sleep(20);
            }
        }

        /// <summary>
        /// Stops work loop.
        /// </summary>
        public void StopWork()
        {
            cancelFlag = true;
        }

        /// <summary>
        /// Assigns node record for processing to this instance. Only valid if <see cref="Status"/> is <see cref="WorkStatus.Idle"/>.
        /// </summary>
        /// <param name="record">Node record to be processed.</param>
        /// <exception cref="Exception">processor is currently busy with other work.</exception>
        public void AssignWork(NodeRecord record)
        {
            lock (scheduleLock)
            {
                if (workStatus != WorkStatus.Idle)
                    throw new Exception("Processor cannot accept new work when it's busy!");

                workStatus = WorkStatus.Queued;
                NodeRecord = record;
            }
        }

        /// <summary>
        /// Pulls children created during last processing into passed list.
        /// </summary>
        /// <param name="resultsTarget">List that should be populated with created children records.</param>
        public void PullResults(List<NodeRecord> resultsTarget)
        {
            foreach (var childNodeRecord in ChildNodeRecords)
                resultsTarget.Add(childNodeRecord);

            ClearState();
        }

        /// <summary>
        /// Loops over all points that should be processed and assigns them either to given node or one of the children.
        /// </summary>
        /// <param name="record">Record of the node that should be processed.</param>
        private void ProcessNodeInternal(NodeRecord record)
        {
            ClearState();
            
            PointCollection.UpdateForNode(record);

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

            // Work is finished, make sure to flush all children data...
            for (var i = 0; i < ChildCounts.Length; ++i)
            {
                if (ChildCounts[i] > 0)
                    FlushChildFile((byte) i);
            }

            // ...and data of this node
            SaveNodeToDisk();
        }

        /// <summary>
        /// Clears internal state of this instance. Should be called after pulling results.
        /// </summary>
        private void ClearState()
        {
            PointCollection.ClearState();
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
            string fileName;
            string path;
            var final = ChildFileCounts[childIndex] == 0 && ChildCounts[childIndex] < Settings.nodeBranchThreshold;

            if (final)
            {
                FinishedPoints += ChildCounts[childIndex];
                fileName = $"{NodeRecord.Identifier}{childIndex.ToString()}";

                foreach (var record in ChildNodeRecords)
                {
                    if (record.Identifier == fileName)
                    {
                        record.PointCount = ChildCounts[childIndex];
                        break;
                    }
                }

                path = Path.Combine(dataPath, fileName + TreeUtility.NodeFileExtension);
            }
            else
            {
                fileName =
                    $"{NodeRecord.Identifier}{childIndex.ToString()}_tmp{(ChildFileCounts[childIndex]++).ToString()}";
                path = Path.Combine(tmpFolderPath, fileName);
            }

            var size = (long) ChildCounts[childIndex] * stride;

            ChildCounts[childIndex] = 0;

            using (var mmf = MemoryMappedFile.CreateFromFile(path, FileMode.Create, fileName, size))
            {
                using (var accessor = mmf.CreateViewAccessor(0, size))
                {
                    unsafe
                    {
                        try
                        {
                            byte* targetPtr = null;
                            accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref targetPtr);

                            fixed (PointCloudPoint* sourcePtr = ChildBuffers[childIndex])
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

            using (var mmf = MemoryMappedFile.CreateFromFile(nodePath, FileMode.Create, NodeRecord.Identifier, size))
            {
                using (var accessor = mmf.CreateViewAccessor(0, size))
                {
                    unsafe
                    {
                        byte* targetPtr = null;
                        accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref targetPtr);

                        try
                        {
                            fixed (void* sourcePtr = nodeData)
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