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
    using UnityEngine;
    using UnityEngine.Rendering;

    public static class Triangulation
    {
        public static Mesh TiangulateMultiPolygon(List<List<Vertex>> polys, string debugName = null)
        {
            var tris = new List<Triangle>();
            foreach (var poly in polys)
            {
                if (poly.Count < 3)
                    continue;

                var triangles = TriangulateDelaunay(poly, debugName);
                if (triangles == null)
                    continue;

                tris.AddRange(triangles);
            }

            if (tris.Count == 0)
                return null;

            var mesh = MeshUtils.TrisToMesh(tris);
            MeshUtils.WeldVertices(mesh);
            return mesh;
        }

        public static List<Triangle> TriangulateEarClipping(List<Vertex> vertices, string debugName = null, bool recursiveCall = false)
        {
            var tris = new List<Triangle>();
            var volatileVertices = new List<Vertex>(vertices);

            if (volatileVertices.Count == 3)
            {
                tris.Add(new Triangle(volatileVertices[0], volatileVertices[1], volatileVertices[2]));
                return tris;
            }

            for (var i = 0; i < volatileVertices.Count; i++)
            {
                volatileVertices[i].previous = volatileVertices[MeshUtils.LoopIndex(i - 1, volatileVertices.Count)];
                volatileVertices[i].next = volatileVertices[MeshUtils.LoopIndex(i + 1, volatileVertices.Count)];
            }

            var ears = new List<Vertex>();

            foreach (var vert in volatileVertices)
            {
                MarkIfReflex(vert);
                MeshUtils.AddToListIfEar(vert, volatileVertices, ears);
            }

            while (true)
            {
                if (volatileVertices.Count == 3)
                {
                    tris.Add(new Triangle(volatileVertices[0], volatileVertices[0].previous, volatileVertices[0].next));
                    break;
                }

                var index = -1;
                var minDist = float.MaxValue;
                for (var i = 0; i < ears.Count; ++i)
                {
                    var point = ears[i];
                    var d01 = Vector3.Distance(point.Position, point.previous.Position);
                    var d02 = Vector3.Distance(point.Position, point.next.Position);
                    var d12 = Vector3.Distance(point.previous.Position, point.next.Position);
                    var localMax = Mathf.Max(d01, Mathf.Max(d02, d12));
                    
                    if (localMax < minDist)
                    {
                        minDist = localMax;
                        index = i;
                    }
                }

                if (ears.Count == 0 || index == -1)
                {
                    if (recursiveCall)
                    {
                        var exceptionMsg = "Can't triangulate poly or its convex hull - terminating.";
                        if (!string.IsNullOrEmpty(debugName))
                            exceptionMsg += $" ({debugName})";

                        throw new Exception(exceptionMsg);
                    }

                    var warningMsg = "Unsupported mesh shape found, falling back to convex hull.";
                    if (!string.IsNullOrEmpty(debugName))
                        warningMsg += $" ({debugName})";

                    Debug.LogWarning(warningMsg);

                    // Poly is invalid - create convex hull and return it instead
                    var hull = MeshUtils.GetConvexHull(vertices);
                    return hull == null ? null : TriangulateEarClipping(hull, debugName, true);
                }

                var current = ears[index];
                var prev = current.previous;
                var next = current.next;

                tris.Add(new Triangle(current, prev, next));

                ears.Remove(current);
                volatileVertices.Remove(current);

                prev.next = next;
                next.previous = prev;

                MarkIfReflex(prev);
                MarkIfReflex(next);

                ears.Remove(prev);
                ears.Remove(next);

                MeshUtils.AddToListIfEar(prev, volatileVertices, ears);
                MeshUtils.AddToListIfEar(next, volatileVertices, ears);
            }

            return tris;
        }

        public static List<HalfEdge> CreateHalfEdgeStructure(List<Triangle> triangles)
        {
            OrientTrianglesClockwise(triangles);

            var halfEdges = new List<HalfEdge>(triangles.Count * 3);

            foreach (var tri in triangles)
            {
                var e0 = new HalfEdge(tri.v0);
                var e1 = new HalfEdge(tri.v1);
                var e2 = new HalfEdge(tri.v2);

                e0.triangle = tri;
                e1.triangle = tri;
                e2.triangle = tri;

                e0.previous = e2;
                e1.previous = e0;
                e2.previous = e1;

                e0.next = e1;
                e1.next = e2;
                e2.next = e0;

                halfEdges.Add(e0);
                halfEdges.Add(e1);
                halfEdges.Add(e2);
            }

            for (var i = 0; i < halfEdges.Count; ++i)
            {
                var hEdge = halfEdges[i];

                var v0 = hEdge.previous.vertex;
                var v1 = hEdge.vertex;

                for (var j = 0; j < halfEdges.Count; ++j)
                {
                    if (i == j)
                        continue;

                    var hOpp = halfEdges[j];

                    if (v0.ApproximatelyEquals(hOpp.vertex) && v1.ApproximatelyEquals(hOpp.previous.vertex))
                    {
                        hEdge.opposite = hOpp;
                        break;
                    }
                }
            }

            return halfEdges;
        }

        public static List<Triangle> TriangulateDelaunay(List<Vertex> vertices, string debugName = null)
        {
            var tris = TriangulateEarClipping(vertices, debugName);
            if (tris == null)
                return null;
            
            var halfEdges = CreateHalfEdgeStructure(tris);
            var iteration = 0;

            while (true)
            {
                var flipped = false;

                foreach (var current in halfEdges)
                {
                    if (current.opposite == null)
                        continue;

                    var v0 = current.vertex;
                    var v1 = current.next.vertex;
                    var v2 = current.previous.vertex;
                    var v3 = current.opposite.next.vertex;

                    if (MeshUtils.GetCircleRelativeDeterminant(v0, v1, v2, v3) < 0f &&
                        CanFlip(v0, v1, v2, v3) &&
                        MeshUtils.GetCircleRelativeDeterminant(v1, v2, v3, v0) >= 0f)
                    {
                        current.Flip();
                        flipped = true;
                    }
                }

                if (!flipped)
                    break;

                if (++iteration > 65536)
                    throw new Exception("Infinite loop encountered during Delaunay triangle swapping.");
            }

            return tris;
        }

        private static void MarkIfReflex(Vertex v)
        {
            v.isReflex = MeshUtils.IsTriangleClockwise(v.previous, v, v.next);
        }

        private static void OrientTrianglesClockwise(List<Triangle> triangles)
        {
            foreach (var tri in triangles)
            {
                if (!MeshUtils.IsTriangleClockwise(tri.v0, tri.v1, tri.v2))
                    tri.ChangeOrientation();
            }
        }

        private static bool CanFlip(Vertex v0, Vertex v1, Vertex v2, Vertex v3)
        {
            var verts = ListPool<Vertex>.Get();
            verts.Add(v0);
            verts.Add(v1);
            verts.Add(v2);
            verts.Add(v3);

            var valid = true;

            for (var i = 0; i < verts.Count; ++i)
            {
                var i1 = MeshUtils.LoopIndex(i + 1, verts.Count);
                var i2 = MeshUtils.LoopIndex(i + 2, verts.Count);
                var d1 = verts[i1].Position - verts[i].Position;
                var d2 = verts[i2].Position - verts[i1].Position;

                if (d1.x * d2.z - d1.z * d2.x > 0)
                {
                    valid = false;
                    break;
                }
            }

            ListPool<Vertex>.Release(verts);
            return valid;
        }
    }
}