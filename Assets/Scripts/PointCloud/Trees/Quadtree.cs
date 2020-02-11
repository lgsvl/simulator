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
    /// Class representing a non-editable version of a quadtree.
    /// </summary>
    public class Quadtree : NodeTree
    {
        /// <summary>
        /// Initializes new, empty quadtree based on data stored under given path.
        /// </summary>
        /// <param name="pathOnDisk">Path under which data for this tree is stored. Must exist.</param>
        /// <param name="loadedPointsLimit">Maximum amount of points that can be loaded into memory at once.</param>
        public Quadtree(string pathOnDisk, int loadedPointsLimit) : base(pathOnDisk, loadedPointsLimit)
        {
        }

        /// <inheritdoc/>
        protected override NodeRecord CreateNodeRecord(NodeMetaData data)
        {
            var nodeRecord = new QuadtreeNodeRecord(data.Identifier,
                new Bounds(data.BoundsCenter, data.BoundsSize), data.PointCount);
            
            return nodeRecord;
        }
    }
}