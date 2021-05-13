/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Editor.MapMeshes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using ClipperLib;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Rendering;

    public static class MeshUtils
    {
        public const float Tolerance = 0.0001f;

        private static Type handleUtilityType;
        private static MethodInfo intersectRayMeshMethod;
        private static object[] intersectRayMeshParams;

        public static List<List<Vertex>> ClipVatti(List<List<Vertex>> clipPolys, List<Vertex> poly)
        {
            var intClipPolys = clipPolys.Select(p => p.Select(x => new IntPoint(x.x * 1000, x.z * 1000, x.y * 1000)).ToList()).ToList();
            var intPoly = poly.Select(x => new IntPoint(x.x * 1000, x.z * 1000, x.y * 1000)).ToList();

            var clipper = new Clipper {ZFillFunction = ZFillFunction};
            clipper.AddPaths(intClipPolys, PolyType.ptSubject, true);
            clipper.AddPath(intPoly, PolyType.ptClip, true);
            var intResult = new List<List<IntPoint>>();
            clipper.Execute(ClipType.ctUnion, intResult, PolyFillType.pftPositive);

            return intResult.Select(intRes => intRes.Select(x => new Vertex(x.X / 1000f, x.Z / 1000f, x.Y / 1000f)).ToList()).ToList();
        }

        public static List<List<Vertex>> ClipVatti(List<List<Vertex>> polys)
        {
            if (polys.Count == 0)
                return polys;

            var first = polys[0];
            var others = new List<List<Vertex>>();
            for (var i = 1; i < polys.Count; ++i)
                others.Add(polys[i]);

            return ClipVatti(others, first);
        }

        private static void ZFillFunction(IntPoint bot1, IntPoint top1, IntPoint bot2, IntPoint top2, ref IntPoint pt)
        {
            var a0 = new Vertex(bot1.X / 1000f, bot1.Z / 1000f, bot1.Y / 1000f);
            var a1 = new Vertex(top1.X / 1000f, top1.Z / 1000f, top1.Y / 1000f);
            var b0 = new Vertex(bot2.X / 1000f, bot2.Z / 1000f, bot2.Y / 1000f);
            var b1 = new Vertex(top2.X / 1000f, top2.Z / 1000f, top2.Y / 1000f);
            if (AreLinesIntersecting(a0, a1, b0, b1))
                pt.Z = (long) (GetLineLineIntersectionPoint(a0, a1, b0, b1).y * 1000);
            else
                pt.Z = (bot1.Z + bot2.Z + top1.Z + top2.Z) / 4;
        }

        public static bool IntersectRayMesh(Ray ray, Mesh mesh, Matrix4x4 matrix, out RaycastHit hit)
        {
            if (handleUtilityType == null || intersectRayMeshMethod == null)
            {
                handleUtilityType = typeof(Editor).Assembly.GetTypes().FirstOrDefault(t => t.Name == "HandleUtility");
                if (handleUtilityType == null)
                    throw new Exception("HandleUtility type not found. Did Unity version change?");

                intersectRayMeshMethod = handleUtilityType.GetMethod("IntersectRayMesh", (BindingFlags.Static | BindingFlags.NonPublic));
                if (intersectRayMeshMethod == null)
                    throw new Exception("IntersectRayMesh method not found. Did Unity version change?");
            }

            if (intersectRayMeshParams == null)
                intersectRayMeshParams = new object[4];

            intersectRayMeshParams[0] = ray;
            intersectRayMeshParams[1] = mesh;
            intersectRayMeshParams[2] = matrix;
            intersectRayMeshParams[3] = null;
            var result = (bool) intersectRayMeshMethod.Invoke(null, intersectRayMeshParams);
            hit = intersectRayMeshParams[3] as RaycastHit? ?? default;
            return result;
        }

        public static List<List<Vertex>> OptimizePoly(List<List<Vertex>> polys, string debugName = null)
        {
            var tris = new List<Triangle>();
            foreach (var poly in polys)
            {
                if (poly.Count < 3)
                    continue;

                var triangles = Triangulation.TriangulateDelaunay(poly, debugName);
                if (triangles == null)
                    continue;

                tris.AddRange(triangles);
            }

            if (tris.Count == 0)
                return null;

            var mesh = TrisToMesh(tris);

            WeldVertices(mesh);
            return GetEdgeVerticesIgnoreHoles(mesh, debugName);
        }

        public static List<List<Vertex>> OptimizePoly(List<Vertex> poly, string debugName = null)
        {
            var tris = Triangulation.TriangulateDelaunay(poly, debugName);
            if (tris == null)
            {
                var hull = GetConvexHull(poly);
                return hull == null ? null : new List<List<Vertex>> {hull};
            }

            var mesh = TrisToMesh(tris);

            WeldVertices(mesh);
            return GetEdgeVerticesIgnoreHoles(mesh, debugName);
        }

        public static void AddToListIfEar(Vertex v, List<Vertex> vertices, List<Vertex> earVertices)
        {
            if (v.isReflex)
                return;

            if (!vertices.Where(t => t.isReflex).Any(t => IsPointInTriangle(v.previous, v, v.next, t)))
                earVertices.Add(v);
        }

        public static int LoopIndex(int index, int count)
        {
            return (index % count + count) % count;
        }

        public static void RemoveDuplicates(List<Vertex> poly)
        {
            for (var i = 0; i < poly.Count; ++i)
            {
                if (poly[i].ApproximatelyEquals(poly[MeshUtils.LoopIndex(i + 1, poly.Count)]))
                    poly.RemoveAt(i--);

                if (poly.Count <= 3)
                    break;
            }
        }

        public static void WeldVertices(Mesh mesh, float distanceThreshold = 0.01f)
        {
            var verts = mesh.vertices;
            var indices = mesh.triangles;

            var newVerts = new List<Vector3>();
            var newIndicesTemp = new int[indices.Length];
            Array.Copy(indices, newIndicesTemp, indices.Length);

            for (var i = 0; i < verts.Length; ++i)
            {
                var newIndex = -1;
                for (var j = 0; j < newVerts.Count; ++j)
                {
                    if (Vector3.Distance(verts[i], newVerts[j]) < distanceThreshold)
                    {
                        newIndex = j;
                        break;
                    }
                }

                if (newIndex == -1)
                {
                    newIndex = newVerts.Count;
                    newVerts.Add(verts[i]);
                }

                for (var k = 0; k < newIndicesTemp.Length; ++k)
                {
                    if (newIndicesTemp[k] == i)
                        newIndicesTemp[k] = newIndex;
                }
            }

            var newIndices = new List<int>();
            var presentTriangles = new List<Tuple<int, int, int>>();

            for (var i = 0; i < newIndicesTemp.Length; i += 3)
            {
                var v0 = newIndicesTemp[i];
                var v1 = newIndicesTemp[i + 1];
                var v2 = newIndicesTemp[i + 2];

                if (v0 == v1 || v1 == v2 || v0 == v2)
                    continue;

                int min, mid, max;

                if (v0 > v1)
                {
                    min = Mathf.Min(v1, v2);
                    mid = v2 > v1 ? Mathf.Min(v0, v2) : v1;
                    max = Mathf.Max(v0, v2);
                }
                else
                {
                    min = Mathf.Min(v0, v2);
                    mid = v2 > v1 ? v1 : Mathf.Max(v0, v2);
                    max = Mathf.Max(v1, v2);
                }

                var tri = new Tuple<int, int, int>(min, mid, max);
                if (presentTriangles.Contains(tri))
                    continue;

                presentTriangles.Add(tri);

                newIndices.Add(newIndicesTemp[i]);
                newIndices.Add(newIndicesTemp[i + 1]);
                newIndices.Add(newIndicesTemp[i + 2]);
            }

            mesh.Clear();
            mesh.SetVertices(newVerts);
            mesh.SetIndices(newIndices, MeshTopology.Triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.Optimize();
        }

        public static List<List<Vertex>> GetEdgeVerticesIgnoreHoles(Mesh mesh, string debugName = null)
        {
            var edgeDict = new Dictionary<Tuple<int, int>, bool>();
            var verts = mesh.vertices;
            var indices = mesh.triangles;

            for (var i = 0; i < indices.Length; i += 3)
            {
                for (var j = 0; j < 3; ++j)
                {
                    var index = indices[i + j];
                    var nextIndex = indices[i + LoopIndex(j + 1, 3)];
                    edgeDict[new Tuple<int, int>(index, nextIndex)] = true;
                }
            }

            for (var i = 0; i < indices.Length; i += 3)
            {
                for (var j = 0; j < 3; ++j)
                {
                    var index = indices[i + j];
                    var nextIndex = indices[i + LoopIndex(j + 1, 3)];
                    var key = new Tuple<int, int>(nextIndex, index);
                    if (edgeDict.ContainsKey(key))
                        edgeDict[key] = false;
                }
            }

            var result = new List<List<Vertex>>();
            var newVerts = new List<Vertex>();
            var usedIndices = new List<int>();

            bool TryFindBestUnusedVertex(out KeyValuePair<Tuple<int, int>, bool> unusedVert)
            {
                var found = false;
                KeyValuePair<Tuple<int, int>, bool> firstKvp;

                foreach (var kvp in edgeDict)
                {
                    if (!kvp.Value || usedIndices.Contains(kvp.Key.Item1))
                        continue;

                    var thisVert = verts[kvp.Key.Item1];
                    var pointIsInside = false;

                    foreach (var finishedPoly in result)
                    {
                        if (IsPointInPolygon(finishedPoly, thisVert))
                        {
                            usedIndices.Add(kvp.Key.Item1);
                            pointIsInside = true;
                            break;
                        }
                    }

                    if (pointIsInside)
                        continue;

                    if (!found)
                    {
                        found = true;
                        firstKvp = kvp;
                        continue;
                    }

                    var firstVert = verts[firstKvp.Key.Item1];

                    if (thisVert.x < firstVert.x || Mathf.Approximately(thisVert.x, firstVert.x) && thisVert.z < firstVert.z)
                        firstKvp = kvp;
                }

                unusedVert = found ? firstKvp : default;
                return found;
            }

            var usedEdges = new List<KeyValuePair<Tuple<int, int>, bool>>();

            while (TryFindBestUnusedVertex(out var bestKvp))
            {
                var firstIndex = bestKvp.Key.Item1;
                var currentIndex = bestKvp.Key.Item2;
                var iteration = 0;
                newVerts.Add(new Vertex(verts[currentIndex]));
                usedIndices.Add(currentIndex);

                while (currentIndex != firstIndex)
                {
                    var nextItem = edgeDict.FirstOrDefault(x => x.Value && x.Key.Item1 == currentIndex && !usedEdges.Contains(x));
                    var def = default(KeyValuePair<Tuple<int, int>, bool>);
                    if (nextItem.Equals(def))
                    {
                        // TODO: zero-surface vertices welded together break this, review
                        var warningMsg = "Unsupported mesh shape found, falling back to convex hull.";
                        if (!string.IsNullOrEmpty(debugName))
                            warningMsg += $" ({debugName})";
                        Debug.LogWarning(warningMsg);
                        
                        result.Clear();
                        var allVerts = verts.Select(x => new Vertex(x)).ToList();
                        result.Add(MeshUtils.GetConvexHull(allVerts));
                        return result;
                    }

                    currentIndex = nextItem.Key.Item2;
                    newVerts.Add(new Vertex(verts[currentIndex]));
                    usedIndices.Add(currentIndex);
                    usedEdges.Add(nextItem);

                    if (++iteration > 65536)
                        throw new Exception("Infinite loop encountered when looking for external edges.");
                }

                newVerts.Reverse();
                result.Add(new List<Vertex>(newVerts));
                newVerts.Clear();
            }

            return result;
        }

        public static Mesh TrisToMesh(List<Triangle> tris, bool weldVertices = true)
        {
            var verts = new List<Vector3>();
            var indices = new List<int>();

            foreach (var triangle in tris)
            {
                var vertCount = verts.Count;

                verts.Add(triangle.v0.Position);
                verts.Add(triangle.v1.Position);
                verts.Add(triangle.v2.Position);

                indices.Add(vertCount);
                indices.Add(vertCount + 1);
                indices.Add(vertCount + 2);
            }

            var mesh = new Mesh();
            mesh.SetVertices(verts);
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            if (weldVertices)
            {
                WeldVertices(mesh);
                return mesh;
            }

            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.Optimize();
            return mesh;
        }

        public static bool IsPointInTriangle(Vertex t0, Vertex t1, Vertex t2, Vertex point)
        {
            var den = (t0.x - t2.x) * (t1.z - t2.z) + (t2.x - t1.x) * (t0.z - t2.z);
            var a = ((t1.z - t2.z) * (point.x - t2.x) + (t2.x - t1.x) * (point.z - t2.z)) / den;
            var b = ((t2.z - t0.z) * (point.x - t2.x) + (t0.x - t2.x) * (point.z - t2.z)) / den;
            var c = 1 - a - b;
            return a > 0f && a < 1f && b > 0f && b < 1f && c > 0f && c < 1f;
        }

        public static bool IsTriangleClockwise(Vertex t0, Vertex t1, Vertex t2)
        {
            return (t0.x * t1.z + t2.x * t0.z + t1.x * t2.z - t0.x * t2.z - t2.x * t1.z - t1.x * t0.z) <= 0;
        }

        public static bool AreLinesIntersecting(Vertex a0, Vertex a1, Vertex b0, Vertex b1)
        {
            return AreLinesIntersecting(a0.Position, a1.Position, b0.Position, b1.Position);
        }

        public static bool AreLinesIntersecting(Vector3 a0, Vector3 a1, Vector3 b0, Vector3 b1)
        {
            var den = (a1.x - a0.x) * (b1.z - b0.z) - (a1.z - a0.z) * (b1.x - b0.x);
            if (Mathf.Abs(den) < Tolerance)
                return false;

            var a = ((a0.z - b0.z) * (b1.x - b0.x) - (a0.x - b0.x) * (b1.z - b0.z)) / den;
            var b = ((a0.z - b0.z) * (a1.x - a0.x) - (a0.x - b0.x) * (a1.z - a0.z)) / den;

            return a >= 0f && a <= 1f && b >= 0f && b <= 1f;
        }

        public static Vertex GetLineLineIntersectionPoint(Vertex a0, Vertex a1, Vertex b0, Vertex b1)
        {
            return new Vertex(GetLineLineIntersectionPoint(a0.Position, a1.Position, b0.Position, b1.Position));
        }

        public static Vector3 GetLineLineIntersectionPoint(Vector3 a0, Vector3 a1, Vector3 b0, Vector3 b1)
        {
            var den = (a1.x - a0.x) * (b1.z - b0.z) - (a1.z - a0.z) * (b1.x - b0.x);
            if (Mathf.Abs(den) < Tolerance)
                throw new Exception($"Lines are parallel. Check if lines are intersecting first with {nameof(AreLinesIntersecting)}");

            var l = ((a0.z - b0.z) * (b1.x - b0.x) - (a0.x - b0.x) * (b1.z - b0.z)) / den;
            return a0 + l * (a1 - a0);
        }

        public static bool IsPointInPolygon(List<Vertex> poly, Vector3 point)
        {
            var xMaxPos = poly[0].Position;

            foreach (var polyPoint in poly)
            {
                if (polyPoint.x > xMaxPos.x)
                    xMaxPos = polyPoint.Position;
            }

            var refPoint = xMaxPos + Vector3.right;
            return poly.Where((t, i) => AreLinesIntersecting(point, refPoint, t.Position, poly[LoopIndex(i + 1, poly.Count)].Position)).Count() % 2 != 0;
        }

        public static bool IsAnyPolyPointInsideTriangle(Vertex v0, Vertex v1, Vertex v2, List<Vertex> poly)
        {
            var triangle = ListPool<Vertex>.Get();
            triangle.Add(v0);
            triangle.Add(v1);
            triangle.Add(v2);

            bool pointInside = false;

            foreach (var polyPoint in poly)
            {
                if (polyPoint == v0 || polyPoint == v1 || polyPoint == v2)
                    continue;

                if (IsPointInPolygon(triangle, polyPoint))
                {
                    pointInside = true;
                    break;
                }
            }

            ListPool<Vertex>.Release(triangle);

            return pointInside;
        }

        public static bool IsPointInPolygon(List<Vertex> poly, Vertex point)
        {
            return IsPointInPolygon(poly, point.Position);
        }

        public static List<Vertex> GetConvexHull(List<Vertex> points)
        {
            if (points.Count < 3)
                throw new Exception("Creating convex hull requires at least 2 points.");

            if (points.Count == 3)
                return points;

            var pointsVolatile = new List<Vertex>(points);
            var convexHullPoly = new List<Vertex>();
            var xzMinPos = pointsVolatile[0];

            for (var i = 1; i < pointsVolatile.Count; i++)
            {
                if (pointsVolatile[i].x < xzMinPos.x || Math.Abs(pointsVolatile[i].x - xzMinPos.x) < Tolerance && pointsVolatile[i].z < xzMinPos.z)
                    xzMinPos = pointsVolatile[i];
            }

            convexHullPoly.Add(xzMinPos);
            pointsVolatile.Remove(xzMinPos);
            var currentPoint = convexHullPoly[0];
            var colinearPoints = new List<Vertex>();
            var iteration = 0;

            while (true)
            {
                if (iteration == 2)
                    pointsVolatile.Add(convexHullPoly[0]);

                if (pointsVolatile.Count == 0)
                    return null;

                var nextPoint = pointsVolatile[0];

                for (var i = 1; i < pointsVolatile.Count; ++i)
                {
                    var det = GetLineRelativeDeterminant(currentPoint, nextPoint, pointsVolatile[i]);

                    if (Mathf.Abs(det) < Tolerance)
                        colinearPoints.Add(pointsVolatile[i]);

                    else if (det < 0f)
                    {
                        nextPoint = pointsVolatile[i];
                        colinearPoints.Clear();
                    }
                }

                if (colinearPoints.Count > 0)
                {
                    colinearPoints.Add(nextPoint);
                    colinearPoints = colinearPoints.OrderBy(x => Vector3.SqrMagnitude(x.Position - currentPoint.Position)).ToList();

                    convexHullPoly.AddRange(colinearPoints);
                    currentPoint = colinearPoints[colinearPoints.Count - 1];

                    foreach (var point in colinearPoints)
                        pointsVolatile.Remove(point);

                    colinearPoints.Clear();
                }
                else
                {
                    convexHullPoly.Add(nextPoint);
                    pointsVolatile.Remove(nextPoint);
                    currentPoint = nextPoint;
                }

                if (ReferenceEquals(currentPoint, convexHullPoly[0]))
                {
                    convexHullPoly.RemoveAt(convexHullPoly.Count - 1);
                    break;
                }

                if (++iteration > 65536)
                    throw new Exception("Infinite loop encountered when looking for convex hull.");
            }

            RemoveDuplicates(convexHullPoly);
            return convexHullPoly;
        }

        public static float GetLineRelativeDeterminant(Vertex a0, Vertex a1, Vertex point)
        {
            return (a0.x - point.x) * (a1.z - point.z) - (a0.z - point.z) * (a1.x - point.x);
        }

        public static float GetCircleRelativeDeterminant(Vertex v0, Vertex v1, Vertex v2, Vertex v3)
        {
            var x03 = v0.x - v3.x;
            var x13 = v1.x - v3.x;
            var x23 = v2.x - v3.x;

            var z03 = v0.z - v3.z;
            var z13 = v1.z - v3.z;
            var z23 = v2.z - v3.z;

            var a = x03 * x03 + z03 * z03;
            var b = x13 * x13 + z13 * z13;
            var c = x23 * x23 + z23 * z23;

            return x03 * z13 * c + z03 * b * x23 + a * x13 * z23 - x23 * z13 * a - z23 * b * x03 - c * x13 * z03;
        }

        public static float PointLaneDistance(Vector3 p, List<Vector3> line, out int vertexIndex)
        {
            const float threshold = 0.05f;
            vertexIndex = 0;
            var closest = float.MaxValue;
            for (var i = 1; i < line.Count; ++i)
            {
                var dist = PointLineDistance(p, line[i - 1], line[i]);

                if (dist < closest)
                {
                    vertexIndex = i;
                    closest = dist;
                }

                if (dist < threshold)
                    return dist;
            }

            return closest;
        }

        private static float PointLineDistance(Vector3 p, Vector3 a0, Vector3 a1)
        {
            var aX = a1.x - a0.x;
            var aY = a1.y - a0.y;
            var aZ = a1.z - a0.z;

            if (Mathf.Approximately(aX, 0f) && Mathf.Approximately(aZ, 0f))
            {
                aX = p.x - a0.x;
                aZ = p.z - a0.z;
                return Mathf.Sqrt(aX * aX + aZ * aZ);
            }

            var t = ((p.x - a0.x) * aX + (p.z - a0.z) * aZ) / (aX * aX + aZ * aZ);

            if (t < 0)
            {
                aX = p.x - a0.x;
                aZ = p.z - a0.z;
            }
            else if (t > 1)
            {
                aX = p.x - a1.x;
                aZ = p.z - a1.z;
            }
            else
            {
                var closest = new Vector3(a0.x + t * aX, a0.y + t * aY, a0.z + t * aZ);
                aX = p.x - closest.x;
                aZ = p.z - closest.z;
            }

            return Mathf.Sqrt(aX * aX + aZ * aZ);
        }
    }
}