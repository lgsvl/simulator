/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;

namespace Simulator.Bridge.Data
{
    public class UltrasonicData
    {
        public string Name;
        public string Frame;
        public double Time;
        public uint Sequence;

        public float MinimumDistance;
    }
}
