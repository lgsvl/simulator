/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.PointCloud.Trees
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.MemoryMappedFiles;
    using System.Threading;
    using Unity.Collections.LowLevel.Unsafe;
    using UnityEngine;

    /// <summary>
    /// Class used to manage loading nodes into memory and disposing of them.
    /// </summary>
    public class NodeLoader : IDisposable
    {
        private readonly int pointLimit;
        
        private readonly NodeTree owner;

        private readonly Dictionary<string, Node> loadedNodes = new Dictionary<string, Node>();

        private readonly Queue<Node> loadQueue = new Queue<Node>();

        private readonly Queue<string> unloadQueue = new Queue<string>();

        private Thread loaderThread;
        
        private readonly object queueLock = new object();
        
        private readonly object loadedLock = new object();

        private readonly object shutdownLock = new object();

        private Node nodeInProgress;

        private long totalLoadedPoints;
        
        private bool cancelFlag;
        
        public bool Disposed { get; private set; }

        /// <summary>
        /// Creates new instance of node loader under specified node tree.
        /// </summary>
        /// <param name="owner">Node tree tho which this instance belongs to.</param>
        /// <param name="loadedPointsLimit">Maximum amount of points that can be present in memory at a time.</param>
        public NodeLoader(NodeTree owner, int loadedPointsLimit)
        {
            this.owner = owner;
            pointLimit = loadedPointsLimit;
            Disposed = false;

            StartLoaderThread();
        }

        /// <summary>
        /// Starts thread responsible for loading data from disk.
        /// </summary>
        private void StartLoaderThread()
        {
            if (loaderThread != null)
            {
                Debug.LogWarning("Node loader thread is already running.");
                return;
            }
            
            loaderThread = new Thread(LoadLoop);
            loaderThread.Start();
        }

        /// <summary>
        /// Stops thread responsible for loading data from disk.
        /// </summary>
        private void StopWorkerThread()
        {
            if (loaderThread == null)
            {
                Debug.LogWarning("Node loader thread is not running.");
                return;
            }

            cancelFlag = true;
        }

        /// <summary>
        /// Requests load of specified nodes. This operation is performed on a separate thread.
        /// </summary>
        /// <param name="requestedNodes">Nodes that should be loaded.</param>
        public void RequestLoad(List<string> requestedNodes)
        {
            // TODO: this method can probably do way fewer iterations, optimize if needed
            var addedPoints = 0;

            lock (loadedLock)
            {
                foreach (var nodeId in requestedNodes)
                {
                    if (nodeInProgress?.Identifier == nodeId)
                        continue;
                    
                    if (loadedNodes.ContainsKey(nodeId))
                        continue;

                    var record = owner.NodeRecords[nodeId];
                    var newNode = new Node(record);
                    addedPoints += record.PointCount;
                    
                    lock (queueLock)
                    {
                        loadedNodes.Add(nodeId, newNode);
                        loadQueue.Enqueue(newNode);
                    }
                }
            }

            if (totalLoadedPoints + addedPoints > pointLimit)
            {
                lock (loadedLock)
                {
                    foreach (var loadedNode in loadedNodes)
                    {
                        if (loadedNode.Value.DataState != NodeDataState.InMemory)
                            continue;

                        if (requestedNodes.Contains(loadedNode.Key))
                            continue;
                        
                        unloadQueue.Enqueue(loadedNode.Key);
                    }

                    while (unloadQueue.Count > 0)
                    {
                        var id = unloadQueue.Dequeue();
                        var item = loadedNodes[id];
                        totalLoadedPoints -= item.Points.Length;
                        item.Dispose();
                        loadedNodes.Remove(id);
                    }
                }
            }
        }

        /// <summary>
        /// Returns data for node with specified identifier if it's currently loaded.
        /// </summary>
        /// <param name="identifier">Unique identifier of the node.</param>
        /// <param name="node">Requested node data if data is loaded, null otherwise.</param>
        /// <returns>True if data is loaded, false otherwise.</returns>
        public bool TryGetNode(string identifier, out Node node)
        {
            lock (loadedLock)
            {
                return loadedNodes.TryGetValue(identifier, out node) && node.DataState == NodeDataState.InMemory;
            }
        }

        /// <summary>
        /// Main loading loop executed on worker thread.
        /// </summary>
        private void LoadLoop()
        {
            while (!cancelFlag)
            {
                if (loadQueue.Count > 0)
                {
                    lock (queueLock)
                    {
                        nodeInProgress = loadQueue.Dequeue();
                    }
                }

                lock (shutdownLock)
                {
                    if (nodeInProgress != null && ProcessLoad(nodeInProgress))
                    {
                        lock (loadedLock)
                        {
                            // Dispose might have been called during loading - make sure to dispose of newly loaded node
                            if (cancelFlag)
                            {
                                nodeInProgress.Dispose();
                            }
                            else
                            {
                                nodeInProgress.MarkAsLoaded();
                                totalLoadedPoints += nodeInProgress.Points.Length;
                            }
                        }
                    }
                }

                nodeInProgress = null;

                if (loadQueue.Count == 0)
                    Thread.Sleep(10);
            }
        }

        /// <summary>
        /// Loads data from disk for a single node.
        /// </summary>
        /// <param name="node">Node for which data should be loaded.</param>
        /// <returns>True if load was successful, false otherwise.</returns>
        private bool ProcessLoad(Node node)
        {
            var nodePath = Path.Combine(owner.PathOnDisk, node.Identifier + TreeUtility.NodeFileExtension);
            
            if (!File.Exists(nodePath))
            {
                Debug.LogError($"Data for node {node.Identifier} not found.");
                return false;
            }

            try
            {
                var size = new FileInfo(nodePath).Length;
                var itemSize = UnsafeUtility.SizeOf<PointCloudPoint>();
                var expectedPointCount = owner.NodeRecords[node.Identifier].PointCount;
                
                if (size != itemSize * expectedPointCount)
                {
                    Debug.LogError($"Mismatch between declared ({expectedPointCount}) and actual ({size/itemSize}) point count for node {node.Identifier}.");
                    return false;
                }
                
                using (var mmf = MemoryMappedFile.CreateFromFile(nodePath, FileMode.Open))
                {
                    using (var accessor = mmf.CreateViewAccessor(0, size))
                    {
                        unsafe
                        {
                            byte* sourcePtr = null;
                            accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref sourcePtr);

                            try
                            {
                                var targetPtr = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(node.Points);
                                UnsafeUtility.MemCpy(targetPtr, sourcePtr, size);
                            }
                            finally
                            {
                                accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                            }
                        }
                    }
                }

                return true;
            }
            catch (IOException e)
            {
                Debug.LogError(e.Message);
                return false;
            }
        }

        public void Dispose()
        {
            StopWorkerThread();

            lock (shutdownLock)
            {
                foreach (var loadedNode in loadedNodes)
                    loadedNode.Value.Dispose();
            }
            
            loadedNodes.Clear();
            Disposed = true;
        }
    }
}