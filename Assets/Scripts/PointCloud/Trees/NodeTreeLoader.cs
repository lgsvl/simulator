/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.PointCloud.Trees
{
    using Simulator.Utilities;

    using UnityEngine;
    using Utilities.Attributes;

    /// <summary>
    /// Class used to manage instance of a single node tree in runtime.
    /// </summary>
    [ExecuteInEditMode]
    public class NodeTreeLoader : MonoBehaviour
    {
#pragma warning disable 0649

        [SerializeField]
        [PathSelector(SelectDirectory = true, TruncateToRelative = true)]
        [Tooltip("Path under which data for this tree is stored. Must exist.")]
        private string dataPath = "";

        [SerializeField]
        [Tooltip("Maximum amount of points that can be loaded into memory at once.")]
        private int pointLimit = 10000000;

#pragma warning restore 0649

        private bool corrupted;

        private string lastUsedDataPath;
        
        private NodeTree tree;

        public NodeTree Tree
        {
            get
            {
                if (!enabled)
                    return null;
                
                if (!string.Equals(dataPath, lastUsedDataPath))
                    Cleanup();
                
                if (tree == null && !corrupted && !string.IsNullOrEmpty(dataPath))
                {
                    if (!NodeTree.TryLoadFromDisk(dataPath, pointLimit, out tree))
                    {
                        Debug.LogError($"Unable to load octree under path {dataPath}. Check files.");
                        corrupted = true;
                        tree = null;
                    }
                }

                lastUsedDataPath = dataPath;

                return tree;
            }
        }

        public string GetDataPath()
        {
            return dataPath;
        }

        public string GetFullDataPath()
        {
            return Utility.GetFullPath(dataPath);
        }

        public void UpdateData(string newDataPath)
        {
            Cleanup();
            dataPath = newDataPath;
        }

        private void OnDisable()
        {
            Cleanup();
        }

        private void Cleanup()
        {
            corrupted = false;
            tree?.Dispose();
            tree = null;
        }
    }
}