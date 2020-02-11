/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;

namespace Simulator.Bridge.Data
{
    public enum GearPosition
    {
        Neutral,
        Drive,
        Reverse,
        Parking,
        Low,
    };

    public class VehicleControlData
    {
        // common
        public float? Acceleration; // 0..1
        public float? Breaking; // 0..1

        // autoware
        public float? Velocity;
        public float? SteerAngularVelocity;
        public float? SteerAngle;
        public bool ShiftGearUp;
        public bool ShiftGearDown;

        // apollo 
        public float? SteerRate;
        public float? SteerTarget;
        public double? TimeStampSec;
        public GearPosition? CurrentGear;

        // lgsvl
        public float? SteerInput;
    }
}