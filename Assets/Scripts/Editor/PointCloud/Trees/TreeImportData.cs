/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Editor.PointCloud.Trees
{
    using UnityEngine;

    public class TreeImportData
    {
        private PointCloudVerticalHistogram histogram;

        public Bounds RoadBounds { get; private set; }

        public Bounds Bounds { get; }

        public TreeImportData(Bounds bounds, PointCloudVerticalHistogram histogram)
        {
            Bounds = bounds;
            this.histogram = histogram;

            CalculateRoadBounds();
        }

        public TreeImportData(Bounds bounds)
        {
            Bounds = bounds;
            RoadBounds = bounds;
        }

        private void CalculateRoadBounds()
        {
            histogram.FindPeakRange(out var min, out var max);

            var height = Bounds.size.y;
            var origin = Bounds.min.y;
            var step = height / PointCloudVerticalHistogram.Resolution;

            // Ignore min for now, just use all points below
            // var minY = origin + min * step;
            var minY = origin;
            var maxY = origin + (max + 1) * step;
            var centerY = minY + (maxY - minY) * 0.5f;

            var center = new Vector3(Bounds.center.x, centerY, Bounds.center.z);
            var size = new Vector3(Bounds.size.x, maxY - minY, Bounds.size.z);

            RoadBounds = new Bounds(center, size);
        }
    }
}