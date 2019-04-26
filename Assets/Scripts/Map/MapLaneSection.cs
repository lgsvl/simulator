using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapLaneSection : MapData
{
    //

    public override void Draw()
    {
        var start = transform.position;
        var end = start + transform.up * 2f;
        var size = new Vector3(MapAnnotationTool.PROXIMITY * 0.75f, MapAnnotationTool.PROXIMITY * 0.75f, MapAnnotationTool.PROXIMITY * 0.75f);
        AnnotationGizmos.DrawCubeWaypoint(transform.position, size, laneColor);
        Gizmos.color = laneColor;
        Gizmos.DrawLine(start, end);
        AnnotationGizmos.DrawArrowHead(start, end, laneColor, arrowHeadScale: MapAnnotationTool.ARROWSIZE, arrowPositionRatio: 1f);
        if (MapAnnotationTool.SHOW_HELP)
            UnityEditor.Handles.Label(transform.position, "    LANE_SECTION");
    }
}
