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
    [SensorType("CAN-Bus", new[] { typeof(CanBusData) })]
    public partial class CanBusSensor : SensorBase
    {
        [SensorParameter]
        [Range(1f, 100f)]
        public float Frequency = 10.0f;

        uint SendSequence;
        float NextSend;

        IBridge Bridge;
        IWriter<CanBusData> Writer;

        Rigidbody RigidBody;
        VehicleDynamics Dynamics;
        VehicleActions Actions;
        MapOrigin MapOrigin;

        public override void OnBridgeSetup(IBridge bridge)
        {
            Bridge = bridge;
            Writer = bridge.AddWriter<CanBusData>(Topic);

            RigidBody = GetComponentInParent<Rigidbody>();
            Actions = GetComponentInParent<VehicleActions>();
            Dynamics = GetComponentInParent<VehicleDynamics>();
            MapOrigin = MapOrigin.Find();
        }

        public void Start()
        {
            NextSend = Time.time + 1.0f / Frequency;
        }

        public void Update()
        {
            if (MapOrigin == null)
            {
                return;
            }

            if (Time.time < NextSend)
            {
                return;
            }
            NextSend = Time.time + 1.0f / Frequency;

            float speed = RigidBody.velocity.magnitude;

            var gps = MapOrigin.GetGpsLocation(transform.position);

            var msg = new CanBusData()
            {
                Name = Name,
                Frame = Frame,
                Time = SimulatorManager.Instance.CurrentTime,
                Sequence = SendSequence++,

                Speed = speed,

                Throttle = Dynamics.AccellInput > 0 ? Dynamics.AccellInput : 0,
                Braking = Dynamics.AccellInput < 0 ? -Dynamics.AccellInput : 0,
                Steering = Dynamics.SteerInput,

                ParkingBrake = Dynamics.HandBrake,
                HighBeamSignal = Actions.CurrentHeadLightState == VehicleActions.HeadLightState.HIGH,
                LowBeamSignal = Actions.CurrentHeadLightState == VehicleActions.HeadLightState.LOW,
                HazardLights = Actions.HazardLights,
                FogLights = Actions.FogLights,

                LeftTurnSignal = Actions.LeftTurnSignal,
                RightTurnSignal = Actions.RightTurnSignal,

                Wipers = false,

                InReverse = Dynamics.Reverse,
                Gear = Mathf.RoundToInt(Dynamics.CurrentGear),

                EngineOn = Dynamics.IgnitionStatus == IgnitionStatus.On,
                EngineRPM = Dynamics.CurrentRPM,

                Latitude = gps.Latitude,
                Longitude = gps.Longitude,
                Altitude = gps.Altitude,

                Orientation = transform.rotation,
                Velocity = RigidBody.velocity,
            };

            if (Bridge != null && Bridge.Status == Status.Connected)
            {
                Writer.Write(msg, null);
            }
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
