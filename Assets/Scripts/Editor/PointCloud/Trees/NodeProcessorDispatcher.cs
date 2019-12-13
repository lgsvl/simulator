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
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Class responsible for dispatching tree-building work across multiple worker threads.
    /// </summary>
    public class NodeProcessorDispatcher
    {
        private readonly int itemSize = UnsafeUtility.SizeOf<PointCloudPoint>();

        private readonly string outputPath;
        private readonly string outputTmpPath;

        private readonly Queue<NodeRecord> queue = new Queue<NodeRecord>();
        private readonly List<NodeRecord> resultsList = new List<NodeRecord>();
        private readonly Dictionary<string, NodeRecord> nodeRecords = new Dictionary<string, NodeRecord>();
        private readonly PointCloudPoint[] points;

        public PointCloudPoint[] PublicMaxSizeBuffer;

        private TreeNodeProcessor[] processors;
        private Thread[] threads;

        private int rootTmpFilesCount;
        private int totalPointsCount;
        private int pointCount;

        /// <summary>
        /// Settings used during the tree building process.
        /// </summary>
        private TreeImportSettings Settings { get; }

        public NodeProcessorDispatcher(string outputPath, TreeImportSettings settings)
        {
            this.outputPath = outputPath;
            Settings = settings;

            if (Directory.Exists(outputPath))
                Directory.Delete(outputPath, true);

            Directory.CreateDirectory(outputPath);

            outputTmpPath = Path.Combine(outputPath, "tmp");
            Directory.CreateDirectory(outputTmpPath);

            points = new PointCloudPoint[settings.chunkSize];

            PublicMaxSizeBuffer = new PointCloudPoint[TreeUtility.MaxPointCountPerArray];
        }

        /// <summary>
        /// Returns name of temporary file storing root data for chunk with given index.
        /// </summary>
        private static string GetTemporaryFileName(int chunkIndex)
        {
            return $"r_tmp{chunkIndex.ToString()}";
        }

        /// <summary>
        /// <para>Registers multiple points for further processing.</para>
        /// <para>Points should use world-space coordinates.</para>
        /// </summary>
        /// <param name="pointsChunk">Array in which points are stored.</param>
        /// <param name="itemCount">Amount of valid points in <see cref="pointsChunk"/>.</param>
        public void AddChunk(PointCloudPoint[] pointsChunk, int itemCount)
        {
            var totalChunkCount = itemCount / Settings.chunkSize;
            if (totalChunkCount * Settings.chunkSize < itemCount)
                totalChunkCount++;
            
            var currentChunkIndex = 0;
            var processed = 0;
            const string title = "Flushing data to disk";

            while (processed < itemCount)
            {
                var chunkSize = Math.Min(itemCount - processed, Settings.chunkSize);
                var startIndex = processed;
                currentChunkIndex++;

                try
                {
                    var message = $"Chunk {currentChunkIndex.ToString()}/{totalChunkCount.ToString()}";
                    var progress = (float) currentChunkIndex / totalChunkCount;

                    EditorUtility.DisplayProgressBar(title, message, progress);
                    AddChunkInternal(pointsChunk, startIndex, chunkSize);
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }

                processed += chunkSize;
            }
        }

        /// <summary>
        /// <para>Registers a single point for further processing.</para>
        /// <para>Point should use world-space coordinates.</para>
        /// </summary>
        public void AddPoint(PointCloudPoint point)
        {
            points[pointCount++] = point;
            totalPointsCount++;

            if (pointCount == Settings.chunkSize)
            {
                FlushTmpFile(points, 0, Settings.chunkSize);
                pointCount = 0;
            }
        }

        /// <summary>
        /// <para>Registers multiple points for further processing.</para>
        /// <para>Points should use world-space coordinates.</para>
        /// </summary>
        /// <param name="buffer">Array in which points are stored.</param>
        /// <param name="startIndex">Represents the index in <see cref="buffer"/> at which data begins.</param>
        /// <param name="itemCount">Amount of points (starting from <see cref="itemCount"/>) that should be added. Can't exceed max chunk size.</param>
        /// <exception cref="Exception"><see cref="itemCount"/> is larger than max chunk size.</exception>
        private void AddChunkInternal(PointCloudPoint[] buffer, int startIndex, int itemCount)
        {
            totalPointsCount += itemCount;

            if (itemCount > Settings.chunkSize)
                throw new Exception($"{nameof(AddChunkInternal)} only accepts chunks up to size defined in settings.");

            if (itemCount == Settings.chunkSize)
            {
                FlushTmpFile(buffer, startIndex, itemCount);
                return;
            }

            var firstBatchCount = Math.Min(itemCount, Settings.chunkSize - pointCount);
            var secondBatchCount = itemCount - firstBatchCount;

            Array.Copy(buffer, startIndex, points, pointCount, firstBatchCount);
            pointCount += firstBatchCount;

            if (secondBatchCount > 0)
            {
                FlushTmpFile(points, 0, Settings.chunkSize);
                Array.Copy(buffer, startIndex + firstBatchCount, points, 0, secondBatchCount);
                pointCount = secondBatchCount;
            }
        }

        /// <summary>
        /// Builds tree of all points previously registered through <see cref="AddChunk"/> and <see cref="AddPoint"/> methods. Stores results on disk.
        /// </summary>
        /// <param name="bounds">World-space bounds of all points.</param>
        /// <returns>True if tree build succeeded, false otherwise.</returns>
        public bool ProcessPoints(Bounds bounds)
        {
            // Make sure all points are flushed to disk - buffers are ignored during build
            if (pointCount > 0)
            {
                FlushTmpFile(points, 0, pointCount);
                pointCount = 0;
            }

            processors = new TreeNodeProcessor[Settings.threadCount];
            threads = new Thread[Settings.threadCount];

            var rootNode = new OctreeNodeRecord(TreeUtility.RootNodeIdentifier, bounds, 0);
            nodeRecords.Add(rootNode.Identifier, rootNode);
            var cancelled = false;

            try
            {
                EditorUtility.DisplayProgressBar("Starting threads", "Allocating memory...", 0f);

                // This buffer is no loner needed - clear reference and let GC free the memory
                PublicMaxSizeBuffer = null;
                GC.Collect();

                // Start one processor on each of the requested threads
                for (var i = 0; i < Settings.threadCount; ++i)
                {
                    if (Settings.TreeType == TreeType.Octree)
                        processors[i] = new OctreeNodeProcessor(outputPath, Settings);
                    else
                        processors[i] = new QuadtreeNodeProcessor(outputPath, Settings);
                    
                    threads[i] = new Thread(processors[i].StartWork);
                    threads[i].Start();
                }

                // Assign root processing to first worker thread
                processors[0].AssignWork(rootNode);

                // Lock main thread here until either all work is finished or user cancels the process
                while (BuildLoop(out var busyThreadCount, out var finishedPointCount))
                {
                    var title =
                        $"Building tree ({busyThreadCount.ToString()}/{Settings.threadCount.ToString()} threads in use)";
                    var message = $"{finishedPointCount.ToString()}/{totalPointsCount.ToString()} points";
                    var progress = (float) finishedPointCount / totalPointsCount;

                    if (EditorUtility.DisplayCancelableProgressBar(title, message, progress))
                    {
                        cancelled = true;
                        break;
                    }

                    Thread.Sleep(20);
                }

                // Tree build succeeded - finalize
                if (!cancelled)
                {
                    FinalizeBuild();
                }
            }
            finally
            {
                // Whether process finishes, crashes, or is cancelled, stop worker threads and clear progress bar
                EditorUtility.ClearProgressBar();
                
                foreach (var processor in processors)
                    processor.StopWork();
                
                GC.Collect();
            }

            return !cancelled;
        }

        /// <summary>
        /// Checks tree build progress on all worker threads. 
        /// </summary>
        /// <param name="busyThreadCount">Amount of threads that are currently busy.</param>
        /// <param name="finishedPointCount">Amount of points that finished processing.</param>
        /// <returns>True if build is in progress, false otherwise.</returns>
        /// <exception cref="Exception">one of the threads died unexpectedly.</exception>
        private bool BuildLoop(out int busyThreadCount, out int finishedPointCount)
        {
            var busyCount = 0;
            var finishedCount = 0;

            for (var i = 0; i < processors.Length; ++i)
            {
                if (!threads[i].IsAlive)
                    throw new Exception($"Thread {i} died unexpectedly.");

                finishedCount += processors[i].FinishedPoints;

                // Processor is idle - it might have results, and is ready to accept work
                if (processors[i].Status == TreeNodeProcessor.WorkStatus.Idle)
                {
                    // Fetch children created by node processed on this thread and add them to the queue
                    processors[i].PullResults(resultsList);

                    foreach (var record in resultsList)
                    {
                        nodeRecords.Add(record.Identifier, record);
                        if (record.PointCount == 0)
                            queue.Enqueue(record);
                    }

                    resultsList.Clear();

                    // Get first node in queue and assign it to this processor
                    if (queue.Count > 0)
                    {
                        var record = queue.Dequeue();
                        processors[i].AssignWork(record);
                        busyCount++;
                    }
                }
                else
                    busyCount++;
            }

            busyThreadCount = busyCount;
            finishedPointCount = finishedCount;

            // At least one processor is still working - tree build still in progress
            return busyCount > 0;
        }

        /// <summary>
        /// Saves tree metadata to disk and clears temporary files.
        /// </summary>
        private void FinalizeBuild()
        {
            SaveIndexToDisk();
            Directory.Delete(outputTmpPath);
        }

        /// <summary>
        /// Flushes data from given buffer into temporary file on disk.
        /// </summary>
        /// <param name="buffer">Buffer storing data to flush.</param>
        /// <param name="startIndex">Represents the index in <see cref="buffer"/> at which data begins.</param>
        /// <param name="itemCount">Amount of points (starting from <see cref="itemCount"/>) that should be saved.</param>
        private void FlushTmpFile(PointCloudPoint[] buffer, int startIndex, int itemCount)
        {
            var fileIndex = rootTmpFilesCount++;
            OverwriteTmpFile(fileIndex, buffer, startIndex, itemCount);
        }

        /// <summary>
        /// Creates or overwrites temporary file with root's chunk data.
        /// </summary>
        /// <param name="chunkIndex">Index of the chunk.</param>
        /// <param name="buffer">Buffer storing data to save.</param>
        /// <param name="startIndex">Represents the index in <see cref="buffer"/> at which data begins.</param>
        /// <param name="itemCount">Amount of points (starting from <see cref="itemCount"/>) that should be saved.</param>
        private void OverwriteTmpFile(int chunkIndex, PointCloudPoint[] buffer, int startIndex, long itemCount)
        {
            var fileName = GetTemporaryFileName(chunkIndex);
            var path = Path.Combine(outputTmpPath, fileName);

            if (File.Exists(path))
                File.Delete(path);

            var size = itemCount * itemSize;

            using (var mmf = MemoryMappedFile.CreateFromFile(path, FileMode.Create, fileName, size))
            {
                using (var accessor = mmf.CreateViewAccessor(0, size))
                {
                    unsafe
                    {
                        byte* targetPtr = null;
                        accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref targetPtr);

                        try
                        {
                            fixed (PointCloudPoint* ptr = buffer)
                            {
                                var sourcePtr = ptr + startIndex;
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
        /// Saves index file to disk. Overwrites previous file if it exists.
        /// </summary>
        private void SaveIndexToDisk()
        {
            if (!Directory.Exists(outputPath))
            {
                Debug.LogError($"Specified directory ({outputPath}) does not exist.");
                return;
            }

            // Binary formatter must have a surrogate to serialize Vector3
            var binaryFormatter = TreeUtility.GetBinaryFormatterWithVector3Surrogate();

            // Create index file
            var indexData = new IndexData(Settings.TreeType, nodeRecords.Values);
            var indexPath = Path.Combine(outputPath, "index" + TreeUtility.IndexFileExtension);

            if (File.Exists(indexPath))
                File.Delete(indexPath);

            byte[] allBytes;
            
            using (var ms = new MemoryStream())
            {
                binaryFormatter.Serialize(ms, indexData);
                allBytes = ms.ToArray();
            }

            using (var mmf = MemoryMappedFile.CreateFromFile(indexPath, FileMode.Create, "index", allBytes.Length))
            {
                using (var accessor = mmf.CreateViewAccessor(0, allBytes.Length))
                    accessor.WriteArray(0, allBytes, 0, allBytes.Length);
            }
        }
    }
}