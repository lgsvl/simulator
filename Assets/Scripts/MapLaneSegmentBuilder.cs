/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using UnityEngine;

public class MapLaneSegmentBuilder : MapSegmentBuilder
{
    [Header("Apollo HD Map")]
    public MapLaneSegmentBuilder leftNeighborForward;
    public MapLaneSegmentBuilder rightNeighborForward;
    public MapLaneSegmentBuilder leftNeighborReverse;
    public MapLaneSegmentBuilder rightNeighborReverse;

    [Header("Autoware Vector Map")]
    public Map.Autoware.LaneInfo laneInfo;

    public MapLaneSegmentBuilder() : base() { }

    void OnValidate()
    {
        if (leftNeighborForward != null && leftNeighborForward != this)
        {
            leftNeighborForward.rightNeighborForward = this;
        }
        if (rightNeighborForward != null && rightNeighborForward != this)
        {
            rightNeighborForward.leftNeighborForward = this;
        }
        if (leftNeighborReverse != null && leftNeighborReverse != this)
        {
            leftNeighborReverse.leftNeighborReverse = this;
        }
        if (rightNeighborReverse != null && rightNeighborReverse != this)
        {
            rightNeighborReverse.rightNeighborReverse = this;
        }
    }

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