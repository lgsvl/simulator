/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Simulator.Map;

namespace Simulator.Map
{
    public class MapLaneSection : MapData
    {
        [System.NonSerialized]
        public List<MapLane> lanes = new List<MapLane>();
        [System.NonSerialized]
        public List<MapLane> lanesForward = new List<MapLane>();
        [System.NonSerialized]
        public List<MapLane> lanesReverse = new List<MapLane>();
        private bool? isOneWay;

        public void SetLaneData()
        {
            lanes = new List<MapLane>();
            lanes.AddRange(GetComponentsInChildren<MapLane>());
            lanesForward = new List<MapLane>();
            lanesReverse = new List<MapLane>();

            // for laneSections with branching lanes, all lanes should have at least 3 waypoints
            for (var i = 0; i < lanes.Count; i++)
            {
                var lane = lanes[i];

                // Self-reverse lane is not applied for this. Self-reverse lane is assigned to SOLID_WHITE manually.
                if (lane.isSelfReverseLane)
                    continue;

                isOneWay = true;

                var laneIdx = lane.mapWorldPositions.Count - 1; // index to compute vector from lane to otherLane and distance between those two lanes
                var laneDir = (lane.mapWorldPositions[1] - lane.mapWorldPositions[0]).normalized;

                for (var j = 0; j < lanes.Count; j++)
                {
                    var otherLane = lanes[j];
                    if (lane == otherLane) continue;

                    var otherIdx = otherLane.mapWorldPositions.Count - 1;
                    // Check if these two lanes have same directions by check the dist between 1st pos in lane and (the 1st and last pos in otherLane).
                    var isSameDirection = true;
                    var laneDirection = (lane.mapWorldPositions[laneIdx] - lane.mapWorldPositions[0]).normalized;
                    var otherLaneDirection = (otherLane.mapWorldPositions[otherIdx] - otherLane.mapWorldPositions[0]).normalized;
                    if (Vector3.Dot(laneDirection, otherLaneDirection) < 0)
                    {
                        isSameDirection = false;
                    }

                    if (isSameDirection) // same direction
                    {
                        if (!lanesForward.Contains(lane) && !lanesReverse.Contains(lane))
                            lanesForward.Add(lane);
                        if (!lanesForward.Contains(otherLane) && !lanesReverse.Contains(otherLane))
                            lanesForward.Add(otherLane);
                    }
                    else // opposite direction
                    {
                        isOneWay = false;

                        if (!lanesForward.Contains(lane) && !lanesReverse.Contains(lane))
                            lanesForward.Add(lane);
                        if (!lanesReverse.Contains(otherLane) && !lanesForward.Contains(otherLane))
                            lanesReverse.Add(otherLane);
                    }
                }
            }

            if (isOneWay == null)
                return;

            setLaneRelations(lanes);

            int wayCount = isOneWay.Value ? 1 : 2;
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

                var numLanes = i == 0 ? lanesForward.Count : lanesReverse.Count;
                var cnt = 0;
                while (currentLane != null)
                {
                    edited.Add(currentLane);
                    currentLane.laneNumber = edited.Count;
                    currentLane.laneCount = currentLanes.Count;
                    currentLane = currentLane.leftLaneForward;
                    cnt += 1;
                    if (cnt > numLanes)
                    {
                        Debug.LogError("Erroneous loop! Please check this lane!");
#if UNITY_EDITOR
                        UnityEditor.Selection.activeObject = currentLane.gameObject;
#endif
                        return;
                    }
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

        public float FindDistanceToLine(Vector3 p0, Vector3 p1, Vector3 p2)
        {
            // See http://mathworld.wolfram.com/Point-LineDistance3-Dimensional.html

            var a = p0 - p1;
            var b = p0 - p2;
            var c = p2 - p1;

            return Vector3.Cross(a, b).magnitude / c.magnitude;
        }

        // TODO: we might need to make this a button in Map annotation window
        public void setLaneRelations(List<MapLane> lanes)
        {
            var lineAsLeft2Lanes = new Dictionary<MapLine, List<MapLane>>();
            var lineAsRight2Lanes = new Dictionary<MapLine, List<MapLane>>();
            foreach (var lane in lanes)
            {
                lane.leftLaneForward = null;
                lane.leftLaneReverse = null;
                lane.rightLaneForward = null;
                lane.rightLaneReverse = null;

                lineAsLeft2Lanes.CreateOrAdd(lane.leftLineBoundry, lane);
                lineAsRight2Lanes.CreateOrAdd(lane.rightLineBoundry, lane);
            }

            foreach (var entry in lineAsLeft2Lanes)
            {
                if (entry.Value.Count > 2)
                {
                    Debug.LogError($"Boundary line {entry.Key} has more than 2 lanes associated, please check!");
                }
                else if (entry.Value.Count == 2)
                {
                    entry.Value[0].leftLaneReverse = entry.Value[1];
                    entry.Value[1].leftLaneReverse = entry.Value[0];

                }
                else
                {
                    if (lineAsRight2Lanes.ContainsKey(entry.Key))
                    {
                        entry.Value[0].leftLaneForward = lineAsRight2Lanes[entry.Key][0];
                        lineAsRight2Lanes[entry.Key][0].rightLaneForward = entry.Value[0];
                    }
                }
            }

            foreach (var entry in lineAsRight2Lanes)
            {
                if (entry.Value.Count > 2)
                {
                    Debug.LogError($"Boundary line {entry.Key} has more than 2 lanes associated, please check!");
                }
                else if (entry.Value.Count == 2)
                {
                    entry.Value[0].rightLaneReverse = entry.Value[1];
                    entry.Value[1].rightLaneReverse = entry.Value[0];

                }
                else
                {
                    if (lineAsLeft2Lanes.ContainsKey(entry.Key))
                    {
                        entry.Value[0].rightLaneForward = lineAsLeft2Lanes[entry.Key][0];
                        lineAsLeft2Lanes[entry.Key][0].leftLaneForward = entry.Value[0];
                    }
                }
            }
        }
    }

    public static class Helper
    {
        public static void CreateOrAdd<TKey, TValue> (this IDictionary<TKey, List<TValue>> dict, TKey key, TValue val)
        {
            if (dict.TryGetValue(key, out var list))
            {
               dict[key].Add(val);
            }
            else
            {
                list = new List<TValue>();
                list.Add(val);
                dict.Add(key, list);
            }
        }
    }
}