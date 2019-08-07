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

        AgentController AgentController;

        private void Start()
        {
            AgentController = GetComponentInParent<AgentController>();

            dynamics = GetComponentInParent<VehicleDynamics>();
            actions = GetComponentInParent<VehicleActions>();

            Debug.Assert(dynamics != null);
            Debug.Assert(actions != null);
            Debug.Assert(SimulatorManager.Instance != null);

            controls = SimulatorManager.Instance.controls;

            if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Linux && Application.isEditor)
            {
                // empty
            }
            else
            {
                controls.Vehicle.Direction.started += DirectionStarted;
                controls.Vehicle.Direction.performed += DirectionPerformed;
                controls.Vehicle.Direction.canceled += DirectionCanceled;
                controls.Vehicle.ShiftFirst.performed += ShiftFirstPerformed;
                controls.Vehicle.ShiftReverse.performed += ShiftReversePerformed;
                controls.Vehicle.ParkingBrake.performed += ParkingBrakePerformed;
                controls.Vehicle.Ignition.performed += IgnitionPerformed;
                controls.Vehicle.HeadLights.performed += HeadLightsPerformed;
                controls.Vehicle.IndicatorLeft.performed += IndicatorLeftPerformed;
                controls.Vehicle.IndicatorRight.performed += IndicatorRightPerformed;
                controls.Vehicle.IndicatorHazard.performed += IndicatorHazardPerformed;
                controls.Vehicle.FogLights.performed += FogLightsPerformed;
                controls.Vehicle.InteriorLight.performed += InteriorLightPerformed;
            }
        }
        
        private void DirectionStarted(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            keyboardInput = obj.ReadValue<Vector2>();
        }

        private void DirectionPerformed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            keyboardInput = obj.ReadValue<Vector2>();
        }

        private void DirectionCanceled(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            keyboardInput = Vector2.zero;
        }

        private void ShiftFirstPerformed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            dynamics.ShiftFirstGear();
        }

        private void ShiftReversePerformed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            dynamics.ShiftReverse();
        }

        private void ParkingBrakePerformed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            dynamics.ToggleHandBrake();
        }

        private void IgnitionPerformed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            dynamics.ToggleIgnition();
        }

        private void HeadLightsPerformed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            actions.IncrementHeadLightState();
        }

        private void IndicatorLeftPerformed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            actions.LeftTurnSignal = !actions.LeftTurnSignal;
        }

        private void IndicatorRightPerformed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            actions.RightTurnSignal = !actions.RightTurnSignal;
        }

        private void IndicatorHazardPerformed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            actions.HazardLights = !actions.HazardLights;
        }

        private void FogLightsPerformed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            actions.FogLights = !actions.FogLights;
        }

        private void InteriorLightPerformed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            actions.InteriorLight = !actions.InteriorLight;
        }

        private void Update()
        {
            if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Linux && Application.isEditor)
            {
                // this is a temporary workaround for Unity Editor on Linux
                // see https://issuetracker.unity3d.com/issues/linux-editor-keyboard-when-input-handling-is-set-to-both-keyboard-input-stops-working

                keyboardInput = Vector2.zero;
                if (Input.GetKey(KeyCode.LeftArrow)) keyboardInput.x -= 1;
                if (Input.GetKey(KeyCode.RightArrow)) keyboardInput.x += 1;
                if (Input.GetKey(KeyCode.UpArrow)) keyboardInput.y += 1;
                if (Input.GetKey(KeyCode.DownArrow)) keyboardInput.y -= 1;

                var ctx = new UnityEngine.InputSystem.InputAction.CallbackContext();
                if (Input.GetKeyDown(KeyCode.H)) HeadLightsPerformed(ctx);
                if (Input.GetKeyDown(KeyCode.Comma)) IndicatorLeftPerformed(ctx);
                if (Input.GetKeyDown(KeyCode.Period)) IndicatorRightPerformed(ctx);
                if (Input.GetKeyDown(KeyCode.M)) IndicatorHazardPerformed(ctx);
                if (Input.GetKeyDown(KeyCode.F)) FogLightsPerformed(ctx);
                if (Input.GetKeyDown(KeyCode.PageUp)) ShiftFirstPerformed(ctx);
                if (Input.GetKeyDown(KeyCode.PageDown)) ShiftReversePerformed(ctx);
                if (Input.GetKeyDown(KeyCode.RightShift)) ParkingBrakePerformed(ctx);
                if (Input.GetKeyDown(KeyCode.End)) IgnitionPerformed(ctx);
                if (Input.GetKeyDown(KeyCode.I)) InteriorLightPerformed(ctx);
            }

            if (AgentController.Active)
            {
                SteerInput = Mathf.MoveTowards(SteerInput, keyboardInput.x, Time.deltaTime);
                AccelInput = keyboardInput.y;
            }
        }

        private void OnDestroy()
        {
            if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Linux && Application.isEditor)
            {
                // empty
            }
            else
            {
                controls.Vehicle.Direction.started -= DirectionStarted;
                controls.Vehicle.Direction.performed -= DirectionPerformed;
                controls.Vehicle.Direction.canceled -= DirectionCanceled;
                controls.Vehicle.ShiftFirst.performed -= ShiftFirstPerformed;
                controls.Vehicle.ShiftReverse.performed -= ShiftReversePerformed;
                controls.Vehicle.ParkingBrake.performed -= ParkingBrakePerformed;
                controls.Vehicle.Ignition.performed -= IgnitionPerformed;
                controls.Vehicle.HeadLights.performed -= HeadLightsPerformed;
                controls.Vehicle.IndicatorLeft.performed -= IndicatorLeftPerformed;
                controls.Vehicle.IndicatorRight.performed -= IndicatorRightPerformed;
                controls.Vehicle.IndicatorHazard.performed -= IndicatorHazardPerformed;
                controls.Vehicle.FogLights.performed -= FogLightsPerformed;
                controls.Vehicle.InteriorLight.performed -= InteriorLightPerformed;
            }
        }

        public override void OnBridgeSetup(IBridge bridge)
        {
            // TODO new base class?
        }
    }
}