/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Simulator.Bridge;
using Simulator.Bridge.Data;
using Simulator.Bridge.Ros.Autoware;
using Simulator.Sensors.UI;
using Simulator.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Simulator.Sensors
{
    [SensorType("Vehicle State", new[] { typeof(VehicleStateData) })]

    public class VehicleStateSensor : SensorBase
    {
        VehicleStateData readStateData;
        VehicleActions Actions;
        VehicleDynamics Dynamics;
        double LastStateUpdate = Double.MaxValue;

        void Start()
        {
            Actions = GetComponentInParent<VehicleActions>();
            Dynamics = GetComponentInParent<VehicleDynamics>();
        }

        void Update()
        {
            // Data conversion from Reader
            if (SimulatorManager.Instance.CurrentTime - LastStateUpdate > 0)
            {
                // No fuels supported.

                // Blinker
                if (readStateData.Blinker == 0)
                {
                    Actions.LeftTurnSignal = false;
                    Actions.RightTurnSignal = false;
                    Actions.HazardLights = false;
                }
                else if (readStateData.Blinker == 1)
                    Actions.LeftTurnSignal = true;
                else if (readStateData.Blinker == 2)
                    Actions.RightTurnSignal = true;
                else if (readStateData.Blinker == 3)
                    Actions.HazardLights = true;

                // Headlight
                if (readStateData.HeadLight == 0)
                    Actions.CurrentHeadLightState = VehicleActions.HeadLightState.OFF;
                else if (readStateData.HeadLight == 1)
                    Actions.CurrentHeadLightState = VehicleActions.HeadLightState.LOW;
                else if (readStateData.HeadLight == 2)
                    Actions.CurrentHeadLightState = VehicleActions.HeadLightState.HIGH;

                // No Wiper supported.
                // No WIPER_OFF, WIPER_LOW, WIPER_HIGH, WIPER_CLEAN

                // Manual Gear in Simulator
                // No GEAR_PARK, GEAR_LOW, GEAR_NEUTRAL supported.
                if (readStateData.Gear == 1)
                    Dynamics.ShiftReverse();
                else if (readStateData.Gear == 0)
                    Dynamics.ShiftFirstGear();

                // No info about mode

                // Handbrake
                if (readStateData.HandBrake == true)
                    Dynamics.SetHandBrake(true);
                else
                    Dynamics.SetHandBrake(false);

                // No Horn supported.

                // No Autonomous mode supported.
                // No MODE_MANUAL, MODE_NOT_READY, MODE_AUTONOMOUS, MODE_DISENGAGED
            }
        }

        public override void OnBridgeSetup(IBridge bridge)
        {
            bridge.AddReader<VehicleStateData>(Topic, data =>
            {
                if (data != null)
                {
                    readStateData = data;
                    LastStateUpdate = SimulatorManager.Instance.CurrentTime;
                }
            });
        }

        public override void OnVisualize(Visualizer visualizer)
        {
            Debug.Assert(visualizer != null);
            var graphData = new Dictionary<string, object>()
            {
                {"Left Turn Signal", Actions.LeftTurnSignal},
                {"Right Turn Signal", Actions.RightTurnSignal},
                {"Hazard Light", Actions.HazardLights},
                {"Head Light", Actions.CurrentHeadLightState.ToString()},
                {"Gear", Dynamics.CurrentGear},
                {"Reverse Gear", Dynamics.Reverse},
                {"Hand Brake", Dynamics.HandBrake},
            };

            visualizer.UpdateGraphValues(graphData);
        }

        public override void OnVisualizeToggle(bool state)
        {
        }
    }
}
