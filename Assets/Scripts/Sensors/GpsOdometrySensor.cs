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
using UnityEngine;

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

        float NextSend;
        uint SendSequence;

        IBridge Bridge;
        IWriter<GpsOdometryData> Writer;

        Rigidbody RigidBody;
        MapOrigin MapOrigin;

        public override void OnBridgeSetup(IBridge bridge)
        {
            Bridge = bridge;
            Writer = Bridge.AddWriter<GpsOdometryData>(Topic);
        }

        public void Start()
        {
            RigidBody = GetComponentInParent<Rigidbody>();
            MapOrigin = MapOrigin.Find();

            NextSend = Time.time + 1.0f / Frequency;
        }

        void Update()
        {
            if (MapOrigin == null || Bridge == null || Bridge.Status != Status.Connected)
            {
                return;
            }

            if (Time.time < NextSend)
            {
                return;
            }
            NextSend = Time.time + 1.0f / Frequency;

            var location = MapOrigin.GetGpsLocation(transform.position, IgnoreMapOrigin);

            Writer.Write(new GpsOdometryData()
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
            });
        }
    }
}
