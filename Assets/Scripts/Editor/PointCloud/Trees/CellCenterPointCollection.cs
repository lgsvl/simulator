/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Editor.PointCloud.Trees
{
    using System.Collections.Generic;
    using System.Linq;
    using Simulator.PointCloud;
    using Simulator.PointCloud.Trees;
    using UnityEngine;

    /// <summary>
    /// Class used for filtering and storing points based on distance from subdivided cells' centers.
    /// </summary>
    public class CellCenterPointCollection : IOrganizedPointCollection
    {
        private int[] cellsPerAxis;
        private TreeImportSettings settings;
        private NodeRecord nodeRecord;
        private Vector3 cellStep;

        private readonly Dictionary<int, PointCloudPoint> outputPoints = new Dictionary<int, PointCloudPoint>();

        ///<inheritdoc/>
        public void Initialize(TreeImportSettings treeSettings)
        {
            settings = treeSettings;
            cellsPerAxis = new int[3];
        }

        ///<inheritdoc/>
        public void UpdateForNode(NodeRecord record)
        {
            nodeRecord = record;
            
            var minDistance = Mathf.Min(nodeRecord.Bounds.size.x, nodeRecord.Bounds.size.z) /
                              settings.rootNodeSubdivision;

            cellsPerAxis[0] = Mathf.Max(1, Mathf.CeilToInt(nodeRecord.Bounds.size.x / minDistance));
            cellsPerAxis[1] = Mathf.Max(1, Mathf.CeilToInt(nodeRecord.Bounds.size.y / minDistance));
            cellsPerAxis[2] = Mathf.Max(1, Mathf.CeilToInt(nodeRecord.Bounds.size.z / minDistance));
            cellStep.x = nodeRecord.Bounds.size.x / cellsPerAxis[0];
            cellStep.y = nodeRecord.Bounds.size.y / cellsPerAxis[1];
            cellStep.z = nodeRecord.Bounds.size.z / cellsPerAxis[2];
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
            var relativePosition = point.Position - nodeRecord.Bounds.min;

            var x = (int) (relativePosition.x / cellStep.x);
            var y = (int) (relativePosition.y / cellStep.y);
            var z = (int) (relativePosition.z / cellStep.z);

            // Upper bounds are inclusive, so last set of cells in each axis must also accept points from there 
            if (x >= cellsPerAxis[0])
                x = cellsPerAxis[0] - 1;

            if (y >= cellsPerAxis[1])
                y = cellsPerAxis[1] - 1;

            if (z >= cellsPerAxis[2])
                z = cellsPerAxis[2] - 1;

            var flat = x * cellsPerAxis[1] * cellsPerAxis[2] + y * cellsPerAxis[2] + z;

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
            var squared = cellsPerAxis[1] * cellsPerAxis[2];
            var x = cellIndex / squared;
            var remaining = cellIndex - x * squared;
            var y = remaining / cellsPerAxis[2];
            var z = remaining % cellsPerAxis[2];

            var center = nodeRecord.Bounds.min +
                         new Vector3(cellStep.x * (x + 0.5f), cellStep.y * (y + 0.5f), cellStep.z * (z + 0.5f));

            return center;
        }
    }
}