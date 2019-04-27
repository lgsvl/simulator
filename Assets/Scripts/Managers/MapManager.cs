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

public class MapManager : MonoBehaviour
{
    private GameObject trafficLanesHolder;
    private GameObject intersectionsHolder;

    private MapLane tempLane;
    [System.NonSerialized]
    public List<MapLaneSection> laneSections = new List<MapLaneSection>();
    [System.NonSerialized]
    public List<MapLane> lanes = new List<MapLane>();
    [System.NonSerialized]
    public List<MapLane> trafficLanes = new List<MapLane>();
    [System.NonSerialized]
    public List<MapLine> stopLines = new List<MapLine>();
    public float connectionProximity { get; private set; } = 1.0f;

    // lights
    [System.NonSerialized]
    public List<MapIntersection> intersections = new List<MapIntersection>();
    private float yellowTime = 3f;
    private float allRedTime = 1.5f;
    private float activeTime = 15f;

    public bool isMapInit { get; private set; } = false;

    public void Init()
    {
        trafficLanesHolder = GameObject.FindGameObjectWithTag("MapTrafficLanes");
        intersectionsHolder = GameObject.FindGameObjectWithTag("MapIntersections");
        if (trafficLanesHolder == null || intersectionsHolder == null)
        {
            Debug.LogError("missing MapTrafficLanes and MapIntersections tags, please set in inspector!");
            return;
        }

        GetMapData();
        ProcessLaneData();
        ProcessLaneSections();
        ProcessStopLineData();
        ProcessIntersectionData();
        ProcessTrafficLightData();

        InitTrafficSets();
        isMapInit = true;
    }

    private void GetMapData()
    {
        lanes.AddRange(transform.GetComponentsInChildren<MapLane>());
        trafficLanes.AddRange(trafficLanesHolder.GetComponentsInChildren<MapLane>());
        laneSections.AddRange(transform.GetComponentsInChildren<MapLaneSection>());

        List<MapLine> allMapLines = new List<MapLine>();
        allMapLines.AddRange(transform.GetComponentsInChildren<MapLine>());
        foreach (var line in allMapLines)
        {
            if (line.lineType == MapData.LineType.STOP)
                stopLines.Add(line);
        }
        intersections.AddRange(intersectionsHolder.GetComponentsInChildren<MapIntersection>());
    }

    private void ProcessLaneData()
    {
        foreach (var lane in lanes) // convert local to world pos
        {
            lane.mapWorldPositions.Clear();
            foreach (var localPos in lane.mapLocalPositions)
                lane.mapWorldPositions.Add(lane.transform.TransformPoint(localPos));
        }

        foreach (var lane in lanes) // set connected lanes
        {
            var lastPt = lane.transform.TransformPoint(lane.mapLocalPositions[lane.mapLocalPositions.Count - 1]);
            foreach (var altLane in lanes)
            {
                var firstPt = altLane.transform.TransformPoint(altLane.mapLocalPositions[0]);
                if ((lastPt - firstPt).magnitude < connectionProximity)
                    lane.nextConnectedLanes.Add(altLane);
            }
        }
    }

    private void ProcessLaneSections()
    {
        foreach (var lane in trafficLanes)
            lane.isTrafficLane = true;
        foreach (var section in laneSections)
            section.SetLaneData();
    }

    private void ProcessStopLineData()
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
                bool isIntersected = Utils.CurveSegmentsIntersect(stopline2D, lane2D, out intersects);
                bool isClose = Utils.IsPointCloseToLine(stopline2D[0], stopline2D[stopline2D.Count - 1], lanes2D[lanes2D.Count - 1]);
                if (isIntersected || isClose)
                    lane.stopLine = line;
            }
        }
    }
    
    private void ProcessIntersectionData()
    {
        foreach (var line in stopLines)
        {
            if (line.mapIntersection != null)
            {
                line.mapIntersection.SetIntersectionLaneData();
                //line.GetTrafficLightSet();
                line.mapIntersection.isStopSign = line.isStopSign;
            }
        }
    }

    private void ProcessTrafficLightData()
    {
        foreach (var intersection in intersections)
        {
            intersection.SetLightGroupData(yellowTime, allRedTime, activeTime);
            foreach (var set in intersection.signalGroup)
            {
                set.SetSignalMeshData();
            }
        }
    }

    private void InitTrafficSets()
    {
        foreach (var intersection in intersections)
        {
            //foreach (var set in intersection.lightGroups)
            //{
            //    set.SetLightColor(TrafficLightSetState.Red, red);
            //}
        }

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

                    float d = Utils.SqrDistanceToSegment(p0, p1, position);
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

            var p = Utils.ClosetPointOnSegment(p0, p1, point);

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
