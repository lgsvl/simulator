/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Simulator.Bridge;
using Simulator.Bridge.Data;
using Simulator.Map;
using Simulator.Utilities;
using UnityEngine;
using Simulator.Sensors.UI;

namespace Simulator.Sensors
{
    [SensorType("Vehicle Control", new[] { typeof(VehicleControlData) })]
    public class VehicleControlSensor : SensorBase, IVehicleInputs
    {
        VehicleControlData Data;
        VehicleController Controller;
        VehicleDynamics Dynamics;

        float LastControlUpdate = 0f;
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

        private void Awake()
        {
            LastControlUpdate = Time.time;
            Controller = GetComponentInParent<VehicleController>();
            Dynamics = GetComponentInParent<VehicleDynamics>();
        }

        private void Update()
        {
            var projectedLinVec = Vector3.Project(Dynamics.RB.velocity, transform.forward);
            ActualLinVel = projectedLinVec.magnitude * (Vector3.Dot(Dynamics.RB.velocity, transform.forward) > 0 ? 1.0f : -1.0f);

            var projectedAngVec = Vector3.Project(Dynamics.RB.angularVelocity, transform.up);
            ActualAngVel = projectedAngVec.magnitude * (projectedAngVec.y > 0 ? -1.0f : 1.0f);

            // LastControlUpdate and Time.Time come from Unity.
            if (Time.time - LastControlUpdate >= 0.5)    // > 500ms
            {
                ADAccelInput = ADSteerInput = AccelInput = SteerInput = 0f;
            }
        }

        private void FixedUpdate()
        {
            if (Time.time - LastControlUpdate < 0.5f)
            {
                AccelInput = ADAccelInput;
                SteerInput = ADSteerInput;
            }
        }

        public override void OnBridgeSetup(IBridge bridge)
        {
            bridge.AddReader<VehicleControlData>(Topic, data =>
            {
                LastControlUpdate = Time.time;

                if (data.Velocity.HasValue) // autoware
                {
                    if (data.ShiftGearUp || data.ShiftGearDown)
                    {
                        if (data.ShiftGearUp) Dynamics.GearboxShiftUp();
                        if (data.ShiftGearDown) Dynamics.GearboxShiftDown();

                        ADAccelInput = data.Acceleration.GetValueOrDefault() - data.Breaking.GetValueOrDefault(); // converted from lin accel 
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
                    if (double.IsInfinity(data.Acceleration.GetValueOrDefault()) || double.IsInfinity(data.Breaking.GetValueOrDefault()) ||
                        double.IsNaN(data.Acceleration.GetValueOrDefault()) || double.IsNaN(data.Breaking.GetValueOrDefault()))
                    {
                        return;
                    }

                    var timeStamp = data.TimeStampSec.GetValueOrDefault();
                    var dt = (float)(timeStamp - LastTimeStamp);
                    LastTimeStamp = timeStamp;

                    Debug.Assert(data.Acceleration.GetValueOrDefault() >= 0 && data.Acceleration.GetValueOrDefault() <= 1);
                    Debug.Assert(data.Breaking.GetValueOrDefault() >= 0 && data.Breaking.GetValueOrDefault() <= 1);
                    var linearAccel = AccelerationInputCurve.Evaluate(data.Acceleration.GetValueOrDefault()) - BrakeInputCurve.Evaluate(data.Breaking.GetValueOrDefault());

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
                    ADSteerInput = data.SteerInput.GetValueOrDefault();
                }
            });
        }

        public override void OnVisualize(Visualizer visualizer)
        {
            //
        }

        public override void OnVisualizeToggle(bool state)
        {
            //
        }
    }
}
