/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Editor.PointCloud.Trees
{
    using Simulator.PointCloud.Trees;
    using UnityEngine;

    public struct PointCloudVerticalHistogram
    {
        public const int Resolution = 256;

        private double zMin;
        private double zRange;

        public unsafe fixed int regions[Resolution];

        public PointCloudVerticalHistogram(PointCloudBounds bounds)
        {
            zMin = bounds.MinZ;
            zRange = bounds.MaxZ - bounds.MinZ;
        }

        public unsafe void Add(double z)
        {
            var index = (z - zMin) / zRange * Resolution;
            var i = Mathf.Clamp((int) index, 0, Resolution);
            regions[i]++;
        }

        public unsafe void AddData(int* data)
        {
            for (var i = 0; i < Resolution; ++i)
                regions[i] += data[i];
        }

        public int FindPeak()
        {
            var peak = 0;
            var peakVal = 0;

            unsafe
            {
                for (var i = 0; i < Resolution; ++i)
                {
                    if (regions[i] > peakVal)
                    {
                        peakVal = regions[i];
                        peak = i;
                    }
                }
            }

            return peak;
        }

        private unsafe int CalculateAverage()
        {
            var sum = 0;

            for (var i = 0; i < Resolution; ++i)
                sum += regions[i];

            return sum / Resolution;
        }

        public unsafe void FindPeakRange(out int min, out int max)
        {
            var peak = FindPeak();
            var avg = CalculateAverage();

            min = peak;
            max = peak;

            for (var i = peak; i >= 0; --i)
            {
                min = i;

                if (regions[i] < avg)
                    break;
            }

            for (var i = peak; i < Resolution; ++i)
            {
                max = i;

                if (regions[i] < avg)
                    break;
            }
        }

        public Bounds AlignBounds(TreeImportSettings settings, Bounds bounds)
        {
            var origin = bounds.min.y;
            var step = EditorTreeUtility.GetStepAtTreeLevel(bounds, settings, settings.meshDetailLevel);
            var alignedBounds = TreeUtility.GetRoundedAlignedBounds(bounds, bounds.min, step);
            var alignedOrigin = alignedBounds.min.y;
            var peak = FindPeak();

            var targetCenter = origin + (bounds.size.y / Resolution) * (peak + 0.5f);

            var tmpDst = float.MaxValue;
            var actualCenter = 0f;

            for (var i = 0; i < (int) (alignedBounds.size.y / step); ++i)
            {
                var pos = alignedOrigin + (i + 0.5f) * step;
                var dst = Mathf.Abs(targetCenter - pos);
                if (dst < tmpDst)
                {
                    actualCenter = pos;
                    tmpDst = dst;
                }
                else
                {
                    Debug.Log($"Break at i ({actualCenter:F4})");
                    break;
                }
            }


            var diff = targetCenter - actualCenter;
            if (diff > 0)
                diff -= step;

            var height = bounds.max.y - bounds.min.y;

            var alignedMin = alignedBounds.min;
            alignedMin.y += diff;
            alignedBounds.min = alignedMin;
            Debug.Log($"origin: {origin:F4}, AO: {alignedBounds.min.y:F4}, height: {height:F4}, step: {step:F4}, TC: {targetCenter:F4}, AC: {actualCenter:F4}");

            return alignedBounds;
        }
    }
}