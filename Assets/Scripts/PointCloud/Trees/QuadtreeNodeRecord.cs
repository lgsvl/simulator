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
    /// Class representing a single node in a quadtree hierarchy. Contains no points data.
    /// </summary>
    public class QuadtreeNodeRecord : NodeRecord
    {
        public QuadtreeNodeRecord(string identifier, Bounds bounds, int pointCount) : base(identifier, bounds, pointCount)
        {
            BoundingSphereRadius =
                Mathf.Sqrt(bounds.extents.x * bounds.extents.x + bounds.extents.z * bounds.extents.z);
        }

        /// <inheritdoc/>
        protected override int MaxChildren => 4;

        /// <inheritdoc/>
        public override float CalculateDistanceTo(Vector3 target)
        {
            var offsetVector = target - Bounds.center;
            offsetVector.y = 0;
            return offsetVector.magnitude;
        }
    }
}