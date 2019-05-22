/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


using UnityEngine;
using System.Collections.Generic;
using Map.Apollo;
using HD = global::apollo.hdmap;

public class HDMapSegmentInfo
{
    [System.NonSerialized]
    public string id = null;
    [System.NonSerialized]
    public HD.Lane.LaneTurn laneTurn = HD.Lane.LaneTurn.NO_TURN;
    [System.NonSerialized]
    public float speedLimit;
    [System.NonSerialized]
    public MapSegment leftNeighborSegmentForward = null;
    [System.NonSerialized]
    public MapSegment rightNeighborSegmentForward = null;
    [System.NonSerialized]
    public MapSegment leftNeighborSegmentReverse = null;
    [System.NonSerialized]
    public MapSegment rightNeighborSegmentReverse = null;
    [System.NonSerialized]
    public HD.LaneBoundaryType.Type leftBoundType;
    [System.NonSerialized]
    public HD.LaneBoundaryType.Type rightBoundType;
}

public class VectorMapSegmentInfo
{
    [System.NonSerialized]
    public List<Map.Autoware.LaneInfo> laneInfos = new List<Map.Autoware.LaneInfo>();
}

[System.Serializable]
public class MapSegment
{
    public List<Vector3> targetLocalPositions = new List<Vector3>();
    [System.NonSerialized]
    public MapSegmentBuilder builder;
    [System.NonSerialized]
    public List<Vector3> targetWorldPositions = new List<Vector3>();
    [System.NonSerialized]
    public List<MapSegment> befores = new List<MapSegment>();
    [System.NonSerialized]
    public List<MapSegment> afters = new List<MapSegment>();

    [System.NonSerialized]
    public HDMapSegmentInfo hdmapInfo;

    [System.NonSerialized]
    public VectorMapSegmentInfo vectormapInfo;

    public void Clear()
    {        
        targetWorldPositions.Clear();
        befores.Clear();
        afters.Clear();
        hdmapInfo = null;
        vectormapInfo = null;
    }
}

public abstract class MapSegmentBuilder : MonoBehaviour
{    
    //UI related
    public bool displayHandles = false;
    private static Color gizmoSurfaceColor = Color.white * new Color(1.0f, 1.0f, 1.0f, 0.1f);
    private static Color gizmoLineColor = Color.white * new Color(1.0f, 1.0f, 1.0f, 0.15f);
    private static Color gizmoSurfaceColor_highlight = Color.white * new Color(1.0f, 1.0f, 1.0f, 0.8f);
    private static Color gizmoLineColor_highlight = Color.white * new Color(1.0f, 1.0f, 1.0f, 1f);

    protected virtual Color GizmoSurfaceColor { get { return gizmoSurfaceColor; } }
    protected virtual Color GizmoLineColor { get { return gizmoLineColor; } }
    protected virtual Color GizmoSurfaceColor_highlight { get { return gizmoSurfaceColor_highlight; } }
    protected virtual Color GizmoLineColor_highlight { get { return gizmoLineColor_highlight; } }

    //segment that holds waypoints
    public MapSegment segment = new MapSegment();

    public virtual void AppendPoint()
    {
        var targetLocalPositions = segment.targetLocalPositions;
        if (targetLocalPositions.Count > 0)
        {
            targetLocalPositions.Add(targetLocalPositions[targetLocalPositions.Count - 1]);
        }
        else
        {
            targetLocalPositions.Add(transform.InverseTransformPoint(transform.position));
        }
    }

    public virtual void PrependPoint()
    {
        var targetLocalPositions = segment.targetLocalPositions;
        if (targetLocalPositions.Count > 0)
        {
            targetLocalPositions.Insert(0, targetLocalPositions[0]);
        }
        else
        {
            targetLocalPositions.Add(transform.InverseTransformPoint(transform.position));
        }
    }

    public virtual void RemoveFirstPoint()
    {
        var targetLocalPositions = segment.targetLocalPositions;
        if (targetLocalPositions.Count > 0)
        {
            targetLocalPositions.RemoveAt(0);
        }
    }

    public virtual void RemoveLastPoint()
    {
        var targetLocalPositions = segment.targetLocalPositions;
        if (targetLocalPositions.Count > 0)
        {
            targetLocalPositions.RemoveAt(targetLocalPositions.Count - 1);
        }
    }

    public virtual void ReversePoints()
    {
        segment.targetLocalPositions.Reverse();
    }

    public virtual void ResetPoints()
    {
        var targetLocalPositions = segment.targetLocalPositions;
        for (int i = 0; i < targetLocalPositions.Count; i++)
        {
            targetLocalPositions[i] = transform.InverseTransformPoint(transform.position);
        }
    }

    public bool DoubleSubsegments()
    {
        return Map.MapTool.DoubleSubsegmentResolution(segment);
    }

    public bool HalfSubsegments()
    {
        return Map.MapTool.HalfSubsegmentResolution(segment);
    }

    private void Draw( bool highlight = false)
    {
        if (segment.targetLocalPositions.Count < 2) return;

        var surfaceColor = highlight ? GizmoSurfaceColor_highlight : GizmoSurfaceColor;
        var lineColor = highlight ? GizmoLineColor_highlight : GizmoLineColor;

        Map.Draw.Gizmos.DrawWaypoints(transform, segment.targetLocalPositions, Map.MapTool.PROXIMITY * 0.5f, surfaceColor, lineColor); 
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