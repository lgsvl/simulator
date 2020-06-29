/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using Simulator.Bridge;
using Simulator.Bridge.Data;
using Simulator.Utilities;
using Simulator.Sensors.UI;
using System;
using Unity.Mathematics;

namespace Simulator.Sensors
{
    [SensorType("3D Ground Truth Visualizer", new[] { typeof(Detected3DObjectArray) })]
    public class GroundTruth3DVisualizer : SensorBase
    {
        Detected3DObject[] Detected = Array.Empty<Detected3DObject>();

        WireframeBoxes WireframeBoxes;
        
        public override SensorDistributionType DistributionType => SensorDistributionType.HighLoad;

        void Start()
        {
            WireframeBoxes = SimulatorManager.Instance.WireframeBoxes;
        }

        public override void OnBridgeSetup(BridgeInstance bridge)
        {
            bridge.AddSubscriber<Detected3DObjectArray>(Topic, data => Detected = data.Data);
        }

        public override void OnVisualize(Visualizer visualizer)
        {
            foreach (var detected in Detected)
            {
                Color color;
                switch (detected.Label)
                {
                    case "Car":
                        color = Color.green;
                        break;
                    case "Pedestrian":
                        color = Color.yellow;
                        break;
                    case "bicycle":
                        color = Color.cyan;
                        break;
                    default:
                        color = Color.magenta;
                        break;
                }

                // TODO: inverse transfrom for these?
                // relPos.Set(-relPos.y, relPos.z, relPos.x);
                // relRot.Set(-relRot.y, relRot.z, relRot.x, relRot.w);

                var transform = Matrix4x4.TRS((float3)detected.Position, detected.Rotation, Vector3.one);
                WireframeBoxes.Draw(transform, Vector3.zero, detected.Scale, color);
            }
        }

        public override void OnVisualizeToggle(bool state)
        {
            //
        }
    }
}
