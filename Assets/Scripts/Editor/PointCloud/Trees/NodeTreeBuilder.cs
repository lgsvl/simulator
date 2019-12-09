namespace Simulator.Editor.PointCloud.Trees
{
    using System.Collections.Generic;
    using System.IO;
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
        public static void BuildNodeTree(TreeImportSettings settings)
        {
            var processors = new List<PointProcessor>();

            foreach (var inputFile in settings.inputFiles)
            {
                var processor = CreateProcessor(inputFile);
                if (processor != null)
                    processors.Add(processor);
            }

            if (processors.Count == 0)
            {
                Debug.LogError("All of given point cloud files are invalid or unsupported.");
                return;
            }

            var bounds = CalculateBounds(processors);

            var transformationData = new TransformationData(bounds, settings);

            var unityBounds = bounds.GetUnityBounds(settings);
            var transform = transformationData.TransformationMatrix;
            unityBounds.center = transform.MultiplyPoint3x4(unityBounds.center);
            unityBounds.extents = transform.MultiplyVector(unityBounds.extents);

            NodeProcessorDispatcher dispatcher;

            try
            {
                EditorUtility.DisplayProgressBar("Creating dispatcher", "Preparing target directory...", 0f);
                dispatcher = new NodeProcessorDispatcher(settings.outputPath, settings);
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
                    return;
                }
            }

            if (dispatcher.ProcessPoints(unityBounds))
                Debug.Log("Octree build finished successfully.");
            else
                Debug.Log("Octree build failed.");
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
    }
}