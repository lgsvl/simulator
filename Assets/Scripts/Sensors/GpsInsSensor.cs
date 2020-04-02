/**
 * Copyright (c) 2018 LG Electronics, Inc.
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
    [SensorType("GPS-INS Status", new[] { typeof(GpsInsData) })]
    public class GpsInsSensor : SensorBase
    {
        [SensorParameter]
        [Range(1.0f, 100f)]
        public float Frequency = 12.5f;

        double NextSend;
        uint SendSequence;

        IBridge Bridge;
        IWriter<GpsInsData> Writer;
        
        public override SensorDistributionType DistributionType => SensorDistributionType.LowLoad;

        public override void OnBridgeSetup(IBridge bridge)
        {
            Bridge = bridge;
            Writer = Bridge.AddWriter<GpsInsData>(Topic);
        }

        public void Start()
        {
            NextSend = SimulatorManager.Instance.CurrentTime + 1.0f / Frequency;
        }

        void Update()
        {
            if (Bridge == null || Bridge.Status != Status.Connected)
            {
                return;
            }

            if (SimulatorManager.Instance.CurrentTime < NextSend)
            {
                return;
            }
            NextSend = SimulatorManager.Instance.CurrentTime + 1.0f / Frequency;
            
            Writer.Write(new GpsInsData()
            {
                Name = Name,
                Frame = Frame,
                Time = SimulatorManager.Instance.CurrentTime,
                Sequence = SendSequence++,

                Status = 3,
                PositionType = 56,
            });
        }

        public override void OnVisualize(Visualizer visualizer)
        {
            Debug.Assert(visualizer != null);

            var graphData = new Dictionary<string, object>()
            {
                {"Status", 3},
                {"Position Type", 56}
            };
            visualizer.UpdateGraphValues(graphData);
        }

        public override void OnVisualizeToggle(bool state)
        {
            //
        }
    }
}
