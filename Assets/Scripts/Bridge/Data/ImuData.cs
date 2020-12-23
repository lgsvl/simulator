/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;

namespace Simulator.Bridge.Data
{
    public class ImuData
    {
        public string Name;
        public string Frame;
        public double Time;
        public uint Sequence;

        public double MeasurementSpan;

        public Vector3 Position;
        public Quaternion Orientation;

        public Vector3 Acceleration;
        public Vector3 LinearVelocity;
        public Vector3 AngularVelocity;
    }

    public class CorrectedImuData
    {
        public string Name;
        public string Frame;
        public double Time;
        public uint Sequence;

        public double MeasurementSpan;

        public Vector3 Position;
        public Quaternion Orientation;

        public Vector3 Acceleration;
        public Vector3 LinearVelocity;
        public Vector3 AngularVelocity;
        public Vector3 GlobalAcceleration;
        public Vector3 GlobalAngularVelocity;
    }
}
