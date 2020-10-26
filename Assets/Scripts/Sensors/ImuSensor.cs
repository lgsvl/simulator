/**
 * Copyright (c) 2019-2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Simulator.Bridge;
using Simulator.Bridge.Data;
using Simulator.Utilities;
using Simulator.Sensors.UI;
using System.Collections;
using Simulator.Analysis;

#pragma warning disable CS0649

namespace Simulator.Sensors
{
    [SensorType("IMU", new[] { typeof(ImuData) })]
    class ImuSensor : SensorBase
    {
        [SensorParameter]
        public string CorrectedTopic;

        [SensorParameter]
        public string CorrectedFrame;

        uint Sequence;

        BridgeInstance Bridge;
        Publisher<ImuData> Publish;
        Publisher<CorrectedImuData> CorrectedWriter;

        Queue<Tuple<double, float, Action>> MessageQueue =
            new Queue<Tuple<double, float, Action>>();
        bool Destroyed = false;
        bool IsFirstFixedUpdate = true;
        double LastTimestamp;

        Rigidbody RigidBody;
        Vector3 LastVelocity;

        [AnalysisMeasurement(MeasurementType.Distance)]
        float minX;
        [AnalysisMeasurement(MeasurementType.Distance)]
        float maxX;
        [AnalysisMeasurement(MeasurementType.Angle)]
        float minGyroX;
        [AnalysisMeasurement(MeasurementType.Angle)]
        float maxGyroX;
        [AnalysisMeasurement(MeasurementType.Distance)]
        float minY;
        [AnalysisMeasurement(MeasurementType.Distance)]
        float maxY;
        [AnalysisMeasurement(MeasurementType.Angle)]
        float minGyroY;
        [AnalysisMeasurement(MeasurementType.Angle)]
        float maxGyroY;
        [AnalysisMeasurement(MeasurementType.Distance)]
        float minZ;
        [AnalysisMeasurement(MeasurementType.Distance)]
        float maxZ;
        [AnalysisMeasurement(MeasurementType.Angle)]
        float minGyroZ;
        [AnalysisMeasurement(MeasurementType.Angle)]
        float maxGyroZ;

        ImuData latestData;
        
        public override SensorDistributionType DistributionType => SensorDistributionType.HighLoad;

        public override void OnBridgeSetup(BridgeInstance bridge)
        {
            Bridge = bridge;
            Publish = Bridge.AddPublisher<ImuData>(Topic);
            if (!string.IsNullOrEmpty(CorrectedTopic))
            {
                CorrectedWriter = Bridge.AddPublisher<CorrectedImuData>(CorrectedTopic);
            }
        }

        void Start()
        {
            RigidBody = GetComponentInParent<Rigidbody>();
            Task.Run(Publisher);
        }

        void OnDestroy()
        {
            Destroyed = true;
        }

        void Publisher()
        {
            var nextPublish = Stopwatch.GetTimestamp();

            while (!Destroyed)
            {
                long now = Stopwatch.GetTimestamp();
                if (now < nextPublish)
                {
                    Thread.Sleep(0);
                    continue;
                }

                Tuple<double, float, Action> msg = null;
                lock (MessageQueue)
                {
                    if (MessageQueue.Count > 0)
                    {
                        msg = MessageQueue.Dequeue();
                    }
                }

                if (msg != null)
                {
                    try
                    {
                        msg.Item3();
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogException(ex, this);
                    }
                    nextPublish = now + (long)(Stopwatch.Frequency * msg.Item2);
                    LastTimestamp = msg.Item1;
                }
            }
        }

        void FixedUpdate()
        {
            if (IsFirstFixedUpdate)
            {
                lock (MessageQueue)
                {
                    MessageQueue.Clear();
                }
                IsFirstFixedUpdate = false;
            }

            var time = SimulatorManager.Instance.CurrentTime;
            if (time < LastTimestamp)
            {
                return;
            }

            var position = transform.position;
            position.Set(position.z, -position.x, position.y);
            minX = Mathf.Min(minX, position.x);
            maxX = Mathf.Max(maxX, position.x);
            minY = Mathf.Min(minY, position.y);
            maxY = Mathf.Max(maxY, position.y);
            minZ = Mathf.Min(minZ, position.z);
            maxZ = Mathf.Max(maxZ, position.z);

            var velocity = transform.InverseTransformDirection(RigidBody.velocity);
            velocity.Set(velocity.z, -velocity.x, velocity.y);
            var acceleration = (velocity - LastVelocity) / Time.fixedDeltaTime;
            LastVelocity = velocity;

            var localGravity = transform.InverseTransformDirection(Physics.gravity);
            acceleration -= new Vector3(localGravity.z, -localGravity.x, localGravity.y);

            var angularVelocity = RigidBody.angularVelocity;
            angularVelocity.Set(-angularVelocity.z, angularVelocity.x, -angularVelocity.y); // converting to right handed xyz

            var orientation = transform.rotation;
            orientation.Set(-orientation.z, orientation.x, -orientation.y, orientation.w); // converting to right handed xyz
            minGyroX = Mathf.Min(minGyroX, orientation.x);
            maxGyroX = Mathf.Max(maxGyroX, orientation.x);
            minGyroY = Mathf.Min(minGyroY, orientation.y);
            maxGyroY = Mathf.Max(maxGyroY, orientation.y);
            minGyroZ = Mathf.Min(minGyroZ, orientation.z);
            maxGyroZ = Mathf.Max(maxGyroZ, orientation.z);

            var data = new ImuData()
            {
                Name = Name,
                Frame = Frame,
                Time = time,
                Sequence = Sequence,

                MeasurementSpan = Time.fixedDeltaTime,

                Position = position,
                Orientation = orientation,

                Acceleration = acceleration,
                LinearVelocity = velocity,
                AngularVelocity = angularVelocity,
            };

            latestData = data;

            var correctedData = new CorrectedImuData()
            {
                Name = Name,
                Frame = CorrectedFrame,
                Time = time,
                Sequence = Sequence,

                MeasurementSpan = Time.fixedDeltaTime,

                Position = position,
                Orientation = orientation,

                Acceleration = acceleration,
                LinearVelocity = velocity,
                AngularVelocity = angularVelocity,
            };

            if (Bridge == null || Bridge.Status != Status.Connected)
            {
                return;
            }

            lock (MessageQueue)
            {
                MessageQueue.Enqueue(Tuple.Create(time, Time.fixedDeltaTime, (Action)(() =>
                {
                    if (Bridge != null && Bridge.Status == Status.Connected)
                    {
                        Publish(data);
                        CorrectedWriter?.Invoke(correctedData);
                    }
                })));
            }

            Sequence++;
        }

        void Update()
        {
            IsFirstFixedUpdate = true;
        }

        public override void OnVisualize(Visualizer visualizer)
        {
            UnityEngine.Debug.Assert(visualizer != null);

            if (latestData == null)
            {
                return;
            }

            var graphData = new Dictionary<string, object>()
            {
                {"Measurement Span", latestData.MeasurementSpan},
                {"Position", latestData.Position},
                {"Orientation", latestData.Orientation},
                {"Acceleration", latestData.Acceleration},
                {"Linear Velocity", latestData.LinearVelocity},
                {"Angular Velocity", latestData.AngularVelocity}
            };
            visualizer.UpdateGraphValues(graphData);
        }

        public override void OnVisualizeToggle(bool state)
        {
            //
        }
    }
}
