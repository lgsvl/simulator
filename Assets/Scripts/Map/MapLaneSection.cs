/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HD = global::apollo.hdmap;

public class MapLaneSection : MonoBehaviour
{
    [System.NonSerialized]
    public List<MapLaneSegmentBuilder> lanes = new List<MapLaneSegmentBuilder>();
    [System.NonSerialized]
    public List<MapLaneSegmentBuilder> lanesForward = new List<MapLaneSegmentBuilder>();
    [System.NonSerialized]
    public List<MapLaneSegmentBuilder> lanesReverse = new List<MapLaneSegmentBuilder>();
    private bool isOneWay = true;

    public void SetLaneData()
    {
        lanes = new List<MapLaneSegmentBuilder>();
        lanes.AddRange(GetComponentsInChildren<MapLaneSegmentBuilder>());
        lanesForward = new List<MapLaneSegmentBuilder>();
        lanesReverse = new List<MapLaneSegmentBuilder>();
        isOneWay = true;

        // for laneSections with branching lanes, all lanes should have at least 3 waypoints
        for (var i = 0; i < lanes.Count; i++)
        {
            var lane = lanes[i];
            // set default left/right bound type.
            lane.leftBoundType = HD.LaneBoundaryType.Type.DOTTED_WHITE;
            lane.rightBoundType = HD.LaneBoundaryType.Type.DOTTED_WHITE;

            var idx = 1; // index to compute vector from lane to otherLane and distance between those two lanes
            var laneDir = (lane.segment.targetWorldPositions[1] - lane.segment.targetWorldPositions[0]).normalized;
            var minDistLeft = 50f;
            var minDistRight = 50f;

            for (var j = 0; j < lanes.Count; j++)
            {
                var otherLane = lanes[j];
                if (lane == otherLane) continue;

                // Check if these two lanes have same directions by check the dist between 1st pos in lane and (the 1st and last pos in otherLane).
                var isSameDirection = true;
                var distFirstToFirst = Vector3.Distance(lane.segment.targetWorldPositions[0], otherLane.segment.targetWorldPositions[0]);
                var distFirstToLast = Vector3.Distance(lane.segment.targetWorldPositions[0], otherLane.segment.targetWorldPositions[otherLane.segment.targetWorldPositions.Count - 1]);
                if (distFirstToFirst > distFirstToLast)
                {
                    isSameDirection = false;
                }

                if (isSameDirection) // same direction
                {
                    var cross = Vector3.Cross(laneDir, (otherLane.segment.targetWorldPositions[idx] - lane.segment.targetWorldPositions[idx]).normalized).y;
                    var dist = Mathf.RoundToInt(Vector3.Distance(lane.segment.targetWorldPositions[idx], otherLane.segment.targetWorldPositions[idx]));

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
                else // opposite direction
                {

                    isOneWay = false;
                    var cross = Vector3.Cross(laneDir, (otherLane.segment.targetWorldPositions[otherLane.segment.targetWorldPositions.Count - 1 - idx] - lane.segment.targetWorldPositions[idx]).normalized).y;
                    var dist = Mathf.RoundToInt(Vector3.Distance(lane.segment.targetWorldPositions[idx], otherLane.segment.targetWorldPositions[otherLane.segment.targetWorldPositions.Count - 1 - idx]));

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
                    lane.rightBoundType = HD.LaneBoundaryType.Type.CURB;
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

            // set left boundary type for the left most lane
            if (isOneWay)
            {
                edited[edited.Count-1].leftBoundType = HD.LaneBoundaryType.Type.CURB;
            }
            else
            {
                edited[edited.Count-1].leftBoundType = HD.LaneBoundaryType.Type.DOUBLE_YELLOW;
            }
        }
    }
}
