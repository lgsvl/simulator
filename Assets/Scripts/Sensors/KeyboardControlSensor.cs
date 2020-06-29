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
    [SensorType("Keyboard Control", new System.Type[] { })]
    public class KeyboardControlSensor : SensorBase, IVehicleInputs
    {
        public float SteerInput { get; private set; } = 0f;
        public float AccelInput { get; private set; } = 0f;
        public float BrakeInput { get; private set; } = 0f;

        private SimulatorControls controls;
        private IVehicleDynamics dynamics;
        private VehicleActions actions;
        private Vector2 keyboardInput = Vector2.zero;
        private AgentController AgentController;

        private void Start()
        {
            AgentController = GetComponentInParent<AgentController>();

            dynamics = GetComponentInParent<IVehicleDynamics>();
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
                controls.VehicleKeyboard.Direction.started += DirectionStarted;
                controls.VehicleKeyboard.Direction.performed += DirectionPerformed;
                controls.VehicleKeyboard.Direction.canceled += DirectionCanceled;
                controls.VehicleKeyboard.ShiftFirst.performed += ShiftFirstPerformed;
                controls.VehicleKeyboard.ShiftReverse.performed += ShiftReversePerformed;
                controls.VehicleKeyboard.ParkingBrake.performed += ParkingBrakePerformed;
                controls.VehicleKeyboard.Ignition.performed += IgnitionPerformed;
                controls.VehicleKeyboard.HeadLights.performed += HeadLightsPerformed;
                controls.VehicleKeyboard.IndicatorLeft.performed += IndicatorLeftPerformed;
                controls.VehicleKeyboard.IndicatorRight.performed += IndicatorRightPerformed;
                controls.VehicleKeyboard.IndicatorHazard.performed += IndicatorHazardPerformed;
                controls.VehicleKeyboard.FogLights.performed += FogLightsPerformed;
                controls.VehicleKeyboard.InteriorLight.performed += InteriorLightPerformed;
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
                controls.VehicleKeyboard.Direction.started -= DirectionStarted;
                controls.VehicleKeyboard.Direction.performed -= DirectionPerformed;
                controls.VehicleKeyboard.Direction.canceled -= DirectionCanceled;
                controls.VehicleKeyboard.ShiftFirst.performed -= ShiftFirstPerformed;
                controls.VehicleKeyboard.ShiftReverse.performed -= ShiftReversePerformed;
                controls.VehicleKeyboard.ParkingBrake.performed -= ParkingBrakePerformed;
                controls.VehicleKeyboard.Ignition.performed -= IgnitionPerformed;
                controls.VehicleKeyboard.HeadLights.performed -= HeadLightsPerformed;
                controls.VehicleKeyboard.IndicatorLeft.performed -= IndicatorLeftPerformed;
                controls.VehicleKeyboard.IndicatorRight.performed -= IndicatorRightPerformed;
                controls.VehicleKeyboard.IndicatorHazard.performed -= IndicatorHazardPerformed;
                controls.VehicleKeyboard.FogLights.performed -= FogLightsPerformed;
                controls.VehicleKeyboard.InteriorLight.performed -= InteriorLightPerformed;
            }
        }

        public override void OnBridgeSetup(BridgeInstance bridge)
        {
            //
        }

        public override void OnVisualize(Visualizer visualizer)
        {
            Debug.Assert(visualizer != null);
            var graphData = new Dictionary<string, object>()
            {
                {"Accel", AccelInput},
                {"Steer", SteerInput},
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
