/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Editor.PointCloud.Trees
{
    using System.IO;
    using Plugins;
    using Simulator.PointCloud;
    using UnityEditor;
    using UnityEngine;

    public class LazPointProcessor : PointProcessor
    {
        public LazPointProcessor(string filePath) : base(filePath)
        {
        }

        ///<inheritdoc/>
        public override PointCloudBounds CalculateBounds()
        {
            using (var laz = new Laszip(FilePath))
            {
                var bounds = new PointCloudBounds()
                {
                    MinX = laz.MinX,
                    MinY = laz.MinY,
                    MinZ = laz.MinZ,
                    MaxX = laz.MaxX,
                    MaxY = laz.MaxY,
                    MaxZ = laz.MaxZ,
                };

                return bounds;
            }
        }

        public override PointCloudVerticalHistogram GenerateHistogram(PointCloudBounds bounds)
        {
            var fileName = Path.GetFileName(FilePath);
            var result = new PointCloudVerticalHistogram(bounds);

            using (var laz = new Laszip(FilePath))
            {
                long count = laz.Count;

                try
                {
                    for (long i = 0; i < count; i++)
                    {
                        if (i % (1024 * 8) == 0)
                        {
                            var progress = (double) i / count;
                            EditorUtility.DisplayProgressBar(
                                $"Generating histogram ({fileName})",
                                $"{i:N0} points",
                                (float) progress);
                        }

                        var p = laz.GetNextPoint();
                        result.Add(p.Z);
                    }
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }
            }

            return result;
        }

        ///<inheritdoc/>
        public override bool ConvertPoints(NodeProcessorDispatcher target, TransformationData transformationData)
        {
            var fileName = Path.GetFileName(FilePath);
            var progressTitle = $"Processing {fileName}";

            using (var laz = new Laszip(FilePath))
            {
                long count = laz.Count;

                bool hasColor = laz.HasColor;
                var transform = transformationData.TransformationMatrix;

                try
                {
                    for (long i = 0; i < count; i++)
                    {
                        if (i % (1024 * 8) == 0)
                        {
                            var progress = (double) i / count;
                            if (EditorUtility.DisplayCancelableProgressBar(progressTitle, $"{i:N0} points",
                                (float) progress))
                            {
                                return false;
                            }
                        }

                        var p = laz.GetNextPoint();
                        double x = (p.X + transformationData.OutputCenterX) * transformationData.OutputScaleX;
                        double y = (p.Y + transformationData.OutputCenterY) * transformationData.OutputScaleY;
                        double z = (p.Z + transformationData.OutputCenterZ) * transformationData.OutputScaleZ;

                        var pt = transform.MultiplyVector(new Vector3((float) x, (float) y, (float) z));

                        byte intensity;
                        if (transformationData.LasRGB8BitWorkaround)
                        {
                            intensity = (byte) p.Intensity;
                        }
                        else
                        {
                            intensity = (byte) (p.Intensity >> 8);
                        }

                        uint color = (uint) (intensity << 24);
                        if (hasColor)
                        {
                            if (transformationData.LasRGB8BitWorkaround)
                            {
                                byte r = (byte) p.Red;
                                byte g = (byte) p.Green;
                                byte b = (byte) p.Blue;
                                color |= (uint) ((b << 16) | (g << 8) | r);
                            }
                            else
                            {
                                byte r = (byte) (p.Red >> 8);
                                byte g = (byte) (p.Green >> 8);
                                byte b = (byte) (p.Blue >> 8);
                                color |= (uint) ((b << 16) | (g << 8) | r);
                            }
                        }
                        else
                        {
                            color |= (uint) ((intensity << 16) | (intensity << 8) | intensity);
                        }

                        target.AddPoint(new PointCloudPoint()
                        {
                            Position = pt,
                            Color = color,
                        });
                    }
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }
            }

            return true;
        }
    }
}