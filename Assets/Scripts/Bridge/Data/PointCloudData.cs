/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;

namespace Simulator.Bridge.Data
{
    using System;

    public class PointCloudData : IThreadCachedBridgeData<PointCloudData>
    {
        public string Name;
        public string Frame;
        public double Time;
        public uint Sequence;

        public int LaserCount;

        // world to lidar space transform
        public Matrix4x4 Transform;

        // xyz are coordinates in world space, w is intensity
        public Vector4[] Points;

        public int PointCount;

        public void CopyToCache(PointCloudData target)
        {
            target.Name = Name;
            target.Frame = Frame;
            target.Time = Time;
            target.Sequence = Sequence;

            target.LaserCount = LaserCount;
            target.Transform = Transform;

            if (target.Points == null || target.Points.Length < PointCount)
                target.Points = new Vector4[PointCount];

            Array.Copy(Points, target.Points, PointCount);

            target.PointCount = PointCount;
        }
    }
}
