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

namespace Simulator.Sensors
{
    public class GroundTruthVisualizer : SensorBase
    {
        Detected3DObject[] Detected;

        WireframeBoxes WireframeBoxes;

        void Start()
        {
            WireframeBoxes = SimulatorManager.Instance.WireframeBoxes;
        }

        public override void OnBridgeSetup(IBridge bridge)
        {
            bridge.AddReader<Detected3DObjectArray>(Topic, data => Detected = data.Data);
        }

        void Update()
        {
            if (Detected == null)
            {
                return;
            }

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

                var transform = Matrix4x4.TRS(detected.Position, detected.Rotation, Vector3.one);
                WireframeBoxes.Draw(transform, Vector3.zero, detected.Scale, color);
            }
        }
    }
}
