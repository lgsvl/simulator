/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Editor.MapLineDetection
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Map.LineDetection;
    using UnityEngine;
    using UnityEngine.Rendering;

    public static class LineUtils
    {
        public static float LineLineDistance(Line a, Line b)
        {
            return LineLineDistance(a.Start, a.End, b.Start, b.End);
        }

        public static float LineLineAngle(Line a, Line b)
        {
            return Mathf.Min(Vector2.Angle(a.Vector, b.Vector), Vector2.Angle(a.Vector, -b.Vector));
        }

        private static float LineLineDistance(Vector2 a0, Vector2 a1, Vector2 b0, Vector2 b1)
        {
            LineLineIntersection(a0, a1, b0, b1, out _, out var segmentsIntersecting, out var intersection);

            if (segmentsIntersecting)
                return 0;

            var bestDist = float.MaxValue;

            var currentDist = PointLineDistance(a0, b0, b1, out var closest);
            if (currentDist < bestDist)
                bestDist = currentDist;

            currentDist = PointLineDistance(a1, b0, b1, out closest);
            if (currentDist < bestDist)
                bestDist = currentDist;

            currentDist = PointLineDistance(b0, a0, a1, out closest);
            if (currentDist < bestDist)
                bestDist = currentDist;

            currentDist = PointLineDistance(b1, a0, a1, out closest);
            if (currentDist < bestDist)
                bestDist = currentDist;

            return bestDist;
        }

        public static void LineLineIntersection(
            Vector2 a0, Vector2 a1, Vector2 b0, Vector2 b1,
            out bool linesIntersect, out bool segmentsIntersect,
            out Vector2 intersection)
        {
            var aX = a1.x - a0.x;
            var aY = a1.y - a0.y;
            var bX = b1.x - b0.x;
            var bY = b1.y - b0.y;

            var den = aY * bX - aX * bY;

            if (Mathf.Approximately(den, 0f))
            {
                linesIntersect = false;
                segmentsIntersect = false;
                intersection = new Vector2(float.NaN, float.NaN);
                return;
            }

            linesIntersect = true;

            var t1 = ((a0.x - b0.x) * bY + (b0.y - a0.y) * bX) / den;
            var t2 = ((b0.x - a0.x) * aY + (a0.y - b0.y) * aX) / -den;

            intersection = new Vector2(a0.x + aX * t1, a0.y + aY * t1);
            segmentsIntersect = t1 >= 0 && t1 <= 1 && t2 >= 0 && t2 <= 1;
        }

        public static float PointLineDistance(Vector2 point, Line line)
        {
            return PointLineDistance(point, line.Start, line.End, out _);
        }

        private static float PointLineDistance(Vector2 p, Vector2 a0, Vector2 a1, out Vector2 closest)
        {
            var aX = a1.x - a0.x;
            var aY = a1.y - a0.y;

            if (Mathf.Approximately(aX, 0f) && Mathf.Approximately(aY, 0f))
            {
                closest = a0;
                aX = p.x - a0.x;
                aY = p.y - a0.y;
                return Mathf.Sqrt(aX * aX + aY * aY);
            }

            var t = ((p.x - a0.x) * aX + (p.y - a0.y) * aY) / (aX * aX + aY * aY);

            if (t < 0)
            {
                closest = a0;
                aX = p.x - a0.x;
                aY = p.y - a0.y;
            }
            else if (t > 1)
            {
                closest = a1;
                aX = p.x - a1.x;
                aY = p.y - a1.y;
            }
            else
            {
                closest = new Vector2(a0.x + t * aX, a0.y + t * aY);
                aX = p.x - closest.x;
                aY = p.y - closest.y;
            }

            return Mathf.Sqrt(aX * aX + aY * aY);
        }

        private static Line FindBestLinearFit(List<Vector2> points, List<float> weights)
        {
            var n = weights.Sum();

            var xSquareWeighted = 0f;
            var ySquareWeighted = 0f;
            var xWeighted = 0f;
            var yWeighted = 0f;
            var xyWeighted = 0f;

            for (var i = 0; i < points.Count; ++i)
            {
                var p = points[i];
                var w = weights[i];
                xSquareWeighted += p.x * p.x * w;
                ySquareWeighted += p.y * p.y * w;
                xWeighted += p.x * w;
                yWeighted += p.y * w;
                xyWeighted += p.x * w * p.y;
            }

            var dA = ySquareWeighted - (yWeighted * yWeighted) / n;
            var dB = xSquareWeighted - (xWeighted * xWeighted) / n;
            var dC = (xWeighted * yWeighted) / n - xyWeighted;
            var d = 0.5f * (dA - dB) / dC;

            // TODO: select better answer analytically if possible
            var a1 = -d + Mathf.Sqrt(d * d + 1);
            var a2 = -d - Mathf.Sqrt(d * d + 1);

            var b1 = (yWeighted - a1 * xWeighted) / n;
            var b2 = (yWeighted - a2 * xWeighted) / n;

            if (ErrorSquaredWeighted(points, weights, a1, b1) < ErrorSquaredWeighted(points, weights, a2, b2))
            {
                var pA = new Vector2(0, b1);
                var pB = new Vector2(1, a1 + b1);
                return new Line(pA, pB);
            }
            else
            {
                var pA = new Vector2(0, b2);
                var pB = new Vector2(1, a2 + b2);
                return new Line(pA, pB);
            }
        }

        public static Line GenerateLinearBestFit(List<Line> lines, out float maxDist, out float avgDist)
        {
            if (lines.Count == 0)
                throw new Exception("No data available to create best fit.");

            var points = ListPool<Vector2>.Get();
            var weights = ListPool<float>.Get();

            var min = new Vector3(float.MaxValue, float.MaxValue);
            var max = new Vector3(float.MinValue, float.MinValue);

            foreach (var line in lines)
            {
                min = Vector2.Min(line.Start, min);
                min = Vector2.Min(line.End, min);
                max = Vector2.Max(line.Start, max);
                max = Vector2.Max(line.End, max);
            }

            var flipped = false;
            if (max.x - min.x < max.y - min.y)
            {
                flipped = true;
                foreach (var line in lines)
                    line.FlipAxes();
            }

            foreach (var line in lines)
            {
                var vec = line.Vector;
                var weight = vec.magnitude;
                points.Add(line.Start);
                points.Add(line.End);
                weights.Add(weight);
                weights.Add(weight);
            }

            var bestLine = FindBestLinearFit(points, weights);
            var pA = bestLine.Start;
            var pB = bestLine.End;

            ListPool<Vector2>.Release(points);
            ListPool<float>.Release(weights);

            var lowestVal = float.MaxValue;
            var highestVal = float.MinValue;
            var lowestPoint = Vector2.zero;
            var highestPoint = Vector2.zero;
            var distSum = 0f;
            var maximumDist = 0f;

            void TryUpdateRange(Vector2 point, Vector2 originalPoint, float t)
            {
                var distance = Vector2.Distance(point, originalPoint);
                distSum += distance;

                if (distance > maximumDist)
                    maximumDist = distance;

                if (t < lowestVal)
                {
                    lowestVal = t;
                    lowestPoint = point;
                }

                if (t > highestVal)
                {
                    highestVal = t;
                    highestPoint = point;
                }
            }

            foreach (var line in lines)
            {
                var point = NearestPointOnLine(pA, pB, line.Start, false, out var t);
                TryUpdateRange(point, line.Start, t);
                point = NearestPointOnLine(pA, pB, line.End, false, out t);
                TryUpdateRange(point, line.End, t);
            }

            avgDist = distSum / lines.Count / 2;
            maxDist = maximumDist;
            var result = new Line(lowestPoint, highestPoint);
            if (flipped)
            {
                foreach (var line in lines)
                    line.FlipAxes();

                result.FlipAxes();
            }

            return result;
        }

        public static void SplitByLength(ApproximatedLine line, out ApproximatedLine splitA, out ApproximatedLine splitB)
        {
            var bestFit = line.BestFitLine;
            var s = bestFit.Start;
            var e = bestFit.End;

            splitA = new ApproximatedLine(line.Settings);
            splitB = new ApproximatedLine(line.Settings);

            foreach (var ln in line.lines)
            {
                NearestPointOnLine(s, e, ln.Start, out var startDen);
                NearestPointOnLine(s, e, ln.End, out var endDen);

                if (startDen <= 0.5f && endDen <= 0.5f)
                    splitA.lines.Add(ln);
                else if (startDen > 0.5f && endDen > 0.5f)
                    splitB.lines.Add(ln);
                else
                {
                    var aDen = Mathf.Min(startDen, endDen);
                    var bDen = Mathf.Max(startDen, endDen);
                    var aPt = startDen < endDen ? ln.Start : ln.End;
                    var bPt = startDen < endDen ? ln.End : ln.Start;
                    var p = (0.5f - aDen) / (bDen - aDen);
                    var mPt = Vector3.Lerp(aPt, bPt, p);

                    splitA.lines.Add(new Line(aPt, mPt));
                    splitB.lines.Add(new Line(mPt, bPt));
                }
            }

            if (splitA.lines.Count == 0 || splitB.lines.Count == 0)
            {
                foreach (var ln in line.lines)
                {
                    NearestPointOnLine(s, e, ln.Start, out _);
                    NearestPointOnLine(s, e, ln.End, out _);
                }
            }

            splitA.color = line.color;
            splitB.color = line.color;

            splitA.Recalculate();
            splitB.Recalculate();
        }

        public static void RemovePastThresholdLines(ApproximatedLine line)
        {
            var settings = line.Settings;
            var thr = settings.worstFitThreshold;

            var jointLines = new List<ApproximatedLine>();

            foreach (var ln in line.lines)
            {
                var found = false;
                foreach (var jointLine in jointLines)
                {
                    if (jointLine.TryAddLine(ln, settings.jointLineThreshold))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    jointLines.Add(new ApproximatedLine(ln, settings));
            }

            var changed = true;
            var safety = 0;
            while (changed)
            {
                if (safety++ > 5000)
                {
                    Debug.LogError("Broken infinite loop");
                    return;
                }

                changed = false;
                for (var i = 0; i < jointLines.Count - 1; ++i)
                {
                    for (var j = i + 1; j < jointLines.Count; ++j)
                    {
                        if (jointLines[i].TryMerge(jointLines[j], settings.jointLineThreshold))
                        {
                            jointLines.Remove(jointLines[j]);
                            changed = true;
                            break;
                        }
                    }

                    if (changed)
                        break;
                }
            }

            foreach (var jointLine in jointLines)
                jointLine.Recalculate();

            var bestLength = float.MinValue;
            var bestLine = jointLines[0];

            foreach (var jointLine in jointLines)
            {
                var length = jointLine.BestFitLine.Length;
                if (length > bestLength)
                {
                    bestLength = length;
                    bestLine = jointLine;
                }
            }

            for (var i = 0; i < line.lines.Count; ++i)
            {
                if (LineLineDistance(line.lines[i].Start, line.lines[i].End, bestLine.BestFitLine.Start, bestLine.BestFitLine.Start) > thr)
                    line.lines.RemoveAt(i--);
            }

            line.Recalculate();
        }

        private static float ErrorSquaredWeighted(List<Vector2> points, List<float> weights, float a, float b)
        {
            var result = 0f;

            for (var i = 0; i < points.Count; ++i)
            {
                var p = points[i];
                var apX = p.x;
                var apY = p.y - b;

                var aSq = 1 + a * a;
                var apSq = apX + apY * a;
                var t = apSq / aSq;
                var dX = p.x - t;
                var dY = p.y - (b + a * t);
                result += (dX * dX + dY * dY) * weights[i];
            }

            return result;
        }

        public static Vector2 NearestPointOnLine(Vector2 a0, Vector2 a1, Vector2 p, out float den, bool clampToSegment = false)
        {
            var flipped = Mathf.Abs(a0.x - a1.x) < Mathf.Abs(a0.y - a1.y);
            return NearestPointOnLine(a0, a1, p, flipped, out den, clampToSegment);
        }

        public static Vector2 NearestPointOnLine(Vector2 a0, Vector2 a1, Vector2 p, bool flipped, out float den, bool clampToSegment = false)
        {
            var apX = flipped ? p.y - a0.y : p.x - a0.x;
            var apY = flipped ? p.x - a0.x : p.y - a0.y;
            var aX = flipped ? a1.y - a0.y : a1.x - a0.x;
            var aY = flipped ? a1.x - a0.x : a1.y - a0.y;

            var aSq = aX * aX + aY * aY;
            var apSq = apX * aX + apY * aY;
            var t = apSq / aSq;
            if (clampToSegment)
                t = Mathf.Clamp01(t);

            den = t;

            var rX = a0.x + aX * t;
            var rY = a0.y + aY * t;

            var result = new Vector2(flipped ? rY : rX, flipped ? rX : rY);
            return result;
        }

        public static int LoopIndex(int index, int count)
        {
            return (index % count + count) % count;
        }

        public static float EstimateUvToWorldScale(MeshFilter meshFilter, Texture texture)
        {
            var mesh = meshFilter.sharedMesh;
            var uv = mesh.uv;
            var triangles = mesh.triangles;
            var verts = mesh.vertices;

            // Assume uniform scale, it should be 1 anyway
            var localToWorldScale = meshFilter.transform.lossyScale.x;
            var ratioSum = 0f;

            for (var i = 0; i < triangles.Length; i += 3)
            {
                for (var j = 0; j < 3; ++j)
                {
                    var j0 = j;
                    var j1 = LoopIndex(j + 1, 3);

                    var localDist = Vector3.Distance(verts[triangles[i + j0]], verts[triangles[i + j1]]);
                    var uvDist = Vector2.Distance(uv[triangles[i + j0]], uv[triangles[i + j1]]);
                    ratioSum += localDist / uvDist;
                }
            }

            var result = localToWorldScale * ratioSum / triangles.Length;
            return result;
        }

        public static bool IsSegmentDuplicate(SegmentedLine3D a, SegmentedLine3D b, float threshold)
        {
            if (Vector3.Distance(a.Start, b.Start) < threshold && Vector3.Distance(a.End, b.End) < threshold)
                return true;
            if (Vector3.Distance(a.Start, b.End) < threshold && Vector3.Distance(a.End, b.Start) < threshold)
                return true;
            return false;
        }
    }
}