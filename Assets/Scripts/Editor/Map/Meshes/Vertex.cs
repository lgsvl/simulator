/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Editor.MapMeshes
{
    using UnityEngine;

    public class Vertex
    {
        public float x;
        public float y;
        public float z;

        public Vertex previous;
        public Vertex next;

        public bool isReflex;

        public Vector3 Position
        {
            get { return new Vector3(x, y, z); }
            set
            {
                x = value.x;
                y = value.y;
                z = value.z;
            }
        }

        public Vector2 Position2D
        {
            get { return new Vector2(x, z); }
            set
            {
                x = value.x;
                z = value.y;
            }
        }

        public static Vertex operator +(Vertex a, Vertex b) => new Vertex(a.Position + b.Position);
        public static Vertex operator -(Vertex a, Vertex b) => new Vertex(a.Position - b.Position);
        public static Vertex operator *(float multiplier, Vertex v) => new Vertex(v.Position * multiplier);
        public static Vertex operator *(Vertex v, float multiplier) => multiplier * v;

        public float SqrMagnitude2D => x * x + z * z;

        public Vertex(Vector3 position)
        {
            Position = position;
        }

        public Vertex(Vertex vert)
        {
            Position = vert.Position;
        }

        public Vertex(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public bool ApproximatelyEquals(Vertex vertex)
        {
            return Mathf.Approximately(x, vertex.x) && Mathf.Approximately(z, vertex.z);
        }

        public void ClearTemporaryData()
        {
            previous = null;
            next = null;
            isReflex = false;
        }

        public override string ToString()
        {
            return Position.ToString();
        }
    }
}