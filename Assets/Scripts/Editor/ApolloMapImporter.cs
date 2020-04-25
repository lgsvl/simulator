/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Simulator.Map;
using Unity.Mathematics;
using Utility = Simulator.Utilities.Utility;

namespace Simulator.Editor
{
using apollo.hdmap;
    public class ApolloMapImporter
    {
        EditorSettings Settings;

        bool IsMeshNeeded; // Boolean value for traffic light/sign mesh importing.
        static float DownSampleDistanceThreshold; // DownSample distance threshold for points to keep 
        static float DownSampleDeltaThreshold; // For down sampling, delta threshold for curve points 
        bool ShowDebugIntersectionArea = false; // Show debug area for intersection area to find left_turn lanes
        GameObject TrafficLanes;
        GameObject SingleLaneRoads;
        GameObject Intersections;
        MapOrigin MapOrigin;
        Map ApolloMap;
        Dictionary<string, MapIntersection> Id2MapIntersection = new Dictionary<string, MapIntersection>();
        Dictionary<string, MapLane> Id2Lane = new Dictionary<string, MapLane>();
        Dictionary<string, Lane> Id2ApolloLane = new Dictionary<string, Lane>();
        Dictionary<string, MapLine> Id2LeftLineBoundary = new Dictionary<string, MapLine>();
        Dictionary<string, MapLine> Id2RightLineBoundary = new Dictionary<string, MapLine>();
        Dictionary<string, MapLine> Id2StopLine = new Dictionary<string, MapLine>();
        Dictionary<string, string> LaneId2JunctionId = new Dictionary<string, string>();
        Dictionary<string, GameObject> LaneId2MapLaneSection = new Dictionary<string, GameObject>(); // map lane section id is same as road id
        Dictionary<string, Tuple<string, double>> SignalSignId2LaneIdStartS = new Dictionary<string, Tuple<string, double>>();

        public ApolloMapImporter(float downSampleDistanceThreshold, float downSampleDeltaThreshold, bool isMeshNeeded)
        {
            DownSampleDistanceThreshold = downSampleDistanceThreshold;
            DownSampleDeltaThreshold = downSampleDeltaThreshold;
            IsMeshNeeded = isMeshNeeded;
        }

        public void Import(string filePath)
        {
            Settings = EditorSettings.Load();

            if (ImportApolloMapCalculate(filePath))
            {
                Debug.Log("Successfully imported Apollo HD Map!\nPlease check your imported intersections and adjust if they are wrongly grouped.");
                Debug.Log("Currently yield signs are imported as stop signs for NPCs.");
                Debug.LogWarning("!!! You need to adjust the triggerBounds for each MapIntersection.");
            }
            else
            {
                Debug.LogError("Failed to import Apollo HD Map.");
            }
        }

        bool ImportApolloMapCalculate(string filePath)
        {
            using (var fs = File.OpenRead(filePath))
            {
                ApolloMap = ProtoBuf.Serializer.Deserialize<Map>(fs);
            }

            var mapName = "Map" + "_" + Encoding.UTF8.GetString(ApolloMap.header.vendor) + "_" + Encoding.UTF8.GetString(ApolloMap.header.district);
            if (!CreateMapHolders(filePath, mapName)) return false;

            MapOrigin = MapOrigin.Find(); // get or create a map origin
            if (!CreateOrUpdateMapOrigin(ApolloMap, MapOrigin))
            {
                Debug.LogWarning("Could not find latitude or/and longitude in map header Or not supported projection, mapOrigin is not updated.");
            }

            ImportJunctions();
            ImportLanes();
            ImportRoads();
            ConnectLanes();
            ImportOverlaps();
            ImportStopSigns();
            ImportSignals();
            SetIntersectionTriggerBounds();
            ImportYields();
            UpdateStopLines(); // Update stoplines using intersecting lanes end points
            // SetLaneYieldLanes();
            // ImportCrossWalks();
            // ImportParkingSpaces();
            // ImportSpeedBumps();
            // ImportClearAreas();
            return true;
        }

        bool CreateMapHolders(string filePath, string mapName)
        {
            // Create Map object
            var fileName = Path.GetFileName(filePath).Split('.')[0];
            
            // Check existence of same name map
            if (GameObject.Find(mapName)) 
            {
                Debug.LogError("A map with same name exists, cancelling map importing.");
                return false;
            }

            GameObject map = new GameObject(mapName);
            var mapHolder = map.AddComponent<MapHolder>();

            // Create TrafficLanes and Intersections under Map
            TrafficLanes = new GameObject("TrafficLanes");
            Intersections = new GameObject("Intersections");
            SingleLaneRoads = new GameObject("SingleLaneRoads");
            TrafficLanes.transform.parent = map.transform;
            Intersections.transform.parent = map.transform;
            SingleLaneRoads.transform.parent = TrafficLanes.transform;

            mapHolder.trafficLanesHolder = TrafficLanes.transform;
            mapHolder.intersectionsHolder = Intersections.transform;

            return true;
        }

        // read map origin, update MapOrigin
        bool CreateOrUpdateMapOrigin(Map apolloMap, MapOrigin mapOrigin)
        {
            var header = apolloMap.header;
            var geoReference = header.projection.proj;
            var items = geoReference.Split('+')
                .Select(s => s.Split('='))
                .Where(s => s.Length > 1)
                .ToDictionary(element => element[0].Trim(), element => element[1].Trim());
            
            if (!items.ContainsKey("proj") || items["proj"] != "utm") return false;

            double latitude, longitude;
            longitude = (header.left + header.right) / 2;
            latitude = (header.top + header.bottom) / 2;

            int zoneNumber;
            if (items.ContainsKey("zone"))
            {
                zoneNumber = int.Parse(items["zone"]);
            }
            else
            {               
                zoneNumber = MapOrigin.GetZoneNumberFromLatLon(latitude, longitude);
            }
            
            mapOrigin.UTMZoneId = zoneNumber;
            mapOrigin.FromLatitudeLongitude(latitude, longitude, out mapOrigin.OriginNorthing, out mapOrigin.OriginEasting);

            return true;
        }

