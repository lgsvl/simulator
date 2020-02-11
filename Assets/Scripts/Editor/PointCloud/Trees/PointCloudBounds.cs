/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Editor.PointCloud.Trees
{
    using System;
    using UnityEngine;

    public struct PointCloudBounds
    {
        public double MinX;
        public double MinY;
        public double MinZ;

        public double MaxX;
        public double MaxY;
        public double MaxZ;

        public bool IsValid => MinX <= MaxX && MinY <= MaxY && MinZ <= MaxZ;

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

        public void Encapsulate(PointCloudBounds bounds)
        {
            MinX = Math.Min(MinX, bounds.MinX);
            MinY = Math.Min(MinY, bounds.MinY);
            MinZ = Math.Min(MinZ, bounds.MinZ);
            MaxX = Math.Max(MaxX, bounds.MaxX);
            MaxY = Math.Max(MaxY, bounds.MaxY);
            MaxZ = Math.Max(MaxZ, bounds.MaxZ);
        }

        public Bounds GetUnityBounds(TreeImportSettings settings)
        {
            Bounds result = new Bounds();
            if (settings.normalize)
            {
                result.center = Vector3.zero;
                result.extents = Vector3.one;
            }
            else if (settings.center)
            {
                result.center = Vector3.zero;
                result.extents = new Vector3(
                    (float) (MaxX - MinX) * 0.5f,
                    (float) (MaxY - MinY) * 0.5f,
                    (float) (MaxZ - MinZ) * 0.5f);
            }
            else
            {
                result.SetMinMax(
                    new Vector3((float) MinX, (float) MinY, (float) MinZ),
                    new Vector3((float) MaxX, (float) MaxY, (float) MaxZ));
            }

            return result;
        }
    }
}