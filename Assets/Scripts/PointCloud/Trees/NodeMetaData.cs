namespace Simulator.PointCloud.Trees
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Serializable struct containing meta data of a single node.
    /// </summary>
    [Serializable]
    public struct NodeMetaData
    {
        /// <summary>
        /// Unique identifier of a node. Describes position in tree hierarchy.
        /// </summary>
        public string Identifier;

        /// <summary>
        /// Amount of points present in represented node.
        /// </summary>
        public int PointCount;
        
        /// <summary>
        /// Center of this node's bounds.
        /// </summary>
        public Vector3 BoundsCenter;
        
        /// <summary>
        /// Size of this node's bounds.
        /// </summary>
        public Vector3 BoundsSize;
    }
}