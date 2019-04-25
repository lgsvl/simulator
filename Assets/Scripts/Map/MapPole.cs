/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapPole : MapData
{
    public override void Draw()
    {
        var start = transform.position;
        var end = start + transform.up * 4f;

        AnnotationGizmos.DrawWaypoint(transform.position, MapAnnotationTool.PROXIMITY * 0.35f, poleColor, poleColor);
        Gizmos.color = poleColor;
        Gizmos.DrawLine(start, end);
        AnnotationGizmos.DrawArrowHead(start, end, poleColor, arrowHeadScale: MapAnnotationTool.ARROWSIZE, arrowPositionRatio: 1f);
        if (MapAnnotationTool.SHOW_HELP)
            UnityEditor.Handles.Label(transform.position, "    POLE");
    }
}
