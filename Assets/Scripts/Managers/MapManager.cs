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
        Debug.Log("Init MapManager");

        trafficLanesHolder = GameObject.FindGameObjectWithTag("MapTrafficLanes");
        intersectionsHolder = GameObject.FindGameObjectWithTag("MapIntersections");
        if (trafficLanesHolder == null || intersectionsHolder == null)
        {
            Debug.LogError("missing MapTrafficLanes and MapIntersections tags, please set in inspector!");
            return;
        }

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
                bool isClose = IsPointCloseToLine(stopline2D[0], stopline2D[stopline2D.Count - 1], lanes2D[lanes2D.Count - 1]);
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

    // utils
    private float Cross(Vector2 v1, Vector2 v2)
    {
        return v1.x * v2.y - v1.y * v2.x;
    }

    /// <summary>
    /// Test whether two line segments intersect. If so, calculate the intersection point.
    /// </summary>
    /// <param name="a1">Vector to the start point of a.</param>
    /// <param name="a2">Vector to the end point of a.</param>
    /// <param name="b1">Vector to the start point of b.</param>
    /// <param name="b2">Vector to the end point of b.</param>
    /// <param name="intersection">The point of intersection, if any.</param>
    /// <param name="considerOverlapAsIntersect">Do we consider overlapping lines as intersecting?
    /// </param>
    /// <returns>True if an intersection point was found.</returns>
    private bool LineSegementsIntersect(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2, out Vector2 intersection, bool considerCollinearOverlapAsIntersect = false)
    {
        intersection = new Vector2();

        var r = a2 - a1;
        var s = b2 - b1;
        var rxs = Cross(r, s);
        var qpxr = Cross(b1 - a1, r);

        // If r x s = 0 and (b1 - a1) x r = 0, then the two lines are collinear.
        if (System.Math.Abs(rxs) < 0.0001f && System.Math.Abs(qpxr) < 0.0001f)
        {
            // 1. If either  0 <= (b1 - a1) * r <= r * r or 0 <= (a1 - b1) * s <= * s
            // then the two lines are overlapping,
            if (considerCollinearOverlapAsIntersect)
            {
                if ((0 <= Vector2.Dot(b1 - a1, r) && Vector2.Dot(b1 - a1, r) <= Vector2.Dot(r, r)) || (0 <= Vector2.Dot(a1 - b1, s) && Vector2.Dot(a1 - b1, s) <= Vector2.Dot(s, s)))
                {
                    return true;
                }
            }

            // 2. If neither 0 <= (b1 - a1) * r = r * r nor 0 <= (a1 - b1) * s <= s * s
            // then the two lines are collinear but disjoint.
            // No need to implement this expression, as it follows from the expression above.
            return false;
        }

        // 3. If r x s = 0 and (b1 - a1) x r != 0, then the two lines are parallel and non-intersecting.
        if (System.Math.Abs(rxs) < 0.0001f && !(System.Math.Abs(qpxr) < 0.0001f))
            return false;

        // t = (b1 - a1) x s / (r x s)
        var t = Cross(b1 - a1, s) / rxs;

        // u = (b1 - a1) x r / (r x s)

        var u = Cross(b1 - a1, r) / rxs;

        // 4. If r x s != 0 and 0 <= t <= 1 and 0 <= u <= 1
        // the two line segments meet at the point a1 + t r = b1 + u s.
        if (!(System.Math.Abs(rxs) < 0.0001f) && (0 <= t && t <= 1) && (0 <= u && u <= 1))
        {
            // We can calculate the intersection point using either t or u.
            intersection = a1 + t * r;

            // An intersection was found.
            return true;
        }

        // 5. Otherwise, the two line segments are not parallel but do not intersect.
        return false;
    }

    private bool CurveSegmentsIntersect(List<Vector2> a, List<Vector2> b, out List<Vector2> intersections)
    {
        intersections = new List<Vector2>();
        for (int i = 0; i < a.Count - 1; i++)
        {
            for (int j = 0; j < b.Count - 1; j++)
            {
                Vector2 intersect;
                if (LineSegementsIntersect(a[i], a[i + 1], b[j], b[j + 1], out intersect))
                {
                    intersections.Add(intersect);
                }
            }
        }
        return intersections.Count > 0;
    }

    private bool IsPointCloseToLine(Vector2 p1, Vector2 p2, Vector2 pt)
    {
        bool isClose = false;
        Vector2 closestPt = Vector2.zero;
        float dx = p2.x - p1.x;
        float dy = p2.y - p1.y;

        // Calculate the t that minimizes the distance.
        float t = ((pt.x - p1.x) * dx + (pt.y - p1.y) * dy) / (dx * dx + dy * dy);

        // See if this represents one of the segment's
        // end points or a point in the middle.
        if (t < 0)
        {
            closestPt = new Vector2(p1.x, p1.y);
            dx = pt.x - p1.x;
            dy = pt.y - p1.y;
        }
        else if (t > 1)
        {
            closestPt = new Vector2(p2.x, p2.y);
            dx = pt.x - p2.x;
            dy = pt.y - p2.y;
        }
        else
        {
            closestPt = new Vector2(p1.x + t * dx, p1.y + t * dy);
            dx = pt.x - closestPt.x;
            dy = pt.y - closestPt.y;
        }

        if (Mathf.Sqrt(dx * dx + dy * dy) < connectionProximity)
        {
            isClose = true;
        }
        return isClose;
    }

    private float SqrDistanceToSegment(Vector3 p0, Vector3 p1, Vector3 point)
    {
        var t = Vector3.Dot(point - p0, p1 - p0) / Vector3.SqrMagnitude(p1 - p0);

        Vector3 v = t < 0f ? p0 : t > 1f ? p1 : p0 + t * (p1 - p0);

        return Vector3.SqrMagnitude(point - v);
    }

    public Vector3 ClosetPointOnSegment(Vector3 p0, Vector3 p1, Vector3 point)
    {
        float t = Vector3.Dot(point - p0, p1 - p0) / Vector3.SqrMagnitude(p1 - p0);
        return t < 0f ? p0 : t > 1f ? p1 : p0 + t * (p1 - p0);
    }
}
