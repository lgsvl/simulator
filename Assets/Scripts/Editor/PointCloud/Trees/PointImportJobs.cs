/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Editor.PointCloud.Trees
{
    using System;
    using System.IO.MemoryMappedFiles;
    using System.Linq;
    using System.Collections.Generic;
    using Simulator.PointCloud;
    using Simulator.PointCloud.Trees;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Collections;
    using Unity.Jobs.LowLevel.Unsafe;
    using Unity.Jobs;
    using UnityEditor;
    using UnityEngine;

    public static class PointImportJobs
    {
        private struct InputAccess
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
                        void* ptr = (byte*) Data + index * Stride;
                        return Size == 4 ? *(float*) ptr : *(double*) ptr;
                    }
                }
            }
        }

        private struct ColorAccess
        {
            public int Stride;

            [NativeDisableUnsafePtrRestriction]
            public unsafe void* Color;

            [NativeDisableUnsafePtrRestriction]
            public unsafe void* Intensity;

            [NativeDisableUnsafePtrRestriction]
            public unsafe void* IntensityF;

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
                            var ptr = (byte*) Color + index * Stride;
                            r = ptr[0];
                            g = ptr[1];
                            b = ptr[2];
                            color = (uint) ((b << 16) | (g << 8) | r);
                        }

                        if (Intensity != null || IntensityF != null)
                        {
                            byte intensity;
                            if (Intensity != null)
                            {
                                var ptr = (byte*) Intensity + index * Stride;
                                intensity = *ptr;
                            }
                            else
                            {
                                var ptr = (float*) ((byte*) IntensityF + index * Stride);
                                intensity = (byte) *ptr;
                            }

                            color |= (uint) intensity << 24;
                            if (Color == null)
                            {
                                color |= (uint) ((intensity << 16) | (intensity << 8) | intensity);
                            }
                        }
                        else if (Color != null)
                        {
                            var intensity = (byte) ((r + g + b) / 3);
                            color |= (uint) intensity << 24;
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
        
        unsafe struct PointCloudCreateHistogramJob : IJobParallelFor
        {
            public InputAccess Z;
            
            [NativeDisableUnsafePtrRestriction]
            public PointCloudVerticalHistogram* Histogram;

            [NativeDisableUnsafePtrRestriction]
            public int* Counts;

            [NativeSetThreadIndex]
            public int ThreadIndex;

            public void Execute(int index)
            {
                Histogram[ThreadIndex].Add(Z[index]);
                ++Counts[ThreadIndex];
            }
        }
        
        unsafe struct PointCloudCreateHistogramLasJob : IJobParallelFor
        {
            [NativeDisableUnsafePtrRestriction]
            public byte* Input;
            
            public double InputScaleX;
            public double InputScaleY;
            public double InputScaleZ;

            public double InputOffsetX;
            public double InputOffsetY;
            public double InputOffsetZ;
            
            public int Stride;
            
            [NativeDisableUnsafePtrRestriction]
            public PointCloudVerticalHistogram* Histogram;

            [NativeDisableUnsafePtrRestriction]
            public int* Counts;

            [NativeSetThreadIndex]
            public int ThreadIndex;

            public void Execute(int index)
            {
                int* data = (int*) (Input + Stride * index);

                double z = data[2] * InputScaleZ + InputOffsetZ;
                
                Histogram[ThreadIndex].Add(z);
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
                var x = (X[index] + OutputCenterX) * OutputScaleX;
                var y = (Y[index] + OutputCenterY) * OutputScaleY;
                var z = (Z[index] + OutputCenterZ) * OutputScaleZ;

                var pt = Transform.MultiplyVector(new Vector3((float) x, (float) y, (float) z));

                Output[index] = new PointCloudPoint()
                {
                    Position = pt,
                    Color = Color[index],
                };
                ++Counts[ThreadIndex];
            }
        }

        unsafe struct LasConvertJob : IJobParallelFor
        {
            public bool LasRGB8BitWorkaround;

            [NativeDisableUnsafePtrRestriction]
            public byte* Input;

            [NativeDisableUnsafePtrRestriction]
            public byte* ColorInput;

            public int Stride;

            [NativeDisableUnsafePtrRestriction]
            public PointCloudPoint* Output;

            public Matrix4x4 Transform;

            // TODO: optimize this
            public double InputScaleX;
            public double InputScaleY;
            public double InputScaleZ;

            public double InputOffsetX;
            public double InputOffsetY;
            public double InputOffsetZ;

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
                int* data = (int*) (Input + Stride * index);

                double x = data[0] * InputScaleX + InputOffsetX;
                double y = data[1] * InputScaleY + InputOffsetY;
                double z = data[2] * InputScaleZ + InputOffsetZ;

                x = (x + OutputCenterX) * OutputScaleX;
                y = (y + OutputCenterY) * OutputScaleY;
                z = (z + OutputCenterZ) * OutputScaleZ;

                var pt = Transform.MultiplyVector(new Vector3((float) x, (float) y, (float) z));

                byte intensity;
                {
                    ushort* iptr = (ushort*) (Input + Stride * index + 12);
                    if (LasRGB8BitWorkaround)
                    {
                        intensity = (byte) *iptr;
                    }
                    else
                    {
                        intensity = (byte) (*iptr >> 8);
                    }
                }

                uint color = (uint) (intensity << 24);
                if (ColorInput == null)
                {
                    color |= (uint) ((intensity << 16) | (intensity << 8) | intensity);
                }
                else
                {
                    ushort* rgb = (ushort*) (ColorInput + Stride * index);

                    if (LasRGB8BitWorkaround)
                    {
                        byte r = (byte) rgb[0];
                        byte g = (byte) rgb[1];
                        byte b = (byte) rgb[2];
                        color |= (uint) ((b << 16) | (g << 8) | r);
                    }
                    else
                    {
                        byte r = (byte) (rgb[0] >> 8);
                        byte g = (byte) (rgb[1] >> 8);
                        byte b = (byte) (rgb[2] >> 8);
                        color |= (uint) ((b << 16) | (g << 8) | r);
                    }
                }

                Output[index] = new PointCloudPoint()
                {
                    Position = pt,
                    Color = color,
                };

                ++Counts[ThreadIndex];
            }
        }

        public static PointCloudBounds CalculateBounds(MemoryMappedViewAccessor accessor, long count, int stride,
            List<PointElement> elements, string progressBarTitle = null)
        {
            var maxArraySize = TreeUtility.CalculateMaxArraySize(stride);
            if (count > maxArraySize)
            {
                Debug.LogWarning(
                    $"Too many points ({count:n0}), truncating to {maxArraySize:n0}");
                count = maxArraySize;
            }

            unsafe
            {
                byte* ptr = null;
                accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);

                try
                {
                    var counts = new NativeArray<int>(JobsUtility.MaxJobThreadCount, Allocator.TempJob);
                    var bounds = new NativeArray<PointCloudBounds>(JobsUtility.MaxJobThreadCount, Allocator.TempJob,
                        NativeArrayOptions.UninitializedMemory);

                    for (var i = 0; i < bounds.Length; i++)
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
                            Bounds = (PointCloudBounds*) bounds.GetUnsafePtr(),
                            Counts = (int*) counts.GetUnsafePtr(),
                            ThreadIndex = 0,
                        };

                        var h = job.Schedule((int) count, 65536);
                        while (!h.IsCompleted)
                        {
                            System.Threading.Thread.Sleep(100);

                            var processed = counts.Sum();
                            var progress = (float) ((double) processed / count);

                            EditorUtility.DisplayProgressBar(
                                string.IsNullOrEmpty(progressBarTitle) ? "Calculating bounds" : progressBarTitle,
                                $"{processed:N0} points", progress);
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
                finally
                {
                    accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                }
            }
        }

        public static PointCloudVerticalHistogram GenerateHistogram(MemoryMappedViewAccessor accessor, long count,
            int stride, List<PointElement> elements, PointCloudBounds bounds, string progressBarTitle = null)
        {
            var maxArraySize = TreeUtility.CalculateMaxArraySize(stride);
            if (count > maxArraySize)
            {
                Debug.LogWarning(
                    $"Too many points ({count:n0}), truncating to {maxArraySize:n0}");
                count = maxArraySize;
            }

            unsafe
            {
                byte* ptr = null;
                accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);

                try
                {
                    var counts = new NativeArray<int>(JobsUtility.MaxJobThreadCount, Allocator.TempJob);
                    var histograms = new NativeArray<PointCloudVerticalHistogram>(
                        JobsUtility.MaxJobThreadCount,
                        Allocator.TempJob,
                        NativeArrayOptions.UninitializedMemory);

                    for (var i = 0; i < histograms.Length; i++)
                    {
                        histograms[i] = new PointCloudVerticalHistogram(bounds);
                    }

                    try
                    {
                        var job = new PointCloudCreateHistogramJob()
                        {
                            Z = GetInputAccess(PointElementName.Z, elements, ptr, stride),
                            Histogram = (PointCloudVerticalHistogram*) histograms.GetUnsafePtr(),
                            Counts = (int*) counts.GetUnsafePtr(),
                            ThreadIndex = 0,
                        };

                        var h = job.Schedule((int) count, 65536);
                        while (!h.IsCompleted)
                        {
                            System.Threading.Thread.Sleep(100);

                            var processed = counts.Sum();
                            var progress = (float) ((double) processed / count);

                            EditorUtility.DisplayProgressBar(
                                string.IsNullOrEmpty(progressBarTitle) ? "Preparing vertical histogram" : progressBarTitle,
                                $"{processed:N0} points",
                                progress);
                        }

                        var result = new PointCloudVerticalHistogram(bounds);

                        foreach (var hst in histograms)
                            result.AddData(hst.regions);

                        return result;
                    }
                    finally
                    {
                        EditorUtility.ClearProgressBar();
                        histograms.Dispose();
                        counts.Dispose();
                    }
                }
                finally
                {
                    accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                }
            }
        }
        
        public static PointCloudVerticalHistogram GenerateHistogramLas(MemoryMappedViewAccessor accessor, long count, 
            LasPointProcessor.LasHeader header, PointCloudBounds bounds, string progressBarTitle = null)
        {
            var maxArraySize = TreeUtility.CalculateMaxArraySize(header.PointDataSize);
            if (count > maxArraySize)
            {
                Debug.LogWarning(
                    $"Too many points ({count:n0}), truncating to {maxArraySize:n0}");
                count = maxArraySize;
            }

            unsafe
            {
                byte* ptr = null;
                accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);

                try
                {
                    var counts = new NativeArray<int>(JobsUtility.MaxJobThreadCount, Allocator.TempJob);
                    var histograms = new NativeArray<PointCloudVerticalHistogram>(
                        JobsUtility.MaxJobThreadCount,
                        Allocator.TempJob,
                        NativeArrayOptions.UninitializedMemory);

                    for (var i = 0; i < histograms.Length; i++)
                    {
                        histograms[i] = new PointCloudVerticalHistogram(bounds);
                    }
                    
                    try
                    {
                        var job = new PointCloudCreateHistogramLasJob()
                        {
                            Input = ptr,
                            Stride = header.PointDataSize,

                            InputScaleX = header.ScaleX,
                            InputScaleY = header.ScaleY,
                            InputScaleZ = header.ScaleZ,

                            InputOffsetX = header.OffsetX,
                            InputOffsetY = header.OffsetY,
                            InputOffsetZ = header.OffsetZ,
                            Histogram = (PointCloudVerticalHistogram*) histograms.GetUnsafePtr(),
                            Counts = (int*) counts.GetUnsafePtr(),
                            ThreadIndex = 0,
                        };

                        var h = job.Schedule((int) count, 65536);
                        while (!h.IsCompleted)
                        {
                            System.Threading.Thread.Sleep(100);

                            var processed = counts.Sum();
                            var progress = (float) ((double) processed / count);

                            EditorUtility.DisplayProgressBar(
                                string.IsNullOrEmpty(progressBarTitle) ? "Generating histogram" : progressBarTitle,
                                $"{processed:N0} points",
                                progress);
                        }

                        var result = new PointCloudVerticalHistogram(bounds);

                        foreach (var hst in histograms)
                            result.AddData(hst.regions);

                        return result;
                    }
                    finally
                    {
                        EditorUtility.ClearProgressBar();
                        histograms.Dispose();
                        counts.Dispose();
                    }
                }
                finally
                {
                    accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                }
            }
        }

        public static void ConvertData(MemoryMappedViewAccessor accessor, PointCloudPoint[] target, 
            List<PointElement> elements, TransformationData transformationData, int stride, long count, string progressBarTitle = null)
        {
            var maxArraySize = TreeUtility.CalculateMaxArraySize(stride);
            if (count > maxArraySize)
            {
                Debug.LogWarning(
                    $"Too many points ({count:n0}), truncating to {maxArraySize:n0}");
                count = maxArraySize;
            }

            if (target.Length < count)
            {
                throw new Exception($"Target buffer is too small (length: {target.Length}, required: {count}");
            }

            unsafe
            {
                byte* sourcePtr = null;
                accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref sourcePtr);

                try
                {
                    fixed (PointCloudPoint* targetPtr = target)
                    {
                        var counts = new NativeArray<int>(JobsUtility.MaxJobThreadCount, Allocator.TempJob);
                        try
                        {
                            var job = new PointCloudConvertJob()
                            {
                                X = GetInputAccess(PointElementName.X, elements, sourcePtr, stride),
                                Y = GetInputAccess(PointElementName.Y, elements, sourcePtr, stride),
                                Z = GetInputAccess(PointElementName.Z, elements, sourcePtr, stride),
                                Color = GetColorAccess(elements, sourcePtr, stride),

                                Output = targetPtr,
                                Transform = transformationData.TransformationMatrix,

                                OutputCenterX = transformationData.OutputCenterX,
                                OutputCenterY = transformationData.OutputCenterY,
                                OutputCenterZ = transformationData.OutputCenterZ,

                                OutputScaleX = transformationData.OutputScaleX,
                                OutputScaleY = transformationData.OutputScaleY,
                                OutputScaleZ = transformationData.OutputScaleZ,

                                Counts = (int*) counts.GetUnsafePtr(),
                                ThreadIndex = 0,
                            };

                            var h = job.Schedule((int) count, 65536);
                            while (!h.IsCompleted)
                            {
                                System.Threading.Thread.Sleep(100);

                                var processed = counts.Sum();
                                var progress = (float) ((double) processed / count);
                                EditorUtility.DisplayProgressBar(
                                    string.IsNullOrEmpty(progressBarTitle) ? $"Applying transformation" : progressBarTitle,
                                    $"{processed:N0} points", progress);
                            }
                        }
                        finally
                        {
                            EditorUtility.ClearProgressBar();
                            counts.Dispose();
                        }
                    }
                }
                finally
                {
                    accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                }
            }
        }

        public static void ConvertLasData(MemoryMappedViewAccessor accessor, PointCloudPoint[] target, int count, 
            ref LasPointProcessor.LasHeader header, TransformationData transformationData, string progressBarTitle = null)
        {
            unsafe
            {
                byte* sourcePtr = null;
                accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref sourcePtr);

                try
                {
                    fixed (PointCloudPoint* targetPtr = target)
                    {
                        var counts = new NativeArray<int>(JobsUtility.MaxJobThreadCount, Allocator.TempJob);
                        try
                        {
                            var job = new LasConvertJob()
                            {
                                LasRGB8BitWorkaround = transformationData.LasRGB8BitWorkaround,
                                Input = sourcePtr,
                                Stride = header.PointDataSize,
                                Output = targetPtr,
                                Transform = transformationData.TransformationMatrix,

                                InputScaleX = header.ScaleX,
                                InputScaleY = header.ScaleY,
                                InputScaleZ = header.ScaleZ,

                                InputOffsetX = header.OffsetX,
                                InputOffsetY = header.OffsetY,
                                InputOffsetZ = header.OffsetZ,

                                OutputCenterX = transformationData.OutputCenterX,
                                OutputCenterY = transformationData.OutputCenterY,
                                OutputCenterZ = transformationData.OutputCenterZ,

                                OutputScaleX = transformationData.OutputScaleX,
                                OutputScaleY = transformationData.OutputScaleY,
                                OutputScaleZ = transformationData.OutputScaleZ,

                                Counts = (int*) counts.GetUnsafePtr(),
                                ThreadIndex = 0,
                            };

                            if (header.PointDataFormat == 2)
                            {
                                job.ColorInput = sourcePtr + 20;
                            }
                            else if (header.PointDataFormat == 3 || header.PointDataFormat == 5)
                            {
                                job.ColorInput = sourcePtr + 28;
                            }
                            else if (header.PointDataFormat == 7 || header.PointDataFormat == 8 ||
                                     header.PointDataFormat == 10)
                            {
                                job.ColorInput = sourcePtr + 30;
                            }

                            var h = job.Schedule(count, 65536);
                            while (!h.IsCompleted)
                            {
                                System.Threading.Thread.Sleep(100);

                                int processed = counts.Sum();
                                float progress = (float) ((double) processed / count);
                                EditorUtility.DisplayProgressBar(
                                    string.IsNullOrEmpty(progressBarTitle) ? $"Applying transformation" : progressBarTitle,
                                    $"{processed:N0} points", progress);
                            }
                        }
                        finally
                        {
                            EditorUtility.ClearProgressBar();
                            counts.Dispose();
                        }
                    }
                }
                finally
                {
                    accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                }
            }
        }

        private static unsafe InputAccess GetInputAccess(PointElementName name, List<PointElement> elements, byte* data,
            int stride)
        {
            foreach (var element in elements)
            {
                if (element.Name == name)
                {
                    if (element.Type == PointElementType.Float || element.Type == PointElementType.Double)
                    {
                        var elementSize = PointElement.GetSize(element.Type);
                        return new InputAccess() {Size = elementSize, Stride = stride, Data = data + element.Offset};
                    }
                    else
                    {
                        throw new Exception($"Point Cloud {name} field has unsupported type {element.Type}");
                    }
                }
            }

            throw new Exception($"Point Cloud does not have {name} field");
        }

        private static unsafe ColorAccess GetColorAccess(List<PointElement> elements, byte* data, int stride)
        {
            var result = new ColorAccess()
            {
                Stride = stride,
            };

            for (var i = 0; i < elements.Count; i++)
            {
                if (elements[i].Name == PointElementName.I && elements[i].Type == PointElementType.Byte)
                {
                    result.Intensity = data + elements[i].Offset;
                }
                else if (elements[i].Name == PointElementName.I && elements[i].Type == PointElementType.Float)
                {
                    result.IntensityF = data + elements[i].Offset;
                }
                else if (i < elements.Count - 2
                         && elements[i].Name == PointElementName.R
                         && elements[i + 1].Name == PointElementName.G
                         && elements[i + 2].Name == PointElementName.B
                         && elements[i].Type == PointElementType.Byte
                         && elements[i + 1].Type == PointElementType.Byte
                         && elements[i + 2].Type == PointElementType.Byte)
                {
                    result.Color = data + elements[i].Offset;
                }
            }

            if (result.Intensity == null && result.IntensityF == null && result.Color == null)
            {
                Debug.LogError("Point Cloud has no color and intensity data. Everything will be black!");
            }

            return result;
        }
    }
}