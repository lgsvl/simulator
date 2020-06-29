/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using Simulator.Bridge;
using Simulator.Utilities;
using Simulator.Sensors.UI;
using System.Collections.Generic;

namespace Simulator.Sensors
{
    [SensorType("Cruise Control", new System.Type[] { })]
    public class CruiseControlSensor : SensorBase, IVehicleInputs
    {
        [SensorParameter]
        [Range(0.0f, 200f)]
        public float CruiseSpeed = 0f;

        private IVehicleDynamics dynamics;
        private VehicleController controller;

        public float SteerInput { get; private set; } = 0f;
        public float AccelInput { get; private set; } = 0f;
        public float BrakeInput { get; private set; } = 0f;
        
        public override SensorDistributionType DistributionType => SensorDistributionType.LowLoad;

        private void Start()
        {
            dynamics = GetComponentInParent<IVehicleDynamics>();
            controller = GetComponentInParent<VehicleController>();
        }

        public void Update()
        {
            Debug.Assert(dynamics != null);

            if (controller.AccelInput >= 0)
            {
                AccelInput = dynamics.RB.velocity.magnitude < CruiseSpeed ? 1f : 0f;
            }
        }
        
        public override void OnBridgeSetup(BridgeInstance bridge)
        {
            // TODO new base class?
        }

        public override void OnVisualize(Visualizer visualizer)
        {
            Debug.Assert(visualizer != null);

            var graphData = new Dictionary<string, object>()
            {
                {"Cruise Speed", CruiseSpeed},
                {"Steer Input", SteerInput},
                {"Accel Input", AccelInput},
                {"Speed", dynamics.RB.velocity.magnitude},
                {"Hand Brake", dynamics.HandBrake},
                {"Ignition", dynamics.CurrentIgnitionStatus},
                {"Reverse", dynamics.Reverse},
                {"Gear", dynamics.CurrentGear},
                {"RPM", dynamics.CurrentRPM},
                {"Velocity", dynamics.RB.velocity}
            };
            visualizer.UpdateGraphValues(graphData);
        }

        public override void OnVisualizeToggle(bool state)
        {
            //
        }
    }
}
