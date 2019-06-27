/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using UnityEngine;
using Simulator.Utilities;
using Simulator.Map;

public class MapManager : MonoBehaviour
{
    [System.NonSerialized]
    public List<MapLane> trafficLanes = new List<MapLane>();
    [System.NonSerialized]
    public List<MapIntersection> intersections = new List<MapIntersection>();
    public float totalLaneDist { get; private set; } = 0f;

    // lights
    public float yellowTime { get; private set; } = 3f;
    public float allRedTime { get; private set; } = 1.5f;
    public float activeTime { get; private set; } = 15f;

    private MapManagerData mapData;

    private void Start()
    {
        SetMapData();
        InitTrafficSets(intersections);
    }
    
    private void SetMapData()
    {
        mapData = new MapManagerData();
        if (mapData.MapHolder == null)
            return;
        
        trafficLanes = mapData.GetTrafficLanes();
        intersections = mapData.GetIntersections();
        totalLaneDist = MapManagerData.GetTotalLaneDistance(trafficLanes);
        intersections.ForEach(intersection => intersection.SetTriggerAndState());
    }
    
    private void InitTrafficSets(List<MapIntersection> intersections)
    {
        if (intersections == null) return; // map data may not need intersections but always needs lanes
        intersections.ForEach(intersection => intersection.StartTrafficLightLoop());
    }

    // npc and api
    public MapLane GetClosestLane(Vector3 position)
    {
        MapLane result = null;
        float minDist = float.PositiveInfinity;

        // TODO: this should be optimized
        foreach (var lane in trafficLanes)
        {
            if (lane.mapWorldPositions.Count >= 2)
            {
                for (int i = 0; i < lane.mapWorldPositions.Count - 1; i++)
                {
                    var p0 = lane.mapWorldPositions[i];
                    var p1 = lane.mapWorldPositions[i + 1];

                    float d = Utility.SqrDistanceToSegment(p0, p1, position);
                    if (d < minDist)
                    {
                        minDist = d;
                        result = lane;
                    }
                }
            }
        }
        return result;
    }

    // api
    public void GetPointOnLane(Vector3 point, out Vector3 position, out Quaternion rotation)
    {
        var lane = GetClosestLane(point);
        
        int index = -1;
        float minDist = float.PositiveInfinity;
        Vector3 closest = Vector3.zero;

        for (int i = 0; i < lane.mapWorldPositions.Count - 1; i++)
        {
            var p0 = lane.mapWorldPositions[i];
            var p1 = lane.mapWorldPositions[i + 1];

            var p = Utility.ClosetPointOnSegment(p0, p1, point);

            float d = Vector3.SqrMagnitude(point - p);
            if (d < minDist)
            {
                minDist = d;
                index = i;
                closest = p;
            }
        }

        position = closest;
        rotation = Quaternion.LookRotation(lane.mapWorldPositions[index + 1] - lane.mapWorldPositions[index], Vector3.up);
    }

    public MapLane GetRandomLane()
    {
        return trafficLanes == null || trafficLanes.Count == 0 ? null : trafficLanes[(int)Random.Range(0, trafficLanes.Count)];
    }
}
