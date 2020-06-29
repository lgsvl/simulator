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
    [SensorType("Wheel Control", new System.Type[] { })]
    public class WheelControlSensor : SensorBase, IVehicleInputs
    {
        public float SteerInput { get; private set; } = 0f;
        public float AccelInput { get; private set; } = 0f;
        public float BrakeInput { get; private set; } = 0f;
        private float steer = 0f;
        private float accel = 0f;
        private float brake = 0f;

        private SimulatorControls controls;
        private IVehicleDynamics dynamics;
        private VehicleActions actions;
        private AgentController AgentController;

        private OperatingSystemFamily operatingSystemFamily;

        private void Awake()
        {
            operatingSystemFamily = SystemInfo.operatingSystemFamily;
        }

        private void Start()
        {
            AgentController = GetComponentInParent<AgentController>();

            dynamics = GetComponentInParent<IVehicleDynamics>();
            actions = GetComponentInParent<VehicleActions>();

            Debug.Assert(dynamics != null);
            Debug.Assert(actions != null);
            Debug.Assert(SimulatorManager.Instance != null);

            controls = SimulatorManager.Instance.controls;
            controls.VehicleWheel.Accel.performed += AccelPerformed;
            controls.VehicleWheel.Accel.canceled += AccelCanceled;
            controls.VehicleWheel.Brake.performed += BrakePerformed;
            controls.VehicleWheel.Brake.canceled += BrakeCanceled;
            controls.VehicleWheel.Steer.performed += SteerPerformed;
            controls.VehicleWheel.ButtonA.performed += ButtonA;
            controls.VehicleWheel.ButtonB.performed += ButtonB;
            controls.VehicleWheel.ButtonX.performed += ButtonX;
            controls.VehicleWheel.ButtonY.performed += ButtonY;
            controls.VehicleWheel.ButtonRB.performed += ButtonRB;
            controls.VehicleWheel.ButtonLB.performed += ButtonLB;
            controls.VehicleWheel.ButtonSelect.performed += ButtonSelect;
            controls.VehicleWheel.ButtonStart.performed += ButtonStart;
            controls.VehicleWheel.ButtonRSB.performed += ButtonRSB;
            controls.VehicleWheel.ButtonLSB.performed += ButtonLSB;
            controls.VehicleWheel.ButtonCenter.performed += ButtonCenter;
            controls.VehicleWheel.DPad.performed += DPad;
        }

        private void AccelPerformed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            switch (operatingSystemFamily)
            {
                case OperatingSystemFamily.Other:
                    break;
                case OperatingSystemFamily.MacOSX:
                    break;
                case OperatingSystemFamily.Windows:
                    accel = Mathf.InverseLerp(1f, -1f, obj.ReadValue<float>());
                    break;
                case OperatingSystemFamily.Linux:
                    accel = Mathf.InverseLerp(-1f, 1f, obj.ReadValue<float>());
                    break;
                default:
                    break;
            }
        }

        private void AccelCanceled(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            accel = 0f;
        }

        private void BrakePerformed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            brake = Mathf.InverseLerp(1f, -1f, obj.ReadValue<float>());
        }

        private void BrakeCanceled(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            brake = 0f;
        }

        private void SteerPerformed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            steer = obj.ReadValue<float>();
        }

        private void ButtonA(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            SimulatorManager.Instance?.AgentManager?.SetNextCurrentActiveAgent();
        }

        private void ButtonB(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            SimulatorManager.Instance?.NPCManager?.ToggleNPC();
        }

        private void ButtonX(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            SimulatorManager.Instance?.UIManager?.ToggleVisualizers();
        }

        private void ButtonY(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            SimulatorManager.Instance?.CameraManager?.ToggleCameraState();
        }

        private void ButtonRB(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            accel = obj.ReadValue<float>();
        }

        private void ButtonLB(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            brake = obj.ReadValue<float>();
        }

        private void ButtonSelect(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            SimulatorManager.Instance?.UIManager?.MenuButtonOnClick();
        }

        private void ButtonStart(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            SimulatorManager.Instance?.UIManager?.PauseButtonOnClick();
        }

        private void ButtonRSB(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            actions.IncrementHeadLightState();
        }

        private void ButtonLSB(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            dynamics.ToggleReverse();
        }

        private void ButtonCenter(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            actions.InteriorLight = !actions.InteriorLight;
        }

        private void DPad(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            var dpad = obj.ReadValue<Vector2>();
            if (dpad.y == 1) // up
            {
                //
            }
            else if (dpad.y == -1) // down
            {
                //
            }
            else if (dpad.x == 1) // right
            {
                //
            }
            else if (dpad.x == -1) // left
            {
                //
            }
        }

        private void Update()
        {
            if (AgentController.Active)
            {
                SteerInput = steer;
                AccelInput = accel - brake;
                BrakeInput = brake;
            }
        }

        private void OnDestroy()
        {
            controls.VehicleWheel.Accel.performed -= AccelPerformed;
            controls.VehicleWheel.Accel.canceled -= AccelCanceled;
            controls.VehicleWheel.Brake.performed -= BrakePerformed;
            controls.VehicleWheel.Brake.canceled -= BrakeCanceled;
            controls.VehicleWheel.Steer.performed -= SteerPerformed;
            controls.VehicleWheel.ButtonA.performed -= ButtonA;
            controls.VehicleWheel.ButtonB.performed -= ButtonB;
            controls.VehicleWheel.ButtonX.performed -= ButtonX;
            controls.VehicleWheel.ButtonY.performed -= ButtonY;
            controls.VehicleWheel.ButtonRB.performed -= ButtonRB;
            controls.VehicleWheel.ButtonLB.performed -= ButtonLB;
            controls.VehicleWheel.ButtonSelect.performed -= ButtonSelect;
            controls.VehicleWheel.ButtonStart.performed -= ButtonStart;
            controls.VehicleWheel.ButtonRSB.performed -= ButtonRSB;
            controls.VehicleWheel.ButtonLSB.performed -= ButtonLSB;
            controls.VehicleWheel.ButtonCenter.performed -= ButtonCenter;
            controls.VehicleWheel.DPad.performed -= DPad;
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
                {"Accel", accel},
                {"Steer", steer},
                {"Brake", brake},
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
