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
using Simulator.Api;

public class MapManager : MonoBehaviour
{
    [System.NonSerialized]
    public List<MapLane> trafficLanes = new List<MapLane>();
    [System.NonSerialized]
    public List<MapIntersection> intersections = new List<MapIntersection>();
    [System.NonSerialized]
    public List<MapPedestrian> pedestrianLanes = new List<MapPedestrian>();

    private MapManagerData mapData;

    private void Awake()
    {
        SetMapData();
    }

    private void Start()
    {
        intersections.ForEach(intersection => intersection.StartTrafficLightLoop());
    }

    private void SetMapData()
    {
        mapData = new MapManagerData();
        if (mapData.MapHolder == null)
            return;

        trafficLanes = mapData.GetTrafficLanes();
        intersections = mapData.GetIntersections();
        pedestrianLanes = mapData.GetPedestrianLanes();

        trafficLanes.ForEach(trafficLane => trafficLane.SetTrigger());
        intersections.ForEach(intersection => intersection.SetTriggerAndState());
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

    public int GetLaneNextIndex(Vector3 position, MapLane lane)
    {
        float minDist = float.PositiveInfinity;
        int index = -1;
        
        for (int i = 0; i < lane.mapWorldPositions.Count - 1; i++)
        {
            var p0 = lane.mapWorldPositions[i];
            var p1 = lane.mapWorldPositions[i + 1];

            var p = Utility.ClosetPointOnSegment(p0, p1, position);

            float d = Vector3.SqrMagnitude(position - p);
            if (d < minDist)
            {
                minDist = d;
                index = i + 1;
            }
        }

        return index;
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

    public MapLane GetLane(int index)
    {
        return trafficLanes == null || trafficLanes.Count == 0 ? null : trafficLanes[index];
    }

    public MapPedestrian GetPedPath(int index)
    {
        return pedestrianLanes == null || pedestrianLanes.Count == 0 ? null : pedestrianLanes[index];
    }

    public void Reset()
    {
        foreach (var intersection in intersections)
        {
            intersection.npcsInIntersection.Clear();
            intersection.stopQueue.Clear();
            intersection.SetTriggerAndState();
            intersection.StartTrafficLightLoop();
        }
    }

    public void RemoveNPCFromIntersections(NPCController npc)
    {
        foreach (var intersection in intersections)
        {
            intersection.ExitIntersectionList(npc);
            if (intersection.isStopSignIntersection)
            {
                intersection.ExitStopSignQueue(npc);
            }
        }
    }
}
