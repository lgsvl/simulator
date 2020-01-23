/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;

namespace Simulator.Bridge.Data
{
    public class VehicleStateData
    {
        public double Time;
        public byte Fuel;
        public byte Blinker;
        public byte HeadLight;
        public byte Wiper;
        public byte Gear;
        public byte Mode;
        public bool HandBrake;
        public bool Horn;
        public bool Autonomous;
    }
}