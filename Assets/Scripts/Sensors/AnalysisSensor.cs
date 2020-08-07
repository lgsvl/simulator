/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Simulator.Bridge;
using Simulator.Sensors.UI;
using Simulator.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Simulator.Analysis;
using Simulator.Map;
using System;

namespace Simulator.Sensors
{
    [SensorType("Analysis", new System.Type[] { })]
    public class AnalysisSensor : SensorBase
    {
        /*
         * {
            "type": "Analysis",
            "parent": null,
            "name": "Analysis Sensor",
            "params":
            {
              "SuddenBrakeMax": 10.0,
              "SuddenSteerMax": 10.0,
              "StuckTravelThreshold": 0.1,
              "StuckTimeThreshold": 10.0,
              "MinFPS": 10.0,
              "MinFPSTime": 5.0
            }
          }
         */
        private AgentController AgentController;
        private Rigidbody RB;
        private IVehicleDynamics Dynamics;
        private VehicleActions Actions;
        private VehicleLane Lane;

        private float Distance = 0f;
        private Vector3 PrevPos = new Vector3(0f, 0f, 0f);

        [SensorParameter]
        public float SuddenBrakeMax = 10f;
        private float SpeedMin = float.MaxValue;
        private float SpeedMax = 0f;
        private float SpeedAvg = 0f;
        private float SpeedTotal = 0f;
        private float PrevSpeed = 0f;
        private int SpeedCount = 0;
        private float Speed = 0f;

        private Vector3 Acceleration = new Vector3(0f, 0f, 0f);
        private Vector3 LastLocalAcceleration = new Vector3(0f, 0f, 0f);
        private Vector3 Velocity = new Vector3(0f, 0f, 0f);
        private Vector3 LastLocalVelocity = new Vector3(0f, 0f, 0f);
        private Quaternion Rotation = Quaternion.identity;
        private float AngularVelocity = 0f;
        private float AngularAcceleration = 0f;
        private float Slip = 0f;
        private float AccelLongMin = float.MaxValue;
        private float AccelLongMax = 0f;
        private float AccelLatMin = float.MaxValue;
        private float AccelLatMax = 0f;

        private Vector3 Jerk = new Vector3(0f, 0f, 0f);
        private float JerkLongMin = float.MaxValue;
        private float JerkLongMax = 0f;
        private float JerkLatMin = float.MaxValue;
        private float JerkLatMax = 0f;

        [SensorParameter]
        public float SuddenSteerMax = 10f;
        private float SteerAngleMax = 0f;
        private float SteerAngle = 0f;
        private float PrevSteerAngle = 0f;

        private bool FellOffAdded = false;

        [SensorParameter]
        public float StuckTravelThreshold = 0.1f;
        [SensorParameter]
        public float StuckTimeThreshold = 10.0f;
        private float ThrottleCommand = 0f;
        private float ThrottleCuttoff = 0.05f;
        private Vector3 StuckStartPosition;
        private float StuckTime;
        private bool EgoIsStuck = false;

        [SensorParameter]
        public float MinFPS = 10f;
        [SensorParameter]
        public float MinFPSTime = 5f;
        private float LowFPSCalculatedTime = 0f;
        private float DeltaTime = 0.0f;
        private float MS = 0f;
        private float FPS = 0f;
        private float AveFPS = 0f;
        private bool LowFPS = false;

        private Vector3 StartPosition;

        private MapLane SpeedViolationLane;
        private float SpeedViolationCount = 0f;
        private float SpeedViolationMin = 0f;
        private float SpeedViolationMax = 0f;

        private void Awake()
        {
            AgentController = GetComponentInParent<AgentController>();
            Dynamics = GetComponentInParent<IVehicleDynamics>();
            Actions = GetComponentInParent<VehicleActions>();
            Lane = GetComponentInParent<VehicleLane>();
            RB = GetComponentInParent<Rigidbody>();
        }

        private void Start()
        {
            LowFPSCalculatedTime = 0f;
            StartPosition = transform.position;
            StuckStartPosition = StartPosition;
        }

