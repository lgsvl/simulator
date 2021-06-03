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
    using Simulator.PointCloud.Trees;
    using UnityEngine;
    using UnityEngine.Serialization;
    using Utilities.Attributes;

    /// <summary>
    /// Class used to store all settings for node tree generation.
    /// </summary>
    [Serializable]
    public class TreeImportSettings : ScriptableObject
    {
        /// <summary>
        /// Represents sampling method used for selecting points when building node tree.
        /// </summary>
        public enum SamplingMethod
        {
            /// Subdivides space into smaller cells and picks points nearest to their centers.
            CellCenter,

            /// Picks points with Poisson-disc sampling method.
            PoissonDisc
        }

        /// <summary>
        /// List of files that should be used to build tree.
        /// </summary>
        [Tooltip("List of files that should be used to build tree.")]
        public List<string> inputFiles = new List<string>();

        /// <summary>
        /// Path under which tree files will be stored.
        /// </summary>
        [Tooltip("Path under which tree files will be stored.")]
        [PathSelector(SelectDirectory = true, TruncateToRelative = true)]
        public string outputPath = string.Empty;

        /// <summary>
        /// Internal data structure that should be used by the tree.
        /// </summary>
        [FormerlySerializedAs("TreeType")]
        [Tooltip("Internal data structure that should be used by the tree.")]
        public TreeType treeType = TreeType.Octree;

        /// <summary>
        /// Sampling method that should be used by the tree.
        /// </summary>
        [FormerlySerializedAs("Sampling")]
        [Tooltip("Sampling method that should be used by the tree.")]
        public SamplingMethod sampling = SamplingMethod.CellCenter;

        /// <summary>
        /// Defines distance between points in root node, relative to bounds. 64 means size/64 distance.
        /// </summary>
        [FormerlySerializedAs("nodeCellsPerAxis")]
        [Tooltip("Defines distance between points in root node, relative to bounds. 64 means size/64 distance.")]
        [Range(16, 128)]
        public int rootNodeSubdivision = 64;

        /// <summary>
        /// Defines the minimum amount of points needed to branch a node further.
        /// </summary>
        [Tooltip("Defines the minimum amount of points needed to branch a node further.")]
        [Range(16 * 16 * 2, 128 * 128 * 16)]
        public int nodeBranchThreshold = 64 * 64 * 8;

        /// <summary>
        /// Defines maximum depth of the tree. Points on lower levels will be discarded.
        /// </summary>
        [Range(1, 16)]
        [Tooltip("Defines maximum depth of the tree. Points on lower levels will be discarded.")]
        public int maxTreeDepth = 12;

        /// <summary>
        /// Defines minimum distance between points. Points below this threshold will be discarded.
        /// </summary>
        [Tooltip("Defines minimum distance between points. Points below this threshold will be discarded.")]
        public float minPointDistance = 0.01f;

        /// <summary>
        /// If true, mesh collider will be generated from point cloud data.
        /// </summary>
        [Tooltip("If true, mesh collider will be generated from point cloud data.")]
        public bool generateMesh = false;

        /// <summary>
        /// If true, only road level will be meshed. This is not viable for all data sets.
        /// </summary>
        [Tooltip("If true, only road level will be meshed. This is not viable for all data sets.")]
        public bool roadOnlyMesh = false;

        /// <summary>
        /// Level of detail that should be used to generate collider mesh. 
        /// </summary>
        [Tooltip("Level of detail that should be used to generate collider mesh. Larger maps should use higher level.")]
        [Range(1, 6)]
        public int meshDetailLevel = 3;

        /// <summary>
        /// Amount of passes eroding mesh obstacles. Can be used to flatten generated mesh.
        /// </summary>
        [Range(0, 8)]
        [Tooltip("Amount of passes eroding mesh obstacles. Can be used to flatten generated mesh.")]
        public int erosionPasses = 0;

        /// <summary>
        /// Mesh triangles below this threshold will not be affected by erosion passes.
        /// </summary>
        [Range(0f, 30f)]
        [Tooltip("Mesh triangles below this threshold will not be affected by erosion passes.")]
        public float erosionAngleThreshold = 10f;

        /// <summary>
        /// If true, small groups of triangles will be removed from mesh.
        /// </summary>
        [Tooltip("Mesh triangles below this threshold will not be affected by erosion passes.")]
        public bool removeSmallSurfaces;

        /// <summary>
        /// Group of triangles above this threshold will not be removed if removeSmallSurfaces is set to true.
        /// </summary>
        [Tooltip("Mesh triangles below this threshold will not be affected by erosion passes.")]
        public int smallSurfaceTriangleThreshold = 50;

        /// <summary>
        /// <para>Amount of worker threads used for building the tree.</para>
        /// <para>Note: each thread allocates additional memory.</para>
        /// </summary>
        [Tooltip("Amount of worker threads used for building the tree.\nNote: each thread allocates additional memory.")]
        [Range(1, 64)]
        public int threadCount = 8;

        /// <summary>
        /// Amount of points that will be stored in a single chunk.
        /// Affects per-thread memory requirements during processing.
        /// </summary>
        [Tooltip("Amount of points that will be stored in a single chunk.\nAffects per-thread memory requirements during processing.")]
        [Range(1000000, 16000000)]
        public int chunkSize = 8000000;

        public bool center = true;

        public bool normalize;

        public bool lasRGB8BitWorkaround = true;

        public PointCloudImportAxes axes = PointCloudImportAxes.X_Right_Z_Up_Y_Forward;
    }
}