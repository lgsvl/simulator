/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;

namespace Simulator.Bridge.Data
{
    public class DetectedRadarObject
    {
        public int Id;
        
        public Vector3 SensorAim;
        public Vector3 SensorRight;
        public Vector3 SensorPosition;
        public Vector3 SensorVelocity;
        public double SensorAngle;

        public Vector3 Position;
        public Vector3 Velocity;
        public Vector3 RelativePosition;
        public Vector3 RelativeVelocity;
        public Vector3 ColliderSize;
        public int State;
        public bool NewDetection;
    }

    public class DetectedRadarObjectData
    {
        public string Name;
        public string Frame;
        public double Time;
        public uint Sequence;
        public DetectedRadarObject[] Data;
    }
}
