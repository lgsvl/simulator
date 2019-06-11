/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using Simulator.Bridge;
using Simulator.Bridge.Data;
using Simulator.Utilities;

namespace Simulator.Sensors
{
    [SensorType("Cruise Control", new System.Type[] { })]
    public class CruiseControlSensor : SensorBase, IVehicleInputs
    {
        public float CruiseSpeed = 0f;

        private VehicleDynamics dynamics;
        private VehicleController controller;

        public float SteerInput { get; private set; } = 0f;
        public float AccelInput { get; private set; } = 0f;

        private void Start()
        {
            dynamics = GetComponentInParent<VehicleDynamics>();
            controller = GetComponentInParent<VehicleController>();
        }

        public void Update()
        {
            Debug.Assert(dynamics != null);

            if (controller.AccelInput >= 0)
                AccelInput = dynamics.CurrentSpeed < CruiseSpeed ? 1f : 0f;
        }
        
        public override void OnBridgeSetup(IBridge bridge)
        {
            // TODO new base class?
        }
    }
}