        private void Update()
        {
            CalculateFPS();
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
                { "Distance", Distance },
                { "Speed", Speed },
                { "SpeedMin", SpeedMin },
                { "SpeedMax", SpeedMax },
                { "SpeedAvg", SpeedAvg },
                { "AccelLongMin", AccelLongMin },
                { "AccelLongMax", AccelLongMax },
                { "AccelLatMin", AccelLatMin },
                { "AccelLatMax", AccelLatMax },
                { "JerkLongMin", JerkLongMin },
                { "JerkLongMax", JerkLongMax },
                { "JerkLatMin", JerkLatMin },
                { "JerkLatMax", JerkLatMax },
                { "SteerAngleMax", SteerAngleMax },
                { "MS", MS },
                { "FPS", FPS },
                { "Average FPS", AveFPS },
                { "Start Position", StartPosition },
                { "EgoIsStuck", EgoIsStuck },
                { "FellOffAdded", FellOffAdded },
                { "LowFPS", LowFPS },
                { "CurrentSpeedLimit", SpeedViolationLane?.speedLimit },
                { "SpeedViolationDuration", TimeSpan.FromSeconds(SpeedViolationCount).ToString() },
                { "SpeedViolationMax", SpeedViolationMax },
            };
            visualizer.UpdateGraphValues(graphData);
        }

        public override void OnVisualizeToggle(bool state)
        {
            //
        }

        public override void OnAnalyze()
        {
            CalculateAnalysisValues();
        }

        public override void SetAnalysisData()
        {
            SensorAnalysisData = new Hashtable
            {
                { "Distance", Distance },
                { "SpeedMin", SpeedMin },
                { "SpeedMax", SpeedMax },
                { "SpeedAvg", SpeedAvg },
                { "AccelLongMin", AccelLongMin },
                { "AccelLongMax", AccelLongMax },
                { "AccelLatMin", AccelLatMin },
                { "AccelLatMax", AccelLatMax },
                { "JerkLongMin", JerkLongMin },
                { "JerkLongMax", JerkLongMax },
                { "JerkLatMin", JerkLatMin },
                { "JerkLatMax", JerkLatMax },
                { "SteerAngleMax", SteerAngleMax },
                { "Average FPS", AveFPS },
                { "Start Position", StartPosition },
                { "End Position", transform.position },
            };
        }

