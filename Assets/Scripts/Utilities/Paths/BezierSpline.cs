/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Utilities
{
    using System.Collections.Generic;
    using UnityEngine;

    public class BezierSpline<T> where T : IWaypoint
    {
        private readonly T[] knots;
        private readonly Vector3[] controlPoints1;
        private readonly Vector3[] controlPoints2;
        private readonly List<T> cachedPoints = new List<T>();

        public T[] Knots => knots;
        public float ApproximatedLength { get; private set; }

        public BezierSpline(T[] waypoints, float lineSimplifyTolerance)
        {
            knots = waypoints;
            if (knots.Length < 2)
                return;
            ComputeControlPoints(waypoints, out controlPoints1, out controlPoints2);
            CalculatePoints(lineSimplifyTolerance);
        }

        private void CalculatePoints(float lineSimplifyTolerance)
        {
            cachedPoints.Clear();
            for (var index = 0; index < knots.Length - 1; index++)
            {
                var knot = knots[index];
                var nextKnot = knots[index + 1];

                // Ignore knots in the same position
                if (Vector3.Distance(knot.Position, nextKnot.Position) < Mathf.Epsilon)
                    continue;
                
                // Calculate linear distance between points
                var overestimatedLength = Vector3.Distance(knot.Position, nextKnot.Position);

                // Calculate positions on the Bezier curve
                var overestimatesPointsCount = Mathf.RoundToInt(overestimatedLength * 1.0f / lineSimplifyTolerance);
                var pointsCandidates = new List<Vector3>(overestimatesPointsCount);
                for (var i = 0; i < overestimatesPointsCount; i++)
                {
                    pointsCandidates.Add(GetPoint(index, 1.0f * (i + 1) / overestimatesPointsCount));
                }

                // Simplify control points list, minimizing precached data
                var controlPositions = new List<Vector3>();
                LineUtility.Simplify(pointsCandidates, lineSimplifyTolerance, controlPositions);

                //Check if there are any calculated control positions
                var previousPoint = knot;
                if (controlPositions.Count == 0)
                    return;

                // Calculate distances between control points and cache them
                ApproximatedLength = Vector3.Distance(previousPoint.Position, controlPositions[0]);
                for (var i = 0; i < controlPositions.Count - 1; i++)
                {
                    ApproximatedLength += Vector3.Distance(controlPositions[i], controlPositions[i + 1]);
                }

                // Create control and key points
                for (var i = 0; i < controlPositions.Count; i++)
                {
                    var point = i == controlPositions.Count - 1 ? nextKnot.Clone() : nextKnot.GetControlPoint();
                    //Rotate to next position, keep previous angle at the last control point
                    point.Position = controlPositions[i];
                    var angle = Quaternion.LookRotation(controlPositions[i] - previousPoint.Position).eulerAngles;
                    point.Angle = angle;
                    var waypoint = (T) point;
                    cachedPoints.Add(waypoint);
                    previousPoint = waypoint;
                }
            }
        }

        public Vector3 GetPoint(int knotI, float t)
        {
            return GetPoint(knots[knotI].Position, controlPoints1[knotI], controlPoints2[knotI],
                knots[knotI + 1].Position, t);
        }

        private static Vector3 GetPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            t = Mathf.Clamp01(t);
            var oneMinusT = 1f - t;
            return
                oneMinusT * oneMinusT * oneMinusT * p0 +
                3f * oneMinusT * oneMinusT * t * p1 +
                3f * oneMinusT * t * t * p2 +
                t * t * t * p3;
        }

        public List<T> GetBezierWaypoints()
        {
            return cachedPoints;
        }

        /// <summary>
        /// Calculating control points from https://www.particleincell.com/2012/bezier-splines/
        /// Computes control points given knots K, this is the brain of the operation
        /// </summary>
        /// <param name="knots">Knot points</param>
        /// <param name="p1">Output control points 1</param>
        /// <param name="p2">Output control points 2</param>
        private static void ComputeControlPoints(T[] knots, out Vector3[] p1, out Vector3[] p2)
        {
            var n = knots.Length - 1;
            p1 = new Vector3[n];
            p2 = new Vector3[n];

            // rhs vector
            var a = new float[n];
            var b = new float[n];
            var c = new float[n];
            var r = new Vector3[n];

            // left most segment
            a[0] = 0.0f;
            b[0] = 2.0f;
            c[0] = 1.0f;
            r[0] = knots[0].Position + 2.0f * knots[1].Position;

            // internal segments
            for (var i = 1; i < n - 1; i++)
            {
                a[i] = 1;
                b[i] = 4;
                c[i] = 1;
                r[i] = 4 * knots[i].Position + 2 * knots[i + 1].Position;
            }

            // right segment
            a[n - 1] = 2;
            b[n - 1] = 7;
            c[n - 1] = 0;
            r[n - 1] = 8 * knots[n - 1].Position + knots[n].Position;

            // solves Ax=b with the Thomas algorithm (from Wikipedia)
            for (var i = 1; i < n; i++)
            {
                var m = a[i] / b[i - 1];
                b[i] = b[i] - m * c[i - 1];
                r[i] = r[i] - m * r[i - 1];
            }

            p1[n - 1] = r[n - 1] / b[n - 1];
            for (var i = n - 2; i >= 0; --i)
                p1[i] = (r[i] - c[i] * p1[i + 1]) / b[i];

            // we have p1, now compute p2
            for (var i = 0; i < n - 1; i++)
                p2[i] = 2 * knots[i + 1].Position - p1[i + 1];

            p2[n - 1] = 0.5f * (knots[n].Position + p1[n - 1]);
        }
    }
}