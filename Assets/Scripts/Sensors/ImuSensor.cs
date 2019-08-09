/**
 * Copyright (c) 2019 LG Electronics, Inc.
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

        IBridge Bridge;
        IWriter<ImuData> Writer;
        IWriter<CorrectedImuData> CorrectedWriter;

        Queue<Tuple<double, float, Action>> MessageQueue =
            new Queue<Tuple<double, float, Action>>();
        bool Destroyed = false;
        bool IsFirstFixedUpdate = true;
        double LastTimestamp;

        Rigidbody RigidBody;
        Vector3 LastVelocity;

        public override void OnBridgeSetup(IBridge bridge)
        {
            Bridge = bridge;
            Writer = Bridge.AddWriter<ImuData>(Topic);
            if (!string.IsNullOrEmpty(CorrectedTopic))
            {
                CorrectedWriter = Bridge.AddWriter<CorrectedImuData>(CorrectedTopic);
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
                    catch
                    {
                    }
                    nextPublish = now + (long)(Stopwatch.Frequency * msg.Item2);
                    LastTimestamp = msg.Item1;
                }
            }
        }

        void FixedUpdate()
        {
            if (Bridge == null || Bridge.Status != Status.Connected)
            {
                return;
            }

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

            var velocity = transform.InverseTransformDirection(RigidBody.velocity);
            var acceleration = (velocity - LastVelocity) / Time.fixedDeltaTime;
            LastVelocity = velocity;

            acceleration += transform.InverseTransformDirection(Physics.gravity);

            var angularVelocity = RigidBody.angularVelocity;

            var angles = transform.eulerAngles;
            float roll = -angles.z;
            float pitch = -angles.x;
            float yaw = -angles.y;
            var orientation = Quaternion.Euler(roll, pitch, yaw);

            var data = new ImuData()
            {
                Name = Name,
                Frame = Frame,
                Time = time,
                Sequence = Sequence,

                MeasurementSpan = Time.fixedDeltaTime,

                Position = transform.position,
                Orientation = orientation,

                Acceleration = acceleration,
                LinearVelocity = velocity,
                AngularVelocity = angularVelocity,
            };

            var correctedData = new CorrectedImuData()
            {
                Name = Name,
                Frame = CorrectedFrame,
                Time = time,
                Sequence = Sequence,

                MeasurementSpan = Time.fixedDeltaTime,

                Position = transform.position,
                Orientation = orientation,

                Acceleration = acceleration,
                LinearVelocity = velocity,
                AngularVelocity = angularVelocity,
            };

            lock (MessageQueue)
            {
                MessageQueue.Enqueue(Tuple.Create(time, Time.fixedDeltaTime, (Action)(() => {
                    Writer.Write(data);
                    if (CorrectedWriter != null)
                    {
                        CorrectedWriter.Write(correctedData);
                    }
                })));
            }

            Sequence++;
        }

        void Update()
        {
            IsFirstFixedUpdate = true;
        }
    }
}
