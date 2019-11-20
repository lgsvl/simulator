/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Simulator.Map
{
    public class MapAnnotationTool : MonoBehaviour
    {
        public enum CreateMode
        {
            NONE,
            LANE_LINE,
            SIGNAL,
            SIGN,
            POLE,
            PEDESTRIAN,
            JUNCTION,
            CROSSWALK,
            CLEARAREA,
            PARKINGSPACE,
            SPEEDBUMP,
        };
        public static CreateMode createMode { get; set; } = CreateMode.NONE;

        public enum PedestrianPathType
        {
            SIDEWALK,
            CROSSWALK,
            JAYWALK
        };

        public static bool TOOL_ACTIVE { get; set; } = false;
        public static bool SHOW_HELP { get; set; } = false;
        public static bool SHOW_MAP_ALL { get; set; } = false;
        public static bool SHOW_MAP_SELECTED { get; set; } = false;
        public static float PROXIMITY { get; set; } = 1.0f;
        public static float EXPORT_SCALE_FACTOR = 1.0f;
        public static float ARROWSIZE = 50.0f;
    }
}