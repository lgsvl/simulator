/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using Simulator.Plugins;
using Simulator.PointCloud;

namespace Simulator.Editor.PointCloud
{
    public partial class PointCloudImporter : ScriptedImporter
    {
        PointCloudData ImportLaz(AssetImportContext context)
        {
            var name = Path.GetFileName(context.assetPath);

            using (var laz = new Laszip(context.assetPath))
            {
                long count = laz.Count;
                if (count > MaxPointCount)
                {
                    Debug.LogWarning($"Too many points ({count:n0}), truncating to {MaxPointCount:n0}");
                    count = MaxPointCount;
                }

                var bounds = new PointCloudBounds()
                {
                    MinX = laz.MinX,
                    MinY = laz.MinY,
                    MinZ = laz.MinZ,
                    MaxX = laz.MaxX,
                    MaxY = laz.MaxY,
                    MaxZ = laz.MaxZ,
                };

                double scaleX, scaleY, scaleZ;
                double centerX, centerY, centerZ;

                if (Normalize)
                {
                    centerX = -0.5 * (bounds.MaxX + bounds.MinX);
                    centerY = -0.5 * (bounds.MaxY + bounds.MinY);
                    centerZ = -0.5 * (bounds.MaxZ + bounds.MinZ);

                    scaleX = 2.0 / (bounds.MaxX - bounds.MinX);
                    scaleY = 2.0 / (bounds.MaxY - bounds.MinY);
                    scaleZ = 2.0 / (bounds.MaxZ - bounds.MinZ);
                }
                else if (Center)
                {
                    centerX = -0.5 * (bounds.MaxX + bounds.MinX);
                    centerY = -0.5 * (bounds.MaxY + bounds.MinY);
                    centerZ = -0.5 * (bounds.MaxZ + bounds.MinZ);

                    scaleX = scaleY = scaleZ = 1.0;
                }
                else
                {
                    centerX = centerY = centerZ = 0.0;
                    scaleX = scaleY = scaleZ = 1.0;
                }

                bool hasColor = laz.HasColor;
                var transform = GetTransform();

                var points = new PointCloudPoint[(int)count];

                try
                {
                    for (long i = 0; i < count; i++)
                    {
                        if ((i % (128 * 1024)) == 0)
                        {
                            float progress = (float)((double)i / count);
                            EditorUtility.DisplayProgressBar($"Importing {name}", $"{i:N0} points", progress);
                        }

                        var p = laz.GetNextPoint();
                        double x = (p.X + centerX) * scaleX;
                        double y = (p.Y + centerY) * scaleY;
                        double z = (p.Z + centerZ) * scaleZ;

                        var pt = transform.MultiplyVector(new Vector3((float)x, (float)y, (float)z));

                        byte intensity;
                        if (LasRGB8BitWorkaround)
                        {
                            intensity = (byte)(p.Intensity >> 0);
                        }
                        else
                        {
                            intensity = (byte)(p.Intensity >> 8);
                        }

                        uint color = (uint)(intensity << 24);
                        if (hasColor)
                        {
                            if (LasRGB8BitWorkaround)
                            {
                                byte r = (byte)(p.Red >> 0);
                                byte g = (byte)(p.Green >> 0);
                                byte b = (byte)(p.Blue >> 0);
                                color |= (uint)((b << 16) | (g << 8) | r);
                            }
                            else
                            {
                                byte r = (byte)(p.Red >> 8);
                                byte g = (byte)(p.Green >> 8);
                                byte b = (byte)(p.Blue >> 8);
                                color |= (uint)((b << 16) | (g << 8) | r);
                            }
                        }
                        else
                        {
                            color |= (uint)((intensity << 16) | (intensity << 8) | intensity);
                        }

                        points[i] = new PointCloudPoint()
                        {
                            Position = pt,
                            Color = color,
                        };
                    }
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }

                var unityBounds = GetBounds(bounds);
                unityBounds.center = transform.MultiplyPoint3x4(unityBounds.center);
                unityBounds.extents = transform.MultiplyVector(unityBounds.extents);

                return PointCloudData.Create(points, unityBounds, hasColor, transform.MultiplyPoint3x4(bounds.Center), transform.MultiplyVector(bounds.Extents));
            }
        }
    }
}
