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
    /// Tree node processor used for building octrees.
    /// </summary>
    public class OctreeNodeProcessor : TreeNodeProcessor
    {
        private const int ChildCount = 8;

        public OctreeNodeProcessor(string dataPath, TreeImportSettings settings, TreeImportData importData) : base(dataPath, settings, importData)
        {
            ChildBuffers = new PointCloudPoint[ChildCount][];
            for (var i = 0; i < ChildCount; ++i)
            {
                ChildBuffers[i] = new PointCloudPoint[settings.chunkSize];
            }

            ChildCounts = new int[ChildCount];
            ChildFileCounts = new int[ChildCount];
        }

        /// <inheritdoc/>
        protected override void PassPointToChild(PointCloudPoint point)
        {
            var childIndex = TreeUtility.GetOctreeChildIndex(NodeRecord.Bounds, point.Position);

            if (ChildFileCounts[childIndex] == 0 && ChildCounts[childIndex] == 0)
            {
                var childRecord = CreateChildRecord(childIndex);
                ChildNodeRecords.Add(childRecord);
            }

            ChildBuffers[childIndex][ChildCounts[childIndex]++] = point;

            if (ChildCounts[childIndex] == Settings.chunkSize)
                FlushChildFile(childIndex);
        }

        /// <inheritdoc/>
        protected override NodeRecord CreateChildRecord(byte index)
        {
            var childId = NodeRecord.Identifier + index.ToString();
            var parentCenter = NodeRecord.Bounds.center;
            var parentSize = NodeRecord.Bounds.size;
            var quarterSize = NodeRecord.Bounds.size * 0.25f;
            var childSize = parentSize * 0.5f;

            var centerOffset = Vector3.Scale(quarterSize, TreeUtility.GetOctreeOffsetVector(index));
            var bounds = new Bounds(parentCenter + centerOffset, childSize);

            return new OctreeNodeRecord(childId, bounds, 0);
        }
    }
}