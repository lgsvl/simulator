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
    // TODO: rename it to GroundTruthOverlay
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

        public bool IgnoreUpdates;

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

       private void DrawLineCamera(Vector2 a, Vector2 b, Color color)
        {
            var dir = (b - a).normalized;
            var normal = new Vector2(-dir.y, dir.x);

            var width = LineWidth * Utility.GetDpiScale();
            var size = new Vector2(width / Camera.pixelWidth, width / Camera.pixelHeight);

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
        }

        // min/max is in (0,0) - (width,height) coordinates
        public void DrawBox(Vector2 min, Vector2 max, Color color)
        {
            if (Vertices == null)
                return;

            min = 2f * min * new Vector2(1f / Camera.pixelWidth, 1f / Camera.pixelHeight) - Vector2.one;
            max = 2f * max * new Vector2(1f / Camera.pixelWidth, 1f / Camera.pixelHeight) - Vector2.one;

            var p0 = new Vector2(min.x, max.y);
            var p1 = new Vector2(max.x, min.y);

            DrawLineCamera(min, p0, color);
            DrawLineCamera(p0, max, color);
            DrawLineCamera(max, p1, color);
            DrawLineCamera(p1, min, color);
        }

        // this function gives the maximum
        private float maxi(float[] arr, int n)
        {
            float m = 0;
            for (int i = 0; i < n; ++i)
                if (m < arr[i])
                    m = arr[i];
            return m;
        }

        // this function gives the minimum
        private float mini(float[] arr, int n)
        {
            float m = 1;
            for (int i = 0; i < n; ++i)
                if (m > arr[i])
                    m = arr[i];
            return m;
        }

        // start-end will be clipped by (0,0) - (width,height) window
        public void DrawClippedLine(Vector2 start, Vector2 end, Color color)
        {
            if (Vertices == null)
                return;

            //Debug.Log("start: " + start.ToString() + ", end: " + end.ToString());
            // defining variables
            float p1 = -(end.x - start.x);
            float p2 = -p1;
            float p3 = -(end.y - start.y);
            float p4 = -p3;

            float q1 = start.x;
            float q2 = Camera.pixelWidth - start.x;
            float q3 = start.y;
            float q4 = Camera.pixelHeight - start.y;

            float[] posarr = new float[5];
            float[] negarr = new float[5];
            int posind = 1, negind = 1;
            posarr[0] = 1;
            negarr[0] = 0;

            if ((p1 == 0 && q1 < 0) || (p2 == 0 && q2 < 0) || (p3 == 0 && q3 < 0) || (p4 == 0 && q4 < 0))
            {
                // Line is parallel to clipping window and is outside
                return;
            }
            if (p1 != 0)
            {
                float r1 = q1 / p1;
                float r2 = q2 / p2;
                if (p1 < 0)
                {
                    negarr[negind++] = r1; // for negative p1, add it to negative array
                    posarr[posind++] = r2; // and add p2 to positive array
                }
                else
                {
                    negarr[negind++] = r2;
                    posarr[posind++] = r1;
                }
            }
            if (p3 != 0)
            {
                float r3 = q3 / p3;
                float r4 = q4 / p4;
                if (p3 < 0)
                {
                    negarr[negind++] = r3;
                    posarr[posind++] = r4;
                }
                else
                {
                    negarr[negind++] = r4;
                    posarr[posind++] = r3;
                }
            }

            float xn1, yn1, xn2, yn2;
            float rn1, rn2;
            rn1 = maxi(negarr, negind); // maximum of negative array
            rn2 = mini(posarr, posind); // minimum of positive array

            if (rn1 > rn2)
            { // reject
                // Line is outside the clipping window!
                return;
            }

            xn1 = start.x + p2 * rn1;
            yn1 = start.y + p4 * rn1; // computing new points

            xn2 = start.x + p2 * rn2;
            yn2 = start.y + p4 * rn2;

            //Debug.Log("clipped start: (" + xn1 + ", " + yn1 + "), clipped end: (" + xn2 + ", " + yn2 + ")");
            DrawLine(new Vector2(xn1, yn1), new Vector2(xn2, yn2), color); // the drawing the new line
        }

        // start/end is in (0,0) - (width,height) coordinates
        public void DrawLine(Vector2 start, Vector2 end, Color color)
        {
            if (Vertices == null)
                return;

            start = 2f * start * new Vector2(1f / Camera.pixelWidth, 1f / Camera.pixelHeight) - Vector2.one;
            end = 2f * end * new Vector2(1f / Camera.pixelWidth, 1f / Camera.pixelHeight) - Vector2.one;

            DrawLineCamera(start, end, color);
        }

        private void LateUpdate()
        {
            if (IgnoreUpdates)
                return;

            if (Vertices.Count == 0)
                return;

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

        public void Draw(CommandBuffer cmd)
        {
            if (Vertices == null || Vertices.Count == 0)
                return;

            if (Buffer == null || Vertices.Count > Buffer.count)
            {
                Buffer?.Release();

                int newCount = ((Vertices.Count + BufferGranularity - 1) / BufferGranularity) * BufferGranularity;
                Buffer = new ComputeBuffer(newCount, UnsafeUtility.SizeOf<Vertex>());
                Material.SetBuffer("_Vertices", Buffer);
            }

            Buffer.SetData(Vertices, 0, 0, Vertices.Count);

            cmd.SetRenderTarget(Camera.targetTexture);
            cmd.DrawProcedural(Matrix4x4.identity, Material, 0, MeshTopology.Triangles, Vertices.Count);
        }

        public void Clear()
        {
            Vertices?.Clear();
        }
    }
}
