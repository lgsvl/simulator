/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapSign : MapData
{
    public SignType signType;
    public MapLine stopLine;

    private void Draw()
    {
        var start = transform.position;
        var end = start + transform.up * 2f;

        AnnotationGizmos.DrawWaypoint(transform.position, MapAnnotationTool.PROXIMITY * 0.35f, stopSignColor, stopSignColor);
        Gizmos.color = stopSignColor;
        Gizmos.DrawLine(start, end);
        AnnotationGizmos.DrawArrowHead(start, end, stopSignColor, arrowHeadScale: MapAnnotationTool.ARROWSIZE, arrowPositionRatio: 1f);
    }

    protected virtual void OnDrawGizmos()
    {
        if (MapAnnotationTool.SHOW_MAP_ALL)
            Draw();
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (MapAnnotationTool.SHOW_MAP_SELECTED)
            Draw();
    }
}
