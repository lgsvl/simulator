/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿public class HDMapStopLineSegmentBuilder : MapStopLineSegmentBuilder
{
    public HDMapStopLineSegmentBuilder() : base() { }

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

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
    }
}