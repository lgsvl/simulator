/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class MapSegment
{
    [System.NonSerialized]
    public MapSegmentBuilder builder;
    [System.NonSerialized]
    public List<Vector3> targetWorldPositions = new List<Vector3>();

    public List<Vector3> targetLocalPositions = new List<Vector3>();
    [System.NonSerialized]
    public List<MapSegment> befores = new List<MapSegment>();
    [System.NonSerialized]
    public List<MapSegment> afters = new List<MapSegment>();
}

[System.Serializable]
public class MapLaneSegment : MapSegment
{
    [Header("Apollo HD Map")]
    [System.NonSerialized]
    public string id = "";

    [Header("Autoware Vector Map")]
    [System.NonSerialized]
    public List<Map.Autoware.LaneInfo> laneInfos = new List<Map.Autoware.LaneInfo>();
}

public class MapSegmentBuilder : MonoBehaviour
{
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
}