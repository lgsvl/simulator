/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using Simulator.Bridge;
using Simulator.Bridge.Data;
using UnityEngine;

namespace Simulator.Sensors
{
    public class GroundTruthVisualizer : SensorBase
    {
        private List<Detected3DObject> Detected = new List<Detected3DObject>();
        private List<GameObject> Visualized = new List<GameObject>();
        public GameObject boundingBox;

        public override void OnBridgeSetup(IBridge bridge)
        {
            bridge.AddReader<Detected3DObjectArray>(Topic, data =>
            {
                Detected.Clear();
                Detected.AddRange(data.Data);
            });
        }

        private void Update()
        {
            Visualized.ForEach(Destroy);
            Visualized.Clear();

            foreach (var detected in Detected)
            {
                GameObject bbox = Instantiate(boundingBox, detected.Position, detected.Rotation, transform);
                bbox.transform.localScale *= 1.1f;

                Renderer rend = bbox.GetComponent<Renderer>();
                switch (detected.Label)
                {
                    case "NPC":
                        rend.material.SetColor("_UnlitColor", new Color(0, 1, 0, 0.3f));  // Color.green
                        break;
                    case "Pedestrian":
                        rend.material.SetColor("_UnlitColor", new Color(1, 0.92f, 0.016f, 0.3f));  // Color.yellow
                        break;
                    case "bicycle":
                        rend.material.SetColor("_UnlitColor", new Color(0, 1, 1, 0.3f));  // Color.cyan
                        break;
                    default:
                        rend.material.SetColor("_UnlitColor", new Color(1, 0, 1, 0.3f));  // Color.magenta
                        break;
                }

                bbox.SetActive(true);
                Visualized.Add(bbox);
            }
        }
    }
}
