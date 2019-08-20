/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using System;
using UnityEngine.Rendering;

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

        struct Vertex
        {
            public Vector3 Position;
            public Vector3 Color;
        }

        const int BufferGranularity = 1024;

        List<Vertex> Vertices;
        ComputeBuffer Buffer;

        Material Material;
        public float LineWidth = 5f;
        public Camera Camera;

        void Start()
        {
            Vertices = new List<Vertex>(BufferGranularity);
            Material = new Material(RuntimeSettings.Instance.AAWireBoxShader);
        }

        void OnDestroy()
        {
            Buffer?.Release();
            Destroy(Material);
        }

        // min/max is in (0,0) - (width,height) coordinates
        public void Draw(Vector2 min, Vector2 max, Color color)
        {
            min = 2f * min * new Vector2(1f / Camera.pixelWidth, 1f / Camera.pixelHeight) - Vector2.one;
            max = 2f * max * new Vector2(1f / Camera.pixelWidth, 1f / Camera.pixelHeight) - Vector2.one;

            var width = LineWidth * Utility.GetDpiScale();
            var size = new Vector2(width / Camera.pixelWidth, width / Camera.pixelHeight);

            var p0 = new Vector2(min.x, max.y);
            var p1 = new Vector2(max.x, min.y);

            Action<Vector2, Vector2> line = (Vector2 a, Vector2 b) =>
            {
                var dir = (b - a).normalized;
                var normal = new Vector2(-dir.y, dir.x);

                dir *= size;
                normal *= size;

                var v0 = a - dir + normal;
                var v1 = a - dir - normal;
                var v2 = b + dir + normal;
                var v3 = b + dir - normal;

                Vertices.Add(new Vertex() { Position = v0, Color = (Vector4)color });
                Vertices.Add(new Vertex() { Position = v1, Color = (Vector4)color });
                Vertices.Add(new Vertex() { Position = v2, Color = (Vector4)color });

                Vertices.Add(new Vertex() { Position = v0, Color = (Vector4)color });
                Vertices.Add(new Vertex() { Position = v2, Color = (Vector4)color });
                Vertices.Add(new Vertex() { Position = v3, Color = (Vector4)color });
            };

            line(min, p0);
            line(p0, max);
            line(max, p1);
            line(p1, min);
        }

        void LateUpdate()
        {
            if (Vertices.Count == 0)
            {
                return;
            }

            if (Buffer == null || Vertices.Count > Buffer.count)
            {
                Buffer?.Release();

                int newCount = ((Vertices.Count + BufferGranularity - 1) / BufferGranularity) * BufferGranularity;
                Buffer = new ComputeBuffer(newCount, UnsafeUtility.SizeOf<Vertex>());
                Material.SetBuffer("_Vertices", Buffer);
            }

            Buffer.SetData(Vertices, 0, 0, Vertices.Count);

            // TODO: big number for size to include everything in scene
            var bounds = new Bounds(Vector3.zero, new Vector3(10000, 10000, 10000));
            Graphics.DrawProcedural(Material, bounds, MeshTopology.Triangles, Vertices.Count, 1, Camera, null, ShadowCastingMode.Off, false, layer: 1);
            Vertices.Clear();
        }
    }
}
