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
        public enum LineSource
        {
            HdMap,
            IntensityMap
        }

        public LineSource lineSource = LineSource.HdMap;
        
        public float lineDistanceThreshold = 1f;
        public float lineAngleThreshold = 10f;

        public float maxLineSegmentLength = 10f;
        public float worstFitThreshold = 0.6f;
        public float minWidthThreshold = 0.02f;

        public float jointLineThreshold = 0.1f;

        public float worldSpaceSnapDistance = 0.3f;
        public float worldSpaceSnapAngle = 10f;
    }
}