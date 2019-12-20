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
                Debug.LogError("Map is missing annotation MapHolder and child holders! Please add to scene and set holder transforms");
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

        public List<MapPedestrian> GetPedestrianLanes()
        {
            var pedLanes = new List<MapPedestrian>(GameObject.FindObjectsOfType<MapPedestrian>());
            ProcessPedestrianData(pedLanes);
            return pedLanes;
        }

        public List<MapLaneSection> GetLaneSections()
        {
            var trafficLanesHolder = MapHolder.trafficLanesHolder;
            var laneSections = new List<MapLaneSection>(trafficLanesHolder.transform.GetComponentsInChildren<MapLaneSection>());
            return laneSections;
        }

        public void GetNonLaneObjects()
        {
            var parkingSpaces = GetData<MapParkingSpace>();
            var speedBumps = GetData<MapSpeedBump>();
            var clearAreas = GetData<MapClearArea>();
            var crossWalks = GetData<MapCrossWalk>();
            var junctions = GetData<MapJunction>();
            var signals = GetData<MapSignal>();
            var signs = GetData<MapSign>();

            ProcessParkingSpaceData(parkingSpaces);
            ProcessSpeedBumpData(speedBumps);
            ProcessClearAreaData(clearAreas);
            ProcessCrossWalkData(crossWalks);
            ProcessJunctionData(junctions);
            ProcessSignalData(signals);
            ProcessSignData(signs);
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

            foreach (var lane in lanes) // set connected lanes and spawnable
            {
                lane.Spawnable = true;
                var firstPt = lane.transform.TransformPoint(lane.mapLocalPositions[0]);
                var lastPt = lane.transform.TransformPoint(lane.mapLocalPositions[lane.mapLocalPositions.Count - 1]);
                foreach (var altLane in lanes)
                {
                    if (lane == altLane)
                        continue;
                    var altFirstPt = altLane.transform.TransformPoint(altLane.mapLocalPositions[0]);
                    var altLastPt = altLane.transform.TransformPoint(altLane.mapLocalPositions[altLane.mapLocalPositions.Count - 1]);
                    if ((lastPt - altFirstPt).magnitude < ConnectionProximity)
                    {
                        lane.nextConnectedLanes.Add(altLane);
                        altLane.prevConnectedLanes.Add(lane);
                    }
                    if ((firstPt - altLastPt).magnitude < ConnectionProximity)
                        lane.Spawnable = false;
                }
            }
        }

        private void ProcessPedestrianData(List<MapPedestrian> pedLanes)
        {
            foreach (var ped in pedLanes) // convert local to world pos
            {
                ped.mapWorldPositions.Clear();
                foreach (var localPos in ped.mapLocalPositions)
                {
                    ped.mapWorldPositions.Add(ped.transform.TransformPoint(localPos));
                }
            }
        }

        private void ProcessParkingSpaceData(List<MapParkingSpace> parkingSpaces)
        {
            foreach (var parkingSpace in parkingSpaces) // convert local to world pos
            {
                parkingSpace.mapWorldPositions.Clear();
                foreach (var localPos in parkingSpace.mapLocalPositions)
                    parkingSpace.mapWorldPositions.Add(parkingSpace.transform.TransformPoint(localPos));
            }
        }

        private void ProcessSpeedBumpData(List<MapSpeedBump> speedBumps)
        {
            foreach (var speedBump in speedBumps) // convert local to world pos
            {
                speedBump.mapWorldPositions.Clear();
                foreach (var localPos in speedBump.mapLocalPositions)
                    speedBump.mapWorldPositions.Add(speedBump.transform.TransformPoint(localPos));
            }
        }

        private void ProcessClearAreaData(List<MapClearArea> clearAreas)
        {
            foreach (var clearArea in clearAreas) // convert local to world pos
            {
                clearArea.mapWorldPositions.Clear();
                foreach (var localPos in clearArea.mapLocalPositions)
                    clearArea.mapWorldPositions.Add(clearArea.transform.TransformPoint(localPos));
            }
        }

        private void ProcessCrossWalkData(List<MapCrossWalk> crossWalks)
        {
            foreach (var crossWalk in crossWalks) // convert local to world pos
            {
                crossWalk.mapWorldPositions.Clear();
                foreach (var localPos in crossWalk.mapLocalPositions)
                    crossWalk.mapWorldPositions.Add(crossWalk.transform.TransformPoint(localPos));
            }
        }

        private void ProcessJunctionData(List<MapJunction> junctions)
        {
            foreach (var junction in junctions) // convert local to world pos
            {
                junction.mapWorldPositions.Clear();
                foreach (var localPos in junction.mapLocalPositions)
                    junction.mapWorldPositions.Add(junction.transform.TransformPoint(localPos));
            }
        }

        private void ProcessSignalData(List<MapSignal> signals)
        {
            foreach (var signal in signals)
            {
                signal.stopLine.mapWorldPositions.Clear();
                foreach (var localPos in signal.stopLine.mapLocalPositions)
                    signal.stopLine.mapWorldPositions.Add(signal.transform.TransformPoint(localPos));
            }
        }

        private void ProcessSignData(List<MapSign> signs)
        {
            foreach (var sign in signs)
            {
                sign.stopLine.mapWorldPositions.Clear();
                foreach (var localPos in sign.stopLine.mapLocalPositions)
                    sign.stopLine.mapWorldPositions.Add(sign.transform.TransformPoint(localPos));
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

        public static float GetTotalPedDistance(List<MapPedestrian> peds)
        {
            Debug.Assert(peds != null);
            var pedDist = 0f;
            foreach (var ped in peds)
                pedDist += Vector3.Distance(ped.mapWorldPositions[0], ped.mapWorldPositions[ped.mapWorldPositions.Count - 1]);  // calc value for ped count

            return pedDist;
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
                    lane2D.Add(lanes2D[lanes2D.Count - 2]);
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
