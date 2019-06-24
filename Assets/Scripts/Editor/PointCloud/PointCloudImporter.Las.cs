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
using System.Text;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Simulator.PointCloud;

namespace Simulator.Editor.PointCloud
{
    public partial class PointCloudImporter : ScriptedImporter
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        struct LasHeader
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] Signature;
            public ushort FileSourceId;
            public ushort GlobalEncoding;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] Guid;
            public byte VersionMajor;
            public byte VersionMinor;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] SystemIdentifier;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] GeneratingSoftware;
            public ushort CreateDayOfYear;
            public ushort CreateYear;
            public ushort HeaderSize;
            public uint PointDataOffset;
            public uint VarRecCount;
            public byte PointDataFormat;
            public ushort PointDataSize;
            public uint PointDataCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            public uint[] PointCountReturn;
            public double ScaleX;
            public double ScaleY;
            public double ScaleZ;
            public double OffsetX;
            public double OffsetY;
            public double OffsetZ;
            public double MaxX;
            public double MinX;
            public double MaxY;
            public double MinY;
            public double MaxZ;
            public double MinZ;
            public ulong WaveformDataOffset;
            public ulong ExtendedVarOffset;
            public uint ExtendedVarCount;
            public ulong PointDataCountLong;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
            public ulong[] PointCountReturnLong;
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
                int* data = (int*)(Input + Stride * index);

                double x = data[0] * InputScaleX + InputOffsetX;
                double y = data[1] * InputScaleY + InputOffsetY;
                double z = data[2] * InputScaleZ + InputOffsetZ;

                x = (x + OutputCenterX) * OutputScaleX;
                y = (y + OutputCenterY) * OutputScaleY;
                z = (z + OutputCenterZ) * OutputScaleZ;

                var pt = Transform.MultiplyVector(new Vector3((float)x, (float)y, (float)z));

                byte intensity;
                {
                    ushort* iptr = (ushort*)(Input + Stride * index + 12);
                    if (LasRGB8BitWorkaround)
                    {
                        intensity = (byte)(*iptr >> 0);
                    }
                    else
                    {
                        intensity = (byte)(*iptr >> 8);
                    }
                }

                uint color = (uint)(intensity << 24);
                if (ColorInput == null)
                {
                    color |= (uint)((intensity << 16) | (intensity << 8) | intensity);
                }
                else
                {
                    ushort* rgb = (ushort*)(ColorInput + Stride * index);

                    if (LasRGB8BitWorkaround)
                    {
                        byte r = (byte)(rgb[0] >> 0);
                        byte g = (byte)(rgb[1] >> 0);
                        byte b = (byte)(rgb[2] >> 0);
                        color |= (uint)((b << 16) | (g << 8) | r);
                    }
                    else
                    {
                        byte r = (byte)(rgb[0] >> 8);
                        byte g = (byte)(rgb[1] >> 8);
                        byte b = (byte)(rgb[2] >> 8);
                        color |= (uint)((b << 16) | (g << 8) | r);
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

        PointCloudData ImportLas(AssetImportContext context)
        {
            long size = new FileInfo(context.assetPath).Length;

            using (var file = MemoryMappedFile.CreateFromFile(context.assetPath, FileMode.Open))
            {
                using (var view = file.CreateViewAccessor(0, size, MemoryMappedFileAccess.Read))
                {
                    view.Read<LasHeader>(0, out var header);
                    if (Encoding.ASCII.GetString(header.Signature) != "LASF")
                    {
                        throw new Exception("Incorrect LAS file signature");
                    }

                    if (header.PointDataFormat > 10)
                    {
                        throw new Exception($"Unsupported LAS file format: {header.PointDataFormat}");
                    }

                    long offset = header.PointDataOffset;
                    int stride = header.PointDataSize;
                    long count = header.PointDataCount;

                    if (header.VersionMajor > 1 || header.VersionMajor == 1 && header.VersionMinor >= 4)
                    {
                        if (count == 0)
                        {
                            count = (long)header.PointDataCountLong;
                        }
                    }

                    if (count > MaxPointCount)
                    {
                        Debug.LogWarning($"Too many points ({count:n0}), truncating to {MaxPointCount:n0}");
                        count = MaxPointCount;
                    }

                    var bounds = new PointCloudBounds()
                    {
                        MinX = header.MinX,
                        MinY = header.MinY,
                        MinZ = header.MinZ,
                        MaxX = header.MaxX,
                        MaxY = header.MaxY,
                        MaxZ = header.MaxZ,
                    };

                    unsafe
                    {
                        byte* ptr = null;
                        view.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);

                        try
                        {
                            var points = new PointCloudPoint[(int)count];

                            fixed (PointCloudPoint* output = points)
                            {
                                bool hasColor = LasConvert(context, ptr + offset, stride, (int)count, ref header, bounds, output);

                                var transform = GetTransform();
                                var b = GetBounds(bounds);
                                b.center = transform.MultiplyPoint3x4(b.center);
                                b.extents = transform.MultiplyVector(b.extents);

                                return PointCloudData.Create(points, GetBounds(bounds), hasColor, transform.MultiplyPoint3x4(bounds.Center), transform.MultiplyVector(bounds.Extents));
                            }
                        }
                        finally
                        {
                            view.SafeMemoryMappedViewHandle.ReleasePointer();
                        }
                    }
                }
            }
        }

        unsafe bool LasConvert(AssetImportContext context, byte* ptr, int stride, int count, ref LasHeader header, PointCloudBounds bounds, PointCloudPoint* points)
        {
            var name = Path.GetFileName(context.assetPath);

            var counts = new NativeArray<int>(JobsUtility.MaxJobThreadCount, Allocator.TempJob);
            try
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

                var job = new LasConvertJob()
                {
                    LasRGB8BitWorkaround = LasRGB8BitWorkaround,
                    Input = ptr,
                    Stride = stride,
                    Output = points,
                    Transform = GetTransform(),

                    InputScaleX = header.ScaleX,
                    InputScaleY = header.ScaleY,
                    InputScaleZ = header.ScaleZ,

                    InputOffsetX = header.OffsetX,
                    InputOffsetY = header.OffsetY,
                    InputOffsetZ = header.OffsetZ,

                    OutputCenterX = centerX,
                    OutputCenterY = centerY,
                    OutputCenterZ = centerZ,

                    OutputScaleX = scaleX,
                    OutputScaleY = scaleY,
                    OutputScaleZ = scaleZ,

                    Counts = (int*)counts.GetUnsafePtr(),
                    ThreadIndex = 0,
                };

                bool hasColor = false;

                if (header.PointDataFormat == 2)
                {
                    job.ColorInput = ptr + 20;
                    hasColor = true;
                }
                else if (header.PointDataFormat == 3 || header.PointDataFormat == 5)
                {
                    job.ColorInput = ptr + 28;
                    hasColor = true;
                }
                else if (header.PointDataFormat == 7 || header.PointDataFormat == 8 || header.PointDataFormat == 10)
                {
                    job.ColorInput = ptr + 30;
                    hasColor = true;
                }

                var h = job.Schedule((int)count, 65536);
                while (!h.IsCompleted)
                {
                    System.Threading.Thread.Sleep(100);

                    int processed = counts.Sum();
                    float progress = (float)((double)processed / count);
                    EditorUtility.DisplayProgressBar($"Importing {name}", $"{processed:N0} points", progress);
                }

                return hasColor;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                counts.Dispose();
            }
        }
    }
}

