/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using Simulator.Bridge;
using Simulator.Utilities;
using System.Collections.Generic;
using Simulator.Sensors.UI;

namespace Simulator.Sensors
{
    public class CriteriaState
    {
        public float max_velocity;
        public float min_velocity;
        public float throttle;
        public float brakes;
        public float steering;
        public string gear;
        public float duration;
    };

    [SensorType("Control Calibration", new System.Type[]{})]
    public class ControlCalibrationSensor : SensorBase, IVehicleInputs
    {
        public enum Stage
        {
            Init,
            Apply,
            DeInit,
            End
        }

        public enum VelocityState
        {
            Increasing,
            Decreasing,
            Stay
        }

        public enum WhatToTest
        {
            ForwardThrottle,
            ForwardBrake,
            ReverseThrottle,
            ReverseBrake,
            None
        }

        [SensorParameter]
        public List<CriteriaState> states;
        private SimulatorControls controls;
        private IVehicleDynamics dynamics;
        private VehicleController controller;
        private VehicleActions actions;

        public Stage stage = Stage.Init;
        private int? seq = 0;
        private int? idxApply = 0;
        private float StartTime = 0f;
        private float ElapsedTime = 0f;
        private bool firstRun = true;

        CriteriaState state = null;
        float duration = 0f;
        float upperBound = 0f;
        float lowerBound = 0f;
        float maxVelocity = 0f;
        float minVelocity = 0f;
        private VelocityState velocityState = VelocityState.Increasing;
        private WhatToTest whatToTest = WhatToTest.None;
        public float SteerInput { get; private set; } = 0f;
        public float AccelInput { get; private set; } = 0f;
        public float BrakeInput { get; private set; } = 0f;
        private Vector2 keyboardInput = Vector2.zero;

        AgentController AgentController;
        
        public override SensorDistributionType DistributionType => SensorDistributionType.LowLoad;

        private void Start()
        {
            AgentController = GetComponentInParent<AgentController>();
            dynamics = GetComponentInParent<IVehicleDynamics>();
        }

