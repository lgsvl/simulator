/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapLaneSection : MonoBehaviour
{
    public List<MapLaneSegmentBuilder> lanes = new List<MapLaneSegmentBuilder>();
    private bool isOneWay = true;
    public List<MapLaneSegmentBuilder> lanesForward = new List<MapLaneSegmentBuilder>();
    public List<MapLaneSegmentBuilder> lanesReverse = new List<MapLaneSegmentBuilder>();

    public void SetLaneData()
    {
        lanes = new List<MapLaneSegmentBuilder>();
        lanes.AddRange(GetComponentsInChildren<MapLaneSegmentBuilder>());
        lanesForward = new List<MapLaneSegmentBuilder>();
        lanesReverse = new List<MapLaneSegmentBuilder>();
        isOneWay = true;

        foreach (var lane in lanes)
        {
            var laneDir = (lane.segment.targetWorldPositions[0] - lane.segment.targetWorldPositions[1]).normalized;
            var minDistLeft = 50f;
            var minDistRight = 50f;
            foreach (var otherLane in lanes)
            {
                if (lane == otherLane) continue;

                var otherDir = (otherLane.segment.targetWorldPositions[0] - otherLane.segment.targetWorldPositions[1]).normalized;
                var dot = Mathf.RoundToInt(Vector3.Dot(laneDir, otherDir));
                
                if (dot == 1) // same direction
                {
                    var cross = Vector3.Cross(laneDir, (lane.segment.targetWorldPositions[0] - otherLane.segment.targetWorldPositions[0]).normalized).y;
                    var dist = Mathf.RoundToInt(Vector3.Distance(lane.segment.targetWorldPositions[0], otherLane.segment.targetWorldPositions[0]));
                    
                    if (cross < 0) // otherLane is left of lane
                    {
                        if (dist < minDistLeft) // closest lane left of lane is otherLane
                        {
                            minDistLeft = dist;
                            lane.leftForward = otherLane;
                        }
                    }
                    else if (cross > 0) // otherLane is right of lane
                    {
                        if (dist < minDistRight) // closest lane right of lane is otherLane
                        {
                            minDistRight = dist;
                            lane.rightForward = otherLane;
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
                    var cross = Vector3.Cross(laneDir, (lane.segment.targetWorldPositions[0] - otherLane.segment.targetWorldPositions[otherLane.segment.targetWorldPositions.Count - 1]).normalized).y;
                    var dist = Mathf.RoundToInt(Vector3.Distance(lane.segment.targetWorldPositions[0], otherLane.segment.targetWorldPositions[otherLane.segment.targetWorldPositions.Count - 1]));

                    if (cross < 0) // otherLane is left of lane
                    {
                        if (dist < minDistLeft) // closest lane left of lane is otherLane
                        {
                            minDistLeft = dist;
                            lane.leftReverse = otherLane;
                        }
                    }
                    else if (cross > 0) // otherLane is right of lane
                    {
                        if (dist < minDistRight) // closest lane right of lane is otherLane
                        {
                            minDistRight = dist;
                            lane.rightReverse = otherLane;
                        }
                    }

                    if (!lanesForward.Contains(lane) && !lanesReverse.Contains(lane))
                        lanesForward.Add(lane);
                    if (!lanesReverse.Contains(otherLane) && !lanesForward.Contains(otherLane))
                        lanesReverse.Add(otherLane);
                }
                if (lane.leftForward != null) lane.leftReverse = null; // null lane left reverse if not inside lane TODO right side
            }
        }
        
        int wayCount = isOneWay ? 1 : 2;
        for (int i = 0; i < wayCount; i++)
        {
            MapLaneSegmentBuilder currentLane = null;
            List<MapLaneSegmentBuilder> edited = new List<MapLaneSegmentBuilder>();
            List<MapLaneSegmentBuilder> currentLanes = i == 0 ? lanesForward : lanesReverse;
            
            foreach (var lane in currentLanes)
            {
                if (lane.rightForward == null)
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
                currentLane = currentLane.leftForward;
            }
        }
    }
}
