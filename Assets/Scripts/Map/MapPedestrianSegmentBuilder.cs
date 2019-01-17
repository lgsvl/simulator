/**
* Copyright (c) 2018 LG Electronics, Inc.
*
* This software contains code licensed as described in LICENSE.
*
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapPedestrianSegmentBuilder : MapLineSegmentBuilder
{
    //UI related
    private static Color gizmoSurfaceColor = new Color(0.0f, 1.0f, 0.0f, 0.1f);
    private static Color gizmoLineColor = new Color(0.0f, 1.0f, 0.0f, 0.3f);
    private static Color gizmoSurfaceColor_highlight = new Color(0.1f, 1.0f, 0.1f, 0.85f);
    private static Color gizmoLineColor_highlight = new Color(0.1f, 1.0f, 0.1f, 1.0f);

    protected override Color GizmoSurfaceColor { get { return gizmoSurfaceColor; } }
    protected override Color GizmoLineColor { get { return gizmoLineColor; } }
    protected override Color GizmoSurfaceColor_highlight { get { return gizmoSurfaceColor_highlight; } }
    protected override Color GizmoLineColor_highlight { get { return gizmoLineColor_highlight; } }

    public MapPedestrianSegmentBuilder() : base() { }

    public override void AppendPoint()
    {
        base.AppendPoint();
    }

    public override void RemoveLastPoint()
    {
        base.RemoveLastPoint();
    }

    public override void ResetPoints()
    {
        base.ResetPoints();
    }

    private void Draw(bool highlight = false)
    {
        if (segment.targetLocalPositions.Count < 2) return;

        var surfaceColor = highlight ? GizmoSurfaceColor_highlight : GizmoSurfaceColor;
        var lineColor = highlight ? GizmoLineColor_highlight : GizmoLineColor;

        Map.Draw.Gizmos.DrawWaypoints(transform, segment.targetLocalPositions, Map.MapTool.PROXIMITY * 0.5f, surfaceColor, lineColor);
        Map.Draw.Gizmos.DrawLines(transform, segment.targetLocalPositions, lineColor);
    }

    protected override void OnDrawGizmos()
    {
        if (!Map.MapTool.showMap) return;
        Draw();
    }

    protected override void OnDrawGizmosSelected()
    {
        if (!Map.MapTool.showMap) return;
        Draw(highlight: true);
    }
}
