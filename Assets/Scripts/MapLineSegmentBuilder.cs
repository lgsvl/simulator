/**
* Copyright (c) 2018 LG Electronics, Inc.
*
* This software contains code licensed as described in LICENSE.
*
*/

using System.Collections.Generic;
using UnityEngine;

public abstract class MapLineSegmentBuilder : MapSegmentBuilder
{
    public MapLineSegmentBuilder() : base() { }

    public override void AddPoint()
    {
        base.AddPoint();
    }

    public override void RemovePoint()
    {
        base.RemovePoint();
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

        Map.Draw.Gizmos.DrawWaypoints(transform, segment.targetLocalPositions, Map.Autoware.VectorMapTool.PROXIMITY * 0.5f, surfaceColor, lineColor);
        Map.Draw.Gizmos.DrawLines(transform, segment.targetLocalPositions, lineColor);
    }

    protected override void OnDrawGizmos()
    {
        Draw();
    }

    protected override void OnDrawGizmosSelected()
    {
        Draw(highlight: true);
    }
}