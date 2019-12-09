namespace Simulator.PointCloud.Trees
{
    using UnityEngine;

    /// <summary>
    /// Class representing a single node in a quadtree hierarchy. Contains no points data.
    /// </summary>
    public class QuadtreeNodeRecord : NodeRecord
    {
        public override Bounds Bounds
        {
            get => bounds;
            protected set
            {
                bounds = value;
                BoundingSphereRadius =
                    Mathf.Sqrt(bounds.extents.x * bounds.extents.x + bounds.extents.z * bounds.extents.z);
            }
        }
        
        public QuadtreeNodeRecord(string identifier, Bounds bounds, int pointCount) : base(identifier, bounds, pointCount)
        {
        }

        /// <inheritdoc/>
        protected override int MaxChildren => 4;

        /// <inheritdoc/>
        public override float CalculateDistanceTo(Transform target)
        {
            var offsetVector = target.transform.position - bounds.center;
            offsetVector.y = 0;
            return offsetVector.magnitude;
        }
    }
}