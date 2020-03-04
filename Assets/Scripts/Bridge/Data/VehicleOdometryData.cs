/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;

namespace Simulator.Bridge.Data
{
    public class VehicleOdometryData
    {
        public double Time;
        public float Speed; // in meters per second
        public float SteeringAngleFront;
        public float SteeringAngleBack;
   }
}
