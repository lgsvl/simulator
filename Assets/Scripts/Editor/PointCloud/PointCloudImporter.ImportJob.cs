/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;
using Simulator.PointCloud;

namespace Simulator.Editor.PointCloud
{
    enum PointElementType
    {
        Byte,
        Float,
        Double,
    }

    enum PointElementName
    {
        X, Y, Z,
        R, G, B,
        I,
    }

    struct PointElement
    {
        public PointElementType Type;
        public PointElementName Name;
        public int Offset;

        public static int GetSize(PointElementType type)
        {
            switch (type)
            {
                case PointElementType.Byte: return 1;
                case PointElementType.Float: return 4;
                case PointElementType.Double: return 8;
            }
            throw new Exception($"Unsupported point element type {type}");
        }
    }

    public partial class PointCloudImporter : ScriptedImporter
    {
        struct InputAccess
        {
            public int Size;
            public int Stride;

            [NativeDisableUnsafePtrRestriction]
            public unsafe void* Data;

            public double this[int index]
            {
                get
                {
                    unsafe
                    {
                        void* ptr = (byte*)Data + index * Stride;
                        return Size == 4 ? *(float*)ptr : *(double*)ptr;
                    }
                }
            }
        }

        struct ColorAccess
        {
            public int Stride;

            [NativeDisableUnsafePtrRestriction]
            public unsafe void* Color;

            [NativeDisableUnsafePtrRestriction]
            public unsafe void* Intensity;

            public uint this[int index]
            {
                get
                {
                    unsafe
                    {
                        uint color = 0;

                        byte r = 0;
                        byte g = 0;
                        byte b = 0;
                        if (Color != null)
                        {
                            byte* ptr = (byte*)Color + index * Stride;
                            r = ptr[0];
                            g = ptr[1];
                            b = ptr[2];
                            color = (uint)((b << 16) | (g << 8) | r);
                        }

                        if (Intensity != null)
                        {
                            byte* ptr = (byte*)Intensity + index * Stride;
                            byte intensity = *ptr;
                            color |= (uint)intensity << 24;

                            if (Color == null)
                            {
                                color |= (uint)((intensity << 16) | (intensity << 8) | intensity);
                            }
                        }
                        else if (Color != null)
                        {
                            byte intensity = (byte)((r + g + b) / 3);
                            color |= (uint)intensity << 24;
                        }

                        return color;
                    }
                }
            }
        }

        unsafe struct PointCloudGetBoundsJob : IJobParallelFor
        {
            public InputAccess X;
            public InputAccess Y;
            public InputAccess Z;

            [NativeDisableUnsafePtrRestriction]
            public PointCloudBounds* Bounds;

            [NativeDisableUnsafePtrRestriction]
            public int* Counts;

            [NativeSetThreadIndex]
            public int ThreadIndex;

            public void Execute(int index)
            {
                Bounds[ThreadIndex].Add(X[index], Y[index], Z[index]);
                ++Counts[ThreadIndex];
            }
        }
        unsafe struct PointCloudConvertJob : IJobParallelFor
        {
            public InputAccess X;
            public InputAccess Y;
            public InputAccess Z;
            public ColorAccess Color;

            [NativeDisableUnsafePtrRestriction]
            public PointCloudPoint* Output;

            public Matrix4x4 Transform;

            public double OutputScaleX;
            public double OutputScaleY;
            public double OutputScaleZ;

            public double OutputCenterX;
            public double OutputCenterY;
            public double OutputCenterZ;

            [NativeDisableUnsafePtrRestriction]
            public int* Counts;

            [NativeSetThreadIndex]
            public int ThreadIndex;

            public void Execute(int index)
            {
                double x = (X[index] + OutputCenterX) * OutputScaleX;
                double y = (Y[index] + OutputCenterY) * OutputScaleY;
                double z = (Z[index] + OutputCenterZ) * OutputScaleZ;

                var pt = Transform.MultiplyVector(new Vector3((float)x, (float)y, (float)z));

                Output[index] = new PointCloudPoint()
                {
                    Position = pt,
                    Color = Color[index],
                };
                ++Counts[ThreadIndex];
            }
        }

        PointCloudData ImportPoints(AssetImportContext context, MemoryMappedViewAccessor view, long count, int stride, List<PointElement> elements)
        {
            if (count > MaxPointCount)
            {
                Debug.LogWarning($"Too many points ({count:n0}), truncating to {MaxPointCount:n0}");
                count = MaxPointCount;
            }

            unsafe
            {
                byte* ptr = null;
                view.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);

                try
                {
                    var bounds = GetBounds(context, ptr, stride, (int)count, elements);

                    bool hasColor;
                    var points = Convert(context, ptr, stride, (int)count, elements, bounds, out hasColor);

                    return PointCloudData.Create(points, GetBounds(bounds), hasColor, bounds.Center, bounds.Extents);
                }
                finally
                {
                    view.SafeMemoryMappedViewHandle.ReleasePointer();
                }
            }
        }

