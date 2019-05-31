/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;

namespace Simulator.Utilities
{
    public class AAWireBox : MonoBehaviour
    {
        public struct Box
        {
            public Vector2 Min;
            public Vector2 Max;
            public Vector3 Color;
        }

        const int BufferGranularity = 1024;

        List<Box> Boxes;
        ComputeBuffer Buffer;

        Material Material;
        public float LineWidth = 5f;
        public Camera Camera;

        void Start()
        {
            Boxes = new List<Box>(BufferGranularity);
            Material = new Material(RuntimeSettings.Instance.AAWireBoxShader);
        }

        void OnDestroy()
        {
            Buffer?.Release();
            Destroy(Material);
        }

        public void Draw(Vector2 min, Vector2 max, Color color)
        {
            Boxes.Add(new Box()
            {
                Min = min,
                Max = max,
                Color = (Vector4)color,
            });
        }

        void LateUpdate()
        {
            if (Boxes.Count == 0)
            {
                return;
            }

            if (Buffer == null || Boxes.Count > Buffer.count)
            {
                Buffer?.Release();

                int newCount = ((Boxes.Count + BufferGranularity - 1) / BufferGranularity) * BufferGranularity;
                Buffer = new ComputeBuffer(newCount, UnsafeUtility.SizeOf<Box>());
                Material.SetBuffer("_Boxes", Buffer);
            }

            Buffer.SetData(Boxes, 0, 0, Boxes.Count);
            Material.SetFloat("_LineWidth", LineWidth);

            // TODO: big number for size to include everything in scene
            var bounds = new Bounds(Vector3.zero, new Vector3(10000, 10000, 10000));
            
            Graphics.DrawProcedural(Material, bounds, MeshTopology.Points, Boxes.Count, 1, Camera, null, UnityEngine.Rendering.ShadowCastingMode.Off, true, layer: gameObject.layer);

            Boxes.Clear();
        }
    }
}
