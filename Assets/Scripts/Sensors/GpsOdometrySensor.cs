/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Simulator.Bridge;
using Simulator.Bridge.Data;
using Simulator.Map;
using Simulator.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Simulator.Sensors.UI;

namespace Simulator.Sensors
{
    [SensorType("GPS Odometry", new[] { typeof(GpsOdometryData) })]
    public class GpsOdometrySensor : SensorBase
    {
        [SensorParameter]
        [Range(1.0f, 100f)]
        public float Frequency = 12.5f;

        [SensorParameter]
        public string ChildFrame;

        [SensorParameter]
        public bool IgnoreMapOrigin = false;

        Queue<Tuple<double, Action>> MessageQueue =
            new Queue<Tuple<double, Action>>();

        bool Destroyed = false;
        bool IsFirstFixedUpdate = true;
        double LastTimestamp;

        float NextSend;
        uint SendSequence;

        IBridge Bridge;
        IWriter<GpsOdometryData> Writer;

        Rigidbody RigidBody;
        VehicleDynamics Dynamics;
        MapOrigin MapOrigin;

        private void Awake()
        {
            RigidBody = GetComponentInParent<Rigidbody>();
            Dynamics = GetComponentInParent<VehicleDynamics>();
            MapOrigin = MapOrigin.Find();
        }

        public void Start()
        {
            Task.Run(Publisher);
        }

        void OnDestroy()
        {
            Destroyed = true;
        }

        public override void OnBridgeSetup(IBridge bridge)
        {
            Bridge = bridge;
            Writer = Bridge.AddWriter<GpsOdometryData>(Topic);
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

                Tuple<double, Action> msg = null;
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
                        msg.Item2();
                    }
                    catch
                    {
                    }
                    nextPublish = now + (long)(Stopwatch.Frequency / Frequency);
                    LastTimestamp = msg.Item1;
                }
            }
        }


        void FixedUpdate()
        {
            if (MapOrigin == null || Bridge == null || Bridge.Status != Status.Connected)
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

            var location = MapOrigin.GetGpsLocation(transform.position, IgnoreMapOrigin);

            var data = new GpsOdometryData()
            {
                Name = Name,
                Frame = Frame,
                Time = SimulatorManager.Instance.CurrentTime,
                Sequence = SendSequence++,

                ChildFrame = ChildFrame,
                IgnoreMapOrigin = IgnoreMapOrigin,
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                Altitude = location.Altitude,
                Northing = location.Northing,
                Easting = location.Easting,
                Orientation = transform.rotation,
                ForwardSpeed = Vector3.Dot(RigidBody.velocity, transform.forward),
                Velocity = RigidBody.velocity,
                AngularVelocity = RigidBody.angularVelocity,
                WheelAngle = (Dynamics.axles[0].left.steerAngle + Dynamics.axles[0].right.steerAngle) * 0.5f,
            };
            
            lock (MessageQueue)
            {
                MessageQueue.Enqueue(Tuple.Create(time, (Action)(() => Writer.Write(data))));
            }
        }

        void Update()
        {
            IsFirstFixedUpdate = true;
        }

        public override void OnVisualize(Visualizer visualizer)
        {
            UnityEngine.Debug.Assert(visualizer != null);

            var location = MapOrigin.GetGpsLocation(transform.position, IgnoreMapOrigin);

            var graphData = new Dictionary<string, object>()
            {
                {"Child Frame", ChildFrame},
                {"Ignore MapOrigin", IgnoreMapOrigin},
                {"Latitude", location.Latitude},
                {"Longitude", location.Longitude},
                {"Altitude", location.Altitude},
                {"Northing", location.Northing},
                {"Easting", location.Easting},
                {"Orientation", transform.rotation},
                {"Forward Speed", Vector3.Dot(RigidBody.velocity, transform.forward)},
                {"Velocity", RigidBody.velocity},
                {"Angular Velocity", RigidBody.angularVelocity}
            };
            visualizer.UpdateGraphValues(graphData);
        }

        public override void OnVisualizeToggle(bool state)
        {
            //
        }
    }
}