        void ImportJunctions()
        {
            foreach (var junction in ApolloMap.junction)
            {
                var id = junction.id.id.ToString();
                var mapIntersection = CreateMapIntersection(id);

                var mapJunction = new GameObject("MapJunction_" + id);
                mapJunction.transform.parent = mapIntersection.transform;
                var mapJunctionComponent = mapJunction.AddComponent<MapJunction>();

                mapJunctionComponent.mapWorldPositions = ConvertUTMFromDouble3(junction.polygon.point.Select(x => ToDouble3(x)).ToList());
                UpdateObjPosAndLocalPos(mapIntersection.transform, mapJunctionComponent);
            }
            Debug.Log($"Imported {ApolloMap.junction.Count} junctions.");
        }

        MapIntersection CreateMapIntersection(string id)
        {
            var mapIntersectionObj = new GameObject("MapIntersection_" + id);
            var mapIntersection = mapIntersectionObj.AddComponent<MapIntersection>();
            mapIntersection.transform.parent = Intersections.transform;
            Id2MapIntersection[id] = mapIntersection;
            
            return mapIntersection;
        }

        public List<Vector3> ConvertUTMFromDouble3(List<double3> double3List)
        {
            var vector3List = new List<Vector3>();
            foreach (var point in double3List)
            {
                var vector3 = MapOrigin.FromNorthingEasting(point.y, point.x, false);
                vector3.y = (float)point.z;
                vector3List.Add(vector3);
            }

            return vector3List;
        }

        void UpdateObjPosAndLocalPos(Transform objectTransform, MapDataPoints mapDataPoints)
        {
            var ObjPosition = Lanelet2MapImporter.GetAverage(mapDataPoints.mapWorldPositions);
            objectTransform.position = ObjPosition;
            // Update child local positions after parent position changed 
            UpdateLocalPositions(mapDataPoints);
        }


        void ImportLaneRelations()
        {
            foreach (var entry in Id2Lane)
            {
                var id = entry.Key;
                var lane = entry.Value;
                var apolloLane = Id2ApolloLane[id];
                if (apolloLane.left_neighbor_forward_lane_id.Count > 0) lane.leftLaneForward = Id2Lane[apolloLane.left_neighbor_forward_lane_id[0].id];
                if (apolloLane.right_neighbor_forward_lane_id.Count > 0) lane.rightLaneForward = Id2Lane[apolloLane.right_neighbor_forward_lane_id[0].id];
                if (apolloLane.left_neighbor_reverse_lane_id.Count > 0) lane.leftLaneReverse = Id2Lane[apolloLane.left_neighbor_reverse_lane_id[0].id];
                if (apolloLane.right_neighbor_reverse_lane_id.Count > 0) lane.rightLaneReverse = Id2Lane[apolloLane.right_neighbor_reverse_lane_id[0].id];
            }
        }

        void ImportLanes()
        {
            foreach (var lane in ApolloMap.lane)
            {
                var id = lane.id.id.ToString();
                AddLine(id, lane.left_boundary, true);
                AddLine(id, lane.right_boundary);
                AddLane(id, lane);
            }
            ImportLaneRelations();
            Debug.Log($"Imported {ApolloMap.lane.Count} lanes, left boundary lines and right boundary lines.");
        }

        void AddLine(string id, LaneBoundary boundary, bool isLeft = false)
        {
            var curveSegments = boundary.curve.segment;
            var linePoints = GetPointsFromCurve(curveSegments);
            var linePointsDouble3 = DownSample(linePoints, DownSampleDeltaThreshold, DownSampleDistanceThreshold);

            string lineId;
            if (isLeft) lineId = "MapLine_Left_" + id;
            else lineId = "MapLine_Right_" + id;

            GameObject mapLineObj = new GameObject(lineId);
            mapLineObj.transform.parent = TrafficLanes.transform;

            MapLine mapLine = mapLineObj.AddComponent<MapLine>();
            mapLine.mapWorldPositions = ConvertUTMFromDouble3(linePointsDouble3);
            UpdateObjPosAndLocalPos(mapLineObj.transform, mapLine);

            if (boundary.@virtual) mapLine.lineType = MapData.LineType.VIRTUAL;
            else if (boundary.boundary_type.Count > 0 && boundary.boundary_type[0].types.Count > 0)
                mapLine.lineType = BoundaryTypeToLineType(boundary.boundary_type[0].types[0]);
            else mapLine.lineType = MapData.LineType.UNKNOWN;

            var warning = "Multiple boundary types for one lane boundary is not"
              + "supported yet, currently only the 1st type is used.";
            if (boundary.boundary_type.Count > 1) Debug.LogWarning(warning);

            if (isLeft) Id2LeftLineBoundary[id] = mapLine;
            else Id2RightLineBoundary[id] = mapLine;
        }

        public static List<double3> DownSample(List<double3> points, float downSampleDeltaThreshold, float downSampleDistanceThreshold)
        {
            if (points.Count < 4) return points;
            var sampledPoints = new List<double3>();
            sampledPoints.Capacity = points.Count;
            sampledPoints.Add(points[0]);

            Debug.Assert(points.Count > 1);
            
            double3 currentDir = math.normalize(points[1] - points[0]);
            double3 lastPoint = points[0];
            for (int i = 2; i < points.Count - 1; i++)
            {
                // Check delta, distance from points[i] to current dir
                double delta;
                if (sampledPoints.Count == 1)
                {
                    delta = GetDistancePointToLine(lastPoint, currentDir, points[i]);
                    if (delta > downSampleDeltaThreshold)
                    {
                        sampledPoints.Add(points[1]);
                        sampledPoints.Add(points[i]);
                        currentDir = math.normalize(points[i] - points[1]);
                        lastPoint = points[i];
                        continue;
                    }
                }
                else
                {
                    delta = GetDistancePointToLine(lastPoint, currentDir, points[i]);
                    if (delta > downSampleDeltaThreshold)
                    {
                        sampledPoints.Add(points[i]);
                        currentDir = math.normalize(points[i] - lastPoint);
                        lastPoint = points[i];
                        continue;
                    }
                }

                // Check distance
                if (math.distancesq(points[i], sampledPoints.Last()) > downSampleDistanceThreshold * downSampleDistanceThreshold)
                {
                    sampledPoints.Add(points[i]);
                    currentDir = math.normalize(points[i] - lastPoint);
                    lastPoint = points[i];
                }
            }
            sampledPoints.Add(points[points.Count - 1]);
            // Debug.Log($"Before sample, points number: {points.Count} after sample: {sampledPoints.Count}.");
            return sampledPoints;
        }

