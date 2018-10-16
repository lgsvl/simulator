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
}