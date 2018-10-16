/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;
using Map;
using Map.Autoware;

public class MapBoundaryLineSegmentBuilder : MapLineSegmentBuilder
{
    public BoundLineType lineType;

    public MapBoundaryLineSegmentBuilder() : base() { }

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
}