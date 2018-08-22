/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿public class MapLaneSegmentBuilder : MapSegmentBuilder
{
    public Autoware.LaneInfo laneInfo;
    public MapBoundLineSegmentBuilder leftBoundary;
    public MapBoundLineSegmentBuilder rightBoundary;

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
}