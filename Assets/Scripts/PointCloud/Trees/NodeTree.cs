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
        public string PathOnDisk { get; }

        /// <summary>
        /// Metadata of all nodes present in the tree.
        /// </summary>
        public readonly Dictionary<string, NodeRecord> NodeRecords = new Dictionary<string, NodeRecord>();

        /// <summary>
        /// Reference to instance of node loader used by this tree.
        /// </summary>
        public NodeLoader NodeLoader { get; }

        /// <summary>
        /// World space bounds of this tree.
        /// </summary>
        public Bounds Bounds { get; private set; }

        /// <summary>
        /// Initializes new, empty tree based on data stored under given path.
        /// </summary>
        /// <param name="pathOnDisk">Path under which data for this tree is stored. Must exist.</param>
        /// <param name="loadedPointsLimit">Maximum amount of points that can be loaded into memory at once.</param>
        protected NodeTree(string pathOnDisk, int loadedPointsLimit)
        {
            PathOnDisk = pathOnDisk;
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
        /// <param name="instance">Newly created instance of the node tree.</param>
        /// <returns>True if load was successful, false otherwise.</returns>
        public static bool TryLoadFromDisk(string path, int pointLimit, out NodeTree instance)
        {
            var fullPath = Utility.GetFullPath(path);
            var indexPath = Path.Combine(fullPath, "index" + TreeUtility.IndexFileExtension);

            if (!File.Exists(indexPath))
            {
                instance = null;
                return false;
            }

            NodeTree result = null;

            try
            {
                var indexData = IndexData.ReadFromFile(indexPath);

                if (indexData.TreeType == TreeType.Octree)
                    result = new Octree(fullPath, pointLimit);
                else
                    result = new Quadtree(fullPath, pointLimit);
                
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