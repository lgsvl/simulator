/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Editor.MapLineDetection
{
    using System;
    using UnityEngine;

    [Serializable]
    public class LineDetectionSettings : ScriptableObject
    {
        /// <summary>
        /// Enum describing data source to use for generating line data for segmentation sensor.
        /// </summary>
        public enum LineSource
        {
            /// Only use data from HD map for this environment.
            HdMap,
            /// Only use data from intensity maps on this environment's roads (image processing).
            IntensityMap,
            /// Use data from HD map for this environment, corrected towards lines detected from roads intensity maps (image processing). 
            CorrectedHdMap
        }

        /// <summary>
        /// Data source to use for generating line data for segmentation sensor.
        /// </summary>
        [Tooltip("Data source to use for generating line data for segmentation sensor.")]
        public LineSource lineSource = LineSource.HdMap;
        
        /// <summary>
        /// If true, HD map line data used by lane line sensor will be corrected based on lines detected on intensity maps.
        /// </summary>
        [Tooltip("If true, HD map line data used by lane line sensor will be corrected based on lines detected on intensity maps.")]
        public bool generateLineSensorData = true;

        /// <summary>
        /// <para>Maximum distance between line segments that can be classified as a single lane line.</para>
        /// <para>Used to classify lines detected from intensity maps.</para>
        /// </summary>
        [Tooltip("Maximum distance between line segments that can be classified as a single lane line.\n" +
                 "Used to classify lines detected from intensity maps.")]
        public float lineDistanceThreshold = 1f;
        
        /// <summary>
        /// <para>Maximum angle between line segments that can be classified as a single lane line.</para>
        /// <para>Used to classify lines detected from intensity maps.</para>
        /// </summary>
        [Tooltip("Maximum angle between line segments that can be classified as a single lane line.\n" +
                 "Used to classify lines detected from intensity maps.")]
        public float lineAngleThreshold = 10f;

        /// <summary>
        /// Maximum length of a single detected line segment. Longer lines will be split into multiple segments.
        /// </summary>
        [Tooltip("Maximum length of a single detected line segment. Longer lines will be split into multiple segments.")]
        public float maxLineSegmentLength = 10f;
        
        /// <summary>
        /// <para>Maximum valid distance between line center and segments creating it.</para>
        /// <para>Used to filter out large clusters of spread parallel line segments.</para>
        /// </summary>
        [Tooltip("Maximum valid distance between line center and segments creating it.\n" +
                 "Used to filter out large clusters of spread parallel line segments.")]
        public float worstFitThreshold = 0.6f;

        /// <summary>
        /// <para>Maximum distance between segments that should be considered parts of a single line.</para>
        /// <para>Used to filter out large clusters of spread parallel line segments.</para>
        /// </summary>
        [Tooltip("Maximum distance between segments that should be considered parts of a single line.\n" +
                 "Used to filter out large clusters of spread parallel line segments.")]
        public float jointLineThreshold = 0.1f;

        /// <summary>
        /// <para>Minimum viable width of a lane line.</para>
        /// <para>Used to filter out linear artifacts from detected line segments.</para>
        /// </summary>
        [Tooltip("Minimum viable width of a lane line.\n" +
                 "Used to filter out linear artifacts from detected line segments.")]
        public float minWidthThreshold = 0.02f;

        /// <summary>
        /// <para>Maximum distance between detected lines that could be considered parts of a single curve.</para>
        /// <para>Lines below this threshold will have their ends snap together to create better curve approximation.</para>
        /// </summary>
        [Tooltip("Maximum distance between detected lines that could be considered parts of a single curve.\n" +
                 "Lines below this threshold will have their ends snap together to create better curve approximation.")]
        public float worldSpaceSnapDistance = 0.3f;

        /// <summary>
        /// Maximum distance between detected lines that could be considered separate parts of a single dotted line.
        /// </summary>
        [Tooltip("Maximum distance between detected lines that could be considered separate parts of a single dotted line.")]
        public float worldDottedLineDistanceThreshold = 8f;
        
        /// <summary>
        /// Maximum angle between detected lines that could be considered parts of a single curve (solid or dotted).
        /// </summary>
        [Tooltip("Maximum angle between detected lines that could be considered parts of a single curve (solid or dotted).")]
        public float worldSpaceSnapAngle = 30f;
    }
}