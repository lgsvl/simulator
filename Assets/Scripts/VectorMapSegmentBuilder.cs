/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class Vector3Toggle
{
    public Vector3 pos;
}

[System.Serializable]
public class VectorMapSegment
{
    [System.NonSerialized]
    public VectorMapSegmentBuilder builder;
    [System.NonSerialized]
    public List<Vector3> targetWorldPositions = new List<Vector3>();

    public List<Vector3> targetLocalPositions = new List<Vector3>();
    [System.NonSerialized]
    public List<VectorMapSegment> befores = new List<VectorMapSegment>();
    [System.NonSerialized]
    public List<VectorMapSegment> afters = new List<VectorMapSegment>();
}

[System.Serializable]
public struct LaneInfo
{
    public int laneCount;
    public int laneNumber;
}

[System.Serializable]
public class VectorMapLaneSegment : VectorMapSegment
{
    public List<LaneInfo> laneInfos = new List<LaneInfo>();
}

public class VectorMapSegmentBuilder : MonoBehaviour
{
    public bool showHandles = false;

    public VectorMapSegment segment = new VectorMapSegment();

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