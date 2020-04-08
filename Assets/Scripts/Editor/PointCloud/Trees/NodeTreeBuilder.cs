/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Editor.PointCloud.Trees
{
    using System.Collections.Generic;
    using System.IO;
    using Simulator.Utilities;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Class controlling node tree building process.
    /// </summary>
    public static class NodeTreeBuilder
    {
        /// <summary>
        /// Starts tree building process with given settings.
        /// </summary>
        public static bool BuildNodeTree(TreeImportSettings settings)
        {
            var processors = new List<PointProcessor>();

            foreach (var inputFile in settings.inputFiles)
            {
                var processor = CreateProcessor(Utility.GetFullPath(inputFile));
                if (processor != null)
                    processors.Add(processor);
            }

            if (processors.Count == 0)
            {
                Debug.LogError("All of given point cloud files are invalid or unsupported.");
                return false;
            }

            var bounds = CalculateBounds(processors);

            var transformationData = new TransformationData(bounds, settings);

            var unityBounds = bounds.GetUnityBounds(settings);
            var transform = transformationData.TransformationMatrix;
            unityBounds.center = transform.MultiplyPoint3x4(unityBounds.center);
            unityBounds.extents = transform.MultiplyVector(unityBounds.extents);

            TreeImportData importData = null;

            if (settings.generateMesh && settings.roadOnlyMesh)
            {
                var histogram = GenerateHistogram(processors, bounds);
                importData = new TreeImportData(unityBounds, histogram);
            }
            else
            {
                importData = new TreeImportData(unityBounds);
            }

            NodeProcessorDispatcher dispatcher;
            var fullOutputPath = Utility.GetFullPath(settings.outputPath);

            try
            {
                EditorUtility.DisplayProgressBar("Creating dispatcher", "Preparing target directory...", 0f);
                dispatcher = new NodeProcessorDispatcher(fullOutputPath, settings);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            foreach (var processor in processors)
            {
                if (!processor.ConvertPoints(dispatcher, transformationData))
                {
                    Debug.Log("Import cancelled.");
                    return false;
                }
            }

            if (dispatcher.ProcessPoints(importData))
            {
                dispatcher.GetPointCountResults(out var total, out var used, out var discarded);
                Debug.Log($"Octree build finished successfully.\n" +
                          $"Used points: {used}/{total} ({discarded} discarded on low tree levels)");
                return true;
            }
            else
            {
                Debug.Log("Octree build failed.");
                return false;
            }
        }

        /// <summary>
        /// Creates and returns point processor for a single file.
        /// </summary>
        /// <param name="filePath">Path to file for which processor should be created.</param>
        private static PointProcessor CreateProcessor(string filePath)
        {
            var extension = Path.GetExtension(filePath);

            switch (extension)
            {
                case ".laz":
                    return new LazPointProcessor(filePath);
                case ".las":
                    return new LasPointProcessor(filePath);
                case ".pcd":
                    return new PcdPointProcessor(filePath);
                case ".ply":
                    return new PlyPointProcessor(filePath);
                default:
                    Debug.LogError($"Unsupported Point Cloud format ({extension})");
                    return null;
            }
        }

        /// <summary>
        /// Calculates and returns merged bounds of points from all given processors.
        /// </summary>
        /// <param name="processors">Enumerable collection of point processors.</param>
        private static PointCloudBounds CalculateBounds(IEnumerable<PointProcessor> processors)
        {
            var bounds = PointCloudBounds.Empty;

            foreach (var processor in processors)
            {
                var processorBounds = processor.CalculateBounds();
                bounds.Encapsulate(processorBounds);
            }

            bounds.MinX -= 0.1;
            bounds.MinY -= 0.1;
            bounds.MinZ -= 0.1;
            bounds.MaxX += 0.1;
            bounds.MaxY += 0.1;
            bounds.MaxZ += 0.1;

            return bounds;
        }

        /// <summary>
        /// Calculates and returns merged bounds of points from all given processors.
        /// </summary>
        /// <param name="processors">Enumerable collection of point processors.</param>
        private static PointCloudVerticalHistogram GenerateHistogram(IEnumerable<PointProcessor> processors, PointCloudBounds bounds)
        {
            var result = new PointCloudVerticalHistogram(bounds);

            foreach (var processor in processors)
            {
                unsafe
                {
                    var processorHistogram = processor.GenerateHistogram(bounds);
                    result.AddData(processorHistogram.regions);
                }
            }

            return result;
        }
    }
}