        MapData.LineType BoundaryTypeToLineType(LaneBoundaryType.Type boundaryType)
        {
            if (boundaryType == LaneBoundaryType.Type.DottedYellow) return MapData.LineType.DOTTED_YELLOW;
            else if (boundaryType == LaneBoundaryType.Type.DottedWhite) return MapData.LineType.DOTTED_WHITE;
            else if (boundaryType == LaneBoundaryType.Type.SolidYellow) return MapData.LineType.SOLID_YELLOW;
            else if (boundaryType == LaneBoundaryType.Type.SolidWhite) return MapData.LineType.SOLID_WHITE;
            else if (boundaryType == LaneBoundaryType.Type.DoubleYellow) return MapData.LineType.DOUBLE_YELLOW;
            else if (boundaryType == LaneBoundaryType.Type.Curb) return MapData.LineType.CURB;

            return MapData.LineType.UNKNOWN;
        }

        static double GetDistancePointToLine(double3 start, double3 dir, double3 point)
        {
            double3 AP = point - start;
            double cross1 = AP.y * dir.z - AP.z * dir.y;
            double cross2 = AP.z * dir.x - AP.x * dir.z;
            double cross3 = AP.x * dir.y - AP.y * dir.x;
            return Math.Sqrt(cross1 * cross1 + cross2 * cross2 + cross3 * cross3); // Use cross product to compute distance
        }

        void AddLane(string id, Lane lane)
        {
            var curveSegments = lane.central_curve.segment;
            var lanePoints = GetPointsFromCurve(curveSegments);
            lanePoints = DownSample(lanePoints, DownSampleDeltaThreshold, DownSampleDistanceThreshold);

            GameObject mapLaneObj = new GameObject("MapLane_" + id);

            MapLane mapLane = mapLaneObj.AddComponent<MapLane>();
            mapLane.mapWorldPositions = ConvertUTMFromDouble3(lanePoints);
            UpdateObjPosAndLocalPos(mapLaneObj.transform, mapLane);

            // TODO set self reverse lanes
            // TODO set direction
            mapLane.leftLineBoundry = Id2LeftLineBoundary[id];
            mapLane.rightLineBoundry = Id2RightLineBoundary[id];
            mapLane.laneTurnType = (MapData.LaneTurnType)lane.turn;
            mapLane.speedLimit = (float)lane.speed_limit;
            var junctionId = lane.junction_id;
            if (junctionId != null) MoveLaneToJunction(new List<string>(){id}, junctionId.id.ToString());

            Id2Lane[id] = mapLane;
            Id2ApolloLane[id] = lane;
        }

        void MoveLaneToJunction(List<string> laneIds, string junctionId)
        {
            foreach (var id in laneIds)
            {
                if (LaneId2JunctionId.ContainsKey(id))
                {
                    if (LaneId2JunctionId[id] != junctionId)
                    {
                        Debug.LogWarning($"Lane {id} is belong to more than one junctions, please check.");
                    }
                    continue;
                }
                else if (LaneId2MapLaneSection.ContainsKey(id))
                {
                    LaneId2MapLaneSection[id].transform.parent = Id2MapIntersection[junctionId].transform;
                    break;
                }
                else
                {
                    var intersectionTransform = Id2MapIntersection[junctionId].transform;
                    Id2Lane[id].transform.parent = intersectionTransform;
                    Id2LeftLineBoundary[id].transform.parent = intersectionTransform;
                    Id2RightLineBoundary[id].transform.parent = intersectionTransform;
                }

                LaneId2JunctionId[id] = junctionId;
            }
        }

        void ImportRoads()
        {
            foreach (var road in ApolloMap.road)
            {
                var laneIds = new List<string>();
                foreach (var section in road.section)
                {
                    foreach (var laneId in section.lane_id)
                    {
                        laneIds.Add(laneId.id.ToString());
                    }
                }

                if (laneIds.Count > 1)
                {
                    // Create map lane section
                    GameObject mapLaneSection = new GameObject($"MapLaneSection_{road.id.id}");
                    mapLaneSection.AddComponent<MapLaneSection>();
                    mapLaneSection.transform.parent = TrafficLanes.transform;
                    
                    foreach (var id in laneIds)
                    {
                        LaneId2MapLaneSection[id] = mapLaneSection;
                    }

                    MoveLaneLines(laneIds, mapLaneSection);
                    RemoveExtraBoundaryLines(laneIds);
                }
                else
                {
                    var id = laneIds[0];
                    Id2Lane[id].transform.parent = SingleLaneRoads.transform;
                    Id2LeftLineBoundary[id].transform.parent = SingleLaneRoads.transform;
                    Id2RightLineBoundary[id].transform.parent = SingleLaneRoads.transform;
                }

                var junctionId = road.junction_id;
                if (junctionId != null) MoveLaneToJunction(laneIds, junctionId.id.ToString());
            }
        }
        
        string GetLaneId(string laneName)
        {
            return "lane_" + laneName.Split('_').Last();
        }

        List<Vector3> GetCenterLinePoints(MapLine mapLine1, MapLine mapLine2)
        {
            var points1 = mapLine1.mapWorldPositions;
            var points2 = mapLine2.mapWorldPositions;
            if ((points1.First() - points2.First()).magnitude > (points1.First() - points2.Last()).magnitude)
            {
                points2.Reverse();
            }
            var apolloMapTool = new ApolloMapTool(ApolloMapTool.ApolloVersion.Apollo_5_0);
            return apolloMapTool.ComputeCenterLine(points1, points2);
        }

        string RenameLine(string laneName, string otherLaneName)
        {
            var splittedName = laneName.Split('_');
            var otherLaneNumber = int.Parse(otherLaneName.Split('_').Last());
            var laneNumber = int.Parse(splittedName.Last());
            splittedName[1] = "Shared";
            splittedName[splittedName.Length - 1] = laneNumber < otherLaneNumber ? laneNumber + "_" + otherLaneNumber : otherLaneNumber + "_" + laneNumber;
            return String.Join("_", splittedName);
        }

        void RemoveExtraBoundaryLines(List<string> laneIds)
        {
            var visitedLanesLeft = new HashSet<string>();
            var visitedLanesRight = new HashSet<string>();

            CheckLeftLines(laneIds, visitedLanesLeft, visitedLanesRight);
            CheckRightLines(laneIds, visitedLanesLeft, visitedLanesRight);
        }

