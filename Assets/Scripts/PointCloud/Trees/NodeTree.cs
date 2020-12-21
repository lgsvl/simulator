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

    using Simulator.Utilities;

    using UnityEngine;

    /// <summary>
    /// Class representing a non-editable version of a node tree.
    /// </summary>
    public abstract class NodeTree : IDisposable
    {
        /// <summary>
        /// Path on hard drive under which data is stored.
        /// </summary>
        private string pathOnDisk;

        /// <summary>
        /// Metadata of all nodes present in the tree.
        /// </summary>
        public readonly Dictionary<string, NodeRecord> NodeRecords = new Dictionary<string, NodeRecord>();

        /// <summary>
        /// Reference to instance of node loader used by this tree.
        /// </summary>
        public NodeLoader NodeLoader { get; }

        /// <summary>
        /// Reference to ZIP file storing point cloud data. Only available for environment bundles.
        /// </summary>
        public ZipTreeData ZipData { get; }

        /// <summary>
        /// World space bounds of this tree.
        /// </summary>
        public Bounds Bounds { get; private set; }

        /// <summary>
        /// Initializes new, empty tree based on data stored under given path.
        /// </summary>
        /// <param name="pathOnDisk">Path under which data for this tree is stored. Must exist.</param>
        /// <param name="loadedPointsLimit">Maximum amount of points that can be loaded into memory at once.</param>
        /// <param name="zipData">Environment archive metadata (only applicable to ZIP files).</param>
        protected NodeTree(string pathOnDisk, int loadedPointsLimit, ZipTreeData zipData = null)
        {
            this.pathOnDisk = pathOnDisk;
            ZipData = zipData;
            NodeLoader = new NodeLoader(this, loadedPointsLimit);
        }

        /// <summary>
        /// Reconstructs tree hierarchy based on currently loaded records.
        /// </summary>
        protected void RebuildHierarchy()
        {
            foreach (var record in NodeRecords.Values)
            {
                // Root node - don't look for parent, set its bounds as this tree's bounds
                if (record.Identifier == TreeUtility.RootNodeIdentifier)
                {
                    Bounds = record.Bounds;
                    continue;
                }

                var parentId = record.Identifier.Substring(0, record.Identifier.Length - 1);
                var parentRecord = NodeRecords[parentId];

                parentRecord.AddChild(record);
            }
        }

        /// <summary>
        /// Attempts to load index file with tree meta data from disk and create appropriate instance of node tree.
        /// </summary>
        /// <param name="path">Directory under which tree data is stored.</param>
        /// <param name="pointLimit">Maximum amount of points that tree can store in memory at once.</param>
        /// <param name="dataHash">Hash of the point cloud data (only applicable to ZIP files).</param>
        /// <param name="instance">Newly created instance of the node tree.</param>
        /// <returns>True if load was successful, false otherwise.</returns>
        public static bool TryLoadFromDisk(string path, int pointLimit, string dataHash, out NodeTree instance)
        {
            var fullPath = Utility.GetFullPath(path);
            string indexPath;
            long offset, size;
            ZipTreeData zipData = null;

            if (!string.IsNullOrEmpty(dataHash))
            {
                indexPath = fullPath;
                zipData = new ZipTreeData(indexPath, dataHash);
                const string indexName = "index" + TreeUtility.IndexFileExtension;
                size = zipData.GetEntrySize(indexName);
                offset = zipData.GetEntryOffset(indexName);
            }
            else
            {
                indexPath = Path.Combine(fullPath, "index" + TreeUtility.IndexFileExtension);

                if (!File.Exists(indexPath))
                {
                    instance = null;
                    return false;
                }
                
                size = new FileInfo(indexPath).Length;
                offset = 0;
            }

            NodeTree result = null;

            try
            {
                var indexData = IndexData.ReadFromFile(indexPath, offset, size);

                if (indexData.TreeType == TreeType.Octree)
                    result = new Octree(fullPath, pointLimit, zipData);
                else
                    result = new Quadtree(fullPath, pointLimit, zipData);
                
                foreach (var nodeMetaData in indexData.Data)
                {
                    var nodeRecord = result.CreateNodeRecord(nodeMetaData);
                    result.NodeRecords.Add(nodeRecord.Identifier, nodeRecord);
                }

                result.RebuildHierarchy();
                instance = result;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"{e.Message}\n{e.StackTrace}");
                result?.Dispose();
                instance = null;
                return false;
            }
        }

        /// <summary>
        /// Finds and returns file data for given node.
        /// </summary>
        /// <param name="node">Node for which data should be found.</param>
        /// <param name="path">Path to the file storing data.</param>
        /// <param name="offset">Byte offset at which data for the node starts.</param>
        /// <param name="size">Byte size of data for the node.</param>
        public void GetNodeData(Node node, out string path, out long offset, out long size)
        {
            if (ZipData != null)
            {
                path = pathOnDisk;
                var nodeName = node.Identifier + TreeUtility.NodeFileExtension;
                offset = ZipData.GetEntryOffset(nodeName);
                size = ZipData.GetEntrySize(nodeName);
            }
            else
            {
                path =  Path.Combine(pathOnDisk, node.Identifier + TreeUtility.NodeFileExtension);
                offset = 0;
                size = new FileInfo(path).Length;
            }
        }

        /// <summary>
        /// Creates and returns new node record associated with tree type. 
        /// </summary>
        /// <param name="data">Metadata of the node.</param>
        protected abstract NodeRecord CreateNodeRecord(NodeMetaData data);

        public void Dispose()
        {
            NodeLoader.Dispose();
        }
    }
}