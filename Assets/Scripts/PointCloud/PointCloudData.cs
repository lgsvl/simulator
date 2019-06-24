/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;

namespace Simulator.PointCloud
{
    [Serializable]
    public struct PointCloudPoint
    {
        // world space position
        public Vector3 Position;

        // intensity and color (0xIIBBGGRR)
        public uint Color;
    }

    public class PointCloudData : ScriptableObject
    {
        public int Stride => UnsafeUtility.SizeOf<PointCloudPoint>();

        public int Count => Points.Length;

        public PointCloudPoint[] Points;
        public Bounds Bounds;
        public Vector3 OriginalCenter;
        public Vector3 OriginalExtents;
        public bool HasColor;

        public static PointCloudData Create(PointCloudPoint[] points, Bounds bounds, bool hasColor, Vector3 center, Vector3 extents)
        {
            var data = CreateInstance<PointCloudData>();
            data.Points = points;
            data.Bounds = bounds;
            data.HasColor = hasColor;
            data.OriginalCenter = center;
            data.OriginalExtents = extents;
            return data;
        }
    }
}
