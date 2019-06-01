/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Simulator.Map;

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

            AnnotationGizmos.DrawWaypoint(transform.position, MapAnnotationTool.PROXIMITY * 0.35f, poleColor + selectedColor);
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