        private void Update()
        {
            var currentSpeed = dynamics.RB.velocity.magnitude;

            if (seq == null)
            {
                stage = Stage.End;
            }

            if (stage == Stage.End)
            {
                return;
            }

            if (firstRun && seq != null)
            {
                state = states[seq.Value];
                duration = states[seq.Value].duration;

                state.throttle = state.throttle * 0.01f;
                state.brakes = state.brakes * 0.01f;
                state.steering = state.steering * 0.01f;

                upperBound = states[seq.Value].max_velocity * 0.9f;
                lowerBound = states[seq.Value].min_velocity * 1.1f;
                maxVelocity = states[seq.Value].max_velocity;
                minVelocity = states[seq.Value].min_velocity;
                AccelInput = 0f;
                SteerInput = 0f;

                if (states[seq.Value].gear == "forward")
                {
                    whatToTest = (states[seq.Value].throttle > 0) ? WhatToTest.ForwardThrottle
                        : (states[seq.Value].brakes > 0) ? WhatToTest.ForwardBrake : WhatToTest.None;

                }
                else if (states[seq.Value].gear == "reverse")
                {
                    whatToTest = (states[seq.Value].throttle > 0) ? WhatToTest.ReverseThrottle
                        : (states[seq.Value].brakes > 0) ? WhatToTest.ReverseBrake : WhatToTest.None;
                }

                firstRun = false;
            }

            if (stage == Stage.Init)
            {
                if (whatToTest == WhatToTest.ForwardThrottle || whatToTest == WhatToTest.ForwardBrake)
                {
                    if (whatToTest == WhatToTest.ForwardThrottle && velocityState == VelocityState.Decreasing)
                    {
                        AccelInput = -1.0f;
                        if (currentSpeed < lowerBound)
                        {
                            stage = Stage.Apply;
                            StartTime = Time.time;
                            velocityState = VelocityState.Stay;
                            AccelInput = state.throttle - state.brakes;
                            idxApply = 0;
                        }
                    }
                    else if (whatToTest == WhatToTest.ForwardThrottle && velocityState == VelocityState.Increasing)
                    {
                        AccelInput = 1.0f;
                        if (currentSpeed > lowerBound && currentSpeed < upperBound)
                        {
                            stage = Stage.Apply;
                            StartTime = Time.time;
                            velocityState = VelocityState.Stay;
                            AccelInput = state.throttle - state.brakes;
                            idxApply = 0;
                        }
                    }

                    if (whatToTest == WhatToTest.ForwardBrake && velocityState == VelocityState.Decreasing)
                    {
                        AccelInput = -1.0f;
                        if (currentSpeed < upperBound && currentSpeed > lowerBound)
                        {
                            stage = Stage.Apply;
                            StartTime = Time.time;
                            velocityState = VelocityState.Stay;
                            AccelInput = state.throttle - state.brakes;
                            idxApply = 0;
                        }
                    }
                    else if (whatToTest == WhatToTest.ForwardBrake && velocityState == VelocityState.Increasing)
                    {
                        AccelInput = 1.0f;
                        if (currentSpeed > upperBound)
                        {
                            stage = Stage.Apply;
                            StartTime = Time.time;
                            velocityState = VelocityState.Stay;
                            AccelInput = state.throttle - state.brakes;
                            idxApply = 0;
                        }
                    }
                }
                else if (whatToTest == WhatToTest.ReverseThrottle || whatToTest == WhatToTest.ReverseBrake)
                {
                    if (dynamics.Reverse == true)
                    {
                        if (whatToTest == WhatToTest.ReverseThrottle || whatToTest == WhatToTest.ReverseBrake)
                        {
                            if (whatToTest == WhatToTest.ReverseThrottle && velocityState == VelocityState.Decreasing)
                            {
                                AccelInput = -1.0f;
                                if (currentSpeed < lowerBound)
                                {
                                    stage = Stage.Apply;
                                    StartTime = Time.time;
                                    velocityState = VelocityState.Stay;
                                    AccelInput = state.throttle - state.brakes;
                                    idxApply = 0;
                                }
                            }
                            else if (whatToTest == WhatToTest.ReverseThrottle && velocityState == VelocityState.Increasing)
                            {
                                AccelInput = 1.0f;
                                if (currentSpeed > lowerBound && currentSpeed < upperBound)
                                {
                                    stage = Stage.Apply;
                                    StartTime = Time.time;
                                    velocityState = VelocityState.Stay;
                                    AccelInput = state.throttle - state.brakes;
                                    idxApply = 0;
                                }
                            }

                            if (whatToTest == WhatToTest.ReverseBrake && velocityState == VelocityState.Decreasing)
                            {
                                AccelInput = -1.0f;
                                if (currentSpeed < upperBound && currentSpeed > lowerBound)
                                {
                                    stage = Stage.Apply;
                                    StartTime = Time.time;
                                    velocityState = VelocityState.Stay;
                                    AccelInput = state.throttle - state.brakes;
                                    idxApply = 0;
                                }
                            }
                            else if (whatToTest == WhatToTest.ReverseBrake && velocityState == VelocityState.Increasing)
                            {
                                AccelInput = 1.0f;
                                if (currentSpeed > upperBound)
                                {
                                    stage = Stage.Apply;
                                    StartTime = Time.time;
                                    velocityState = VelocityState.Stay;
                                    AccelInput = state.throttle - state.brakes;
                                    idxApply = 0;
                                }
                            }
                        }
                    }
                    else
                    {
                        dynamics.ShiftReverseAutoGearBox();
                    }
                }
            }
            else if (stage == Stage.Apply)
            {
                idxApply++;
                ElapsedTime = Time.time - StartTime;
                AccelInput = state.throttle - state.brakes;
                SteerInput = -state.steering;

                if (ElapsedTime > duration)
                {
                    stage = Stage.DeInit;
                    ElapsedTime = 0f;
                }

                else if (currentSpeed > maxVelocity || currentSpeed < minVelocity)
                {
                    stage = Stage.Init;
                    duration = duration - ElapsedTime;
                    ElapsedTime = 0f;

                    if (currentSpeed > maxVelocity)
                    {
                        velocityState= VelocityState.Decreasing;
                        AccelInput = 0f;
                        SteerInput = 0f;
                    }
                    else if (currentSpeed < minVelocity)
                    {
                        velocityState = VelocityState.Increasing;
                        AccelInput = 0f;
                        SteerInput = 0f;
                    }
                }
            }

            else if (stage == Stage.DeInit)
            {
                AccelInput = -0.8f;
                SteerInput = 0f;

                if (currentSpeed < 0.5f)
                {
                    stage = Stage.Init;
                    velocityState = VelocityState.Increasing;
                    seq = (seq < states.Count - 1) ? seq+1 : null;
                    firstRun = true;
                }
            }

            //// Every 0.1 sec, it prints out debug msg.
            //if ((int)((Time.time - StartTime) * 10.0f) % 10 == 0)
            //{
            //    Debug.Log($"seq: {seq.Value}/{states.Count}, idxApply: {idxApply.Value}, stage: {stage.ToString()}, ElapsedTime: " +
            //            $"{ElapsedTime}, Duration: {duration}, curr_v: " +
            //            $"{currentSpeed} [max_v: {state.max_velocity}," +
            //            $" min_v: {state.min_velocity}, upper_v: {upperBound}, lower_v: {lowerBound}] AccelInput: {AccelInput} " +
            //            $"[throttle: {state.throttle}, brakes: {state.brakes}], " +
            //            $"SteerInput: {SteerInput} steer: [{state.steering}], gear: " +
            //            $"{dynamics.CurrentGear} [{state.gear}], whatToTest: {whatToTest}, " +
            //            $"velocityState: {velocityState}");
            //}
        }

        public override void OnBridgeSetup(IBridge bridge)
        {
            // TODO new base class?
        }

        //private float mphToMps(float mph)
        //{
        //    return (float)(mph * 0.44704);
        //}

        public override void OnVisualize(Visualizer visualizer)
        {
            Debug.Assert(visualizer != null);

            if (state == null)
            {
                return;
            }

            var graphData = new Dictionary<string, object>()
            {
                {"Seq", seq.Value/states.Count},
                {"Idx Apply", idxApply.Value},
                {"Stage", stage.ToString()},
                {"Elapsed Time", ElapsedTime},
                {"Duration", duration},
                {"Current Velocity", dynamics.RB.velocity.magnitude},
                {"Max Velocity", state.max_velocity},
                {"Min Velocity", state.min_velocity},
                {"Upper Velocity", upperBound},
                {"Lower Velocity", lowerBound},
                {"Accel Input", AccelInput},
                {"Throttle", state.throttle},
                {"Brakes", state.brakes},
                {"Steer Input", SteerInput},
                {"Steer", state.steering},
                {"Gear", dynamics.CurrentGear},
                {"State Gear", state.gear},
                {"What To Test", whatToTest},
                {"State Velocity", velocityState}
            };
            visualizer.UpdateGraphValues(graphData);
        }

        public override void OnVisualizeToggle(bool state)
        {
            //
        }
    }
}
