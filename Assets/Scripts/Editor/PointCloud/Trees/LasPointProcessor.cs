/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Editor.PointCloud.Trees
{
    using System;
    using System.IO;
    using System.IO.MemoryMappedFiles;
    using System.Runtime.InteropServices;
    using System.Text;
    using Simulator.PointCloud.Trees;
    using UnityEngine;

    public class LasPointProcessor : PointProcessor
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct LasHeader
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public readonly byte[] Signature;
            public readonly ushort FileSourceId;
            public readonly ushort GlobalEncoding;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public readonly byte[] Guid;
            public readonly byte VersionMajor;
            public readonly byte VersionMinor;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public readonly byte[] SystemIdentifier;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public readonly byte[] GeneratingSoftware;
            public readonly ushort CreateDayOfYear;
            public readonly ushort CreateYear;
            public readonly ushort HeaderSize;
            public readonly uint PointDataOffset;
            public readonly uint VarRecCount;
            public readonly byte PointDataFormat;
            public readonly ushort PointDataSize;
            public readonly uint PointDataCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            public readonly uint[] PointCountReturn;
            public readonly double ScaleX;
            public readonly double ScaleY;
            public readonly double ScaleZ;
            public readonly double OffsetX;
            public readonly double OffsetY;
            public readonly double OffsetZ;
            public readonly double MaxX;
            public readonly double MinX;
            public readonly double MaxY;
            public readonly double MinY;
            public readonly double MaxZ;
            public readonly double MinZ;
            public readonly ulong WaveformDataOffset;
            public readonly ulong ExtendedVarOffset;
            public readonly uint ExtendedVarCount;
            public readonly ulong PointDataCountLong;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
            public readonly ulong[] PointCountReturnLong;
        }

        private LasHeader header;

        public LasPointProcessor(string filePath) : base(filePath)
        {
            header = ReadHeader();
        }

        private LasHeader ReadHeader()
        {
            long size = new FileInfo(FilePath).Length;
            LasHeader result;

            using (var file = MemoryMappedFile.CreateFromFile(FilePath, FileMode.Open))
            {
                using (var view = file.CreateViewAccessor(0, size, MemoryMappedFileAccess.Read))
                {
                    view.Read(0, out result);
                    if (Encoding.ASCII.GetString(result.Signature) != "LASF")
                    {
                        throw new Exception("Incorrect LAS file signature");
                    }

                    if (result.PointDataFormat > 10)
                    {
                        throw new Exception($"Unsupported LAS file format: {result.PointDataFormat}");
                    }
                }
            }

            return result;
        }
        
        ///<inheritdoc/>
        public override PointCloudBounds CalculateBounds()
        {
            var bounds = new PointCloudBounds()
            {
                MinX = header.MinX,
                MinY = header.MinY,
                MinZ = header.MinZ,
                MaxX = header.MaxX,
                MaxY = header.MaxY,
                MaxZ = header.MaxZ,
            };

            return bounds;
        }

        public override PointCloudVerticalHistogram GenerateHistogram(PointCloudBounds bounds)
        {
            var fileName = Path.GetFileName(FilePath);

            long currentOffset = header.PointDataOffset;
            long processed = 0;
            
            long count = header.PointDataCount;

            if (header.VersionMajor > 1 || header.VersionMajor == 1 && header.VersionMinor >= 4)
            {
                if (count == 0)
                {
                    count = (long)header.PointDataCountLong;
                }
            }

            var result = new PointCloudVerticalHistogram(bounds);
            
            using (var file = MemoryMappedFile.CreateFromFile(FilePath ?? throw new Exception("Input file not found."), FileMode.Open))
            {
                var batchIndex = 0;

                var maxArraySize = TreeUtility.CalculateMaxArraySize(header.PointDataSize);
                var totalBatchCount = Mathf.CeilToInt((float) count / maxArraySize);

                while (processed < count)
                {
                    var batchCount = Math.Min(maxArraySize, count - processed);
                    var batchSize = batchCount * header.PointDataSize;

                    using (var view = file.CreateViewAccessor(currentOffset, batchSize, MemoryMappedFileAccess.Read))
                    {
                        unsafe
                        {
                            batchIndex++;
                            var progressBarTitle =
                                $"Generating histogram ({fileName}, batch {batchIndex.ToString()}/{totalBatchCount.ToString()})";

                            var hst = PointImportJobs.GenerateHistogramLas(view, batchCount, header, bounds, progressBarTitle);
                            result.AddData(hst.regions);
                        }
                    }

                    processed += batchCount;
                    currentOffset += batchSize;
                }
            }

            return result;
        }

        ///<inheritdoc/>
        public override bool ConvertPoints(NodeProcessorDispatcher target, TransformationData transformationData)
        {
            var fileName = Path.GetFileName(FilePath);

            long currentOffset = header.PointDataOffset;
            long processed = 0;
            
            long count = header.PointDataCount;

            if (header.VersionMajor > 1 || header.VersionMajor == 1 && header.VersionMinor >= 4)
            {
                if (count == 0)
                {
                    count = (long)header.PointDataCountLong;
                }
            }

            using (var file = MemoryMappedFile.CreateFromFile(FilePath ?? throw new Exception("Input file not found."), FileMode.Open))
            {
                var batchIndex = 0;

                var maxArraySize = TreeUtility.CalculateMaxArraySize(header.PointDataSize);
                var totalBatchCount = Mathf.CeilToInt((float) count / maxArraySize);

                while (processed < count)
                {
                    var batchCount = Math.Min(maxArraySize, count - processed);
                    var batchSize = batchCount * header.PointDataSize;

                    using (var view = file.CreateViewAccessor(currentOffset, batchSize, MemoryMappedFileAccess.Read))
                    {
                        batchIndex++;
                        var progressBarTitle =
                            $"Converting ({fileName}, batch {batchIndex.ToString()}/{totalBatchCount.ToString()})";
                        var targetBuffer = target.PublicMaxSizeBuffer;

                        PointImportJobs.ConvertLasData(view, targetBuffer, (int) batchCount, ref header, transformationData, progressBarTitle);
                        
                        target.AddChunk(targetBuffer, (int) batchCount);
                    }

                    processed += batchCount;
                    currentOffset += batchSize;
                }
            }

            return true;
        }
    }
}