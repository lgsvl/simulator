/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Simulator.Bridge;
using Simulator.Bridge.Data;
using Simulator.Utilities;
using UnityEngine;
using Simulator.Sensors.UI;
using System.Collections.Generic;

namespace Simulator.Sensors
{
    [SensorType("Vehicle Control", new[] { typeof(VehicleControlData) })]
    public class VehicleControlSensor : SensorBase, IVehicleInputs
    {
        VehicleControlData Data;
        VehicleController Controller;
        IVehicleDynamics Dynamics;

        double LastControlUpdate = 0f;
        float ActualLinVel = 0f;
        float ActualAngVel = 0f;

        public float SteerInput { get; private set; } = 0f;
        public float AccelInput { get; private set; } = 0f;
        public float BrakeInput { get; private set; } = 0f;

        float ADAccelInput = 0f;
        float ADSteerInput = 0f;

        public AnimationCurve AccelerationInputCurve;
        public AnimationCurve BrakeInputCurve;

        double LastTimeStamp = 0;  // from Apollo

        VehicleControlData controlData;

        private enum ControlType
        {
            None,
            AutowareAi,
            Apollo,
            LGSVL,
            AutowareAuto,
        };
        ControlType controlType = ControlType.None;

        private void Awake()
        {
            LastControlUpdate = SimulatorManager.Instance.CurrentTime;
            Controller = GetComponentInParent<VehicleController>();
            Dynamics = GetComponentInParent<IVehicleDynamics>();
        }

        private void Update()
        {
            var projectedLinVec = Vector3.Project(Dynamics.RB.velocity, transform.forward);
            ActualLinVel = projectedLinVec.magnitude * (Vector3.Dot(Dynamics.RB.velocity, transform.forward) > 0 ? 1.0f : -1.0f);

            var projectedAngVec = Vector3.Project(Dynamics.RB.angularVelocity, transform.up);
            ActualAngVel = projectedAngVec.magnitude * (projectedAngVec.y > 0 ? -1.0f : 1.0f);

            // LastControlUpdate and Time.Time come from Unity.
            if (SimulatorManager.Instance.CurrentTime - LastControlUpdate >= 0.5)    // > 500ms
            {
                ADAccelInput = ADSteerInput = AccelInput = SteerInput = 0f;
            }
        }

        private void FixedUpdate()
        {
            if (SimulatorManager.Instance.CurrentTime - LastControlUpdate < 0.5f)
            {
                AccelInput = ADAccelInput;
                SteerInput = ADSteerInput;
            }
        }

        public override void OnBridgeSetup(BridgeInstance bridge)
        {
            bridge.AddSubscriber<VehicleControlData>(Topic, data =>
            {
                controlData = data;
                LastControlUpdate = SimulatorManager.Instance.CurrentTime;

                if (data.Velocity.HasValue) // autoware
                {
                    controlType = ControlType.AutowareAi;
                    if (data.ShiftGearUp || data.ShiftGearDown)
                    {
                        if (data.ShiftGearUp) Dynamics.GearboxShiftUp();
                        if (data.ShiftGearDown) Dynamics.GearboxShiftDown();

                        ADAccelInput = data.Acceleration.GetValueOrDefault() - data.Braking.GetValueOrDefault(); // converted from lin accel 
                        ADSteerInput = data.SteerAngle.GetValueOrDefault(); // angle should be in degrees
                    }
                    else
                    {
                        if (Dynamics.Reverse) return; // TODO move?

                        var linMag = Mathf.Clamp(Mathf.Abs(data.Velocity.GetValueOrDefault() - ActualLinVel), 0f, 1f);
                        ADAccelInput = ActualLinVel < data.Velocity.GetValueOrDefault() ? linMag : -linMag;
                        ADSteerInput = -Mathf.Clamp(data.SteerAngularVelocity.GetValueOrDefault() * 0.5f, -1f, 1f);
                    }
                }
                else if (data.SteerRate.HasValue) // apollo
                {
                    if (double.IsInfinity(data.Acceleration.GetValueOrDefault()) || double.IsInfinity(data.Braking.GetValueOrDefault()) ||
                        double.IsNaN(data.Acceleration.GetValueOrDefault()) || double.IsNaN(data.Braking.GetValueOrDefault()))
                    {
                        return;
                    }

                    controlType = ControlType.Apollo;
                    var timeStamp = data.TimeStampSec.GetValueOrDefault();
                    var dt = (float)(timeStamp - LastTimeStamp);
                    LastTimeStamp = timeStamp;

                    Debug.Assert(data.Acceleration.GetValueOrDefault() >= 0 && data.Acceleration.GetValueOrDefault() <= 1);
                    Debug.Assert(data.Braking.GetValueOrDefault() >= 0 && data.Braking.GetValueOrDefault() <= 1);
                    var linearAccel = AccelerationInputCurve.Evaluate(data.Acceleration.GetValueOrDefault()) - BrakeInputCurve.Evaluate(data.Braking.GetValueOrDefault());

                    var steeringTarget = -data.SteerTarget.GetValueOrDefault();
                    var steeringAngle = Controller.SteerInput;
                    var sgn = Mathf.Sign(steeringTarget - steeringAngle);
                    var steeringRate = data.SteerRate.GetValueOrDefault() * sgn;
                    steeringAngle += steeringRate * dt;

                    if (sgn != steeringTarget - steeringAngle) // to prevent oversteering
                        steeringAngle = steeringTarget;

                    ADSteerInput = steeringAngle;
                    ADAccelInput = linearAccel;

                    if (data.CurrentGear == GearPosition.Reverse)
                    {
                        Dynamics.ShiftReverseAutoGearBox();
                    }
                    else if (data.CurrentGear == GearPosition.Drive)
                    {
                        Dynamics.ShiftFirstGear();
                    }
                }
                else if (data.SteerInput.HasValue) // lgsvl
                {
                    controlType = ControlType.LGSVL;
                    ADSteerInput = data.SteerInput.GetValueOrDefault();
                }
                else if (data.Acceleration.HasValue)
                {
                    controlType = ControlType.AutowareAuto;
                    ADAccelInput = data.Acceleration.GetValueOrDefault() - data.Braking.GetValueOrDefault();
                    ADSteerInput = data.SteerAngle.GetValueOrDefault();
                }
                else
                {
                    controlType = ControlType.None;
                }
            });
        }

