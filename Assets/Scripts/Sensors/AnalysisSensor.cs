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
using System.Linq;

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
              "StuckTravelThreshold": 0.1,
              "StuckTimeThreshold": 10.0,
              "StopLineThreshold": 1.0
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

        private Vector3 StartPosition;

        private MapLane SpeedViolationLane;
        private float SpeedViolationCount = 0f;
        private float SpeedViolationMin = 0f;
        private float SpeedViolationMax = 0f;
        
        public bool CheckingStopLine = false;
        public bool Stopped = false;
        public bool StopLineViolation = false;
        [SensorParameter]
        public float StopLineThreshold = 1f;
        private float SquareStopLineThreshold;
        private List<MapLine> StopLines = new List<MapLine>();
        private Transform StopLineTransform = null;
        
        private void Awake()
        {
            AgentController = GetComponentInParent<AgentController>();
            Dynamics = GetComponentInParent<IVehicleDynamics>();
            Actions = GetComponentInParent<VehicleActions>();
            Lane = GetComponentInParent<VehicleLane>();
            RB = GetComponentInParent<Rigidbody>();
            var mapLines = FindObjectsOfType<MapLine>().ToList();
            foreach (var line in mapLines)
            {
                if (line.lineType == MapData.LineType.STOP)
                {
                    StopLines.Add(line);
                }
            }

            SquareStopLineThreshold = StopLineThreshold * StopLineThreshold;
        }

        private void Start()
        {
            StartPosition = transform.position;
            StuckStartPosition = StartPosition;
        }
        
        private void Update()
        {
            CreateStopTransform();
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
                { "Start Position", StartPosition },
                { "EgoIsStuck", EgoIsStuck },
                { "FellOffAdded", FellOffAdded },
                { "CurrentSpeedLimit", SpeedViolationLane?.speedLimit },
                { "SpeedViolationDuration", TimeSpan.FromSeconds(SpeedViolationCount).ToString() },
                { "SpeedViolationMax", SpeedViolationMax },
                { "StopLineViolation", StopLineViolation },
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
            SensorAnalysisData = new List<AnalysisReportItem>
            {
                new AnalysisReportItem {
                    name = "Distance",
                    type = "distance",
                    value = Distance
                },
                new AnalysisReportItem {
                    name = "SpeedMin",
                    type = "velocity",
                    value = SpeedMin
                },
                new AnalysisReportItem {
                    name = "SpeedMax",
                    type = "velocity",
                    value = SpeedMax
                },
                new AnalysisReportItem {
                    name = "SpeedAvg",
                    type = "velocity",
                    value = SpeedAvg
                },
                new AnalysisReportItem {
                    name = "AccelLongMin",
                    type = "acceleration",
                    value = AccelLongMin
                },
                new AnalysisReportItem {
                    name = "AccelLongMax",
                    type = "acceleration",
                    value = AccelLongMax
                },
                new AnalysisReportItem {
                    name = "AccelLatMin",
                    type = "acceleration",
                    value = AccelLatMin
                },
                new AnalysisReportItem {
                    name = "AccelLatMax",
                    type = "acceleration",
                    value = AccelLatMax
                },
                new AnalysisReportItem {
                    name = "JerkLongMin",
                    type = "jerk",
                    value = JerkLongMin
                },
                new AnalysisReportItem {
                    name = "JerkLongMax",
                    type = "jerk",
                    value = JerkLongMax
                },
                new AnalysisReportItem {
                    name = "JerkLatMin",
                    type = "jerk",
                    value = JerkLatMin
                },
                new AnalysisReportItem {
                    name = "JerkLatMax",
                    type = "jerk",
                    value = JerkLatMax
                },
                new AnalysisReportItem {
                    name = "SteerAngleMax",
                    type = "angle",
                    value = SteerAngleMax
                },
                new AnalysisReportItem {
                    name = "StartPosition",
                    type = "rightHandPos",
                    value = StartPosition
                },
                new AnalysisReportItem {
                    name = "EndPosition",
                    type = "rightHandPos",
                    value = transform.position
                },
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

            PrevSpeed = Speed;

            // steer angle max
            if (SteerAngle > SteerAngleMax)
            {
                SteerAngleMax = SteerAngle;
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

            // stop line violation
            if (StopLineTransform != null && StopLines != null && StopLines.Count > 0)
            {
                MapLine closesStoptLine = StopLines.OrderByDescending(i => SquareDistanceToStopLine(i, StopLineTransform.position)).Last();

                bool shouldStop = false;
                if (closesStoptLine.isStopSign) // For stop sign, we should always stop
                {
                    shouldStop = true;
                }
                else if (closesStoptLine.signals[0].CurrentState == "red") // For traffic light, we should stop if it is red
                {
                    shouldStop = true;
                }

                if (shouldStop && SquareDistanceToStopLine(closesStoptLine, StopLineTransform.position) < SquareStopLineThreshold)
                {
                    StopLineViolation = false;
                    CheckingStopLine = true;
                    // Check if speed is low enough and if the stop line is almost perpendicular to the ego forward direction.
                    if (RB.velocity.magnitude < 0.01f && Mathf.Abs(Vector3.Dot(Vector3.Normalize(closesStoptLine.transform.position - StopLineTransform.position), closesStoptLine.transform.forward)) > 0.7f)
                    {
                        Stopped = true;
                    }
                }
                else if (CheckingStopLine)
                {
                    if (!Stopped)
                    {
                        StopLineViolation = true;
                        StopLineViolationEvent(AgentController.GTID, closesStoptLine.isStopSign);
                    }
                    CheckingStopLine = false;
                    Stopped = false;
                }
            }
        }

        private float SquareDistanceToStopLine(MapLine stopLine, Vector3 point)
        {
            // Assuming stop line is always straigh.
            // So we can use its first and last point to calculate the distance.
            int n = stopLine.mapWorldPositions.Count;
            return Utility.SqrDistanceToSegment(stopLine.mapWorldPositions[0], stopLine.mapWorldPositions[n - 1], StopLineTransform.position);
        }

        private void CreateStopTransform()
        {
            if (StopLineTransform != null)
                return;

            var wheelColliders = Actions.transform.GetComponentsInChildren<WheelCollider>();
            foreach (var col in wheelColliders)
            {
                if (col.name == "FR")
                {
                    StopLineTransform = new GameObject("StopLineTransform").transform;
                    StopLineTransform.transform.SetParent(Actions.transform);
                    StopLineTransform.localPosition = new Vector3(0f, 0f, col.transform.localPosition.z);
                    StopLineTransform.localRotation = Quaternion.identity;
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
        
        private void StopLineViolationEvent(uint id, bool isStopSign)
        {
            Hashtable data = new Hashtable
            {
                { "Id", id },
                { "Type", "StopLineViolation" },
                { "StopType",  isStopSign ? "StopSign" : "RedTrafficLight"},
                { "Time", SimulatorManager.Instance.GetSessionElapsedTimeSpan().ToString() },
                { "Location", transform.position },
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
    }
}
