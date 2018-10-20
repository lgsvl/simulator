/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;
using System.Collections.Generic;
using Map.Apollo;

public class HDMapSegmentInfo
{
    [System.NonSerialized]
    public string id = null;
    [System.NonSerialized]
    public Lane.LaneTurn laneTurn = Lane.LaneTurn.NO_TURN;
    [System.NonSerialized]
    public MapSegment leftNeighborSegmentForward = null;
    [System.NonSerialized]
    public MapSegment rightNeighborSegmentForward = null;
    [System.NonSerialized]
    public MapSegment leftNeighborSegmentReverse = null;
    [System.NonSerialized]
    public MapSegment rightNeighborSegmentReverse = null;
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
    public bool showHandles = false;

    public MapSegment segment = new MapSegment();

    public virtual void AddPoint()
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

    public virtual void RemovePoint()
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

    public virtual void DoublePoints()
    {
        Map.MapTool.DoubleSegmentResolution(segment);
    }

    protected virtual void OnDrawGizmos()
    {  
        //placeholder
    }
}