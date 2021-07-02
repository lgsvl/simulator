/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using UnityEngine;

namespace Simulator.Map
{
    public class MapPole : MapData
    {
        public List<MapSignal> signalLights = new List<MapSignal>();
        public float length = 6.75f;

        public override void Draw()
        {
            var start = transform.position;
            var end = start + transform.up * 4f;

            AnnotationGizmos.DrawWaypoint(transform.position, MapAnnotationTool.WAYPOINT_SIZE * 0.5f, poleColor + selectedColor);
            Gizmos.color = poleColor + selectedColor;
            Gizmos.DrawLine(start, end);
            AnnotationGizmos.DrawArrowHead(start, end, poleColor + selectedColor, arrowHeadScale: MapAnnotationTool.ARROWSIZE, arrowPositionRatio: 1f);
            if (MapAnnotationTool.SHOW_HELP)
            {
#if UNITY_EDITOR
                UnityEditor.Handles.Label(transform.position, "    POLE");
#endif
            }
        }
    }
}