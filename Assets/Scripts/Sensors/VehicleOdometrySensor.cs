/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Simulator.Bridge;
using Simulator.Bridge.Data;
using Simulator.Utilities;
using UnityEngine;
using Simulator.Sensors.UI;
using System.Collections.Generic;

namespace Simulator.Sensors
{
    [SensorType("Vehicle Odometry", new[] { typeof(VehicleOdometryData) })]
    public partial class VehicleOdometrySensor : SensorBase
    {
        [SensorParameter]
        [Range(1f, 100f)]
        public float Frequency = 10.0f;

        uint SendSequence;
        float NextSend;

        IBridge Bridge;
        IWriter<VehicleOdometryData> Writer;

        Rigidbody RigidBody;
        IVehicleDynamics Dynamics;
        VehicleActions Actions;

        VehicleOdometryData msg;
        
        public override SensorDistributionType DistributionType => SensorDistributionType.LowLoad;

        private void Awake()
        {
            RigidBody = GetComponentInParent<Rigidbody>();
            Dynamics = GetComponentInParent<IVehicleDynamics>();
        }

        public override void OnBridgeSetup(IBridge bridge)
        {
            Bridge = bridge;
            Writer = bridge.AddWriter<VehicleOdometryData>(Topic);
        }

        public void Start()
        {
            NextSend = Time.time + 1.0f / Frequency;
        }

        public void Update()
        {
            if (Time.time < NextSend)
            {
                return;
            }
            NextSend = Time.time + 1.0f / Frequency;

            float speed = RigidBody.velocity.magnitude;

            msg = new VehicleOdometryData()
            {
                Time = SimulatorManager.Instance.CurrentTime,
                Speed = speed,
                SteeringAngleFront = Dynamics.WheelAngle,
                SteeringAngleBack = 0f,
            };

            if (Bridge != null && Bridge.Status == Status.Connected)
            {
                Writer.Write(msg, null);
            }

        }

        public override void OnVisualize(Visualizer visualizer)
        {
            Debug.Assert(visualizer != null);

            if (msg == null)
            {
                return;
            }

            var graphData = new Dictionary<string, object>()
            {
                {"Speed", msg.Speed},
                {"Steering Front", msg.SteeringAngleFront},
                {"Steering Back", msg.SteeringAngleBack},
            };
            visualizer.UpdateGraphValues(graphData);
        }

        public override void OnVisualizeToggle(bool state)
        {
            //
        }
    }
}
