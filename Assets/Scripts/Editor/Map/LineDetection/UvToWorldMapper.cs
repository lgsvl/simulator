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
    using Simulator.Map.LineDetection;
    using UnityEngine;

    public static class UvToWorldMapper
    {
        private const float MergeThreshold = 0.05f;

        private enum TriangleSearchResult
        {
            Continues,
            EdgeTermination,
            WithinTriangle
        }

        private static Vector3 GetBarycentric(Vector2 v1, Vector2 v2, Vector2 v3, Vector2 p)
        {
            var barycentric = new Vector3
            {
                x = ((v2.y - v3.y) * (p.x - v3.x) + (v3.x - v2.x) * (p.y - v3.y)) / ((v2.y - v3.y) * (v1.x - v3.x) + (v3.x - v2.x) * (v1.y - v3.y)),
                y = ((v3.y - v1.y) * (p.x - v3.x) + (v1.x - v3.x) * (p.y - v3.y)) / ((v3.y - v1.y) * (v2.x - v3.x) + (v1.x - v3.x) * (v2.y - v3.y))
            };
            barycentric.z = 1 - barycentric.x - barycentric.y;
            return barycentric;
        }

        private static bool IsInTriangle(Vector3 barycentric)
        {
            const float threshold = 0.001f;
            const float min = -threshold;
            const float max = 1.0f + threshold;

            return barycentric.x >= min &&
                   barycentric.x <= max &&
                   barycentric.y >= min &&
                   barycentric.y <= max &&
                   barycentric.z >= min;
        }

        private static bool TryGetWorldPosInTriangle(Vector2 uvPos, Vector3[] verts, int[] tris, Vector2[] uv, Transform trans, int triangleStartIndex, out Vector3 worldPos)
        {
            var i = triangleStartIndex;

            var uv1 = uv[tris[i]];
            var uv2 = uv[tris[i + 1]];
            var uv3 = uv[tris[i + 2]];

            var pos1 = trans.TransformPoint(verts[tris[i]]);
            var pos2 = trans.TransformPoint(verts[tris[i + 1]]);
            var pos3 = trans.TransformPoint(verts[tris[i + 2]]);

            var bPos = GetBarycentric(uv1, uv2, uv3, uvPos);

            if (IsInTriangle(bPos))
            {
                worldPos = bPos.x * pos1 + bPos.y * pos2 + bPos.z * pos3;
                return true;
            }

            worldPos = default;
            return false;
        }

        public static List<SegmentedLine3D> CalculateWorldSpaceNew(List<ApproximatedLine> uvLines, MeshFilter meshFilter, float uvToWorldSpace)
        {
            var result = new List<SegmentedLine3D>();

            foreach (var line in uvLines)
            {
                var segments = GetWorldSpaceLines(line.BestFitLine, meshFilter, uvToWorldSpace);
                var worldLineWidth = line.AverageFit * 2;

                if (worldLineWidth < line.Settings.minWidthThreshold)
                    continue;

                foreach (var segment in segments)
                {
                    // TODO: average width over segment?
                    segment.Width = worldLineWidth;

                    var color = UnityEngine.Random.ColorHSV(0, 1, 0.9f, 1, 0.7f, 1.0f);
                    foreach (var line3D in segment.lines)
                        line3D.color = color;

                    result.Add(segment);
                }
            }

            return result;
        }

        private static List<SegmentedLine3D> GetWorldSpaceLines(Line line, MeshFilter meshFilter, float uvToWorldSpace)
        {
            var mesh = meshFilter.sharedMesh;
            var tris = mesh.triangles;
            var uv = mesh.uv;

            var uvStart = new Vector2(line.Start.x / uvToWorldSpace, line.Start.y / uvToWorldSpace);
            var uvEnd = new Vector2(line.End.x / uvToWorldSpace, line.End.y / uvToWorldSpace);
            var uvDir = (uvEnd - uvStart).normalized;

            var result = new List<SegmentedLine3D>();

            var startUvMatches = new List<Vector2>();
            var endUvMatches = new List<Vector2>();
            var startTriangleMatches = new List<int>();
            var endTriangleMatches = new List<int>();

            for (var i = 0; i < tris.Length; i += 3)
            {
                var uv1 = uv[tris[i]];
                var uv2 = uv[tris[i + 1]];
                var uv3 = uv[tris[i + 2]];

                var uvMin = Vector2.Min(Vector2.Min(uv1, uv2), uv3);
                var uvMax = Vector2.Max(Vector2.Max(uv1, uv2), uv3);

                for (var u = Mathf.Floor(uvMin.x); u < Mathf.Ceil(uvMax.x); ++u)
                {
                    for (var v = Mathf.Floor(uvMin.y); v < Mathf.Ceil(uvMax.y); ++v)
                    {
                        var luvStart = new Vector2(u + uvStart.x, v + uvStart.y);
                        var luvEnd = new Vector2(u + uvEnd.x, v + uvEnd.y);

                        var bStart = GetBarycentric(uv1, uv2, uv3, luvStart);
                        var bEnd = GetBarycentric(uv1, uv2, uv3, luvEnd);

                        if (IsInTriangle(bStart))
                        {
                            var pos = bStart.x * uv1 + bStart.y * uv2 + bStart.z * uv3;
                            startUvMatches.Add(pos);
                            startTriangleMatches.Add(i);
                        }

                        if (IsInTriangle(bEnd))
                        {
                            var pos = bEnd.x * uv1 + bEnd.y * uv2 + bEnd.z * uv3;
                            endUvMatches.Add(pos);
                            endTriangleMatches.Add(i);
                        }
                    }
                }
            }

            if (startUvMatches.Count > 0 || endUvMatches.Count > 0)
            {
                for (var i = 0; i < startUvMatches.Count; ++i)
                {
                    var segment = Traverse(startUvMatches[i], uvDir, startTriangleMatches[i], endTriangleMatches, endUvMatches, meshFilter);
                    result.Add(segment);
                }

                for (var i = 0; i < endUvMatches.Count; ++i)
                {
                    var segment = Traverse(endUvMatches[i], -uvDir, endTriangleMatches[i], startTriangleMatches, startUvMatches, meshFilter);
                    segment.Invert();

                    var duplicate = false;
                    foreach (var resSegment in result)
                    {
                        if (LineUtils.IsSegmentDuplicate(segment, resSegment, MergeThreshold))
                        {
                            duplicate = true;
                            break;
                        }
                    }

                    if (duplicate)
                        continue;

                    result.Add(segment);
                }
            }
            // TODO: Sample mid-points in case only mid-section is visible - below is incomplete
            /*
            else
            {
                // If no ends found - check midpoints
                Debug.LogError("No points on mesh");
                for (var j = 0; j < 8; ++j)
                {
                    var uvMid = Vector3.Lerp(uvStart, uvEnd, (float) j / 8);
                
                    for (var i = 0; i < tris.Length; i += 3)
                    {
                        var uv1 = uv[tris[i]];
                        var uv2 = uv[tris[i + 1]];
                        var uv3 = uv[tris[i + 2]];
                
                        var pos1 = trans.TransformPoint(verts[tris[i]]);
                        var pos2 = trans.TransformPoint(verts[tris[i + 1]]);
                        var pos3 = trans.TransformPoint(verts[tris[i + 2]]);
                
                        var bMid = GetBarycentric(uv1, uv2, uv3, uvStart);
                
                        if (IsInTriangle(bMid))
                        {
                            var pos = bMid.x * pos1 + bMid.y * pos2 + bMid.z * pos3; 
                            line.TryAddMid(pos);
                        }
                    }
                    
                    if (line.midMatches.Count > 0)
                        break;
                }
            }
            */

            return result;
        }

        private static SegmentedLine3D Traverse(Vector3 uvStart, Vector3 uvDir, int triangleStart, List<int> triangleEndMatches, List<Vector2> uvEndMatches, MeshFilter meshFilter)
        {
            var mesh = meshFilter.sharedMesh;
            var trans = meshFilter.transform;

            var verts = mesh.vertices;
            var tris = mesh.triangles;
            var uv = mesh.uv;

            var points = new List<Vector3>();

            if (TryGetWorldPosInTriangle(uvStart, verts, tris, uv, trans, triangleStart, out var wStart))
                points.Add(wStart);
            else
                throw new Exception("Point is not in declared triangle.");

            var safety = 0;
            var lastTriangleStart = triangleStart;
            var lastResult = TryGetNextTriangle(uvStart, uvDir, lastTriangleStart, tris, uv, triangleEndMatches, uvEndMatches, points.Count, out var uvPos, out var nextTriangle);

            while (lastResult == TriangleSearchResult.Continues)
            {
                if (++safety > 20000)
                    throw new Exception("Terminating infinite loop.");

                if (TryGetWorldPosInTriangle(uvPos, verts, tris, uv, trans, lastTriangleStart, out var wPos))
                    points.Add(wPos);
                else
                    throw new Exception("Point is not in declared triangle.");

                lastTriangleStart = nextTriangle;
                lastResult = TryGetNextTriangle(uvPos, uvDir, lastTriangleStart, tris, uv, triangleEndMatches, uvEndMatches, points.Count, out uvPos, out nextTriangle);
            }

            if (lastResult == TriangleSearchResult.EdgeTermination)
            {
                if (TryGetWorldPosInTriangle(uvPos, verts, tris, uv, trans, lastTriangleStart, out var wPos))
                    points.Add(wPos);
                else
                    throw new Exception("Point is not in declared triangle.");
            }
            else
            {
                var found = false;
                for (var i = 0; i < triangleEndMatches.Count; ++i)
                {
                    if (lastTriangleStart != triangleEndMatches[i])
                        continue;

                    if (TryGetWorldPosInTriangle(uvEndMatches[i], verts, tris, uv, trans, lastTriangleStart, out var wPos))
                        points.Add(wPos);
                    else
                        throw new Exception("Point is not in declared triangle.");

                    found = true;
                }

                if (!found)
                    throw new Exception("Unable to locate endpoint.");
            }

            var result = new List<Line3D>();
            for (var i = 1; i < points.Count; ++i)
                result.Add(new Line3D(points[i - 1], points[i]));

            return new SegmentedLine3D(result);
        }

        private static bool TryMatchInTriangleEnd(Vector2 uvStart, Vector2 uvDir, Vector2 uvEnd, int pointCount)
        {
            var distance = Vector3.Distance(uvStart, uvEnd);
            // TODO: threshold should be in world space, not UV space
            if (distance < 0.02f && pointCount == 1)
                return false;

            var startEndDir = uvEnd - uvStart;
            var angle = Vector2.Angle(startEndDir, uvDir);
            return angle < 10f;
        }

        private static TriangleSearchResult TryGetNextTriangle(Vector2 uvStart, Vector2 uvDir, int triangleStart, int[] tris, Vector2[] uv, List<int> triangleEndMatches, List<Vector2> uvEndMatches, int pointCount, out Vector2 intersection, out int nextTriangleStart)
        {
            var inTriangleMatch = -1;
            for (var i = 0; i < triangleEndMatches.Count; ++i)
            {
                if (triangleStart == triangleEndMatches[i])
                {
                    inTriangleMatch = i;
                    break;
                }
            }

            if (inTriangleMatch != -1 && (triangleEndMatches[inTriangleMatch] == triangleStart || TryMatchInTriangleEnd(uvStart, uvDir, uvEndMatches[inTriangleMatch], pointCount)))
            {
                intersection = uvEndMatches[inTriangleMatch];
                nextTriangleStart = triangleStart;
                return TriangleSearchResult.WithinTriangle;
            }

            nextTriangleStart = -1;
            intersection = uvStart;

            var uv1 = uv[tris[triangleStart]];
            var uv2 = uv[tris[triangleStart + 1]];
            var uv3 = uv[tris[triangleStart + 2]];

            var uvEnd = uvStart + uvDir * 50f;
            uvStart = uvStart - uvDir * 50f;

            LineUtils.LineLineIntersection(uv1, uv2, uvStart, uvEnd, out _, out var ints12, out var pos12);
            LineUtils.LineLineIntersection(uv2, uv3, uvStart, uvEnd, out _, out var ints23, out var pos23);
            LineUtils.LineLineIntersection(uv3, uv1, uvStart, uvEnd, out _, out var ints31, out var pos31);

            var indexA = -1;
            var indexB = -1;
            var bestSqrMag = -1f;

            if (ints12 && Vector3.SqrMagnitude(pos12 - uvStart) > bestSqrMag)
            {
                indexA = tris[triangleStart];
                indexB = tris[triangleStart + 1];
                bestSqrMag = Vector3.SqrMagnitude(pos12 - uvStart);
                intersection = pos12;
            }

            if (ints23 && Vector3.SqrMagnitude(pos23 - uvStart) > bestSqrMag)
            {
                indexA = tris[triangleStart + 1];
                indexB = tris[triangleStart + 2];
                bestSqrMag = Vector3.SqrMagnitude(pos23 - uvStart);
                intersection = pos23;
            }

            if (ints31 && Vector3.SqrMagnitude(pos31 - uvStart) > bestSqrMag)
            {
                indexA = tris[triangleStart + 2];
                indexB = tris[triangleStart];
                intersection = pos31;
            }

            bool TriangleMatches(int a1, int a2, int t1, int t2, int t3)
            {
                var a1Match = a1 == t1 || a1 == t2 || a1 == t3;
                var a2Match = a2 == t1 || a2 == t2 || a2 == t3;
                return a1Match && a2Match;
            }

            for (var i = 0; i < tris.Length; i += 3)
            {
                if (i == triangleStart)
                    continue;

                var t1 = tris[i];
                var t2 = tris[i + 1];
                var t3 = tris[i + 2];
                if (TriangleMatches(indexA, indexB, t1, t2, t3))
                {
                    nextTriangleStart = i;
                    break;
                }
            }

            return nextTriangleStart == -1 ? TriangleSearchResult.EdgeTermination : TriangleSearchResult.Continues;
        }
    }
}