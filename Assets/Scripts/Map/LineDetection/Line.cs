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
    public class Line
    {
        [SerializeField]
        public Vector2[] points;

        public Vector2 Start => points[0];
        public Vector2 End => points[1];

        public Vector2 Vector => End - Start;

        public float Angle => Vector2.Angle(Vector2.right, Vector);

        public float Length => Vector.magnitude;

        public Line(Vector2 start, Vector2 end)
        {
            points = new[] {start, end};
        }

        public void FlipAxes()
        {
            for (var i = 0; i < points.Length; ++i)
            {
                var tmp = points[i].x;
                points[i].x = points[i].y;
                points[i].y = tmp;
            }
        }

        public override string ToString()
        {
            return $"{Start.ToString("F4")} - {End.ToString("F4")}";
        }
    }
}