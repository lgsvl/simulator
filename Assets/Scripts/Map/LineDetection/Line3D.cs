/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Map.LineDetection
{
    using System;
    using UnityEngine;

    [Serializable]
    public class Line3D
    {
        public Vector3[] points;

        public float width;

        public Color color = Color.white;

        public Vector3 Start => points[0];
        public Vector3 End => points[1];

        public Vector3 Vector => End - Start;

        public float Angle => Vector3.Angle(Vector3.right, Vector);

        public float Length => Vector.magnitude;

        public Line3D(Vector3 start, Vector3 end)
        {
            points = new[] {start, end};
            width = 0f;
        }

        public Line3D(Vector3 start, Vector3 end, float width)
        {
            points = new[] {start, end};
            this.width = width;
        }

        public void Invert()
        {
            var tmp = points[0];
            points[0] = points[1];
            points[1] = tmp;
        }

        public Line3D Inverted()
        {
            return new Line3D(End, Start, width) {color = color};
        }
    }
}