        void CheckLeftLines(List<string> laneIds, HashSet<string> visitedLanesLeft, HashSet<string> visitedLanesRight)
        {
            MapLane otherLane;

            foreach (var laneId in laneIds)
            {
                if (visitedLanesLeft.Contains(laneId)) continue;
                var lane = Id2Lane[laneId];
                var leftLine = lane.leftLineBoundry;
                if (lane.leftLaneForward != null)
                {
                    otherLane = lane.leftLaneForward;
                    var otherRightLine = otherLane.rightLineBoundry;
                    leftLine.mapWorldPositions = GetCenterLinePoints(leftLine, otherRightLine);
                    UpdateLocalPositions(leftLine);

                    lane.leftLineBoundry = leftLine;
                    GameObject.DestroyImmediate(otherRightLine.gameObject);
                    otherLane.rightLineBoundry = leftLine;

                    leftLine.name = RenameLine(leftLine.name, otherLane.name);
                    visitedLanesRight.Add(GetLaneId(otherLane.name));
                }
                else if (lane.leftLaneReverse != null)
                {
                    otherLane = lane.leftLaneReverse;
                    var otherLeftLine = otherLane.leftLineBoundry;
                    leftLine.mapWorldPositions = GetCenterLinePoints(leftLine, otherLeftLine);
                    UpdateLocalPositions(leftLine);

                    lane.leftLineBoundry = leftLine;
                    GameObject.DestroyImmediate(otherLeftLine.gameObject);
                    otherLane.leftLineBoundry = leftLine;

                    leftLine.name = RenameLine(leftLine.name, otherLane.name);
                    visitedLanesLeft.Add(GetLaneId(otherLane.name));
                }
                visitedLanesLeft.Add(laneId);
            }
        }

        void CheckRightLines(List<string> laneIds, HashSet<string> visitedLanesLeft, HashSet<string> visitedLanesRight)
        {
            MapLane otherLane;

            foreach (var laneId in laneIds)
            {
                if (visitedLanesRight.Contains(laneId)) continue;
                var lane = Id2Lane[laneId];
                var rightLine = lane.rightLineBoundry;

                if (lane.rightLaneForward != null)
                {
                    otherLane = lane.rightLaneForward;
                    var otherLeftLine = otherLane.leftLineBoundry;
                    rightLine.mapWorldPositions = GetCenterLinePoints(rightLine, otherLeftLine);
                    UpdateLocalPositions(rightLine);

                    lane.rightLineBoundry = rightLine;
                    GameObject.DestroyImmediate(otherLeftLine.gameObject);
                    otherLane.leftLineBoundry = rightLine;

                    rightLine.name = RenameLine(rightLine.name, otherLane.name);
                    visitedLanesLeft.Add(GetLaneId(otherLane.name));
                }
                else if (lane.rightLaneReverse != null)
                {
                    otherLane = lane.rightLaneReverse;
                    var otherRightLine = otherLane.rightLineBoundry;
                    rightLine.mapWorldPositions = GetCenterLinePoints(rightLine, otherRightLine);
                    UpdateLocalPositions(rightLine);

                    lane.rightLineBoundry = rightLine;
                    GameObject.DestroyImmediate(otherRightLine.gameObject);
                    otherLane.rightLineBoundry = rightLine;

                    rightLine.name = RenameLine(rightLine.name, otherLane.name);
                    visitedLanesRight.Add(GetLaneId(otherLane.name));
                }
            }
        }

        void MoveLaneLines(List<string> laneIds, GameObject mapLaneSection)
        {
            var lanePositions = new List<Vector3>();
            foreach (var laneId in laneIds)
            {
                var lane = Id2Lane[laneId];
                lane.transform.parent = mapLaneSection.transform;
                UpdateObjPosAndLocalPos(lane.transform, lane);
                lanePositions.Add(lane.transform.position);

                var leftLine = Id2LeftLineBoundary[laneId];
                leftLine.transform.parent = mapLaneSection.transform;
                UpdateObjPosAndLocalPos(leftLine.transform, leftLine);

                var rightLine = Id2RightLineBoundary[laneId];
                rightLine.transform.parent = mapLaneSection.transform;
                UpdateObjPosAndLocalPos(rightLine.transform, rightLine);

            }
            // Update mapLaneSection transform based on all lanes and update all lane's positions
            mapLaneSection.transform.position = Lanelet2MapImporter.GetAverage(lanePositions);
            UpdateChildrenPositions(mapLaneSection);
        }

        // Update children positions after parent position changed from 0 to keep children world positions same
        static void UpdateChildrenPositions(GameObject parent)
        {
            foreach (Transform child in parent.transform)
            {
                child.transform.position -= parent.transform.position;
            }
        }

        void ConnectLanes()
        {
            var visitedLaneIdsEnd = new HashSet<string>(); // lanes whose end point has been visited
            var visitedLaneIdsStart = new HashSet<string>(); // lanes whose start point has been visited
            foreach (var lane in ApolloMap.lane)
            {
                var laneId = lane.id.id.ToString();
                var predecessorIds = lane.predecessor_id;
                var successorIds = lane.successor_id;
                var positions = Id2Lane[laneId].mapWorldPositions;
                if (predecessorIds != null)
                {
                    foreach (var predecessorId in predecessorIds.Select(x => x.id.ToString()))
                    {
                        Id2Lane[laneId].befores.Add(Id2Lane[predecessorId]);
                        if (!visitedLaneIdsEnd.Contains(predecessorId)) AdjustStartOrEndPoint(positions, predecessorId, true);
                        visitedLaneIdsEnd.Add(predecessorId);
                    }
                }

                if (successorIds != null)
                {
                    foreach (var successorId in successorIds.Select(x => x.id.ToString()))
                    {
                        Id2Lane[laneId].afters.Add(Id2Lane[successorId]);
                        if (!visitedLaneIdsStart.Contains(successorId)) AdjustStartOrEndPoint(positions, successorId, false);
                        visitedLaneIdsStart.Add(successorId);
                    }
                }
            }
        }