        unsafe PointCloudPoint[] Convert(AssetImportContext context, byte* ptr, int stride, int count, List<PointElement> elements, PointCloudBounds bounds, out bool hasColor)
        {
            var name = Path.GetFileName(context.assetPath);

            var counts = new NativeArray<int>(JobsUtility.MaxJobThreadCount, Allocator.TempJob);
            try
            {
                var points = new PointCloudPoint[count];

                fixed (PointCloudPoint* output = points)
                {
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

                    var job = new PointCloudConvertJob()
                    {
                        X = GetInputAccess(PointElementName.X, elements, ptr, stride),
                        Y = GetInputAccess(PointElementName.Y, elements, ptr, stride),
                        Z = GetInputAccess(PointElementName.Z, elements, ptr, stride),
                        Color = GetColorAccess(context, elements, ptr, stride),

                        Output = output,
                        Transform = GetTransform(),

                        OutputCenterX = centerX,
                        OutputCenterY = centerY,
                        OutputCenterZ = centerZ,

                        OutputScaleX = scaleX,
                        OutputScaleY = scaleY,
                        OutputScaleZ = scaleZ,

                        Counts = (int*)counts.GetUnsafePtr(),
                        ThreadIndex = 0,
                    };

                    var h = job.Schedule((int)count, 65536);
                    while (!h.IsCompleted)
                    {
                        System.Threading.Thread.Sleep(100);

                        int processed = counts.Sum();
                        float progress = (float)((double)processed / count);
                        EditorUtility.DisplayProgressBar($"Importing {name}", $"{processed:N0} points", progress);
                    }

                    hasColor = job.Color.Color != null;
                    return points;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                counts.Dispose();
            }
        }

        unsafe PointCloudBounds GetBounds(AssetImportContext context, byte* ptr, int stride, int count, List<PointElement> elements)
        {
            var name = Path.GetFileName(context.assetPath);

            var counts = new NativeArray<int>(JobsUtility.MaxJobThreadCount, Allocator.TempJob);
            var bounds = new NativeArray<PointCloudBounds>(JobsUtility.MaxJobThreadCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            for (int i = 0; i < bounds.Length; i++)
            {
                bounds[i] = PointCloudBounds.Empty;
            }

            try
            {
                var job = new PointCloudGetBoundsJob()
                {
                    X = GetInputAccess(PointElementName.X, elements, ptr, stride),
                    Y = GetInputAccess(PointElementName.Y, elements, ptr, stride),
                    Z = GetInputAccess(PointElementName.Z, elements, ptr, stride),
                    Bounds = (PointCloudBounds*)bounds.GetUnsafePtr(),
                    Counts = (int*)counts.GetUnsafePtr(),
                    ThreadIndex = 0,
                };

                var h = job.Schedule((int)count, 65536);
                while (!h.IsCompleted)
                {
                    System.Threading.Thread.Sleep(100);

                    int processed = counts.Sum();
                    float progress = (float)((double)processed / count);
                    EditorUtility.DisplayProgressBar($"Importing {name}", $"{processed:N0} points", progress);
                }

                var result = PointCloudBounds.Empty;

                foreach (var b in bounds)
                {
                    if (b.IsValid)
                    {
                        result.Add(b.MinX, b.MinY, b.MinZ);
                        result.Add(b.MaxX, b.MaxY, b.MaxZ);
                    }
                }

                return result;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                bounds.Dispose();
                counts.Dispose();
            }
        }

        static unsafe InputAccess GetInputAccess(PointElementName name, List<PointElement> elements, byte* data, int stride)
        {
            foreach (var element in elements)
            {
                if (element.Name == name)
                {
                    if (element.Type == PointElementType.Float || element.Type == PointElementType.Double)
                    {
                        int elementSize = PointElement.GetSize(element.Type);
                        return new InputAccess() { Size = elementSize, Stride = stride, Data = data + element.Offset };
                    }
                    else
                    {
                        throw new Exception($"Point Cloud {name} field has unsupported type {element.Type}");
                    }
                }
            }

            throw new Exception($"Point Cloud does not have {name} field");
        }

        static unsafe ColorAccess GetColorAccess(AssetImportContext context, List<PointElement> elements, byte* data, int stride)
        {
            var result = new ColorAccess()
            {
                Stride = stride,
            };

            for (int i = 0; i < elements.Count; i++)
            {
                if (elements[i].Name == PointElementName.I && elements[i].Type == PointElementType.Byte)
                {
                    result.Intensity = data + elements[i].Offset;
                }
                else if (i < elements.Count - 2
                    && elements[i].Name == PointElementName.R && elements[i + 1].Name == PointElementName.G && elements[i + 2].Name == PointElementName.B
                    && elements[i].Type == PointElementType.Byte && elements[i + 1].Type == PointElementType.Byte && elements[i + 2].Type == PointElementType.Byte)
                {
                    result.Color = data + elements[i].Offset;
                }
            }

            if (result.Intensity == null && result.Color == null)
            {
                context.LogImportError("Point Cloud has no color and intensity data. Everything will be black!");
            }

            return result;
        }

    }
}