        public override void OnVisualize(Visualizer visualizer)
        {
            Debug.Assert(visualizer != null);
            var graphData = new Dictionary<string, object>()
            {
                {"Control Type", controlType},
                {"AD Accel Input", ADAccelInput},
                {"AD Steer Input", ADSteerInput},
                {"Last Control Update", LastControlUpdate},
                {"Actual Linear Velocity", ActualLinVel},
                {"Actual Angular Velocity", ActualAngVel},
            };

            switch (controlType)
            {
                case ControlType.None:
                    break;
                case ControlType.AutowareAi:
                    if (controlData == null)
                    {
                        return;
                    }
                    graphData.Add("Shift Up", controlData.ShiftGearUp);
                    graphData.Add("Shift Down", controlData.ShiftGearDown);
                    graphData.Add("Acceleration", controlData.Acceleration.GetValueOrDefault());
                    graphData.Add("Braking", controlData.Braking.GetValueOrDefault());
                    graphData.Add("Steer Angle", controlData.SteerAngle.GetValueOrDefault());
                    graphData.Add("Velocity", controlData.Velocity.GetValueOrDefault());
                    graphData.Add("Steer Angle Velocity", controlData.SteerAngularVelocity.GetValueOrDefault());
                    break;
                case ControlType.AutowareAuto:
                    if (controlData == null)
                    {
                        return;
                    }
                    graphData.Add("Acceleration", controlData.Acceleration.GetValueOrDefault());
                    graphData.Add("Braking", controlData.Braking.GetValueOrDefault());
                    graphData.Add("Steer Angle", controlData.SteerAngle.GetValueOrDefault());
                    break;
                case ControlType.Apollo:
                    if (controlData == null)
                    {
                        return;
                    }
                    graphData.Add("Acceleration", controlData.Acceleration.GetValueOrDefault());
                    graphData.Add("Braking", controlData.Braking.GetValueOrDefault());
                    graphData.Add("Time Stamp Sec", controlData.TimeStampSec.GetValueOrDefault());
                    graphData.Add("Steer Rate", controlData.SteerRate.GetValueOrDefault());
                    graphData.Add("Steer Target", controlData.SteerTarget.GetValueOrDefault());
                    graphData.Add("Gear", controlData.CurrentGear);
                    break;
                case ControlType.LGSVL:
                    if (controlData == null)
                    {
                        return;
                    }
                    graphData.Add("Steer Input", controlData.SteerInput.GetValueOrDefault());
                    break;
                default:
                    break;
            }

            visualizer.UpdateGraphValues(graphData);
        }

        public override void OnVisualizeToggle(bool state)
        {
            //
        }
    }
}