        // Make current lane's start/end point same as predecessor/successor lane's end/start point
        void AdjustStartOrEndPoint(List<Vector3> positions, string connectLaneId, bool adjustEndPoint)
        {
            MapLane connectLane = Id2Lane[connectLaneId];
            var connectLaneWorldPositions = connectLane.mapWorldPositions;
            var connectLaneLocalPositions = connectLane.mapLocalPositions;
            if (adjustEndPoint)
            {
                connectLaneWorldPositions[connectLaneWorldPositions.Count - 1] = positions.First();
                connectLaneLocalPositions[connectLaneLocalPositions.Count - 1] = connectLane.transform.InverseTransformPoint(positions.First());
            }
            else
            {
                connectLaneWorldPositions[0] = positions.Last();
                connectLaneLocalPositions[0] = connectLane.transform.InverseTransformPoint(positions.Last());
            }
        }
        void ImportOverlaps()
        {
            foreach ( var overlap in ApolloMap.overlap)
            {
                var overlapId = overlap.id.ToString();
                if (overlap.@object.Count > 2)
                {
                    Debug.LogWarning("More than 2 overlap objects in one overlap, not supported yet.");
                    continue;
                }

                string signalSignId = null, laneId = null;
                double startS = 0;
                foreach (var obj in overlap.@object)
                {
                    if (obj.lane_overlap_info != null)
                    {
                        laneId = obj.id.id.ToString();
                        startS = obj.lane_overlap_info.start_s;
                    }
                    else if (obj.signal_overlap_info != null || obj.stop_sign_overlap_info != null || obj.yield_sign_overlap_info != null)
                    {
                        if (obj.id != null) signalSignId = obj.id.id.ToString();
                    }
                }

                if (signalSignId != null && laneId != null) SignalSignId2LaneIdStartS[signalSignId] = Tuple.Create(laneId, startS);
            }
        }

        void ImportStopSigns()
        {
            foreach (var stopSign in ApolloMap.stop_sign)
            {
                var id = stopSign.id.id.ToString();
                if (!SignalSignId2LaneIdStartS.ContainsKey(id))
                {
                    Debug.LogError($"StopSign {id} has no corresponding overlap, skipping importing it.");
                    continue;
                }
                
                CreateStopLine(id, stopSign.stop_line, true);
                var overlapLaneIdStartS = SignalSignId2LaneIdStartS[id];
                SetStopLineRotation(id, overlapLaneIdStartS, out MapLine stopLine);
                
                var intersectionId = GetIntersectionId(overlapLaneIdStartS);
                if (intersectionId == null)
                {
                    Debug.LogError($"No nearest intersection found for this stop sign {id}! Cannot assign it under an existing intersection.");
                    CreateMapIntersection(id);
                    Debug.LogWarning($"Please manually adjust the intersection: MapIntersection_{id}");
                    intersectionId = id;
                }
                Id2MapIntersection[intersectionId].isStopSignIntersection = true;

                CreateStopSign(id, intersectionId);
                MoveBackIfOnIntersectionLane(id, overlapLaneIdStartS);
                stopLine.transform.parent = Id2MapIntersection[intersectionId].transform;
                UpdateLocalPositions(stopLine);
            }

            Debug.Log($"Imported {ApolloMap.stop_sign.Count} Stop Signs.");
        }
        
        void CreateStopLine(string id, List<Curve> curves, bool isStopSign)
        {
            GameObject mapLineObj = new GameObject("MapLineStop_" + id);
            var mapLine = mapLineObj.AddComponent<MapLine>();
            mapLine.lineType = MapData.LineType.STOP;
            mapLine.isStopSign = isStopSign;

            if (curves.Count > 1) Debug.LogWarning($"Found multiple stop lines for same signal / stop_sign, not supported yet.");
            var curveSegments = curves[0].segment;
            var linePointsDouble3 = GetPointsFromCurve(curveSegments);
            mapLine.mapWorldPositions = ConvertUTMFromDouble3(linePointsDouble3);
            UpdateObjPosAndLocalPos(mapLineObj.transform, mapLine);
            Id2StopLine[id] = mapLine;
        }

        void SetStopLineRotation(string id, Tuple<string, double> overlapLaneIdStartS, out MapLine stopLine)
        {
            var overlapLaneDirection = GetDirection(overlapLaneIdStartS);
            stopLine = Id2StopLine[id];
            stopLine.transform.rotation = Quaternion.LookRotation(overlapLaneDirection);
        }

        string GetIntersectionId(Tuple<string, double> laneIdStartS)
        {
            var laneId = laneIdStartS.Item1;
            var startS = laneIdStartS.Item2;
            if (LaneId2JunctionId.ContainsKey(laneId)) return LaneId2JunctionId[laneId];

            var preJunctionId = GetJunctionId(Id2Lane[laneId].befores);
            var sucJunctionId = GetJunctionId(Id2Lane[laneId].afters);
            if (preJunctionId != null && sucJunctionId != null)
            {
                return GetNearestIntersectionId(laneId, startS, preJunctionId, sucJunctionId);
            }

            return preJunctionId != null ? preJunctionId : sucJunctionId;
        }
        
        string GetJunctionId(List<MapLane> lanes)
        {
            foreach (var lane in lanes)
            {
                var laneId = GetLaneId(lane.name);
                if (LaneId2JunctionId.ContainsKey(laneId)) return LaneId2JunctionId[laneId];
            }

            return null;
        }

        string GetNearestIntersectionId(string laneId, double startS, string preJunctionId, string sucJunctionId)
        {
            var worldPositions = Id2Lane[laneId].mapWorldPositions;
            var nearestIdx = GetNearestIdx(startS, worldPositions);
            var nearestPos = worldPositions[nearestIdx];
            // TODO: maybe other logics needed here.
            // Check StartS is more close to the beginning of the lane or the end of the lane.
            if ((nearestPos - worldPositions.First()).magnitude < (nearestPos - worldPositions.Last()).magnitude) return preJunctionId;
            else return sucJunctionId;
        }

        static int GetNearestIdx(double s, List<Vector3> lanePositions)
        {
            var curS = 0f;
            var nearestIdx = lanePositions.Count - 1;
            for (int i = 1; i < lanePositions.Count; i++)
            {
                curS += (lanePositions[i] - lanePositions[i - 1]).magnitude;
                if (curS >= s)
                {
                    nearestIdx = i;
                    break;
                }
            }

            return nearestIdx;
        }

