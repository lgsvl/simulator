/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static Utilities.Utility;

public class MapManager : MonoBehaviour
{
    private Transform trafficLanesHolder;
    private Transform intersectionsHolder;
    [System.NonSerialized]
    List<MapLane> trafficLanes = new List<MapLane>();
    [System.NonSerialized]
    public List<MapIntersection> intersections = new List<MapIntersection>();
    public float connectionProximity { get; private set; } = 1.0f;
    public float totalLaneDist { get; private set; } = 0f;

    // lights
    public float yellowTime { get; private set; } = 3f;
    public float allRedTime { get; private set; } = 1.5f;
    public float activeTime { get; private set; } = 15f;

    public bool isMapInit { get; private set; } = false;
    
    private void Start()
    {
        var mapHolder = FindObjectOfType<MapHolder>();
        if (mapHolder == null)
        {
            Debug.LogError("missing MapHolder, please add MapHolder.cs component to map object and set holder transforms");
            return;
        }

        trafficLanesHolder = mapHolder.trafficLanesHolder;
        intersectionsHolder = mapHolder.intersectionsHolder;
        SetMapData();
    }

    private void SetMapData()
    {
        var lanes = new List<MapLane>();
        lanes.AddRange(trafficLanesHolder.transform.parent.GetComponentsInChildren<MapLane>());
        ProcessLaneData(lanes);
        
        trafficLanes.AddRange(trafficLanesHolder.GetComponentsInChildren<MapLane>());
        foreach (var lane in trafficLanes)
            lane.isTrafficLane = true;

        var laneSections = new List<MapLaneSection>();
        laneSections.AddRange(trafficLanesHolder.transform.GetComponentsInChildren<MapLaneSection>());
        ProcessLaneSections(laneSections);

        var stopLines = new List<MapLine>();
        List<MapLine> allMapLines = new List<MapLine>();
        allMapLines.AddRange(trafficLanesHolder.transform.parent.GetComponentsInChildren<MapLine>());
        foreach (var line in allMapLines)
        {
            if (line.lineType == MapData.LineType.STOP)
                stopLines.Add(line);
        }
        ProcessStopLineData(stopLines, lanes);

        intersections.AddRange(intersectionsHolder.GetComponentsInChildren<MapIntersection>());
        ProcessIntersectionData(intersections);
        InitTrafficSets(intersections);
        isMapInit = true;
    }

    private void ProcessLaneData(List<MapLane> lanes)
    {
        foreach (var lane in lanes) // convert local to world pos
        {
            lane.mapWorldPositions.Clear();
            foreach (var localPos in lane.mapLocalPositions)
                lane.mapWorldPositions.Add(lane.transform.TransformPoint(localPos));
        }

        foreach (var lane in lanes) // set connected lanes
        {
            totalLaneDist += Vector3.Distance(lane.mapWorldPositions[0], lane.mapWorldPositions[lane.mapWorldPositions.Count - 1]);  // calc value for npc count

            var lastPt = lane.transform.TransformPoint(lane.mapLocalPositions[lane.mapLocalPositions.Count - 1]);
            foreach (var altLane in lanes)
            {
                var firstPt = altLane.transform.TransformPoint(altLane.mapLocalPositions[0]);
                if ((lastPt - firstPt).magnitude < connectionProximity)
                    lane.nextConnectedLanes.Add(altLane);
            }
        }
    }

    private void ProcessLaneSections(List<MapLaneSection> laneSections)
    {
        foreach (var section in laneSections)
            section.SetLaneData();
    }

    private void ProcessStopLineData(List<MapLine> stopLines, List<MapLane> lanes)
    {
        foreach (var line in stopLines) // convert local to world pos
        {
            line.mapWorldPositions.Clear();
            foreach (var localPos in line.mapLocalPositions)
                line.mapWorldPositions.Add(line.transform.TransformPoint(localPos));
        }

        foreach (var line in stopLines) // set stop lines
        {
            List<Vector2> stopline2D = line.mapWorldPositions.Select(p => new Vector2(p.x, p.z)).ToList();

            foreach (var lane in lanes)
            {
                // check if any points intersect with segment
                List<Vector2> intersects = new List<Vector2>();
                var lanes2D = lane.mapWorldPositions.Select(p => new Vector2(p.x, p.z)).ToList();
                var lane2D = new List<Vector2>();
                lane2D.Add(lanes2D[lanes2D.Count - 1]);
                bool isIntersected = CurveSegmentsIntersect(stopline2D, lane2D, out intersects);
                bool isClose = IsPointCloseToLine(stopline2D[0], stopline2D[stopline2D.Count - 1], lanes2D[lanes2D.Count - 1], connectionProximity);
                if (isIntersected || isClose)
                    lane.stopLine = line;
            }
        }
    }
    
    private void ProcessIntersectionData(List<MapIntersection> intersections)
    {
        foreach (var intersection in intersections)
            intersection.SetIntersectionData();
    }
    
    private void InitTrafficSets(List<MapIntersection> intersections)
    {
        foreach (var intersection in intersections)
            intersection.StartTrafficLightLoop();
    }

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

                    float d = SqrDistanceToSegment(p0, p1, position);
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

            var p = ClosetPointOnSegment(p0, p1, point);

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
