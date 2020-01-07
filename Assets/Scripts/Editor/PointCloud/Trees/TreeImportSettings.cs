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
        /// Path under which tree files will be stored.
        /// </summary>
        [Header("Files")]
        [Tooltip("Path under which tree files will be stored.")]
        [PathSelector(SelectDirectory = true)]
        public string outputPath = string.Empty;

        /// <summary>
        /// List of files that should be used to build tree.
        /// </summary>
        [Tooltip("List of files that should be used to build tree.")]
        [PathSelector(AllowedExtensions = "laz,las,pcd,ply")]
        public List<string> inputFiles = new List<string>();

        /// <summary>
        /// Internal data structure that should be used by the tree.
        /// </summary>
        [Header("Tree Settings")]
        [Tooltip("Internal data structure that should be used by the tree.")]
        public TreeType TreeType = TreeType.Octree;

        /// <summary>
        /// Sampling method that should be used by the tree.
        /// </summary>
        [Tooltip("Sampling method that should be used by the tree.")]
        public SamplingMethod Sampling = SamplingMethod.CellCenter;
        
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
        /// <para>Amount of worker threads used for building the tree.</para>
        /// <para>Note: each thread allocates additional memory.</para>
        /// </summary>
        [Header("Build Settings")]
        [Tooltip("Amount of worker threads used for building the tree.\nNote: each thread allocates additional memory.")]
        [Range(1, 32)]
        public int threadCount = 16;

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