        void CreateStopSign(string id, string intersectionId)
        {
            var intersection = Id2MapIntersection[intersectionId];
            var stopLine = Id2StopLine[id];
            var stopSignLocation = GetStopSignMeshLocation(stopLine);

            var mapSignObj = new GameObject("MapStopSign_" + id);
            mapSignObj.transform.position = stopSignLocation;
            mapSignObj.transform.rotation = Quaternion.LookRotation(-stopLine.transform.forward);
            mapSignObj.transform.parent = intersection.transform;

            var mapSign = mapSignObj.AddComponent<MapSign>();
            mapSign.signType = MapData.SignType.STOP;
            mapSign.stopLine = stopLine;
            mapSign.boundOffsets = new Vector3(0f, 2.55f, 0f);
            mapSign.boundScale = new Vector3(0.95f, 0.95f, 0f);

            // Create stop sign mesh
            if (IsMeshNeeded) CreateStopSignMesh(id, intersection, mapSign);
        }

        void CreateStopSignMesh(string id, MapIntersection intersection, MapSign mapSign)
        {
            GameObject stopSignPrefab = Settings.MapStopSignPrefab;
            var stopSignObj = UnityEngine.Object.Instantiate(stopSignPrefab, mapSign.transform.position + mapSign.boundOffsets, mapSign.transform.rotation);
            stopSignObj.transform.parent = intersection.transform;
            stopSignObj.name = "MapStopSignMesh_" + id;
        }

        Vector3 GetStopSignMeshLocation(MapLine stopLine)
        {
            float dist = 2; // we create stop sign 2 meters to the right of the right end point of the stopline
            var worldPositions = stopLine.mapWorldPositions;
            var stopLineLength = (worldPositions.Last() - worldPositions.First()).magnitude;
            var meshLocation = stopLine.transform.position + stopLine.transform.right * (dist + stopLineLength / 2);
            meshLocation -= stopLine.transform.forward * 1; // move stop sign 1 meter back

            return meshLocation;
        }

        // Make sure stop line is intersecting with the correct lane, not an intersection lane.
        void MoveBackIfOnIntersectionLane(string id, Tuple<string, double> overlapLaneIdStartS)
        {
            var laneId = overlapLaneIdStartS.Item1;
            var startS = overlapLaneIdStartS.Item2;
            if (LaneId2JunctionId.ContainsKey(laneId))
            {
                var stopLine = Id2StopLine[id];
                var dir = -stopLine.transform.forward;
                
                var befores = Id2Lane[laneId].befores;
                // If multiple before lanes, hard to know where to move the stop line
                if (befores.Count > 1)
                    Debug.LogWarning($"stopLine {id} might not in correct position, please check and move back yourself if necessary.");
                
                // move 1 meter more over the last point of the predecessor lane
                var sqrDist = Utility.SqrDistanceToSegment(stopLine.mapWorldPositions.First(), 
                    stopLine.mapWorldPositions.Last(), befores[0].mapWorldPositions.Last());
                var dist = math.sqrt(sqrDist) + 1; 
                stopLine.transform.position += dir * (float)dist;
                stopLine.mapWorldPositions = stopLine.mapWorldPositions.Select(x => x + dir * (float)dist).ToList();
            }
        }

        public static void UpdateLocalPositions(MapDataPoints mapDataPoints)
        {
            var localPositions = mapDataPoints.mapLocalPositions;
            var worldPositions = mapDataPoints.mapWorldPositions;
            localPositions.Clear();
            for (int i = 0; i < worldPositions.Count; i++)
            {
                localPositions.Add(mapDataPoints.transform.InverseTransformPoint(worldPositions[i]));
            }
        }

        static List<double3> GetPointsFromCurve(List<CurveSegment> curveSegments)
        {
            var points = new List<double3>();
            foreach (var curveSegment in curveSegments)
            {
                foreach (var point in curveSegment.line_segment.point)
                {
                    points.Add(ToDouble3(point));
                }
            }

            return points;
        }

        Vector3 GetDirection(Tuple<string, double> laneIdStartS)
        {
            var laneId = laneIdStartS.Item1;
            if (LaneId2JunctionId.ContainsKey(laneId))
            {
                // current lane is an intersection lane, return predecessor lane direction
                var predLanePositions = Id2Lane[laneId].befores[0].mapWorldPositions;
                return (predLanePositions.Last() - predLanePositions[predLanePositions.Count - 2]).normalized;
            }
            
            var s = laneIdStartS.Item2;
            var lanePositions = Id2Lane[laneId].mapWorldPositions;
            int nearestIdx = GetNearestIdx(s, lanePositions);
            return (lanePositions[nearestIdx] - lanePositions[nearestIdx - 1]).normalized;
        }

        void ImportSignals()
        {
            foreach (var signal in ApolloMap.signal)
            {
                var id = signal.id.id.ToString();
                if (!SignalSignId2LaneIdStartS.ContainsKey(id))
                {
                    Debug.LogError($"Signal {id} has no corresponding overlap, skipping importing it.");
                    continue;
                }

                CreateStopLine(id, signal.stop_line, false);
                var overlapLaneIdStartS = SignalSignId2LaneIdStartS[id];
                SetStopLineRotation(id, overlapLaneIdStartS, out MapLine stopLine);

                var intersectionId = GetIntersectionId(overlapLaneIdStartS);
                if (intersectionId == null)
                {
                    Debug.LogError($"No nearest intersection found for this stop signal {id}! Cannot assign it under an existing intersection.");
                    CreateMapIntersection(id);
                    Debug.LogWarning($"Please manually adjust the intersection: MapIntersection_{id}");
                    intersectionId = id;
                }

                CreateSignal(signal, intersectionId);
                MoveBackIfOnIntersectionLane(id, overlapLaneIdStartS);
                stopLine.transform.parent = Id2MapIntersection[intersectionId].transform;
                UpdateLocalPositions(stopLine);
            }

            Debug.Log($"Imported {ApolloMap.signal.Count} Signals.");
        }

