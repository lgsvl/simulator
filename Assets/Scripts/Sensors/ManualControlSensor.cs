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
    [SensorType("Manual Control", new System.Type[]{})]
    public class ManualControlSensor : SensorBase
    {
        private SimulatorControls controls;
        private VehicleDynamics dynamics;
        private VehicleActions actions;
        private VehicleController controller;
        
        private void Start()
        {
            dynamics = GetComponentInParent<VehicleDynamics>();
            actions = GetComponentInParent<VehicleActions>();
            controller = GetComponentInParent<VehicleController>();

            Debug.Assert(dynamics != null);
            Debug.Assert(actions != null);
            Debug.Assert(controller != null);
            Debug.Assert(SimulatorManager.Instance != null);

            controls = SimulatorManager.Instance.controls;
            controls.Vehicle.Direction.started += ctx => controller.DirectionInput = ctx.ReadValue<Vector2>();
            controls.Vehicle.Direction.performed += ctx => controller.DirectionInput = ctx.ReadValue<Vector2>();
            controls.Vehicle.Direction.canceled += ctx => controller.DirectionInput = Vector2.zero;
            controls.Vehicle.ShiftFirst.performed += ctx => dynamics.ShiftFirstGear();
            controls.Vehicle.ShiftReverse.performed += ctx => dynamics.ShiftReverse();
            controls.Vehicle.ParkingBrake.performed += ctx => dynamics.ToggleHandBrake();
            controls.Vehicle.Ignition.performed += ctx => dynamics.ToggleIgnition();
            controls.Vehicle.HeadLights.performed += ctx => actions.IncrementHeadLightState();
            controls.Vehicle.IndicatorLeft.performed += ctx => actions.LeftTurnSignal = !actions.LeftTurnSignal;
            controls.Vehicle.IndicatorRight.performed += ctx => actions.RightTurnSignal = !actions.RightTurnSignal;
            controls.Vehicle.IndicatorHazard.performed += ctx => actions.HazardLights = !actions.HazardLights;
            controls.Vehicle.FogLights.performed += ctx => actions.FogLights = !actions.FogLights;
            controls.Vehicle.InteriorLight.performed += ctx => actions.InteriorLight = !actions.InteriorLight;
        }

        public override void OnBridgeSetup(IBridge bridge)
        {
            // TODO new base class?
        }
    }
}