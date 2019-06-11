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
    public class ManualControlSensor : SensorBase, IVehicleInputs
    {
        private SimulatorControls controls;
        private VehicleDynamics dynamics;
        private VehicleActions actions;

        public float SteerInput { get; private set; } = 0f;
        public float AccelInput { get; private set; } = 0f;
        private Vector2 keyboardInput = Vector2.zero;
        private void Start()
        {
            dynamics = GetComponentInParent<VehicleDynamics>();
            actions = GetComponentInParent<VehicleActions>();

            Debug.Assert(dynamics != null);
            Debug.Assert(actions != null);
            Debug.Assert(SimulatorManager.Instance != null);

            controls = SimulatorManager.Instance.controls;
            controls.Vehicle.Direction.started += ctx => keyboardInput = ctx.ReadValue<Vector2>();
            controls.Vehicle.Direction.performed += ctx => keyboardInput = ctx.ReadValue<Vector2>();
            controls.Vehicle.Direction.canceled += ctx => keyboardInput = Vector2.zero;
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

        private void Update()
        {
            SteerInput = keyboardInput.x;
            AccelInput = keyboardInput.y;
        }

        public override void OnBridgeSetup(IBridge bridge)
        {
            // TODO new base class?
        }
    }
}