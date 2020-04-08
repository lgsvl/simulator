/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Editor.PointCloud.Trees
{
    using System.Collections.Generic;
    using Simulator.PointCloud;
    using Simulator.PointCloud.Trees;
    using UnityEngine;

    /// <summary>
    /// Class used for filtering and storing points based on Poisson-disk method.
    /// </summary>
    public class PoissonDiskPointCollection : IOrganizedPointCollection
    {
        private const int CellMultiplier = 8;

        private Bounds rootBounds;
        private TreeImportSettings settings;
        private int[] cellsPerAxis;
        private Vector3 cellStep;
        private float minDistanceSquared;
        private NodeRecord nodeRecord;

        private readonly Dictionary<int, List<PointCloudPoint>> outputPoints = new Dictionary<int, List<PointCloudPoint>>();

        public float MinDistance { get; private set; }

        public void Initialize(TreeImportSettings treeSettings, TreeImportData importData)
        {
            rootBounds = importData.Bounds;
            settings = treeSettings;
            cellsPerAxis = new int[3];
        }

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
                while (0.5f * newDist > minimumDistance)
                    newDist *= 0.5f;

                minimumDistance = newDist;
            }

            minDistanceSquared = minimumDistance * minimumDistance;
            MinDistance = minimumDistance;

            cellsPerAxis[0] = Mathf.Max(1, Mathf.CeilToInt(nodeRecord.Bounds.size.x / (minimumDistance * CellMultiplier)));
            cellsPerAxis[1] = Mathf.Max(1, Mathf.CeilToInt(nodeRecord.Bounds.size.y / (minimumDistance * CellMultiplier)));
            cellsPerAxis[2] = Mathf.Max(1, Mathf.CeilToInt(nodeRecord.Bounds.size.z / (minimumDistance * CellMultiplier)));
            cellStep.x = nodeRecord.Bounds.size.x / cellsPerAxis[0];
            cellStep.y = nodeRecord.Bounds.size.y / cellsPerAxis[1];
            cellStep.z = nodeRecord.Bounds.size.z / cellsPerAxis[2];
        }

        public void ClearState()
        {
            outputPoints.Clear();
        }

        public bool TryAddPoint(PointCloudPoint point, out PointCloudPoint? replacedPoint)
        {
            replacedPoint = null;
            GetCellCoords(point, out var x, out var y, out var z);

            for (var i = x - 1; i <= x + 1; ++i)
            {
                if (i < 0 || i >= cellsPerAxis[0])
                    continue;

                for (var j = y - 1; j <= y + 1; ++j)
                {
                    if (j < 0 || j >= cellsPerAxis[1])
                        continue;

                    for (var k = z - 1; k <= z + 1; ++k)
                    {
                        if (k < 0 || k >= cellsPerAxis[2])
                            continue;

                        if (PositionConflicts(point, i, j, k))
                            return false;
                    }
                }
            }

            var index = GetFlatIndex(x, y, z);
            if (!outputPoints.ContainsKey(index))
                outputPoints.Add(index, new List<PointCloudPoint>());

            outputPoints[index].Add(point);
            return true;
        }

        public PointCloudPoint[] ToArray()
        {
            var count = 0;
            foreach (var outputPointGroup in outputPoints)
                count += outputPointGroup.Value.Count;

            var result = new PointCloudPoint[count];
            var offset = 0;

            foreach (var outputPointGroup in outputPoints)
            {
                foreach (var point in outputPointGroup.Value)
                {
                    result[offset++] = point;
                }
            }

            return result;
        }

        private void GetCellCoords(PointCloudPoint point, out int x, out int y, out int z)
        {
            var relativePosition = point.Position - nodeRecord.Bounds.min;

            x = (int) (relativePosition.x / cellStep.x);
            y = (int) (relativePosition.y / cellStep.y);
            z = (int) (relativePosition.z / cellStep.z);

            if (x >= cellsPerAxis[0])
                x = cellsPerAxis[0] - 1;

            if (y >= cellsPerAxis[1])
                y = cellsPerAxis[1] - 1;

            if (z >= cellsPerAxis[2])
                z = cellsPerAxis[2] - 1;
        }

        private bool PositionConflicts(PointCloudPoint point, int cellX, int cellY, int cellZ)
        {
            var index = GetFlatIndex(cellX, cellY, cellZ);
            if (!outputPoints.ContainsKey(index))
                return false;

            foreach (var p in outputPoints[index])
            {
                var squaredDistance = Vector3.SqrMagnitude(point.Position - p.Position);
                if (squaredDistance < minDistanceSquared)
                    return true;
            }

            return false;
        }

        private int GetFlatIndex(int x, int y, int z)
        {
            return x * cellsPerAxis[1] * cellsPerAxis[2] + y * cellsPerAxis[2] + z;
        }
    }
}