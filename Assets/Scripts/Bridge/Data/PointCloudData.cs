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
    using Unity.Collections;

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
        // NativePoints can be used instead of Points if BridgeMessageDispatcher is used - copy from native array to
        // managed array will happen in CopyToCache. This saves one copy in sensor code.
        public Vector4[] Points;
        public NativeArray<Vector4> NativePoints;

        public int PointCount;

        public void CopyToCache(PointCloudData target)
        {
            target.Name = Name;
            target.Frame = Frame;
            target.Time = Time;
            target.Sequence = Sequence;

            target.LaserCount = LaserCount;
            target.Transform = Transform;
            target.PointCount = PointCount;

            if (target.Points == null || target.Points.Length < PointCount)
                target.Points = new Vector4[PointCount];

            // Final target is always managed array - native arrays can't be accessed outside of the main thread and
            // have to be properly disposed. Since copy has to happen anyway, it's easier to just target managed array.
            if (Points != null)
                Array.Copy(Points, target.Points, PointCount);
            else if (NativePoints.IsCreated)
                NativePoints.CopyTo(target.Points);
        }

        public int GetHash()
        {
            return 0;
        }
    }
}