        private void CalculateAnalysisValues()
        {
            Distance += Vector3.Distance(transform.position, PrevPos) / 1000;
            PrevPos = transform.position;

            Vector3 posDelta = RB.velocity;
            Vector3 velocityDelta = (posDelta - Velocity) / Time.fixedDeltaTime;
            Vector3 accelDelta = (velocityDelta - Acceleration) / Time.fixedDeltaTime;
            Speed = RB.velocity.magnitude;
            Jerk = accelDelta;
            Acceleration = velocityDelta;
            Velocity = posDelta;

            float angleDelta = Quaternion.Angle(Rotation, transform.rotation) / Time.fixedDeltaTime;
            float angularVelocityDelta = (angleDelta - AngularVelocity) / Time.fixedDeltaTime;
            AngularAcceleration = angularVelocityDelta;
            AngularVelocity = angleDelta;
            Rotation = transform.rotation;
            Slip = Vector3.Angle(RB.velocity, transform.forward);
            SteerAngle = Dynamics.WheelAngle;

            // Compute local acceleration and jerk
            var localVelocity = transform.InverseTransformDirection(RB.velocity);
            localVelocity.Set(localVelocity.z, -localVelocity.x, localVelocity.y);
            var localAcceleration = (localVelocity - LastLocalVelocity) / Time.fixedDeltaTime;
            LastLocalVelocity = localVelocity;
            var localGravity = transform.InverseTransformDirection(Physics.gravity);
            localAcceleration -= new Vector3(localGravity.z, -localGravity.x, localGravity.y);

            var localJerk = (localAcceleration - LastLocalAcceleration) / Time.fixedDeltaTime;
            LastLocalAcceleration = localAcceleration;

            float AccelLong = localAcceleration.x;
            float AccelLat = localAcceleration.y;
            UpdateMinMax(AccelLong, ref AccelLongMin, ref AccelLongMax);
            UpdateMinMax(AccelLat, ref AccelLatMin, ref AccelLatMax);

            float JerkLong = localJerk.x;
            float JerkLat = localJerk.y;
            UpdateMinMax(JerkLong, ref JerkLongMin, ref JerkLongMax);
            UpdateMinMax(JerkLat, ref JerkLatMin, ref JerkLatMax);

            // min max avg speed
            SpeedTotal += Speed;
            SpeedCount++;
            SpeedAvg = SpeedTotal / SpeedCount;
            UpdateMinMax(Speed, ref SpeedMin, ref SpeedMax);

            RaycastHit hit = new RaycastHit();
            int mapLayerMask = LayerMask.GetMask("Default");
            var origin = transform.position + Vector3.up * 5f;
            if (!FellOffAdded && !Physics.Raycast(origin, Vector3.down, out hit, 10f, mapLayerMask) && RB.velocity.y < -1.0)
            {
                FellOffEvent(AgentController.GTID);
                FellOffAdded = true;
            }

            ThrottleCommand = Dynamics.AccellInput;
            if (!EgoIsStuck && ThrottleCommand > ThrottleCuttoff && Vector3.Distance(transform.position, StuckStartPosition) < StuckTravelThreshold)
            {
                StuckTime += Time.fixedDeltaTime;
                if (StuckTime > StuckTimeThreshold)
                {
                    StuckEvent(AgentController.GTID);
                    EgoIsStuck = true;
                }
            }
            else
            {
                StuckStartPosition = transform.position;
                StuckTime = 0f;
            }

            if (Mathf.Abs(PrevSpeed - Speed) > SuddenBrakeMax)
            {
                SuddenBrakeEvent(AgentController.GTID);
            }
            PrevSpeed = Speed;

            // steer angle max
            if (SteerAngle > SteerAngleMax)
            {
                SteerAngleMax = SteerAngle;
            }
            if (Mathf.Abs(PrevSteerAngle - SteerAngle) > SuddenSteerMax)
            {
                SuddenSteerEvent(AgentController.GTID);
            }
            PrevSteerAngle = SteerAngle;

            // traffic speed
            if (Speed > Lane?.CurrentMapLane?.speedLimit)
            {
                SpeedViolationLane = Lane.CurrentMapLane;
                SpeedViolationCount += Time.fixedDeltaTime;
                UpdateMinMax(Speed, ref SpeedViolationMin, ref SpeedViolationMax);
            }
            else
            {
                if (SpeedViolationCount > 0 && SpeedViolationLane != null)
                {
                    SpeedViolationEvent(AgentController.GTID, SpeedViolationLane);
                }
                SpeedViolationCount = 0f;
                SpeedViolationMax = 0f;
                SpeedViolationLane = null;
            }
        }

        private void CalculateFPS()
        {
            if (LowFPS)
                return;

            DeltaTime += (Time.unscaledDeltaTime - DeltaTime) * 0.1f;
            MS = DeltaTime * 1000.0f;
            FPS = 1.0f / DeltaTime;
            AveFPS = Time.frameCount / Time.time;
            if (FPS < MinFPS)
            {
                LowFPSCalculatedTime += Time.deltaTime;
                if (LowFPSCalculatedTime >= MinFPSTime)
                {
                    LowFPSEvent(AgentController.GTID);
                    LowFPSCalculatedTime = 0f;
                    LowFPS = true;
                }
            }
        }

        private void UpdateMinMax(float value, ref float min, ref float max)
        {
            if (value < min)
            {
                min = value;
            }

            if (value > max)
            {
                max = value;
            }
        }