        void CreateSignal(Signal signal, string intersectionId)
        {
            if (signal.type != Signal.Type.Mix3Vertical) Debug.LogError("Simulator currently only support Mix3Vertical signals.");

            var id = signal.id.id.ToString();
            var intersection = Id2MapIntersection[intersectionId];
            var mapSignalObj = new GameObject("MapSignal_" + id);
            var signalPosition = GetSignalPosition(signal);
            mapSignalObj.transform.position = signalPosition;
            var stopLine = Id2StopLine[id];
            mapSignalObj.transform.rotation = Quaternion.LookRotation(-stopLine.transform.forward);
            mapSignalObj.transform.parent = intersection.transform;

            var mapSignal = mapSignalObj.AddComponent<MapSignal>();
            mapSignal.signalData = new List<MapData.SignalData> {
                new MapData.SignalData() { localPosition = Vector3.up * 0.4f, signalColor = MapData.SignalColorType.Red },
                new MapData.SignalData() { localPosition = Vector3.zero, signalColor = MapData.SignalColorType.Yellow },
                new MapData.SignalData() { localPosition = Vector3.up * -0.4f, signalColor = MapData.SignalColorType.Green },
            };

            mapSignal.boundScale = new Vector3(0.65f, 1.5f, 0.0f);
            mapSignal.stopLine = stopLine;
            mapSignal.signalType = MapData.SignalType.MIX_3_VERTICAL;

            mapSignal.signalLightMesh = GetSignalMesh(id, intersection, mapSignal);
        }

        Renderer GetSignalMesh(string id, MapIntersection intersection, MapSignal mapSignal)
        {
            var mapSignalMesh = UnityEngine.Object.Instantiate(Settings.MapTrafficSignalPrefab, mapSignal.transform.position, mapSignal.transform.rotation);
            mapSignalMesh.transform.parent = intersection.transform;
            mapSignalMesh.name = "MapSignalMeshVertical_" + id;
            var mapSignalMeshRenderer = mapSignalMesh.AddComponent<SignalLight>().GetComponent<Renderer>();

            return mapSignalMeshRenderer;
        }

        Vector3 GetSignalPosition(Signal signal)
        {
            // use the middle signal light location as the position of the signal.
            var centerLightPosition = signal.subsignal[1].location;
            var position = MapOrigin.FromNorthingEasting(centerLightPosition.y, centerLightPosition.x, false);
            // Uncomment following line to use the actual height
            position.y = (float)centerLightPosition.z - MapOrigin.AltitudeOffset;
            return position;
        }

        // Experimental features
        void SetIntersectionTriggerBounds()
        {
            foreach (var pair in Id2MapIntersection)
            {
                var id = pair.Key;
                var mapIntersection = pair.Value;

                var direction = FindADirection(mapIntersection); // find any straight line as the major direction of intersection

                GetBoundsPoints(mapIntersection, direction, out Vector3 leftBottom, out Vector3 rightBottom, out Vector3 rightTop, out Vector3 leftTop);
                
                if (ShowDebugIntersectionArea)
                {
                    var time = 60; // time showing debug rectangles
                    Debug.DrawLine(leftBottom, rightBottom, Color.red, time);
                    Debug.DrawLine(rightBottom, rightTop, Color.red, time);
                    Debug.DrawLine(leftTop, rightTop, Color.red, time);
                    Debug.DrawLine(leftTop, leftBottom, Color.red, time);
                    Debug.DrawLine(leftBottom, mapIntersection.transform.position, Color.yellow, time); 
                }
                // TODO: update mapIntersection.triggerBounds based on the four corners  // offset by 2m inwards, 10 m height
            }
        }

        static void GetBoundsPoints(MapIntersection mapIntersection,  Vector3 direction, out Vector3 leftBottom, out Vector3 rightBottom, out Vector3 rightTop, out Vector3 leftTop)
        {
            // use dot product to get min/max values
            var perpendicularDir = Vector3.Cross(direction, Vector3.up);
            ComputeProjectedMinMaxPoints(mapIntersection, direction, out Vector3 minPoint, out Vector3 maxPoint);
            ComputeProjectedMinMaxPoints(mapIntersection, perpendicularDir, out Vector3 minPerpendicularPoint, out Vector3 maxPerpendicularPoint);
            
            leftBottom = GetIntersectingPoint(perpendicularDir, direction, minPoint, minPerpendicularPoint);
            leftBottom.y = minPoint.y;
            rightBottom = GetIntersectingPoint(perpendicularDir, direction, minPoint, maxPerpendicularPoint);
            rightBottom.y = minPoint.y;
            rightTop = GetIntersectingPoint(perpendicularDir, direction, maxPoint, maxPerpendicularPoint);
            rightTop.y = maxPoint.y;
            leftTop = GetIntersectingPoint(perpendicularDir, direction, maxPoint, minPerpendicularPoint);
            leftTop.y = maxPoint.y;
        }

        // http://geomalgorithms.com/a05-_intersect-1.html
        static Vector3 GetIntersectingPoint(Vector3 uDir, Vector3 vDir, Vector3 pPoint, Vector3 qPoint)
        {
            var wVec = ToVector2(pPoint) - ToVector2(qPoint);
            var sI = (vDir.z * wVec.x - vDir.x * wVec.y) / (vDir.x * uDir.z - vDir.z * uDir.x);
            var intersectPoint = ToVector2(pPoint) + sI * ToVector2(uDir);

            return ToVector3(intersectPoint);
        }

        static void ComputeProjectedMinMaxPoints(MapIntersection mapIntersection, Vector3 direction, out Vector3 minPoint, out Vector3 maxPoint)
        {
            var min = float.MaxValue;
            var max = float.MinValue;
            minPoint = Vector3.zero;
            maxPoint = Vector3.zero;

            // Get bounds from all intersection lines's end points
            foreach (Transform child in mapIntersection.transform)
            {
                var mapLine = child.GetComponent<MapLine>();
                if (mapLine != null)
                {
                    var firstPt = mapLine.mapWorldPositions.First();
                    var lastPt = mapLine.mapWorldPositions.Last();
                    var projectedFirstPt = Vector3.Dot(direction, firstPt);
                    var projectedLastPt = Vector3.Dot(direction, lastPt);
                    Vector3 tempMinPoint, tempMaxPoint;
                    float tempMin, tempMax;
                    if (projectedFirstPt < projectedLastPt) 
                    {
                        tempMinPoint = firstPt;
                        tempMin = projectedFirstPt;
                        tempMaxPoint = lastPt;
                        tempMax = projectedLastPt;
                    }
                    else
                    {
                        tempMinPoint = lastPt;
                        tempMin = projectedLastPt;
                        tempMaxPoint = firstPt;
                        tempMax = projectedFirstPt;
                    }
                    
                    if (tempMin < min)
                    {
                        min = tempMin;
                        minPoint = tempMinPoint;
                    }
                    if (tempMax > max)
                    {
                        max = tempMax;
                        maxPoint = tempMaxPoint;
                    }
                }
            }
        }
        Vector3 FindADirection(MapIntersection mapIntersection)
        {
            var direction = Vector3.right;
            var maxProduct = 0f;
            foreach (Transform child in mapIntersection.transform)
            {
                var mapLane = child.GetComponent<MapLane>();
                if (mapLane != null)
                {
                    // Check if the lane is a straight lane
                    var worldPositions = mapLane.mapWorldPositions;
                    var firstTwoPointsDir = (worldPositions[1] - worldPositions[0]).normalized;
                    var lastTwoPointsDir = (worldPositions[worldPositions.Count - 1] - worldPositions[worldPositions.Count - 2]).normalized;

                    var product = Vector3.Dot(firstTwoPointsDir, lastTwoPointsDir);
                    if (product > 0.9 && product > maxProduct)
                    {
                        direction = firstTwoPointsDir;
                        maxProduct = product;
                    }
                }
            }
            
            return direction;
        }

