/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.PointCloud.Trees
{
    using UnityEngine;

    /// <summary>
    /// Class representing a single node in a tree hierarchy. Contains no points data.
    /// </summary>
    public abstract class NodeRecord
    {
        /// <summary>
        /// Amount of points present in represented node.
        /// </summary>
        public int PointCount;
        
        /// <summary>
        /// Unique identifier of a represented node. Describes position in tree hierarchy.
        /// </summary>
        public string Identifier { get; }

        /// <summary>
        /// World space bounds of a represented node.
        /// </summary>
        public Bounds Bounds { get; }

        /// <summary>
        /// Radius of bounding sphere of a represented node.
        /// </summary>
        public float BoundingSphereRadius { get; protected set; }

        /// <summary>
        /// True if represented node has any children, false otherwise.
        /// </summary>
        public bool HasChildren => Children != null;

        /// <summary>
        /// Array of all children under represented node.
        /// </summary>
        public NodeRecord[] Children { get; private set; }
        
        /// <summary>
        /// A maximum amount of children that a node can have. Depends on tree type.
        /// </summary>
        protected abstract int MaxChildren { get; }

        protected NodeRecord(string identifier, Bounds bounds, int pointCount)
        {
            Identifier = identifier;
            Bounds = bounds;
            BoundingSphereRadius = Vector3.Magnitude(bounds.extents);
            PointCount = pointCount;
        }

        /// <summary>
        /// Marks given node record as a child of this one.
        /// </summary>
        public void AddChild(NodeRecord nodeRecord)
        {
            var childId = nodeRecord.Identifier;
            
            // Last char in identifier is index of the child on this level
            var childIndex = childId[childId.Length - 1] - '0';
            
            if (Children == null)
                Children = new NodeRecord[MaxChildren];

            Children[childIndex] = nodeRecord;
        }

        /// <summary>
        /// Calculates distance between this node and given position in the same space.
        /// </summary>
        public abstract float CalculateDistanceTo(Vector3 target);
    }
}