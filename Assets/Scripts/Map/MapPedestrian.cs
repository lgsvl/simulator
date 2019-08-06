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
    public class MapPedestrian : MapDataPoints
    {
        public MapAnnotationTool.PedestrianPathType type = MapAnnotationTool.PedestrianPathType.SIDEWALK;
        public int PedVolume { get; set; } = 1;

        public override void Draw()
        {
            if (mapLocalPositions.Count < 2) return;

            AnnotationGizmos.DrawWaypoints(transform, mapLocalPositions, MapAnnotationTool.PROXIMITY * 0.5f, pedestrianColor + selectedColor);
            AnnotationGizmos.DrawLines(transform, mapLocalPositions, pedestrianColor + selectedColor);
            AnnotationGizmos.DrawArrowHeads(transform, mapLocalPositions, pedestrianColor + selectedColor);
            if (MapAnnotationTool.SHOW_HELP)
            {
#if UNITY_EDITOR
                UnityEditor.Handles.Label(transform.position, "    PEDESTRIAN " + type);
#endif
            }
        }
    }
}
