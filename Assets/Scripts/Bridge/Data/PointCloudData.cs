/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using Unity.Collections;

namespace Simulator.Bridge.Data
{
    public class PointCloudData
    {
        public string Name;
        public string Frame;
        public double Time;
        public uint Sequence;

        public int LaserCount;

        // world to lidar space transform
        public Matrix4x4 Transform;

        // xyz are coordinates in world space, w is intensity
        public NativeArray<Vector4> Points;
   }
}
