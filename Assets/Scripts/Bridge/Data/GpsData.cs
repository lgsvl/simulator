/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;

namespace Simulator.Bridge.Data
{
    public class GpsData
    {
        public string Name;
        public string Frame;
        public double Time;
        public uint Sequence;

        public bool IgnoreMapOrigin;
        public double Latitude;
        public double Longitude;
        public double Altitude;

        public double Northing;
        public double Easting;
        public double[] PositionCovariance; // 9x9 matrix
        public Quaternion Orientation;
    }

    public class GpsOdometryData
    {
        public string Name;
        public string Frame;
        public double Time;
        public uint Sequence;

        public string ChildFrame;

        public bool IgnoreMapOrigin;
        public double Latitude;
        public double Longitude;
        public double Altitude;

        public double Northing;
        public double Easting;

        public Quaternion Orientation;

        public float ForwardSpeed; // m/s
        public Vector3 Velocity;
        public Vector3 AngularVelocity;
        public float WheelAngle; // rad
    }

    public class GpsInsData
    {
        public string Name;
        public string Frame;
        public double Time;
        public uint Sequence;

        public uint Status;
        public uint PositionType;
    }
}
