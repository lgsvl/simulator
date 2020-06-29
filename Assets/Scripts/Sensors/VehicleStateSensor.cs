/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Simulator.Bridge;
using Simulator.Bridge.Data;
using Simulator.Sensors.UI;
using Simulator.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace Simulator.Sensors
{
    [SensorType("Vehicle State", new[] { typeof(VehicleStateData) })]

    public class VehicleStateSensor : SensorBase
    {
        VehicleStateData StateData;

        VehicleActions Actions;
        IVehicleDynamics Dynamics;
        
        public override SensorDistributionType DistributionType => SensorDistributionType.LowLoad;

        void Start()
        {
            Actions = GetComponentInParent<VehicleActions>();
            Dynamics = GetComponentInParent<IVehicleDynamics>();
            StateData = new VehicleStateData();
        }

        public override void OnBridgeSetup(BridgeInstance bridge)
        {
            bridge.AddSubscriber<VehicleStateData>(Topic, data =>
            {
                if (data != null)
                {
                    if (StateData.Blinker != data.Blinker)
                    {
                        if (data.Blinker == 0)
                        {
                            if (Actions.LeftTurnSignal)
                                Actions.LeftTurnSignal = false;
                            if (Actions.RightTurnSignal)
                                Actions.RightTurnSignal = false;
                            if (Actions.HazardLights)
                                Actions.HazardLights = false;
                        }
                        else if (data.Blinker == 1)
                            Actions.LeftTurnSignal = true;
                        else if (data.Blinker == 2)
                            Actions.RightTurnSignal = true;
                        else if (data.Blinker == 3)
                            Actions.HazardLights = true;
                    }
                    if (StateData.HeadLight != data.HeadLight)
                    {
                        if (data.HeadLight == 0)
                            Actions.CurrentHeadLightState = VehicleActions.HeadLightState.OFF;
                        else if (data.HeadLight == 1)
                            Actions.CurrentHeadLightState = VehicleActions.HeadLightState.LOW;
                        else if (data.HeadLight == 2)
                            Actions.CurrentHeadLightState = VehicleActions.HeadLightState.HIGH;
                    }
                    if (StateData.Gear != data.Gear)
                    {
                        if (data.Gear == 1)
                            Dynamics.ShiftReverse();
                        else if (data.Gear == 0)
                            Dynamics.ShiftFirstGear();
                    }
                    if (StateData.HandBrake != data.HandBrake)
                    {
                        if (data.HandBrake == true)
                            Dynamics.SetHandBrake(true);
                        else
                            Dynamics.SetHandBrake(false);
                    }

                    StateData = data;
                }
            });
        }

        public override void OnVisualize(Visualizer visualizer)
        {
            Debug.Assert(visualizer != null);
            var graphData = new Dictionary<string, object>()
            {
                {"Left Turn Signal", StateData.Blinker == 1},
                {"Right Turn Signal", StateData.Blinker == 2},
                {"Hazard Light", StateData.Blinker == 3},
                {"Head Light", Actions.CurrentHeadLightState.ToString()},
                {"Reverse Gear", Dynamics.Reverse},
                {"Gear", Dynamics.CurrentGear},
                {"Hand Brake", Dynamics.HandBrake},
            };

            visualizer.UpdateGraphValues(graphData);
        }

        public override void OnVisualizeToggle(bool state)
        {
        }
    }
}
