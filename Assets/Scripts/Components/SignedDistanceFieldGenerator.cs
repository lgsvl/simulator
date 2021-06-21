/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Components
{
    using PointCloud.Trees;
    using Utilities;
    using UnityEngine;
    using UnityEngine.Rendering;

    public static class SignedDistanceFieldGenerator
    {
        public class SignedDistanceFieldData
        {
            public RenderTexture texture;
            public Bounds bounds;
            public float step;
            public bool[][][] voxels;
        }

        private class Triangle
        {
            public readonly Vector3 v0, v1, v2;
            public readonly Bounds bounds;

            public Triangle(Vector3 v0, Vector3 v1, Vector3 v2)
            {
                this.v0 = v0;
                this.v1 = v1;
                this.v2 = v2;

                var min = Vector3.Min(Vector3.Min(v0, v1), v2);
                var max = Vector3.Max(Vector3.Max(v0, v1), v2);

                bounds.SetMinMax(min, max);
            }
        }

        public static void CalculateSize(GameObject obj, int maxResolution, out Vector3Int resolution, out Bounds bounds, out float step)
        {
            var meshFilters = obj.GetComponentsInChildren<MeshFilter>();
            var aabb = new Bounds();

            foreach (var meshFilter in meshFilters)
                EncapsulateWorldSpace(meshFilter, ref aabb);

            aabb.Expand(2f);
            var maxDist = Mathf.Max(Mathf.Max(aabb.size.x, aabb.size.y), aabb.size.z);
            step = maxDist / maxResolution;

            var size = aabb.size;
            var add = Vector3.zero;
            for (var i = 0; i < 3; ++i)
            {
                var diff = Mathf.Ceil(size[i] / step) * step - size[i];
                add[i] = diff;
            }

            aabb.Expand(add);
            size = aabb.size;

            var width = Mathf.CeilToInt(size.x / step);
            var height = Mathf.CeilToInt(size.y / step);
            var depth = Mathf.CeilToInt(size.z / step);

            resolution = new Vector3Int(width, height, depth);
            bounds = aabb;
        }

        public static SignedDistanceFieldData Generate(GameObject obj, ComputeShader cs, Vector3Int resolution, Bounds bounds, float step)
        {
            var width = resolution.x;
            var height = resolution.y;
            var depth = resolution.z;

            var meshFilters = obj.GetComponentsInChildren<MeshFilter>();

            var voxelRes = new[] {width, height, depth};

            var resVoxels = new bool[width][][];
            for (var i = 0; i < width; ++i)
            {
                resVoxels[i] = new bool[height][];
                for (var j = 0; j < height; ++j)
                {
                    resVoxels[i][j] = new bool[depth];
                }
            }

            foreach (var meshFilter in meshFilters)
                Voxelize(meshFilter, bounds, resolution, step, resVoxels);

            var count = width * height * depth;
            var bufferCpu = new float[count];

            for (var x = 0; x < width; ++x)
            {
                for (var z = 0; z < depth; ++z)
                {
                    var filled = false;

                    for (var y = height - 1; y >= 0; --y)
                    {
                        var full = resVoxels[x][y][z] || filled;
                        if (!filled && full)
                            filled = true;

                        var pos = TreeUtility.Flatten(x, y, z, voxelRes);
                        bufferCpu[pos] = full ? 1f : 0f;
                    }
                }
            }

            var buffer = new ComputeBuffer(count, sizeof(float));
            buffer.SetData(bufferCpu);

            var texture = new RenderTexture(width, height, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Default)
            {
                dimension = TextureDimension.Tex3D,
                volumeDepth = depth,
                enableRandomWrite = true,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.Create();

            var initializeKernel = cs.FindKernel("Initialize");
            cs.SetTexture(initializeKernel, "_Texture", texture);
            cs.SetBuffer(initializeKernel, "_VoxelBuffer", buffer);
            cs.SetVector("_TexSize", new Vector4(width, height, depth, 0f));
            cs.Dispatch(initializeKernel, HDRPUtilities.GetGroupSize(width, 8),
                HDRPUtilities.GetGroupSize(height, 8), HDRPUtilities.GetGroupSize(depth, 8));

            buffer.Dispose();

            return new SignedDistanceFieldData()
            {
                texture = texture,
                bounds = bounds,
                voxels = resVoxels,
                step = step
            };
        }

        private static void Voxelize(MeshFilter meshFilter, Bounds bounds, Vector3Int resolution, float step, bool[][][] voxels)
        {
            var width = resolution.x;
            var height = resolution.y;
            var depth = resolution.z;

            var voxelBounds = new Bounds[width][][];
            for (var i = 0; i < width; ++i)
            {
                voxelBounds[i] = new Bounds[height][];
                for (var j = 0; j < height; ++j)
                {
                    voxelBounds[i][j] = new Bounds[depth];
                }
            }

            var voxelSize = Vector3.one * step;
            var vecHalf = Vector3.one * 0.5f;

            for (var x = 0; x < width; ++x)
            {
                for (var y = 0; y < height; ++y)
                {
                    for (var z = 0; z < depth; ++z)
                    {
                        var p = bounds.min + (new Vector3(x, y, z) + vecHalf) * step;
                        var aabb = new Bounds(p, voxelSize);
                        voxelBounds[x][y][z] = aabb;
                    }
                }
            }

            var mesh = meshFilter.sharedMesh;
            var vertices = mesh.vertices;
            var indices = mesh.triangles;

            for (var i = 0; i < indices.Length; i += 3)
            {
                var tri = new Triangle(
                    meshFilter.transform.TransformPoint(vertices[indices[i]]),
                    meshFilter.transform.TransformPoint(vertices[indices[i + 1]]),
                    meshFilter.transform.TransformPoint(vertices[indices[i + 2]])
                );

                var min = tri.bounds.min - bounds.min;
                var max = tri.bounds.max - bounds.min;
                var sX = Mathf.Clamp(Mathf.FloorToInt(min.x / step), 0, width - 1);
                var sY = Mathf.Clamp(Mathf.FloorToInt(min.y / step), 0, height - 1);
                var sZ = Mathf.Clamp(Mathf.FloorToInt(min.z / step), 0, depth - 1);
                var eX = Mathf.Clamp(Mathf.CeilToInt(max.x / step), 0, width - 1);
                var eY = Mathf.Clamp(Mathf.CeilToInt(max.y / step), 0, height - 1);
                var eZ = Mathf.Clamp(Mathf.CeilToInt(max.z / step), 0, depth - 1);

                for (var x = sX; x <= eX; ++x)
                {
                    for (var y = sY; y <= eY; ++y)
                    {
                        for (var z = sZ; z <= eZ; ++z)
                        {
                            if (Intersects(tri, voxelBounds[x][y][z]))
                                voxels[x][y][z] = true;
                        }
                    }
                }
            }
        }

        private static bool Intersects(Triangle tri, Bounds bounds)
        {
            var c = bounds.center;
            var hSize = bounds.max - c;

            var v0 = tri.v0 - c;
            var v1 = tri.v1 - c;
            var v2 = tri.v2 - c;

            var f0 = v1 - v0;
            var f1 = v2 - v1;
            var f2 = v0 - v2;

            var a0 = new Vector3(Mathf.Abs(f0.x), Mathf.Abs(f0.y), Mathf.Abs(f0.z));
            var a1 = new Vector3(Mathf.Abs(f1.x), Mathf.Abs(f1.y), Mathf.Abs(f1.z));
            var a2 = new Vector3(Mathf.Abs(f2.x), Mathf.Abs(f2.y), Mathf.Abs(f2.z));

            var a00 = new Vector3(0, -f0.z, f0.y);
            var a01 = new Vector3(0, -f1.z, f1.y);
            var a02 = new Vector3(0, -f2.z, f2.y);
            var a10 = new Vector3(f0.z, 0, -f0.x);
            var a11 = new Vector3(f1.z, 0, -f1.x);
            var a12 = new Vector3(f2.z, 0, -f2.x);
            var a20 = new Vector3(-f0.y, f0.x, 0);
            var a21 = new Vector3(-f1.y, f1.x, 0);
            var a22 = new Vector3(-f2.y, f2.x, 0);

            if (Max(v0.x, v1.x, v2.x) < -hSize.x || Min(v0.x, v1.x, v2.x) > hSize.x) return false;
            if (Max(v0.y, v1.y, v2.y) < -hSize.y || Min(v0.y, v1.y, v2.y) > hSize.y) return false;
            if (Max(v0.z, v1.z, v2.z) < -hSize.z || Min(v0.z, v1.z, v2.z) > hSize.z) return false;
            if (!CheckAxis(v0, v1, v2, a00, hSize, a0.z, a0.y)) return false;
            if (!CheckAxis(v0, v1, v2, a01, hSize, a1.z, a1.y)) return false;
            if (!CheckAxis(v0, v1, v2, a02, hSize, a2.z, a2.y)) return false;
            if (!CheckAxis(v0, v1, v2, a10, hSize, a0.z, a0.x)) return false;
            if (!CheckAxis(v0, v1, v2, a11, hSize, a1.z, a1.x)) return false;
            if (!CheckAxis(v0, v1, v2, a12, hSize, a2.z, a2.x)) return false;
            if (!CheckAxis(v0, v1, v2, a20, hSize, a0.y, a0.x)) return false;
            if (!CheckAxis(v0, v1, v2, a21, hSize, a1.y, a1.x)) return false;
            if (!CheckAxis(v0, v1, v2, a22, hSize, a2.y, a2.x)) return false;

            var normal = Vector3.Cross(f1, f0).normalized;
            var plane = new Plane(normal, Vector3.Dot(normal, tri.v0));
            return Intersects(plane, bounds);
        }

        private static float Max(float f0, float f1, float f2)
        {
            return Mathf.Max(Mathf.Max(f0, f1), f2);
        }

        private static float Min(float f0, float f1, float f2)
        {
            return Mathf.Min(Mathf.Min(f0, f1), f2);
        }

        private static bool CheckAxis(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 axis, Vector3 aabbExtents, float a0, float a1)
        {
            var p0 = Vector3.Dot(v0, axis);
            var p1 = Vector3.Dot(v1, axis);
            var p2 = Vector3.Dot(v2, axis);
            var r = aabbExtents.x * Mathf.Abs(a0) + aabbExtents.y * Mathf.Abs(a1);
            return !(Mathf.Max(-Max(p0, p1, p2), Min(p0, p1, p2)) > r);
        }

        private static bool Intersects(Plane plane, Bounds bounds)
        {
            var a = bounds.extents.x * Mathf.Abs(plane.normal.x) + bounds.extents.y * Mathf.Abs(plane.normal.y) + bounds.extents.z * Mathf.Abs(plane.normal.z);
            var b = Vector3.Dot(plane.normal, bounds.center) - plane.distance;
            return Mathf.Abs(b) <= a;
        }

        private static void EncapsulateWorldSpace(MeshFilter mf, ref Bounds bounds)
        {
            var mesh = mf.sharedMesh;
            var verts = mesh.vertices;
            var trans = mf.transform;

            if (bounds == default)
                bounds = new Bounds(trans.TransformPoint(verts[0]), Vector3.zero);

            foreach (var vert in verts)
                bounds.Encapsulate(trans.TransformPoint(vert));
        }
    }
}