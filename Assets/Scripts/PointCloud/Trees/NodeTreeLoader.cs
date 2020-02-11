/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.PointCloud.Trees
{
    using UnityEngine;
    using Utilities.Attributes;

    /// <summary>
    /// Class used to manage instance of a single node tree in runtime.
    /// </summary>
    public class NodeTreeLoader : MonoBehaviour
    {
#pragma warning disable 0649

        [SerializeField]
        [PathSelector(SelectDirectory = true)]
        [Tooltip("Path under which data for this tree is stored. Must exist.")]
        private string dataPath = "";

        [SerializeField]
        [Tooltip("Maximum amount of points that can be loaded into memory at once.")]
        private int pointLimit = 10000000;

#pragma warning restore 0649

        private bool corrupted;
        
        private NodeTree tree;

        public NodeTree Tree
        {
            get
            {
                if (tree == null && !corrupted)
                {
                    if (!NodeTree.TryLoadFromDisk(dataPath, pointLimit, out tree))
                    {
                        Debug.LogError($"Unable to load octree under path {dataPath}. Check files.");
                        corrupted = true;
                        tree = null;
                    }
                }

                return tree;
            }
        }
        
        private void OnDestroy()
        {
            tree?.Dispose();
            tree = null;
        }
    }
}