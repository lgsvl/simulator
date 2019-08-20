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
    public class WireframeBoxes : MonoBehaviour
    {
        public struct Box
        {
            public Matrix4x4 Transform;
            public Vector3 Center;
            public Vector3 Size;
            public Vector3 Color;
        }

        const int BufferGranularity = 1024;

        List<Box> Boxes;
        ComputeBuffer Buffer;

        Material Material;

        [Range(1, 8)]
        public float LineWidth = 2;

        [Range(1.0f, 1.5f)]
        public float Padding = 1.1f;

        void Start()
        {
            Boxes = new List<Box>(BufferGranularity);
            Material = new Material(RuntimeSettings.Instance.WireframeBoxShader);
        }

        void OnDestroy()
        {
            Buffer?.Release();
            Destroy(Material);
        }

        public void Draw(Matrix4x4 transform, Vector3 center, Vector3 size, Color color)
        {
            Boxes.Add(new Box()
            {
                Transform = transform,
                Center = center,
                Size = size * Padding,
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
            Material.SetFloat("_LineWidth", LineWidth * Utility.GetDpiScale());

            // TODO: big number for size to include everything in scene
            var bounds = new Bounds(Vector3.zero, new Vector3(10000, 10000, 10000));

            Graphics.DrawProcedural(Material, bounds, MeshTopology.Points, Boxes.Count, layer: LayerMask.NameToLayer("Sensor"));

            Boxes.Clear();
        }
    }
}
