/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Editor.PointCloud.Trees
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Simulator.PointCloud;
    using Simulator.PointCloud.Trees;
    using UnityEngine;

    /// <summary>
    /// Class used for filtering and storing points based on distance from subdivided cells' centers.
    /// </summary>
    public class CellCenterPointCollection : IOrganizedPointCollection
    {
        private Bounds rootBounds;
        private Bounds alignedBounds;
        private int[] cellsPerAxis;
        private TreeImportSettings settings;
        private NodeRecord nodeRecord;
        private float cellStep;

        private readonly Dictionary<int, PointCloudPoint> outputPoints = new Dictionary<int, PointCloudPoint>();

        public float MinDistance => cellStep;

        ///<inheritdoc/>
        public void Initialize(TreeImportSettings treeSettings, TreeImportData importData)
        {
            rootBounds = importData.Bounds;
            settings = treeSettings;
            cellsPerAxis = new int[3];
        }

        ///<inheritdoc/>
        public void UpdateForNode(NodeRecord record)
        {
            var minDistance = Mathf.Min(record.Bounds.size.x, record.Bounds.size.z) /
                              settings.rootNodeSubdivision;

            UpdateForNode(record, minDistance);
        }

        public void UpdateForNode(NodeRecord record, float minimumDistance, bool alignDistance = false)
        {
            nodeRecord = record;

            if (alignDistance)
            {
                var newDist = Mathf.Min(rootBounds.size.x, rootBounds.size.z) / settings.rootNodeSubdivision;
                while (0.5f * newDist > settings.minPointDistance)
                    newDist *= 0.5f;

                minimumDistance = newDist;
            }

            cellStep = minimumDistance;
            alignedBounds = TreeUtility.GetRoundedAlignedBounds(nodeRecord.Bounds, rootBounds.min, minimumDistance);

            EditorTreeUtility.CalculateGridSize(alignedBounds, minimumDistance, cellsPerAxis);
        }

        ///<inheritdoc/>
        public void ClearState()
        {
            outputPoints.Clear();
        }

        ///<inheritdoc/>
        public bool TryAddPoint(PointCloudPoint point, out PointCloudPoint? replacedPoint)
        {
            var cellIndex = GetCellIndex(point);

            // Cell is already occupied, check distance to center
            if (outputPoints.TryGetValue(cellIndex, out var current))
            {
                // New point is closer - place it here, pass old point to child
                if (NewPointIsBetter(cellIndex, current.Position, point.Position))
                {
                    outputPoints[cellIndex] = point;
                    replacedPoint = current;
                    return true;
                }

                // Old point is closer - pass new point to child
                else
                {
                    replacedPoint = null;
                    return false;
                }
            }

            // Cell is empty - place new point there
            else
            {
                outputPoints[cellIndex] = point;
                replacedPoint = null;
                return true;
            }
        }

        ///<inheritdoc/>
        public PointCloudPoint[] ToArray()
        {
            return outputPoints.Values.ToArray();
        }

        /// <summary>
        /// Returns index of a sub-cell covering a fragment of space that given point occupies.
        /// </summary>
        private int GetCellIndex(PointCloudPoint point)
        {
            var relativePosition = point.Position - alignedBounds.min;

            var x = (int) (relativePosition.x / cellStep);
            var y = (int) (relativePosition.y / cellStep);
            var z = (int) (relativePosition.z / cellStep);

            // Upper bounds are inclusive, so last set of cells in each axis must also accept points from there 
            if (x >= cellsPerAxis[0])
                x = cellsPerAxis[0] - 1;

            if (y >= cellsPerAxis[1])
                y = cellsPerAxis[1] - 1;

            if (z >= cellsPerAxis[2])
                z = cellsPerAxis[2] - 1;

            var flat = x + y * cellsPerAxis[0] + z * cellsPerAxis[0] * cellsPerAxis[1];

            return flat;
        }

        /// <summary>
        /// Checks if new point is better suited to occupy cell with given index than the old one.
        /// </summary>
        /// <param name="cellIndex">Index of the occupied cell.</param>
        /// <param name="oldPosition">Position of the old point.</param>
        /// <param name="newPosition">Position of the new point.</param>
        /// <returns>True if new point is better, false otherwise.</returns>
        private bool NewPointIsBetter(int cellIndex, Vector3 oldPosition, Vector3 newPosition)
        {
            var cellCenter = GetCellCenter(cellIndex);

            return (newPosition - cellCenter).sqrMagnitude < (oldPosition - cellCenter).sqrMagnitude;
        }

        /// <summary>
        /// Returns center position of a cell with given index.
        /// </summary>
        /// <param name="cellIndex"></param>
        /// <returns></returns>
        private Vector3 GetCellCenter(int cellIndex)
        {
            var squared = cellsPerAxis[0] * cellsPerAxis[1];
            var z = cellIndex / squared;
            var remaining = cellIndex - z * squared;
            var y = remaining / cellsPerAxis[0];
            var x = remaining % cellsPerAxis[0];

            var center = alignedBounds.min +
                         new Vector3(cellStep * (x + 0.5f), cellStep * (y + 0.5f), cellStep * (z + 0.5f));

            return center;
        }

        public void FlushMeshGenerationData(string outputDirectory)
        {
            if (outputPoints.Count == 0)
                return;

            var data = outputPoints.ToDictionary(x => x.Key, x => x.Value.Position);

            EditorTreeUtility.SaveMeshGenerationData(
                Path.Combine(outputDirectory, $"{nodeRecord.Identifier}.meshdata"),
                cellsPerAxis,
                data);

            bool IsEdgeCell(int id)
            {
                var xyz = TreeUtility.Unflatten(id, cellsPerAxis);
                return (xyz.x == 0) || (xyz.y == 0) || (xyz.z == 0);
            }

            var edgeData = data
                .Where(x => IsEdgeCell(x.Key))
                .ToDictionary(x => x.Key, x => x.Value);

            if (edgeData.Count == 0)
                return;

            EditorTreeUtility.SaveMeshGenerationData(
                Path.Combine(outputDirectory, $"{nodeRecord.Identifier}.meshedge"),
                cellsPerAxis,
                edgeData);
        }
    }
}