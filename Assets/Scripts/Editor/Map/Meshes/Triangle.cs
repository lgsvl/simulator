/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Editor.MapMeshes
{
    using UnityEngine;

    public class Triangle
    {
        public Vertex v0;
        public Vertex v1;
        public Vertex v2;

        public bool IsClockwise => MeshUtils.IsTriangleClockwise(v0, v1, v2);

        public Triangle(Vertex v0, Vertex v1, Vertex v2)
        {
            this.v0 = v0;
            this.v1 = v1;
            this.v2 = v2;
        }

        public Triangle(Vector3 v0, Vector3 v1, Vector3 v2)
        {
            this.v0 = new Vertex(v0);
            this.v1 = new Vertex(v1);
            this.v2 = new Vertex(v2);
        }
        
        public void ChangeOrientation()
        {
            var temp = v0;
            v0 = v1;
            v1 = temp;
        }
    }
}