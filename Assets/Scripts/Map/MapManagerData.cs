/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Simulator.Utilities;

namespace Simulator.Map
{
    public class MapManagerData
    {
        public float ConnectionProximity { get; private set; } = 1.0f;
        public MapHolder MapHolder { get; private set; }

        public MapManagerData()
        {
            MapHolder = Object.FindObjectOfType<MapHolder>();
            if (MapHolder == null)
            {
                Debug.LogError("Map is missing MapHolder component! Please add MapHolder.cs component to scene and set holder transforms");
                return;
            }
        }

        public List<MapLane> GetTrafficLanes()
        {
            var trafficLanesHolder = MapHolder.trafficLanesHolder;

            var lanes = new List<MapLane>(trafficLanesHolder.transform.parent.GetComponentsInChildren<MapLane>());
            ProcessLaneData(lanes);

            var trafficLanes = new List<MapLane>(trafficLanesHolder.GetComponentsInChildren<MapLane>());
            foreach (var lane in trafficLanes)
                lane.isTrafficLane = true;

            var laneSections = new List<MapLaneSection>(trafficLanesHolder.transform.GetComponentsInChildren<MapLaneSection>());
            ProcessLaneSections(laneSections);

            var allMapLines = new List<MapLine>(trafficLanesHolder.transform.parent.GetComponentsInChildren<MapLine>());

            ProcessLineData(allMapLines, lanes);
            return trafficLanes;
        }

        public List<MapIntersection> GetIntersections()
        {
            if (MapHolder.intersectionsHolder == null) return null; // fine to have no intersections

            var intersectionsHolder = MapHolder.intersectionsHolder;

            var intersections = new List<MapIntersection>(intersectionsHolder.GetComponentsInChildren<MapIntersection>());
            ProcessIntersectionData(intersections);
            return intersections;
        }

        public List<T> GetData<T>()
        {
            var data = new List<T>(MapHolder.transform.GetComponentsInChildren<T>());
            return data;
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
                var lastPt = lane.transform.TransformPoint(lane.mapLocalPositions[lane.mapLocalPositions.Count - 1]);
                foreach (var altLane in lanes)
                {
                    var firstPt = altLane.transform.TransformPoint(altLane.mapLocalPositions[0]);
                    if ((lastPt - firstPt).magnitude < ConnectionProximity)
                        lane.nextConnectedLanes.Add(altLane);
                }
            }
        }

        public static float GetTotalLaneDistance(List<MapLane> lanes)
        {
            Debug.Assert(lanes != null);
            var totalLaneDist = 0f;
            foreach (var lane in lanes)
                totalLaneDist += Vector3.Distance(lane.mapWorldPositions[0], lane.mapWorldPositions[lane.mapWorldPositions.Count - 1]);  // calc value for npc count

            return totalLaneDist;
        }

        private void ProcessLaneSections(List<MapLaneSection> laneSections)
        {
            foreach (var section in laneSections)
                section.SetLaneData();
        }

        private void ProcessLineData(List<MapLine> allMapLines, List<MapLane> lanes)
        {
            foreach (var line in allMapLines) // convert local to world pos
            {
                line.mapWorldPositions.Clear();
                foreach (var localPos in line.mapLocalPositions)
                    line.mapWorldPositions.Add(line.transform.TransformPoint(localPos));
            }

            var stopLines = new List<MapLine>();
            foreach (var line in allMapLines)
            {
                if (line.lineType == MapData.LineType.STOP)
                    stopLines.Add(line);
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
                    bool isIntersected = Utility.CurveSegmentsIntersect(stopline2D, lane2D, out intersects);
                    bool isClose = Utility.IsPointCloseToLine(stopline2D[0], stopline2D[stopline2D.Count - 1], lanes2D[lanes2D.Count - 1], ConnectionProximity);
                    if (isIntersected || isClose)
                        lane.stopLine = line;
                }
            }
        }

        private void ProcessIntersectionData(List<MapIntersection> intersections)
        {
            intersections.ForEach(intersection => intersection.SetIntersectionData());
        }
    }
}
