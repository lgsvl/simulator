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
    [SensorType("2D Ground Truth Visualizer", new[] { typeof(Detected2DObjectArray) })]
    public class GroundTruth2DVisualizer : SensorBase
    {
        [SensorParameter]
        [Range(1, 1920)]
        public int Width = 1920;

        [SensorParameter]
        [Range(1, 1080)]
        public int Height = 1080;

        [SensorParameter]
        [Range(1.0f, 90.0f)]
        public float FieldOfView = 60.0f;

        [SensorParameter]
        [Range(0.01f, 1000.0f)]
        public float MinDistance = 0.1f;

        [SensorParameter]
        [Range(0.01f, 2000.0f)]
        public float MaxDistance = 1000.0f;

        public Camera Camera;

        RenderTexture activeRT;

        Detected2DObject[] Detected;

        AAWireBox SolidAABox;

        void Start()
        {
            activeRT = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
            {
                dimension = UnityEngine.Rendering.TextureDimension.Tex2D,
                antiAliasing = 1,
                useMipMap = false,
                useDynamicScale = false,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            activeRT.Create();

            Camera = GetComponentInChildren<Camera>();
            Camera.targetTexture = activeRT;
            Camera.fieldOfView = FieldOfView;
            Camera.nearClipPlane = MinDistance;
            Camera.farClipPlane = MaxDistance;

            SolidAABox = gameObject.AddComponent<AAWireBox>();
            SolidAABox.Camera = Camera;
        }

        public override void OnBridgeSetup(IBridge bridge)
        {
            bridge.AddReader<Detected2DObjectArray>(Topic, data => Detected = data.Data);
        }

        void OnDestroy()
        {
            activeRT.Release();
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

                var transform = Matrix4x4.TRS(detected.Position, Quaternion.identity, Vector3.one);
                SolidAABox.Draw(detected.Position - detected.Scale / 2, detected.Position + detected.Scale/2, color);
            }
        }
    }
}
