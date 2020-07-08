/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Editor.MapMeshes
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Class used to store settings for HD map mesh generation.
    /// </summary>
    [Serializable]
    public class MapMeshSettings : ScriptableObject
    {
        /// <summary>
        /// Should mesh colliders be generated?
        /// </summary>
        [Tooltip("Should mesh colliders be generated?")]
        public bool createCollider = true;

        /// <summary>
        /// Should renderers be added to generated meshes?
        /// </summary>
        [Tooltip("Should renderers be added to generated meshes?")]
        public bool createRenderers = true;

        /// <summary>
        /// If true, connected lanes will have their holes fixed within the threshold.
        /// </summary>
        [Tooltip("If true, connected lanes will have their holes fixed within the threshold.")]
        public bool snapLaneEnds = true;

        /// <summary>
        /// If true, external lane vertices will be pushed out to create roadside.
        /// </summary>
        [Tooltip("If true, external lane vertices will be pushed out to create roadside.")]
        public bool pushOuterVerts = true;

        /// <summary>
        /// Distance in meters to push external lane vertices (if enabled).
        /// </summary>
        [Tooltip("Distance in meters to push external lane vertices (if enabled).")]
        public float pushDistance = 1f;

        /// <summary>
        /// Distance in meters within which lane ends will be snapped together (if enabled).
        /// </summary>
        [Tooltip("Distance in meters within which lane ends will be snapped together (if enabled).")]
        public float snapThreshold = 1f;

        /// <summary>
        /// Road UV coordinates will be tiled on this distance (in meters).
        /// </summary>
        [Tooltip("Road UV coordinates will be tiled on this distance (in meters).")]
        public float roadUvUnit = 5f;

        /// <summary>
        /// Lane line UV coordinates will be tiled on this distance (in meters).
        /// </summary>
        [Tooltip("Lane line UV coordinates will be tiled on this distance (in meters).")]
        public float lineUvUnit = 3f;

        /// <summary>
        /// Distance in meters to push lane line above the ground (to avoid clipping).
        /// </summary>
        [Tooltip("Distance in meters to push lane line above the ground (to avoid clipping).")]
        public float lineBump = 0.02f;

        /// <summary>
        /// Width in meters of a single lane line.
        /// </summary>
        [Tooltip("Width in meters of a single lane line.")]
        public float lineWidth = 0.15f;
    }
}