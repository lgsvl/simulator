using System;
using System.Collections.Generic;
using UnityEngine;

using apollo.hdmap;
using apollo.common;
using Util;

public class LaneInfo
{
    public LaneInfo(Lane lane)
    {
        _lane = lane;
        SetLeftBoundary();
        SetRightBoundary();

        SetLeftBoundaryType();
        SetRightBoundaryType();

    }

    public Id id { get; set; }
    public Lane _lane;
    public List<PointENU> leftBoundary { get; set; }
    public List<PointENU> rightBoundary { get; set; }
    public LaneBoundaryType.Type leftBoundaryType { get; set; }
    public LaneBoundaryType.Type rightBoundaryType { get; set; }

    public string name => "Lane";

    public List<Id> predecessor_id()
    {
        List<Id> ids = new List<Id>();
        foreach(var id in _lane.predecessor_id) {
            ids.Add(id);
        }
        return ids;
    }

    public List<Id> successor_id()
    {
        List<Id> ids = new List<Id>();
        foreach(var id in _lane.successor_id)
        {
            ids.Add(id);
        }
        return ids;
    }

    private void SetLeftBoundaryType()
    {
        leftBoundaryType = _lane.right_boundary.boundary_type[0].types[0];
        //Debug.LogError("left type:" + _lane.left_boundary.boundary_type[0].types[0]);
    }

    private void SetRightBoundaryType()
    {
        rightBoundaryType = _lane.right_boundary.boundary_type[0].types[0];
        //Debug.LogError("right type:" + _lane.right_boundary.boundary_type[0].types[0]);
    }

    private void SetLeftBoundary()
    {
        List<PointENU> points = new List<PointENU>();
        foreach(var seg in _lane.left_boundary.curve.segment)
        {
            foreach(var point in seg.line_segment.point)
            {
                PointENU _point = Tools.Transform(point);
                points.Add(_point);
            }
        }

        leftBoundary = points;
    }

    private void SetRightBoundary()
    {
        List<PointENU> points = new List<PointENU>();
        foreach (var seg in _lane.right_boundary.curve.segment)
        {
            foreach (var point in seg.line_segment.point)
            {
                PointENU _point = Tools.Transform(point);
                points.Add(_point);
            }
        }

        rightBoundary = points;
    }

    public List<Id> GetLeftNeighborForwardLaneId()
    {
        List<Id> ids = new List<Id>();
        foreach (var id in _lane.left_neighbor_forward_lane_id)
        {
            ids.Add(id);
        }
        return ids;
    }

    public List<Id> GetRightNeighborForwardLaneId()
    {
        List<Id> ids = new List<Id>();
        foreach (var id in _lane.right_neighbor_forward_lane_id)
        {
            ids.Add(id);
        }
        return ids;
    }


}
