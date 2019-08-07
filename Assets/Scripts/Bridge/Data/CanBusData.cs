/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;

namespace Simulator.Bridge.Data
{
    public class CanBusData
    {
        public string Name;
        public string Frame;
        public double Time;
        public uint Sequence;

        public float Speed; // in meters per second

        public float Throttle; // [0 .. 1]
        public float Braking; // [0 .. 1]
        public float Steering; // [-1 .. +1]

        public bool ParkingBrake;
        public bool HighBeamSignal;
        public bool LowBeamSignal;
        public bool HazardLights;
        public bool FogLights;

        public bool LeftTurnSignal;
        public bool RightTurnSignal;

        public bool Wipers;

        public bool InReverse;
        public int Gear;

        public bool EngineOn;
        public float EngineRPM;

        public double Latitude;
        public double Longitude;
        public double Altitude;

        public Quaternion Orientation;
        public Vector3 Velocity;
   }
}
