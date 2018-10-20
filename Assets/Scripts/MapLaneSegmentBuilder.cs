/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using UnityEngine;
using Map.Apollo;

public class MapLaneSegmentBuilder : MapSegmentBuilder
{
    [Header("Apollo HD Map")]
    public Lane.LaneTurn laneTurn = Lane.LaneTurn.NO_TURN;
    public MapLaneSegmentBuilder leftNeighborForward;
    public MapLaneSegmentBuilder rightNeighborForward;
    public MapLaneSegmentBuilder leftNeighborReverse;
    public MapLaneSegmentBuilder rightNeighborReverse;

    [Header("Autoware Vector Map")]
    public Map.Autoware.LaneInfo laneInfo;

    //UI related
    private static Color gizmoSurfaceColor = Color.cyan * (new Color(1.0f, 1.0f, 1.0f, 0.1f));
    private static Color gizmoLineColor = Color.cyan;

    public MapLaneSegmentBuilder() : base() { }

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

    public override void DoublePoints()
    {
        base.DoublePoints();
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        var localPositions = segment.targetLocalPositions;

        var pointCount = localPositions.Count;

        if (pointCount < 2)
        {
            return;
        }

        var nodeRadius = Map.Autoware.VectorMapTool.PROXIMITY * 0.5f;

        for (int i = 0; i < pointCount - 1; i++)
        {
            var start = transform.TransformPoint(localPositions[i]);
            var end = transform.TransformPoint(localPositions[i + 1]);
            Gizmos.color = gizmoSurfaceColor;
            Gizmos.DrawSphere(start, nodeRadius);
            Gizmos.DrawWireSphere(start, nodeRadius);
            Map.Draw.Gizmos.DrawArrow(start, end, gizmoLineColor, Map.Autoware.VectorMapTool.ARROWSIZE);
        }
        
        Gizmos.color = gizmoSurfaceColor;
        var last = transform.TransformPoint(localPositions[pointCount - 1]);
        Gizmos.DrawSphere(last, nodeRadius);
        Gizmos.DrawWireSphere(last, nodeRadius);
    }
}