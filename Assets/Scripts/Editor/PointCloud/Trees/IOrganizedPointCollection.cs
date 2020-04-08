/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Editor.PointCloud.Trees
{
    using Simulator.PointCloud;
    using Simulator.PointCloud.Trees;
    using UnityEngine;

    /// <summary>
    /// Interface for storing node points in organized manner.
    /// </summary>
    public interface IOrganizedPointCollection
    {
        /// <summary>
        /// Minimum distance between points in this collection.
        /// </summary>
        float MinDistance { get; }

        /// <summary>
        /// Initializes this instance. Must be called before any other methods.
        /// </summary>
        /// <param name="settings">Settings used for building node tree.</param>
        /// <param name="rootBounds">Bounds of tree's root node.</param>
        void Initialize(TreeImportSettings settings, TreeImportData importData);

        /// <summary>
        /// Updates cached values and internal state of this collection to match given node record.
        /// </summary>
        void UpdateForNode(NodeRecord nodeRecord);

        /// <summary>
        /// Updates cached values and internal state of this collection to match given node record.
        /// </summary>
        /// <param name="nodeRecord">Record to match</param>
        /// <param name="minimumDistance">Minimum distance between points. Overrides auto-calculated value.</param>
        /// <param
        ///     name="alignDistance">
        /// </param>
        void UpdateForNode(NodeRecord nodeRecord, float minimumDistance, bool alignDistance = false);

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