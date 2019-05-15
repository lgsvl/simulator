/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;

namespace Simulator.Bridge.Data
{
    public class Detected3DObject
    {
        public uint Id;
        public string Label;
        public double Score;

        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;

        public Vector3 LinearVelocity;
        public Vector3 AngularVelocity;
    }

    public class Detected3DObjectData
    {
        public uint Sequence;
        public string Frame;
        public Detected3DObject[] Data;
    }

    public class Detected3DObjectArray
    {
        public Detected3DObject[] Data;
    }
}
