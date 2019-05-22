/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Map.Apollo;
using HD = global::apollo.hdmap;

public class MapSpeedBumpBuilder : MapSegmentBuilder
{
    [Header("Apollo HD Map")]
    
    public GameObject nearestLaneGameObject;
    //UI related
    private static Color gizmoSurfaceColor = new Color(0.0f, 1.0f, 1.0f, 0.1f);
    private static Color gizmoLineColor = new Color(0.0f, 1.0f, 1.0f, 0.15f);
    private static Color gizmoSurfaceColor_highlight = new Color(0.1f, 1.0f, 1.0f, 0.85f);
    private static Color gizmoLineColor_highlight = new Color(0.1f, 1.0f, 1.0f, 1.0f);

    protected override Color GizmoSurfaceColor { get { return gizmoSurfaceColor; } }
    protected override Color GizmoLineColor { get { return gizmoLineColor; } }
    protected override Color GizmoSurfaceColor_highlight { get { return gizmoSurfaceColor_highlight; } }
    protected override Color GizmoLineColor_highlight { get { return gizmoLineColor_highlight; } }

    public Map.BoundLineType lineType;

    public MapSpeedBumpBuilder() : base() { }

    public override void AppendPoint()
    {
        base.AppendPoint();
    }

    public override void PrependPoint()
    {
        base.PrependPoint();
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
        Map.Draw.Gizmos.DrawLines(transform, segment.targetLocalPositions, lineColor); // put?
        Map.Draw.Gizmos.DrawArrowHeads(transform, segment.targetLocalPositions, GizmoLineColor);
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