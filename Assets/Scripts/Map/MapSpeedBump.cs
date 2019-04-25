/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapSpeedBump : MapData
{
    public bool displayHandles = false;
    public List<Vector3> mapLocalPositions = new List<Vector3>();
    [System.NonSerialized]
    public List<Vector3> mapWorldPositions = new List<Vector3>();

    public override void Draw()
    {
        if (mapLocalPositions.Count < 2) return;

        AnnotationGizmos.DrawWaypoints(transform, mapLocalPositions, MapAnnotationTool.PROXIMITY * 0.5f, speedBumpColor, speedBumpColor);
        AnnotationGizmos.DrawLines(transform, mapLocalPositions, speedBumpColor);
        if (MapAnnotationTool.SHOW_HELP)
            UnityEditor.Handles.Label(transform.position, "    SPEEDBUMP");
    }
}
