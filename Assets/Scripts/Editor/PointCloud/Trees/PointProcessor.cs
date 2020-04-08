/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Editor.PointCloud.Trees
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.MemoryMappedFiles;
    using Simulator.PointCloud.Trees;
    using UnityEngine;

    /// <summary>
    /// Class used to process points from a single file.
    /// </summary>
    public abstract class PointProcessor
    {
        protected class DefaultHeaderData
        {
            public long DataOffset;
            public long DataCount;
            public int DataStride;

            public readonly List<PointElement> Elements = new List<PointElement>();
        }

        protected readonly string FilePath;

        protected PointProcessor(string filePath)
        {
            this.FilePath = filePath;
        }

        /// <summary>
        /// Default <see cref="CalculateBounds"/> implementation for formats using <see cref="DefaultHeaderData"/>.
        /// </summary>
        /// <param name="headerData">Data extracted from file header.</param>
        /// <returns>Bounds of points in given file.</returns>
        protected PointCloudBounds CalculateBoundsDefault(DefaultHeaderData headerData)
        {
            var fileName = Path.GetFileName(FilePath);

            long currentOffset = headerData.DataOffset;
            long processed = 0;

            var result = PointCloudBounds.Empty;

            using (var file = MemoryMappedFile.CreateFromFile(FilePath ?? throw new Exception("Input file not found."), FileMode.Open))
            {
                var batchIndex = 0;

                var maxArraySize = TreeUtility.CalculateMaxArraySize(headerData.DataStride);
                var totalBatchCount = Mathf.CeilToInt((float) headerData.DataCount / maxArraySize);

                while (processed < headerData.DataCount)
                {
                    var batchCount = Math.Min(maxArraySize, headerData.DataCount - processed);
                    var batchSize = batchCount * headerData.DataStride;

                    using (var view = file.CreateViewAccessor(currentOffset, batchSize, MemoryMappedFileAccess.Read))
                    {
                        batchIndex++;
                        var progressBarTitle =
                            $"Calculating bounds ({fileName}, batch {batchIndex.ToString()}/{totalBatchCount.ToString()})";

                        var batchBounds = PointImportJobs.CalculateBounds(view, batchCount,
                            headerData.DataStride, headerData.Elements, progressBarTitle);

                        result.Encapsulate(batchBounds);
                    }

                    processed += batchCount;
                    currentOffset += batchSize;
                }
            }

            return result;
        }
        
        protected PointCloudVerticalHistogram GenerateHistogramDefault(DefaultHeaderData headerData, PointCloudBounds bounds)
        {
            var fileName = Path.GetFileName(FilePath);

            long currentOffset = headerData.DataOffset;
            long processed = 0;

            var result = new PointCloudVerticalHistogram(bounds);

            using (var file = MemoryMappedFile.CreateFromFile(FilePath ?? throw new Exception("Input file not found."), FileMode.Open))
            {
                var batchIndex = 0;

                var maxArraySize = TreeUtility.CalculateMaxArraySize(headerData.DataStride);
                var totalBatchCount = Mathf.CeilToInt((float) headerData.DataCount / maxArraySize);

                while (processed < headerData.DataCount)
                {
                    var batchCount = Math.Min(maxArraySize, headerData.DataCount - processed);
                    var batchSize = batchCount * headerData.DataStride;

                    using (var view = file.CreateViewAccessor(currentOffset, batchSize, MemoryMappedFileAccess.Read))
                    {
                        unsafe
                        {
                            batchIndex++;
                            var progressBarTitle =
                                $"Calculating bounds ({fileName}, batch {batchIndex.ToString()}/{totalBatchCount.ToString()})";

                            var batchHistogram = PointImportJobs.GenerateHistogram(view, batchCount,
                                headerData.DataStride, headerData.Elements, bounds, progressBarTitle);

                            result.AddData(batchHistogram.regions);
                        }
                    }

                    processed += batchCount;
                    currentOffset += batchSize;
                }
            }

            return result;
        }

        /// <summary>
        /// Default <see cref="ConvertPoints"/> implementation for formats using <see cref="DefaultHeaderData"/>.
        /// </summary>
        /// <param name="headerData">Data extracted from file header.</param>
        /// <param name="target">Target processor dispatcher to which transformed points should be passed.</param>
        /// <param name="transformationData">Data used for transformation of the points.</param>
        /// <returns>True if conversion finished successfully, false otherwise.</returns>
        protected bool ConvertPointsDefault(DefaultHeaderData headerData, NodeProcessorDispatcher target,
            TransformationData transformationData)
        {
            var fileName = Path.GetFileName(FilePath);

            long currentOffset = headerData.DataOffset;
            long processed = 0;

            using (var file = MemoryMappedFile.CreateFromFile(FilePath, FileMode.Open))
            {
                var batchIndex = 0;

                var maxArraySize = TreeUtility.CalculateMaxArraySize(headerData.DataStride);
                var totalBatchCount =
                    Mathf.CeilToInt((float) headerData.DataCount / maxArraySize);

                while (processed < headerData.DataCount)
                {
                    var batchCount = Math.Min(maxArraySize, headerData.DataCount - processed);
                    var batchSize = batchCount * headerData.DataStride;

                    using (var view = file.CreateViewAccessor(currentOffset, batchSize, MemoryMappedFileAccess.Read))
                    {
                        batchIndex++;
                        var progressBarTitle =
                            $"Converting ({fileName}, batch {batchIndex.ToString()}/{totalBatchCount.ToString()})";
                        var targetBuffer = target.PublicMaxSizeBuffer;

                        PointImportJobs.ConvertData(view, targetBuffer, headerData.Elements, transformationData,
                            headerData.DataStride, batchCount, progressBarTitle);

                        target.AddChunk(targetBuffer, (int) batchCount);
                    }

                    processed += batchCount;
                    currentOffset += batchSize;
                }
            }

            return true;
        }

        /// <summary>
        /// Calculates and returns <see cref="PointCloudBounds"/> of points in file assigned to this processor.
        /// </summary>
        public abstract PointCloudBounds CalculateBounds();

        public abstract PointCloudVerticalHistogram GenerateHistogram(PointCloudBounds bounds);

        /// <summary>
        /// Converts points in file assigned to this processor using given transformation data and feeds them to target dispatcher.
        /// </summary>
        /// <param name="target">Target processor dispatcher to which transformed points should be passed.</param>
        /// <param name="transformationData">Data used for transformation of the points.</param>
        /// <returns>True if conversion finished successfully, false otherwise.</returns>
        public abstract bool ConvertPoints(NodeProcessorDispatcher target, TransformationData transformationData);
    }
}