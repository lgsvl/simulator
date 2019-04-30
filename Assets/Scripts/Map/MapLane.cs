/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapLane : MapData
{
    public bool displayHandles = false;
    public List<Vector3> mapLocalPositions = new List<Vector3>();
    [System.NonSerialized]
    public List<Vector3> mapWorldPositions = new List<Vector3>();
    [System.NonSerialized]
    public List<MapLane> befores = new List<MapLane>();
    [System.NonSerialized]
    public List<MapLane> afters = new List<MapLane>();

    [System.NonSerialized]
    public string id = null; // TODO move to mapdata?
    [System.NonSerialized]
    public MapLane leftLaneForward = null;
    [System.NonSerialized]
    public MapLane rightLaneForward = null;
    [System.NonSerialized]
    public MapLane leftLaneReverse = null;
    [System.NonSerialized]
    public MapLane rightLaneReverse = null;
    [System.NonSerialized]
    public int laneCount;
    [System.NonSerialized]
    public int laneNumber;

    // TODO create list in map tool?
    //[System.NonSerialized]
    //public List<Map.Autoware.LaneInfo> laneInfos = new List<Map.Autoware.LaneInfo>();

    public List<MapLane> yieldToLanes = new List<MapLane>(); // TODO calc
    [System.NonSerialized]
    public List<MapLane> nextConnectedLanes = new List<MapLane>();
    [System.NonSerialized]
    public MapLine stopLine = null;
    public bool isTrafficLane { get; set; } = false;

    public LaneTurnType laneTurnType = LaneTurnType.NO_TURN;
    public LaneBoundaryType leftBoundType;
    public LaneBoundaryType rightBoundType;
    public float speedLimit = 20.0f;

    public override void Draw()
    {
        if (mapLocalPositions.Count < 2) return;

        AnnotationGizmos.DrawWaypoints(transform, mapLocalPositions, MapAnnotationTool.PROXIMITY * 0.5f, laneColor + selectedColor);
        AnnotationGizmos.DrawLines(transform, mapLocalPositions, laneColor + selectedColor);
        AnnotationGizmos.DrawArrowHeads(transform, mapLocalPositions, laneColor + selectedColor);
        if (MapAnnotationTool.SHOW_HELP)
            UnityEditor.Handles.Label(transform.position, "    LANE " + laneTurnType);
    }
}
