/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.IO;
using UnityEngine;
using UnityEditor.Experimental.AssetImporters;
using Unity.Collections.LowLevel.Unsafe;
using Simulator.PointCloud;

namespace Simulator.Editor.PointCloud
{
    public enum PointCloudImportAxes
    {
        X_Right_Y_Up_Z_Forward = 0,
        X_Right_Z_Up_Y_Forward = 1,
    }

    [ScriptedImporter(1, new[] { "pcd", "ply", "las", "laz" })]
    public partial class PointCloudImporter : ScriptedImporter
    {
        readonly long MaxPointCount = 2000 * 1024 * 1024 / UnsafeUtility.SizeOf<PointCloudPoint>();

        public PointCloudImportAxes Axes = PointCloudImportAxes.X_Right_Z_Up_Y_Forward;

        public bool Center = true;

        public bool Normalize = false;

        public bool LasRGB8BitWorkaround = false;

        public override void OnImportAsset(AssetImportContext context)
        {
            PointCloudData data;

            var ext = Path.GetExtension(context.assetPath);
            try
            {
                if (ext == ".pcd")
                {
                    data = ImportPcd(context);
                }
                else if (ext == ".ply")
                {
                    data = ImportPly(context);
                }
                else if (ext == ".las")
                {
                    data = ImportLas(context);
                }
                else if (ext == ".laz")
                {
                    data = ImportLaz(context);
                }
                else
                {
                    context.LogImportError("Unsupported Point Cloud format");
                    return;
                }
            }
            catch (Exception ex)
            {
                context.LogImportError(ex.Message);
                return;
            }

            Debug.Assert(data != null);
            data.name = Path.GetFileNameWithoutExtension(context.assetPath);

            var obj = new GameObject();
            var renderer = obj.AddComponent<PointCloudRenderer>();
            renderer.Data = data;

            if (data.HasColor)
            {
                renderer.Colorize = PointCloudRenderer.ColorizeType.Colors;
            }

            if (data.Count > 16 * 1000 * 1000)
            {
                renderer.ConstantSize = true;
                renderer.PixelSize = 1.0f;
            }

            context.AddObjectToAsset("prefab", obj);
            context.AddObjectToAsset("data", data);

            context.SetMainObject(obj);
        }

        Matrix4x4 GetTransform()
        {
            if (Axes == PointCloudImportAxes.X_Right_Y_Up_Z_Forward)
            {
                return Matrix4x4.identity;
            }
            else if (Axes == PointCloudImportAxes.X_Right_Z_Up_Y_Forward)
            {
                return new Matrix4x4(new Vector4(1, 0, 0), new Vector4(0, 0, 1), new Vector4(0, 1, 0), Vector4.zero);
            }
            else
            {
                Debug.Assert(false);
                return Matrix4x4.identity;
            }
        }

        struct PointCloudBounds
        {
            public double MinX;
            public double MinY;
            public double MinZ;

            public double MaxX;
            public double MaxY;
            public double MaxZ;

            public bool IsValid => MinX <= MaxX && MinY <= MaxY && MinZ <= MaxZ;

            public Vector3 Center =>
                new Vector3((float)((MaxX + MaxX) * 0.5), (float)((MaxY + MinY) * 0.5), (float)((MaxZ + MaxZ) * 0.5));

            public Vector3 Extents =>
                new Vector3((float)((MaxX - MinX) * 0.5), (float)((MaxY - MinY) * 0.5), (float)((MaxZ - MinZ) * 0.5));

            public static readonly PointCloudBounds Empty = new PointCloudBounds()
            {
                MinX = double.MaxValue,
                MinY = double.MaxValue,
                MinZ = double.MaxValue,
                MaxX = double.MinValue,
                MaxY = double.MinValue,
                MaxZ = double.MinValue,
            };

            public void Add(double x, double y, double z)
            {
                MinX = Math.Min(MinX, x);
                MinY = Math.Min(MinY, y);
                MinZ = Math.Min(MinZ, z);
                MaxX = Math.Max(MaxX, x);
                MaxY = Math.Max(MaxY, y);
                MaxZ = Math.Max(MaxZ, z);
            }
        }

        Bounds GetBounds(PointCloudBounds bounds)
        {
            Bounds result = new Bounds();
            if (Normalize)
            {
                result.center = Vector3.zero;
                result.extents = Vector3.one;
            }
            else if (Center)
            {
                result.center = Vector3.zero;
                result.extents = new Vector3(
                    (float)(bounds.MaxX - bounds.MinX) * 0.5f,
                    (float)(bounds.MaxY - bounds.MinY) * 0.5f,
                    (float)(bounds.MaxZ - bounds.MinZ) * 0.5f);
            }
            else
            {
                result.SetMinMax(
                    new Vector3((float)bounds.MinX, (float)bounds.MinY, (float)bounds.MinZ),
                    new Vector3((float)bounds.MaxX, (float)bounds.MaxY, (float)bounds.MaxZ));
            }
            return result;
        }
    }
}
