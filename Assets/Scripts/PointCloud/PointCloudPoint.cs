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
}