        void ImportYields()
        {
            foreach (var yield in ApolloMap.yield)
            {
                var id = yield.id.id.ToString();
                CreateStopLine(id, yield.stop_line, true);
                if (!SignalSignId2LaneIdStartS.ContainsKey(id))
                {
                    Debug.LogWarning($"yield {id} does not have an overlap associated!");
                    continue;
                }
                var overlapLaneIdStartS = SignalSignId2LaneIdStartS[id];
                SetStopLineRotation(id, overlapLaneIdStartS, out MapLine stopLine);
                var intersectionId = GetIntersectionId(overlapLaneIdStartS);
                if (intersectionId == null)
                {
                    Debug.LogError($"No nearest intersection found for this yield sign {id}! Cannot assign it under an existing intersection.");
                    CreateMapIntersection(id);
                    Debug.LogWarning($"Please manually adjust the intersection: MapIntersection_{id}");
                    intersectionId = id;
                }
                Id2MapIntersection[intersectionId].isStopSignIntersection = true;

                CreateStopSign(id, intersectionId); // TODO: Create yield sign once we have yield sign for NPCs
                MoveBackIfOnIntersectionLane(id, overlapLaneIdStartS);
                stopLine.transform.parent = Id2MapIntersection[intersectionId].transform;
                UpdateLocalPositions(stopLine);
            }

            Debug.Log($"Imported {ApolloMap.yield.Count} yield signs.");
        }

        void UpdateStopLines()
        {
            foreach (var entry in Id2StopLine)
            {
                var stopLine = entry.Value;
                // Compute intersecting lanes
                var intersectingLanes = GetOrderedIntersectingLanes(stopLine);
                if (intersectingLanes.Count == 0) continue; // No intersecting lanes found for this stop line.
                var firstMapLanePositions = intersectingLanes[0].mapWorldPositions;
                var laneDir = (firstMapLanePositions.Last() - firstMapLanePositions[firstMapLanePositions.Count - 2]).normalized;

                // Compute points based on lane end points
                var endPoints = intersectingLanes.Select(lane => lane.mapWorldPositions.Last()).ToList();
                var newStopLinePositions = new List<Vector3>();
                for (int i = 0; i < endPoints.Count; i++)
                {
                    var endPoint = endPoints[i];
                    var normalDir = Vector3.Cross(laneDir, Vector3.up).normalized;
                    var halfLaneWidth = 2;
                    newStopLinePositions.Add(endPoint + normalDir * halfLaneWidth - laneDir * 0.5f); 
                    if (i == endPoints.Count - 1) newStopLinePositions.Add(endPoint - normalDir * halfLaneWidth - laneDir * 0.5f);
                }
                // Update stop line mapWorldPositions with new computed points
                stopLine.mapWorldPositions = newStopLinePositions;
                stopLine.transform.position = Lanelet2MapImporter.GetAverage(newStopLinePositions);
                UpdateLocalPositions(stopLine);
            }
        }

        List<MapLane> GetOrderedIntersectingLanes(MapLine stopLine)
        {
            var stopLinePositions = stopLine.mapWorldPositions;
            var intersectingLanes = new List<MapLane>();
            foreach (var entry in Id2Lane)
            {
                var mapLane = entry.Value;
                var positions = mapLane.mapWorldPositions;
                // last two points of the lane
                var p1 = positions[positions.Count - 2];
                var p2 = positions.Last();
                
                // check with every segment of the stop line
                for (var i = 0; i < stopLinePositions.Count - 1; i++)
                {
                    var p3 = stopLinePositions[i];
                    var p4 = stopLinePositions[i + 1];
                    var isIntersect = Utility.LineSegementsIntersect(ToVector2(p1), ToVector2(p2), ToVector2(p3), ToVector2(p4), out Vector2 intersection);
                    if (isIntersect)
                    {
                        intersectingLanes.Add(mapLane);
                        break;
                    }
                }
            }
            if (intersectingLanes.Count == 0)
            {
                Debug.LogWarning($"stopLine {stopLine.name} have no intersecting lanes");
            }
            else if (intersectingLanes.Count == 1) return intersectingLanes;
            else
            {
                intersectingLanes = OrderLanes(intersectingLanes);
            }

            return intersectingLanes;
        }

        List<MapLane> OrderLanes(List<MapLane> intersectingLanes)
        {
            // Pick any lane, compute normal direction, get distance to the lane and order lanes 
            var theLane = intersectingLanes[0];
            var p1 = ToVector2(theLane.mapWorldPositions[theLane.mapWorldPositions.Count - 2]);
            var p2 = ToVector2(theLane.mapWorldPositions.Last());
            var dir = p2 - p1;
            var rightNormalDir = new Vector2(dir.y, -dir.x);

            var distance2mapLane = new Dictionary<float, MapLane>();
            foreach (var lane in intersectingLanes)
            {
                var endPoint = lane.mapWorldPositions.Last();
                var dist = Vector2.Dot(rightNormalDir, ToVector2(endPoint) - p1);
                distance2mapLane[dist] = lane;
            }

            return distance2mapLane.OrderBy(entry => entry.Key).Select(entry => entry.Value).ToList();
        }

        static Vector2 ToVector2(Vector3 pt)
        {
            return new Vector2(pt.x, pt.z);
        }

        static Vector3 ToVector3(Vector2 p)
        {
            return new Vector3(p.x, 0f, p.y);
        }

        static double3 ToDouble3(apollo.common.PointENU point)
        {
            return new double3(point.x, point.y, point.z);
        }


    }
}
