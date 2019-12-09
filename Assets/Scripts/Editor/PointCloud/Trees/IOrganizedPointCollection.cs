namespace Simulator.Editor.PointCloud.Trees
{
    using Simulator.PointCloud;
    using Simulator.PointCloud.Trees;

    /// <summary>
    /// Interface for storing node points in organized manner.
    /// </summary>
    public interface IOrganizedPointCollection
    {
        /// <summary>
        /// Initializes this instance. Must be called before any other methods.
        /// </summary>
        /// <param name="settings">Settings used for building node tree.</param>
        void Initialize(TreeImportSettings settings);

        /// <summary>
        /// Updates cached values and internal state of this collection to match given node record.
        /// </summary>
        void UpdateForNode(NodeRecord nodeRecord);

        /// <summary>
        /// Clears internal state of this collection.
        /// </summary>
        void ClearState();

        /// <summary>
        /// Attempts to add given point to this collection, following rules imposed by implementation.
        /// </summary>
        /// <param name="point">Point to add.</param>
        /// <param name="replacedPoint">If not null, stores point from collection that was replaced by new <see cref="point"/>.</param>
        /// <returns>True if point was added, false otherwise.</returns>
        bool TryAddPoint(PointCloudPoint point, out PointCloudPoint? replacedPoint);

        /// <summary>
        /// Creates and returns an array from points currently stored in this collection.
        /// </summary>
        PointCloudPoint[] ToArray();
    }
}