        private void StuckEvent(uint id)
        {
            Hashtable data = new Hashtable
            {
                { "Id", id },
                { "Type", "Stuck" },
                { "Time", SimulatorManager.Instance.GetSessionElapsedTimeSpan().ToString() },
                { "Status", AnalysisManager.AnalysisStatusType.Failed },
            };
            SimulatorManager.Instance.AnalysisManager.AddEvent(data);
        }

        private void FellOffEvent(uint id)
        {
            Hashtable data = new Hashtable
            {
                { "Id", id },
                { "Type", "FallOff" },
                { "Time", SimulatorManager.Instance.GetSessionElapsedTimeSpan().ToString() },
                { "Status", AnalysisManager.AnalysisStatusType.Failed },
            };
            SimulatorManager.Instance.AnalysisManager.AddEvent(data);
        }

        private void SuddenBrakeEvent(uint id)
        {
            Hashtable data = new Hashtable
            {
                { "Id", id },
                { "Type", "SuddenBrake" },
                { "Time", SimulatorManager.Instance.GetSessionElapsedTimeSpan().ToString() },
                { "Status", AnalysisManager.AnalysisStatusType.Failed },
            };
            SimulatorManager.Instance.AnalysisManager.AddEvent(data);
        }

        private void SuddenSteerEvent(uint id)
        {
            Hashtable data = new Hashtable
            {
                { "Id", id },
                { "Type", "SuddenSteer" },
                { "Time", SimulatorManager.Instance.GetSessionElapsedTimeSpan().ToString() },
                { "Status", AnalysisManager.AnalysisStatusType.Failed },
            };
            SimulatorManager.Instance.AnalysisManager.AddEvent(data);
        }

        private void TrafficViolationEvent(uint id)
        {
            Hashtable data = new Hashtable
            {
                { "Id", id },
                { "Type", "TrafficViolation" },
                { "Time", SimulatorManager.Instance.GetSessionElapsedTimeSpan().ToString() },
                { "Status", AnalysisManager.AnalysisStatusType.Failed },
            };
            SimulatorManager.Instance.AnalysisManager.AddEvent(data);
        }

        private void StopLineViolationEvent(uint id)
        {
            Hashtable data = new Hashtable
            {
                { "Id", id },
                { "Type", "StopLineViolation" },
                { "Time", SimulatorManager.Instance.GetSessionElapsedTimeSpan().ToString() },
                { "Status", AnalysisManager.AnalysisStatusType.Failed },
            };
            SimulatorManager.Instance.AnalysisManager.AddEvent(data);
        }

        private void SpeedViolationEvent(uint id, MapLane laneData)
        {
            Hashtable data = new Hashtable
            {
                { "Id", id },
                { "Type", "SpeedViolation" },
                { "Time", SimulatorManager.Instance.GetSessionElapsedTimeSpan().ToString() },
                { "Location", transform.position },
                { "SpeedLimit", laneData.speedLimit },
                { "MaxSpeed", SpeedViolationMax },
                { "Duration", TimeSpan.FromSeconds(SpeedViolationCount).ToString() },
                { "Status", AnalysisManager.AnalysisStatusType.Failed },
            };
            SimulatorManager.Instance.AnalysisManager.AddEvent(data);
        }

        private void LaneViolationEvent(uint id)
        {
            Hashtable data = new Hashtable
            {
                { "Id", id },
                { "Type", "LaneViolation" },
                { "Time", SimulatorManager.Instance.GetSessionElapsedTimeSpan().ToString() },
                { "Status", AnalysisManager.AnalysisStatusType.Failed },
            };
            SimulatorManager.Instance.AnalysisManager.AddEvent(data);
        }

        private void LowFPSEvent(uint id)
        {
            Hashtable data = new Hashtable
            {
                { "Id", id },
                { "Type", "LowFPS" },
                { "Time", SimulatorManager.Instance.GetSessionElapsedTimeSpan().ToString() },
                { "MS", MS },
                { "FPS", FPS },
                { "Average FPS", AveFPS },
                { "Status", AnalysisManager.AnalysisStatusType.Failed },
            };
            SimulatorManager.Instance.AnalysisManager.AddEvent(data);
        }
    }
}
