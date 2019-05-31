/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;

namespace Simulator.Bridge.Data
{
    public class Detected2DObject
    {
        public uint Id;
        public string Label;
        public double Score;

        public Vector2 Position;
        public Vector2 Scale;

        public Vector3 LinearVelocity;
        public Vector3 AngularVelocity;
    }

    public class Detected2DObjectData
    {
        public uint Sequence;
        public string Frame;
        public double Time;
        public Detected2DObject[] Data;
    }

    public class Detected2DObjectArray
    {
        public Detected2DObject[] Data;
    }
}
