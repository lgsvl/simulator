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
    /// Class representing a non-editable version of an octree.
    /// </summary>
    public class Octree : NodeTree
    {
        /// <summary>
        /// Initializes new, empty octree based on data stored under given path.
        /// </summary>
        /// <param name="pathOnDisk">Path under which data for this tree is stored. Must exist.</param>
        /// <param name="loadedPointsLimit">Maximum amount of points that can be loaded into memory at once.</param>
        /// <param name="zipData">Data about environment zip file (if applicable).</param>
        public Octree(string pathOnDisk, int loadedPointsLimit, ZipTreeData zipData = null) : base(pathOnDisk, loadedPointsLimit, zipData)
        {
        }

        /// <inheritdoc/>
        protected override NodeRecord CreateNodeRecord(NodeMetaData data)
        {
            var nodeRecord = new OctreeNodeRecord(data.Identifier,
                new Bounds(data.BoundsCenter, data.BoundsSize), data.PointCount);
            
            return nodeRecord;
        }
    }
}