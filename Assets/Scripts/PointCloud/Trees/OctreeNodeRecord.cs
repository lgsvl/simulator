namespace Simulator.PointCloud.Trees
{
    using UnityEngine;

    /// <summary>
    /// Class representing a single node in an octree hierarchy. Contains no points data.
    /// </summary>
    public class OctreeNodeRecord : NodeRecord
    {
        public OctreeNodeRecord(string identifier, Bounds bounds, int pointCount) : base(identifier, bounds, pointCount)
        {
        }

        /// <inheritdoc/>
        protected override int MaxChildren => 8;

        /// <inheritdoc/>
        public override float CalculateDistanceTo(Vector3 target)
        {
            return Vector3.Distance(Bounds.center, target);
        }
    }
}