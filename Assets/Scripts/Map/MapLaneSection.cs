/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapLaneSection : MapData
{
    [System.NonSerialized]
    public List<MapLane> lanes = new List<MapLane>();
    [System.NonSerialized]
    public List<MapLane> lanesForward = new List<MapLane>();
    [System.NonSerialized]
    public List<MapLane> lanesReverse = new List<MapLane>();
    private bool isOneWay = true;

    public void SetLaneData()
    {
        lanes = new List<MapLane>();
        lanes.AddRange(GetComponentsInChildren<MapLane>());
        lanesForward = new List<MapLane>();
        lanesReverse = new List<MapLane>();
        isOneWay = true;

        foreach (var lane in lanes)
        {
            var laneDir = (lane.mapWorldPositions[0] - lane.mapWorldPositions[1]).normalized;
            var minDistLeft = 50f;
            var minDistRight = 50f;
            foreach (var otherLane in lanes)
            {
                if (lane == otherLane) continue;

                var otherDir = (otherLane.mapWorldPositions[0] - otherLane.mapWorldPositions[1]).normalized;
                var dot = Mathf.RoundToInt(Vector3.Dot(laneDir, otherDir));

                if (dot == 1) // same direction
                {
                    var cross = Vector3.Cross(laneDir, (lane.mapWorldPositions[0] - otherLane.mapWorldPositions[0]).normalized).y;
                    var dist = Mathf.RoundToInt(Vector3.Distance(lane.mapWorldPositions[0], otherLane.mapWorldPositions[0]));

                    if (cross < 0) // otherLane is left of lane
                    {
                        if (dist < minDistLeft) // closest lane left of lane is otherLane
                        {
                            minDistLeft = dist;
                            lane.leftLaneForward = otherLane;
                        }
                    }
                    else if (cross > 0) // otherLane is right of lane
                    {
                        if (dist < minDistRight) // closest lane right of lane is otherLane
                        {
                            minDistRight = dist;
                            lane.rightLaneForward = otherLane;
                        }
                    }

                    if (!lanesForward.Contains(lane) && !lanesReverse.Contains(lane))
                        lanesForward.Add(lane);
                    if (!lanesForward.Contains(otherLane) && !lanesReverse.Contains(otherLane))
                        lanesForward.Add(otherLane);
                }
                else if (dot == -1) // opposite direction
                {
                    isOneWay = false;
                    var cross = Vector3.Cross(laneDir, (lane.mapWorldPositions[0] - otherLane.mapWorldPositions[otherLane.mapWorldPositions.Count - 1]).normalized).y;
                    var dist = Mathf.RoundToInt(Vector3.Distance(lane.mapWorldPositions[0], otherLane.mapWorldPositions[otherLane.mapWorldPositions.Count - 1]));

                    if (cross < 0) // otherLane is left of lane
                    {
                        if (dist < minDistLeft) // closest lane left of lane is otherLane
                        {
                            minDistLeft = dist;
                            lane.leftLaneReverse = otherLane;
                        }
                    }
                    else if (cross > 0) // otherLane is right of lane
                    {
                        if (dist < minDistRight) // closest lane right of lane is otherLane
                        {
                            minDistRight = dist;
                            lane.rightLaneReverse = otherLane;
                        }
                    }

                    if (!lanesForward.Contains(lane) && !lanesReverse.Contains(lane))
                        lanesForward.Add(lane);
                    if (!lanesReverse.Contains(otherLane) && !lanesForward.Contains(otherLane))
                        lanesReverse.Add(otherLane);
                }
                if (lane.leftLaneForward != null) lane.leftLaneReverse = null; // null lane left reverse if not inside lane TODO right side
            }
        }

        int wayCount = isOneWay ? 1 : 2;
        for (int i = 0; i < wayCount; i++)
        {
            MapLane currentLane = null;
            List<MapLane> edited = new List<MapLane>();
            List<MapLane> currentLanes = i == 0 ? lanesForward : lanesReverse;

            foreach (var lane in currentLanes)
            {
                if (lane.rightLaneForward == null)
                {
                    currentLane = lane;
                    break;
                }
            }

            while (currentLane != null)
            {
                edited.Add(currentLane);
                currentLane.laneNumber = edited.Count;
                currentLane.laneCount = currentLanes.Count;
                currentLane = currentLane.leftLaneForward;
            }
        }
    }

    public override void Draw()
    {
        var start = transform.position;
        var end = start + transform.up * 2f;
        var size = new Vector3(MapAnnotationTool.PROXIMITY * 0.75f, MapAnnotationTool.PROXIMITY * 0.75f, MapAnnotationTool.PROXIMITY * 0.75f);
        AnnotationGizmos.DrawCubeWaypoint(transform.position, size, laneColor + selectedColor);
        Gizmos.color = laneColor + selectedColor;
        Gizmos.DrawLine(start, end);
        AnnotationGizmos.DrawArrowHead(start, end, laneColor + selectedColor, arrowHeadScale: MapAnnotationTool.ARROWSIZE, arrowPositionRatio: 1f);
        if (MapAnnotationTool.SHOW_HELP)
        {
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position, "    LANE_SECTION");
#endif
        }
    }
}
