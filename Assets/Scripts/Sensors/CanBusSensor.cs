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

namespace Simulator.Sensors
{
    [SensorType("CAN-Bus", new[] { typeof(CanBusData) })]
    public partial class CanBusSensor : SensorBase
    {
        [SensorParameter]
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
            if (MapOrigin == null || Bridge == null || Bridge.Status != Status.Connected)
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
                Sequence = SendSequence,

                Speed = speed,

                Throttle = Dynamics.accellInput > 0 ? Dynamics.accellInput : 0,
                Breaking = Dynamics.accellInput < 0 ? -Dynamics.accellInput : 0,
                Steering = Dynamics.steerInput,

                ParkingBrake = Dynamics.isHandBrake,
                HighBeamSignal = Actions.currentHeadLightState == VehicleActions.HeadLightState.HIGH,
                LowBeamSignal = Actions.currentHeadLightState == VehicleActions.HeadLightState.LOW,
                HazardLights = Actions.isHazard,
                FogLights = Actions.isFog,

                LeftTurnSignal = Actions.isIndicatorLeft,
                RightTurnSignal = Actions.isIndicatorRight,

                Wipers = false,

                InReverse = Dynamics.isReverse,
                Gear = Mathf.RoundToInt(Dynamics.currentGear),

                EngineOn = Dynamics.ignitionStatus == IgnitionStatus.On,
                EngineRPM = Dynamics.currentRPM,

                Latitude = gps.Latitude,
                Longitude = gps.Longitude,
                Altitude = gps.Altitude,

                Orientation = transform.rotation,
                Velocity = RigidBody.velocity,
            };

            Writer.Write(msg);
            SendSequence++;
        }
    }
}
