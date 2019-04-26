/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapIntersection : MapData
{

    public override void Draw()
    {
        var start = transform.position;
        var end = start + transform.up * 6f;

        AnnotationGizmos.DrawWaypoint(transform.position, MapAnnotationTool.PROXIMITY * 0.5f, intersectionColor);
        Gizmos.color = intersectionColor;
        Gizmos.DrawLine(start, end);
        AnnotationGizmos.DrawArrowHead(start, end, intersectionColor, arrowHeadScale: MapAnnotationTool.ARROWSIZE, arrowPositionRatio: 1f);
        if (MapAnnotationTool.SHOW_HELP)
            UnityEditor.Handles.Label(transform.position, "    INTERSECTION");
    }
}
