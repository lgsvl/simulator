/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using UnityEngine;

public class VectorMapPoleBuilder : MonoBehaviour
{
    public List<MapSignalLightBuilder> signalLights;
    public float length = 6.75f;

    //UI related
    private static Color gizmoSurfaceColor = Color.white * new Color(1.0f, 1.0f, 1.0f, 0.1f);
    private static Color gizmoLineColor = Color.white * new Color(1.0f, 1.0f, 1.0f, 0.15f);
    private static Color gizmoSurfaceColor_highlight = Color.white * new Color(1.0f, 1.0f, 1.0f, 0.8f);
    private static Color gizmoLineColor_highlight = Color.white * new Color(1.0f, 1.0f, 1.0f, 1f);

    protected virtual Color GizmoSurfaceColor { get { return gizmoSurfaceColor; } }
    protected virtual Color GizmoLineColor { get { return gizmoLineColor; } }
    protected virtual Color GizmoSurfaceColor_highlight { get { return gizmoSurfaceColor_highlight; } }
    protected virtual Color GizmoLineColor_highlight { get { return gizmoLineColor_highlight; } }

    private void Draw(bool highlight = false)
    {
        var start = transform.position;
        var end = start + transform.forward * length;

        var surfaceColor = highlight ? GizmoSurfaceColor_highlight : GizmoSurfaceColor;
        var lineColor = highlight ? GizmoLineColor_highlight : GizmoLineColor;

        Map.Draw.Gizmos.DrawWaypoint(transform.position, Map.Autoware.VectorMapTool.PROXIMITY * 0.5f, surfaceColor, lineColor);
        Gizmos.color = lineColor;
        Gizmos.DrawLine(start, end);
        Map.Draw.Gizmos.DrawArrowHead(start, end, lineColor, arrowHeadScale: Map.Autoware.VectorMapTool.ARROWSIZE, arrowPositionRatio: 1f);
    }

    protected virtual void OnDrawGizmos()
    {
        Draw();
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Draw(highlight: true);
    }
}