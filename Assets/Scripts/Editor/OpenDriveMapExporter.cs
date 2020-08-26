/**
 * Copyright (c) 2019-2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEditor;
using UnityEngine;
using Simulator.Map;
using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using System.Xml;
using Schemas;
using System.Collections.Generic;


namespace Simulator.Editor
{
    public interface ILaneLineDataCommon<T> where T : ILaneLineDataCommon<T>
    {
        List<T> befores { get; set; }
        List<T> afters { get; set; }
    }

    public class PositionsData
    {
        public List<Vector3> mapLocalPositions = new List<Vector3>();
        public List<Vector3> mapWorldPositions = new List<Vector3>();
        public GameObject go;
        public PositionsData() {}
        public PositionsData(MapDataPoints mapDataPoints)
        {
            mapLocalPositions = new List<Vector3>(mapDataPoints.mapLocalPositions);
            mapWorldPositions = new List<Vector3>(mapDataPoints.mapWorldPositions);
            go = mapDataPoints.gameObject;
        }
    }

    public class LineData: PositionsData, ILaneLineDataCommon<LineData>
    {
        public MapLine mapLine;
        public static Dictionary<MapLine, LineData> Line2LineData = new Dictionary<MapLine, LineData>();
        public List<LineData> befores { get; set; } = new List<LineData>();
        public List<LineData> afters { get; set; } = new List<LineData>();
        public LineData() {}
        public LineData(MapLine line) : base(line)
        {
            mapLine = line;
            Line2LineData[line] = this;
        }
    }

    public class LaneData : PositionsData, ILaneLineDataCommon<LaneData>
    {
        public MapLane mapLane = null;
        public static Dictionary<MapLane, LaneData> Lane2LaneData = new Dictionary<MapLane, LaneData>();
        public List<LaneData> befores { get; set; } = new List<LaneData>();
        public List<LaneData> afters { get; set; } = new List<LaneData>();
        public LineData leftLineData = null;
        public LineData rightLineData = null;
        public laneType type = laneType.driving;
        public float speedLimit = 20f;
        public LaneData(MapLane lane) : base(lane)
        {
            mapLane = lane;
            Lane2LaneData[lane] = this;
            speedLimit = lane.speedLimit;
        }

        public LaneData(MapPedestrian mapPedestrian) : base(mapPedestrian)
        {
            type = laneType.sidewalk;
            speedLimit = 6.7056f; // 15 mph
        }
    }

    public class OpenDriveMapExporter
    {
        float FakePedestrianLaneWidth = 1.5f;
        double SideWalkHeight = 0.12;
        MapOrigin MapOrigin;
        MapManagerData MapAnnotationData;
        OpenDRIVE Map;
        HashSet<LaneData> LanesData;
        HashSet<LineData> LinesData;
        List<MapIntersection> Intersections;
        Dictionary<LaneData, uint> LaneData2RoadId = new Dictionary<LaneData, uint>();
        Dictionary<uint, List<LaneData>> RoadId2LanesData = new Dictionary<uint, List<LaneData>>();
        Dictionary<LaneData, int> LaneData2LaneId = new Dictionary<LaneData, int>(); // lane to its laneId inside OpenDRIVE road
        Dictionary<LaneData, uint> LaneData2JunctionId = new Dictionary<LaneData, uint>();
        Dictionary<uint, List<Vector3>> RoadId2RefLinePositions = new Dictionary<uint, List<Vector3>>(); // roadId to corresponding reference MapLine positions with correct order
        uint UniqueId;
        OpenDRIVERoad[] Roads;
        List<OpenDRIVEJunction> Junctions = new List<OpenDRIVEJunction>();
        HashSet<uint> JunctionRoadIds = new HashSet<uint>();

        public bool Calculate()
        {
            MapOrigin = MapOrigin.Find();
            if (MapOrigin == null)
            {
                return false;
            }

            MapAnnotationData = new MapManagerData();
            var allLanes = new HashSet<MapLane>(MapAnnotationData.GetData<MapLane>());
            var areAllLanesWithBoundaries = Lanelet2MapExporter.AreAllLanesWithBoundaries(allLanes, true);
            if (!areAllLanesWithBoundaries) return false;

            Intersections = MapAnnotationData.GetIntersections();
            MapAnnotationData.GetTrafficLanes();

            // Initial collection
            var laneSegments= new HashSet<MapLane>(MapAnnotationData.GetData<MapLane>());
            var lineSegments = new HashSet<MapLine>(MapAnnotationData.GetData<MapLine>());
            var signalLights = new List<MapSignal>(MapAnnotationData.GetData<MapSignal>());
            var crossWalkList = new List<MapCrossWalk>(MapAnnotationData.GetData<MapCrossWalk>());
            var mapSignList = new List<MapSign>(MapAnnotationData.GetData<MapSign>());
            var mapPedestrianPaths = new List<MapPedestrian>(MapAnnotationData.GetData<MapPedestrian>());

            foreach (var mapSign in mapSignList)
            {
                if (mapSign.signType == MapData.SignType.STOP && mapSign.stopLine != null)
                {
                    mapSign.stopLine.stopSign = mapSign;
                }
            }

            LinesData = GetLinesData(lineSegments);
            LanesData = GetLanesData(laneSegments);

            if (!LinkSegments(LanesData)) return false;
            if (!LinkSegments(LinesData)) return false;

            if (!CheckNeighborLanes(laneSegments)) return false;

            var location = MapOrigin.GetGpsLocation(MapOrigin.transform.position);
            var geoReference = " +proj=tmerc +lat_0="+ location.Latitude + " +lon_0=" + location.Longitude;
            geoReference += " +k=1 +x_0=" + location.Easting + " +y_0=" + location.Northing + " +datum=WGS84 +units=m +no_defs "; 
            Map = new OpenDRIVE()
            {
                header = new OpenDRIVEHeader()
                {
                    revMajor = (ushort)1,
                    revMajorSpecified = true,
                    revMinor = (ushort)4,
                    revMinorSpecified = true,
                    name = "",
                    version = 1.00f,
                    versionSpecified = true,
                    date = System.DateTime.Now.ToString("ddd, MMM dd HH':'mm':'ss yyy"),
                    vendor = "LGSVL",
                    north = 0,
                    northSpecified = true,
                    south = 0,
                    southSpecified = true,
                    east = 0,
                    eastSpecified = true,
                    west = 0,
                    westSpecified = true,
                    geoReference = geoReference,
                }
            };

            Lanelet2MapExporter.AlignPointsInLines(LanesData);

            CreateFakeLaneDataForPedPaths(mapPedestrianPaths);
            ComputeRoads();

            return true;
        }

        void CreateFakeLaneDataForPedPaths(List<MapPedestrian> mapPedestrianPaths)
        {
            foreach (var mapPedestrianPath in mapPedestrianPaths)
            {
                MapAnnotations.AddWorldPositions(mapPedestrianPath);
                var pedLaneData = new LaneData(mapPedestrianPath);
                ComputeFakeLineData(pedLaneData, out LineData leftLineData, out LineData rightLineData);
                pedLaneData.leftLineData = leftLineData;
                pedLaneData.rightLineData = rightLineData;
                LanesData.Add(pedLaneData);
            }
        }

        void ComputeFakeLineData(LaneData laneData, out LineData leftLineData, out LineData rightLineData)
        {
            var points = laneData.mapWorldPositions;
            leftLineData = new LineData();
            rightLineData = new LineData();

            for (int i = 0; i < points.Count; i++)
            {
                Vector3 leftNormalDir = OpenDriveMapImporter.GetNormalDir(points, i, true);
                leftLineData.mapWorldPositions.Add(points[i] + leftNormalDir * FakePedestrianLaneWidth / 2);
                rightLineData.mapWorldPositions.Add(points[i] - leftNormalDir * FakePedestrianLaneWidth / 2);
            }
        }

        public static HashSet<LaneData> GetLanesData(HashSet<MapLane> laneSegments)
        {
            LaneData.Lane2LaneData.Clear();
            var lanesData = new HashSet<LaneData>();
            foreach (var lane in laneSegments)
            {
                var laneData = new LaneData(lane);
                laneData.leftLineData = LineData.Line2LineData[lane.leftLineBoundry];
                laneData.rightLineData = LineData.Line2LineData[lane.rightLineBoundry];
                lanesData.Add(laneData);
                LaneData.Lane2LaneData[lane] = laneData;
            }
            return lanesData;
        }

        public static HashSet<LineData> GetLinesData(HashSet<MapLine> lines)
        {
            LineData.Line2LineData.Clear();
            var linesData = new HashSet<LineData>();
            foreach (var line in lines)
            {
                var lineData = new LineData(line);
                linesData.Add(lineData);
                LineData.Line2LineData[line] = lineData;
            }
            return linesData;
        }

        // Link before and after lanes/lines
        public static bool LinkSegments<T>(HashSet<T> segments) where T : PositionsData, ILaneLineDataCommon<T>
        {
            foreach (var segment in segments)
            {
                // clear
                segment.befores.Clear();
                segment.afters.Clear();

                if (typeof(T) == typeof(LineData))
                    if ((segment as LineData).mapLine.lineType == MapData.LineType.STOP) continue;

                // Each segment must have at least 2 waypoints for calculation, otherwise exit
                while (segment.mapLocalPositions.Count < 2)
                {
                    Debug.LogError("Some segment has less than 2 waypoints. Cancelling map generation.");
                    return false;
                }

                // Link lanes/lines
                var firstPt = segment.go.transform.TransformPoint(segment.mapLocalPositions[0]);
                var lastPt = segment.go.transform.TransformPoint(segment.mapLocalPositions[segment.mapLocalPositions.Count - 1]);

                foreach (var segmentCmp in segments)
                {
                    if (segment == segmentCmp)
                    {
                        continue;
                    }
                    if (typeof(T) == typeof(LineData))
                        if ((segmentCmp as LineData).mapLine.lineType == MapData.LineType.STOP) continue;

                    var firstPt_cmp = segmentCmp.go.transform.TransformPoint(segmentCmp.mapLocalPositions[0]);
                    var lastPt_cmp = segmentCmp.go.transform.TransformPoint(segmentCmp.mapLocalPositions[segmentCmp.mapLocalPositions.Count - 1]);

                    if ((firstPt - lastPt_cmp).magnitude < MapAnnotationTool.PROXIMITY / MapAnnotationTool.EXPORT_SCALE_FACTOR)
                    {
                        segmentCmp.mapLocalPositions[segmentCmp.mapLocalPositions.Count - 1] = segmentCmp.go.transform.InverseTransformPoint(firstPt);
                        segmentCmp.mapWorldPositions[segmentCmp.mapWorldPositions.Count - 1] = firstPt;
                        segment.befores.Add(segmentCmp);
                    }

                    if ((lastPt - firstPt_cmp).magnitude < MapAnnotationTool.PROXIMITY / MapAnnotationTool.EXPORT_SCALE_FACTOR)
                    {
                        segmentCmp.mapLocalPositions[0] = segmentCmp.go.transform.InverseTransformPoint(lastPt);
                        segmentCmp.mapWorldPositions[0] = lastPt;
                        segment.afters.Add(segmentCmp);
                    }
                }
            }

            return true;
        }

        public static bool CheckNeighborLanes(HashSet<MapLane> LaneSegments)
        {
            // Check validity of lane segment builder relationship but it won't warn you if have A's right lane to be null or B's left lane to be null
            foreach (var laneSegment in LaneSegments)
            {
                if (laneSegment.leftLaneForward != null && laneSegment != laneSegment.leftLaneForward.rightLaneForward ||
                    laneSegment.rightLaneForward != null && laneSegment != laneSegment.rightLaneForward.leftLaneForward)
                {
                    Debug.Log("Some lane segments neighbor relationships are wrong. Cancelling map generation.");
#if UNITY_EDITOR
                    UnityEditor.Selection.activeObject = laneSegment.gameObject;
                    Debug.Log("Please fix the selected lane.");
#endif
                    return false;
                }
            }

            return true;
        }

        // Return MapLine data from neighborLaneSectionLanes and return correct order of positions for reference line
        LineData GetRefLineAndPositions(List<LaneData> neighborLaneSectionLanesData, out List<Vector3> positions)
        {
            // Lanes are stored from left most to right most
            // Reference line is the left boundary line of the left most lane
            var refLineData = neighborLaneSectionLanesData[0].leftLineData;
            if (refLineData == null)
            {
                Debug.LogError($"Lane: {neighborLaneSectionLanesData[0].go.name} has no left boundary line, its object instance ID is {neighborLaneSectionLanesData[0].go.GetInstanceID()}");
            }

            positions = refLineData.mapWorldPositions;
            // Make sure refLine positions has same direction as the road lanes
            var leftLanePositions = neighborLaneSectionLanesData[0].mapWorldPositions; // pick any lane
            if (Vector3.Dot((positions.Last() - positions.First()), (leftLanePositions.Last() - leftLanePositions.First())) < 0)
            {
                positions.Reverse();
            }

            return refLineData;
        }

        // Function to get neighbor lanes in the same road
        List<LaneData> GetNeighborForwardLaneSectionLanesData(LaneData self, bool fromLeft)
        {
            if (self == null || self.mapLane == null)
            {
                return new List<LaneData>();
            }

            var lane = self.mapLane;
            if (fromLeft)
            {
                if (lane.leftLaneForward == null)
                {
                    return new List<LaneData>();
                }
                else
                {
                    var ret = new List<LaneData>();
                    ret.AddRange(GetNeighborForwardLaneSectionLanesData(LaneData.Lane2LaneData[lane.leftLaneForward], true));
                    ret.Add(LaneData.Lane2LaneData[lane.leftLaneForward]);
                    return ret;
                }
            }
            else
            {
                if (lane.rightLaneForward == null)
                {
                    return new List<LaneData>();
                }
                else
                {
                    var ret = new List<LaneData>();
                    ret.Add(LaneData.Lane2LaneData[lane.rightLaneForward]);
                    ret.AddRange(GetNeighborForwardLaneSectionLanesData(LaneData.Lane2LaneData[lane.rightLaneForward], false));
                    return ret;
                }
            }
        }


        void ComputeRoads()
        {
            var visitedLanesData = new HashSet<LaneData>();
            var neighborForwardLaneSectionLanesData = new List<List<LaneData>>();
            foreach (var laneSegment in LanesData)
            {
                if (visitedLanesData.Contains(laneSegment))
                {
                    continue;
                }

                var lefts = GetNeighborForwardLaneSectionLanesData(laneSegment, true);  // Left forward lanes from furthest to nearest
                var rights = GetNeighborForwardLaneSectionLanesData(laneSegment, false);  // Right forward lanes from nearest to furthest

                var laneSectionLanesData = new List<LaneData>();
                laneSectionLanesData.AddRange(lefts);
                laneSectionLanesData.Add(laneSegment);
                laneSectionLanesData.AddRange(rights);

                foreach (var l in laneSectionLanesData)
                {
                    if (!visitedLanesData.Contains(l))
                    {
                        visitedLanesData.Add(l);
                    }
                }
                neighborForwardLaneSectionLanesData.Add(laneSectionLanesData);
            }
            // Debug.Log($"We got {neighborForwardLaneSectionLanes.Count} laneSections, {LaneSegments.Count} lanes");

            var neighborLaneSectionLanesIdx = new List<List<int>>(); // Final laneSections idx after grouping
            visitedLanesData.Clear();
            var laneData2LaneSectionIdx = new Dictionary<LaneData, int>(); // LaneData to the index of laneSection in neighborLaneSectionLanes

            void AddToLaneData2Index(List<LaneData> laneSectionLanesData, int index)
            {
                foreach (var lane in laneSectionLanesData)
                {
                    laneData2LaneSectionIdx[lane] = index;
                }
            }

            for (var i = 0; i < neighborForwardLaneSectionLanesData.Count; i ++)
            {
                var laneSectionLanesData1 = neighborForwardLaneSectionLanesData[i];
                var leftMostLaneData1 = laneSectionLanesData1.First();
                var rightMostLaneData1 = laneSectionLanesData1.Last();

                if (visitedLanesData.Contains(leftMostLaneData1) || visitedLanesData.Contains(rightMostLaneData1)) continue;

                visitedLanesData.Add(leftMostLaneData1);
                visitedLanesData.Add(rightMostLaneData1);
                var found = false;
                // Find the neighbor laneSectionLanes
                for (var j = i+1; j < neighborForwardLaneSectionLanesData.Count; j ++)
                {
                    var laneSectionLanesData2 = neighborForwardLaneSectionLanesData[j];
                    var leftMostLaneData2 = laneSectionLanesData2.First();
                    var rightMostLaneData2 = laneSectionLanesData2.Last();

                    if (leftMostLaneData2.type == laneType.sidewalk || rightMostLaneData2.type == laneType.sidewalk) continue;

                    if (leftMostLaneData1 == leftMostLaneData2 && rightMostLaneData1 == rightMostLaneData2) continue;
                    if (visitedLanesData.Contains(leftMostLaneData2) || visitedLanesData.Contains(rightMostLaneData2)) continue;

                    // Consider both left-hand/right-hand traffic
                    var leftHandTraffic = (leftMostLaneData1.mapLane.leftLineBoundry == leftMostLaneData2.mapLane.leftLineBoundry);
                    var rightHandTraffic = (rightMostLaneData1.mapLane.rightLineBoundry == rightMostLaneData2.mapLane.rightLineBoundry);
                    if (leftHandTraffic || rightHandTraffic)
                    {
                        visitedLanesData.Add(leftMostLaneData2);
                        visitedLanesData.Add(rightMostLaneData2);
                        neighborLaneSectionLanesIdx.Add(new List<int>() {i, j});

                        AddToLaneData2Index(laneSectionLanesData1, neighborLaneSectionLanesIdx.Count-1);
                        AddToLaneData2Index(laneSectionLanesData2, neighborLaneSectionLanesIdx.Count-1);

                        found = true;
                    }
                }

                if (!found)
                {
                    // If the neighbor laneSectionLane not found, it is a one way road
                    neighborLaneSectionLanesIdx.Add(new List<int>() {i});
                    AddToLaneData2Index(laneSectionLanesData1, neighborLaneSectionLanesIdx.Count-1);
                }
            }

            // Debug.Log($"We got {neighborLaneSectionLanesIdx.Count} laneSections");

            // Find out starting lanes and start from them and go through all laneSections
            var startingLanesData = FindStartingLanesData();
            var visitedNLSLIdx = new HashSet<int>(); // visited indices of NLSL(neighborLaneSectionLanesIdx) list

            Vector3 GetDirection(LaneData mapLane)
            {
                var positions = mapLane.mapWorldPositions;
                return positions.Last() - positions.First();
            }

            Roads = new OpenDRIVERoad[neighborLaneSectionLanesIdx.Count];
            uint roadId = 0;
            foreach (var startingLaneData in startingLanesData)
            {
                // BFS until the lane has 0 afters
                var startingLaneNLSLIdx = laneData2LaneSectionIdx[startingLaneData];
                if (visitedNLSLIdx.Contains(startingLaneNLSLIdx)) continue;

                var queue = new Queue<LaneData>();
                queue.Enqueue(startingLaneData);

                while (queue.Any())
                {
                    var curLaneData = queue.Dequeue();
                    var curNLSLIdx = laneData2LaneSectionIdx[curLaneData];
                    if (visitedNLSLIdx.Contains(curNLSLIdx)) continue;
                    // Make a road for the laneSection curLane is in
                    // Reference line should have the same direction with curLane
                    var road = new OpenDRIVERoad()
                    {
                        name = "",
                        id = roadId.ToString(),
                    };

                    List<LaneData> consideredLanesData = new List<LaneData>();
                    LineData refLineData;
                    List<Vector3> refLinePositions;
                    // One Way lane section
                    if (neighborLaneSectionLanesIdx[curNLSLIdx].Count == 1)
                    {
                        var neighborLaneSectionLanesData = neighborForwardLaneSectionLanesData[neighborLaneSectionLanesIdx[curNLSLIdx][0]];

                        refLineData = GetRefLineAndPositions(neighborLaneSectionLanesData, out refLinePositions);

                        var onlyLine = false;
                        if (neighborLaneSectionLanesData[0].type == laneType.sidewalk) onlyLine = true;
                        AddPlanViewElevationLateral(refLinePositions, road, onlyLine);
                        AddLanes(road, refLineData, neighborLaneSectionLanesData, neighborLaneSectionLanesData);

                        visitedNLSLIdx.Add(curNLSLIdx);
                        consideredLanesData = neighborLaneSectionLanesData;
                    }
                    // Two Way lane section
                    else
                    {
                        var neighborLaneSectionLanes1 = neighborForwardLaneSectionLanesData[neighborLaneSectionLanesIdx[curNLSLIdx][0]];
                        var neighborLaneSectionLanes2 = neighborForwardLaneSectionLanesData[neighborLaneSectionLanesIdx[curNLSLIdx][1]];

                        // Check curLane direction, reference line should have same direction with it
                        var direction1 = GetDirection(neighborLaneSectionLanes1[0]);
                        var curDirection = GetDirection(curLaneData);

                        List<LaneData> leftNeighborLaneSectionLanes, rightNeighborLaneSectionLanes;
                        if (Vector3.Dot(direction1, curDirection) > 0)
                        {
                            refLineData = GetRefLineAndPositions(neighborLaneSectionLanes1, out refLinePositions);
                            rightNeighborLaneSectionLanes = neighborLaneSectionLanes1;
                            leftNeighborLaneSectionLanes = neighborLaneSectionLanes2;
                        }
                        else
                        {
                            refLineData = GetRefLineAndPositions(neighborLaneSectionLanes2, out refLinePositions);
                            rightNeighborLaneSectionLanes = neighborLaneSectionLanes2;
                            leftNeighborLaneSectionLanes = neighborLaneSectionLanes1;
                        }

                        AddPlanViewElevationLateral(refLinePositions, road);
                        AddLanes(road, refLineData, leftNeighborLaneSectionLanes, rightNeighborLaneSectionLanes, false);

                        visitedNLSLIdx.Add(laneData2LaneSectionIdx[neighborLaneSectionLanes1[0]]);
                        visitedNLSLIdx.Add(laneData2LaneSectionIdx[neighborLaneSectionLanes2[0]]);
                        consideredLanesData.AddRange(neighborLaneSectionLanes1);
                        consideredLanesData.AddRange(neighborLaneSectionLanes2);
                    }

                    GetLaneSectionLanesAfterLanes(queue, consideredLanesData, visitedNLSLIdx, laneData2LaneSectionIdx);
                    Add2Lane2RoadId(roadId, consideredLanesData);

                    // Add speed limit
                    var maxSpeedMPH = consideredLanesData[0].speedLimit * 2.23694;
                    road.type = new OpenDRIVERoadType[1]{
                        new OpenDRIVERoadType(){
                            speed = new OpenDRIVERoadTypeSpeed(){
                                max = maxSpeedMPH.ToString(),
                                unit = unit.mph,
                                unitSpecified = true,
                            },
                            s = 0,
                            sSpecified = true,
                            type = roadType.town,
                            typeSpecified = true,
                        }
                    };

                    road.objects = new OpenDRIVERoadObjects();
                    road.signals = new OpenDRIVERoadSignals();
                    Roads[roadId] = road;
                    RoadId2RefLinePositions[roadId] = refLinePositions;

                    // for (int i = 0; i < consideredLanesData.Count; i++)
                    // {
                    //     var msg = $"Instance ID: {consideredLanesData[i].go.GetInstanceID()} original name {consideredLanesData[i].go.name}";
                    //     msg += $" road Id {roadId} lane Id {LaneData2LaneId[consideredLanesData[i]]} ";
                    //     Debug.Log(msg, consideredLanesData[i].go);
                    // }
                    roadId += 1;
                }
            }

            // Create controller for pairs of signals in each intersection
            var controllers = new List<OpenDRIVEController>();

            UniqueId = roadId;
            AddJunctions(UniqueId, controllers);

            foreach (var NLSLIdxList in neighborLaneSectionLanesIdx)
            {
                var roadBeforeLanesData = new HashSet<LaneData>(); // lanes before current road
                var roadAfterLanesData = new HashSet<LaneData>();

                // One way road
                if (NLSLIdxList.Count == 1)
                {
                    UpdateLaneLink(roadBeforeLanesData, roadAfterLanesData, neighborForwardLaneSectionLanesData[NLSLIdxList[0]]);
                }
                // Two way Roads
                else
                {
                    UpdateLaneLink(roadBeforeLanesData, roadAfterLanesData, neighborForwardLaneSectionLanesData[NLSLIdxList[0]]);
                    UpdateLaneLink(roadBeforeLanesData, roadAfterLanesData, neighborForwardLaneSectionLanesData[NLSLIdxList[1]]);
                }

                var curRoadId = LaneData2RoadId[neighborForwardLaneSectionLanesData[NLSLIdxList[0]][0]];
                UpdateRoadLink(roadBeforeLanesData, roadAfterLanesData, curRoadId);
            }

            Map.road = Roads;
            Map.controller = controllers.ToArray();
            Map.junction = Junctions.ToArray();
        }

        void AddJunctions(uint roadId, List<OpenDRIVEController> controllers)
        {
            uint firstJunctionId = roadId;
            uint junctionId = firstJunctionId;
            UniqueId += (uint)Intersections.Count;

            // Add junctions, assume all intersection lanes are grouped under MapIntersection objects
            foreach (var mapIntersection in Intersections)
            {
                var junction = new OpenDRIVEJunction()
                {
                    id = junctionId.ToString(),
                    name = "",
                };
                var intersectionLanes = mapIntersection.transform.GetComponentsInChildren<MapLane>();
                var updatedRoadIds = new HashSet<uint>();

                // Tuple: (incomingRoadId, connectingRoadId, contactPoint)
                var connections2LaneLink = new Dictionary<Tuple<uint, uint, contactPoint>, List<OpenDRIVEJunctionConnectionLaneLink>>();
                var allBeforeAfterLanesData = new List<LaneData>();
                foreach (var lane in intersectionLanes)
                {
                    var laneData = LaneData.Lane2LaneData[lane];
                    LaneData2JunctionId[laneData] = junctionId;

                    var incomingLanesData = laneData.befores;
                    var beforeAfterLanes = new List<LaneData>(incomingLanesData);
                    beforeAfterLanes.AddRange(laneData.afters);
                    allBeforeAfterLanesData.AddRange(beforeAfterLanes);

                    UpdateConnections2LaneLink(connections2LaneLink, laneData, incomingLanesData);

                    // Update corresponding road header's junctionId
                    roadId = LaneData2RoadId[laneData];
                    JunctionRoadIds.Add(roadId);
                    if (updatedRoadIds.Contains(roadId)) continue;
                    Roads[roadId].junction = junctionId.ToString();
                }

                // Add signal/sign for each intersection
                var mapSignals = mapIntersection.transform.GetComponentsInChildren<MapSignal>();
                var mapStopLines = mapIntersection.transform.GetComponentsInChildren<MapLine>().Where(line => line.isStopSign == true).ToList();
                mapIntersection.SetIntersectionData(); // set facingGroup and oppFacingGroup, to get controller
                var facingGroupSignalIds = new List<uint>();
                var oppFacingGroupSignalIds = new List<uint>();
                var roadId2Signals = new Dictionary<uint, List<OpenDRIVERoadSignalsSignal>>();
                var roadId2SignalReferences = new Dictionary<uint, List<OpenDRIVERoadSignalsSignalReference>>();

                // Create dictionary from stop line to roadId
                var stopLine2RoadId = new Dictionary<MapLine, uint>();
                foreach (var laneData in allBeforeAfterLanesData)
                {
                    var roadIdOfLane = LaneData2RoadId[laneData];
                    var stopLine = laneData.mapLane.stopLine;
                    if (stopLine == null || stopLine2RoadId.ContainsKey(stopLine)) continue;
                    stopLine2RoadId[stopLine] = roadIdOfLane;
                }

                foreach (var mapSignal in mapSignals)
                {
                    // Find the nearest approaching road to this signal
                    var pairedRoadId = GetPairedRoadId(stopLine2RoadId, mapSignal);
                    if (mapIntersection.facingGroup.Contains(mapSignal)) facingGroupSignalIds.Add(UniqueId);
                    if (mapIntersection.oppFacingGroup.Contains(mapSignal)) oppFacingGroupSignalIds.Add(UniqueId);

                    var roadIdStopLine = stopLine2RoadId[mapSignal.stopLine];
                    // Create signal
                    var isOnRoad = pairedRoadId == roadIdStopLine; // Check if the signal created will on the intended road
                    var signal = CreateSignalFromMapSignal(pairedRoadId, mapSignal, isOnRoad);
                    roadId2Signals.CreateOrAdd(pairedRoadId, signal);


                    // Create signal reference on the intended road if signal is not created on the intended road
                    if (pairedRoadId != roadIdStopLine)
                    {
                        var orien = GetOrientation(RoadId2RefLinePositions[roadIdStopLine], mapSignal.transform.position, mapSignal.transform.forward);
                        var s = GetSAndT(mapSignal.transform.position, orien, roadIdStopLine, true, out double t);

                        // Not very clear about how t is computed for road referencing a signal
                        var signalReference = new OpenDRIVERoadSignalsSignalReference()
                        {
                            s = s,
                            sSpecified = true,
                            t = t,
                            tSpecified = true,
                            id = signal.id,
                            orientation = orien,
                            orientationSpecified = true,
                        };
                        roadId2SignalReferences.CreateOrAdd(roadIdStopLine, signalReference);
                    }
                }

               // Create controller, currently every intersection only has two controllers TODO
                var controllerIds = new List<uint>();
                if (mapSignals.Length > 0)
                {
                    controllerIds.Add(UniqueId);
                    controllers.Add(CreateController(facingGroupSignalIds));
                    controllerIds.Add(UniqueId);
                    controllers.Add(CreateController(oppFacingGroupSignalIds));

                    var junctionControllers = new OpenDRIVEJunctionController[2]
                    {
                        new OpenDRIVEJunctionController(){id = controllerIds[0].ToString(), type = ""},
                        new OpenDRIVEJunctionController(){id = controllerIds[1].ToString(), type = ""},
                    };

                    junction.controller = junctionControllers;
                }

                var mapSigns = mapIntersection.transform.GetComponentsInChildren<MapSign>();
                foreach (var mapSign in mapSigns)
                {
                    // Find the nearest road to this sign
                    if (!stopLine2RoadId.ContainsKey(mapSign.stopLine))
                    {
                        Debug.LogError($"Cannot find the nearest entering road for {mapSign.name}, skipping it.");
                        continue;
                    }
                    var nearestRoadId = stopLine2RoadId[mapSign.stopLine];

                    // Create signal from sign
                    var signal = CreateSignalFromSign(nearestRoadId, mapSign);
                    roadId2Signals.CreateOrAdd(nearestRoadId, signal);
                }

                // Add signals and signalReferences
                foreach (var pair in roadId2Signals)
                {
                    var signal = new List<OpenDRIVERoadSignalsSignal>(pair.Value);
                    if (Roads[pair.Key].signals != null && Roads[pair.Key].signals.signal != null)
                    {
                        signal.AddRange(Roads[pair.Key].signals.signal);
                    }

                    var signals = new OpenDRIVERoadSignals();
                    signals.signal = signal.ToArray();
                    Roads[pair.Key].signals = signals;
                }

                foreach (var pair in roadId2SignalReferences)
                {
                    var id = pair.Key;
                    if (Roads[id].signals == null)
                    {
                        Roads[id].signals = new OpenDRIVERoadSignals();
                    }

                    var signalReference = new List<OpenDRIVERoadSignalsSignalReference>(pair.Value);
                    if (Roads[id].signals.signalReference != null)
                    {
                        signalReference.AddRange(Roads[id].signals.signalReference);
                    }
                    Roads[id].signals.signalReference = signalReference.ToArray();
                }

                junction.connection = CreateConnections(connections2LaneLink);
                Junctions.Add(junction);
                junctionId += 1;
            }
        }

        void UpdateConnections2LaneLink(
            Dictionary<Tuple<uint, uint, contactPoint>, List<OpenDRIVEJunctionConnectionLaneLink>> connections2LaneLink,
            LaneData laneData, List<LaneData> incomingLanesData)
        {
            var connectingRoadId = LaneData2RoadId[laneData];
            var connectingLaneId = LaneData2LaneId[laneData];

            foreach (var incomingLaneData in incomingLanesData)
            {
                var incomingRoadId = LaneData2RoadId[incomingLaneData];
                var refPoints = RoadId2RefLinePositions[incomingRoadId];
                var incomingLaneId = LaneData2LaneId[incomingLaneData];
                var closetRoadPoint = incomingLaneId < 0 ? refPoints.Last() : refPoints.First();
                var key = Tuple.Create(incomingRoadId, connectingRoadId, GetContactPoint(incomingRoadId, closetRoadPoint, laneData));
                if (connections2LaneLink.ContainsKey(key))
                {
                    connections2LaneLink[key].Add(
                        new OpenDRIVEJunctionConnectionLaneLink()
                        {
                            from = LaneData2LaneId[incomingLaneData],
                            to = connectingLaneId,
                            fromSpecified = true,
                            toSpecified = true,
                        }
                    );
                }
                else
                {
                    connections2LaneLink[key] = new List<OpenDRIVEJunctionConnectionLaneLink>()
                        {
                            new OpenDRIVEJunctionConnectionLaneLink()
                            {
                                from = LaneData2LaneId[incomingLaneData],
                                to = connectingLaneId,
                                fromSpecified = true,
                                toSpecified = true,
                            }
                        };
                }
            }
        }

        OpenDRIVEJunctionConnection[] CreateConnections(Dictionary<Tuple<uint, uint, contactPoint>, List<OpenDRIVEJunctionConnectionLaneLink>> connections2LaneLink)
        {
            var connections = new OpenDRIVEJunctionConnection[connections2LaneLink.Keys.Count];
            var index = 0;
            foreach (var entry in connections2LaneLink)
            {
                var laneLinks = entry.Value.ToArray();
                connections[index++] = new OpenDRIVEJunctionConnection()
                {
                    laneLink = laneLinks,
                    id = index.ToString(),
                    incomingRoad = entry.Key.Item1.ToString(),
                    connectingRoad = entry.Key.Item2.ToString(),
                    contactPoint = entry.Key.Item3,
                    contactPointSpecified = true,
                };
            }

            return connections;
        }

        double GetSAndT(Vector3 signalSignPos, orientation orien, uint roadId, bool isOnRoad, out double t)
        {
            double s = 0;
            var refLinePositions = RoadId2RefLinePositions[roadId];
            // Get the neareset segment of the refline to the signal, orientation.Item is + (same direction), orientation.Item1 is -
            Vector3 p0 = refLinePositions[0], p1 = refLinePositions[1];
            if (orien == orientation.Item && !isOnRoad)
            {
                s = Roads[roadId].length;
                p1 = refLinePositions.Last();
                p0 = refLinePositions[refLinePositions.Count-2];
            }
            else if (orien == orientation.Item1 && isOnRoad)
            {
                s = Roads[roadId].length;
                p0 = refLinePositions.Last();
                p1 = refLinePositions[refLinePositions.Count-2];
            }
            else if (orien == orientation.Item1 && !isOnRoad)
            {
                s = 0;
                p1 = refLinePositions.First();
                p0 = refLinePositions[1];
            }

            var dist2First = (signalSignPos - refLinePositions.First()).magnitude;
            var dist2Last = (signalSignPos - refLinePositions.Last()).magnitude;
            if (dist2First > dist2Last && s == 0)
            {
                s = Roads[roadId].length;
                p0 = refLinePositions.Last();
                p1 = refLinePositions[refLinePositions.Count-2];
                if (orien == orientation.Item1)
                {
                    p1 = refLinePositions.Last();
                    p0 = refLinePositions[refLinePositions.Count-2];
                }
            }
            else if (dist2First < dist2Last && s == Roads[roadId].length)
            {
                s = 0;
                p0 = refLinePositions[0];
                p1 = refLinePositions[1];
                if (orien == orientation.Item1)
                {
                    p1 = refLinePositions[0];
                    p0 = refLinePositions[1];
                }
            }

            t = GetT(signalSignPos, p0, p1);
            if (Math.Abs(t) > 40)
            {
                Debug.LogError($"t is {t}, signal might be associated with wrong s, please check signals near road {roadId}");
            }

            // Check left or right
            var vec1 = ToVector3(ToVector2(p0)) - ToVector3(ToVector2(p1)); // Set y to 0
            var vec2 = ToVector3(ToVector2(signalSignPos)) - ToVector3(ToVector2(p1));
            var cross = Vector3.Cross(vec1, vec2).y;
            if ((cross > 0 && orien == orientation.Item1) || (cross < 0 && orien == orientation.Item))
            {
                t = -t;
            }

            return s;
        }

        static double GetT(Vector3 signalSignPos, Vector3 p0, Vector3 p1)
        {
            double t;
            float x1 = p0.x, y1 = p0.z, x2 = p1.x, y2 = p1.z, x0 = signalSignPos.x, y0 = signalSignPos.z;
            t = Math.Abs((y2 - y1) * x0 - (x2 - x1) * y0 + x2 * y1 - y2 * x1) / Math.Sqrt((y2 - y1) * (y2 - y1) + (x2 - x1) * (x2 - x1));
            return t;
        }

        uint GetPairedRoadId(Dictionary<MapLine, uint> stopLine2RoadId, MapSignal mapSignal)
        {
            Vector3 GetNormalizedVector(List<Vector3> positions)
            {
                return (positions.Last() - positions.First()).normalized;
            }

            MapLine pairedStopLine = null;
            var stopLine = mapSignal.stopLine;
            foreach (var pair in stopLine2RoadId)
            {
                var curStopLine = pair.Key;
                if (curStopLine == stopLine) continue;
                // check stop line forward with curStopLine forward
                var vec1 = GetNormalizedVector(curStopLine.mapWorldPositions);
                var vec2 = GetNormalizedVector(stopLine.mapWorldPositions);
                var dot = Vector3.Dot(vec1, vec2);
                if (dot > 0.866 || dot < -0.866)
                {
                    pairedStopLine = curStopLine;
                }
            }

            // if paired road not found or signal is more close to the stopLine than to the paired one, return roadId of stopLine
            var roadId = stopLine2RoadId[stopLine];
            if (pairedStopLine == null) return roadId;

            var signalPosition = mapSignal.transform.position;
            var pairedStopLinePos = (pairedStopLine.mapWorldPositions.Last() + pairedStopLine.mapWorldPositions.First())/2;
            var stopLinePos = (stopLine.mapWorldPositions.Last() + stopLine.mapWorldPositions.First())/2;
            if ((signalPosition - stopLinePos).magnitude < (signalPosition - pairedStopLinePos).magnitude) return roadId;

            return stopLine2RoadId[pairedStopLine];
        }

        OpenDRIVEController CreateController(List<uint> signalIds)
        {
            var controller = new OpenDRIVEController()
            {
                id = UniqueId.ToString(),
                name = "ctrl-" + UniqueId,
            };
            UniqueId++;

            var controls = new OpenDRIVEControllerControl[signalIds.Count];
            for (int i = 0; i < signalIds.Count; i++)
            {
                controls[i] = new OpenDRIVEControllerControl()
                {
                    signalId = signalIds[i].ToString(),
                    type = "",
                };
            }
            controller.control = controls;

            return controller;
        }

        int GetClosestPointIdx(List<Vector3> refLinePositions, Vector3 signalSignPos)
        {
            int closestIdx = 0;
            float minDistance = float.MaxValue;
            for (int i = 0; i < refLinePositions.Count - 1; ++i)
            {
                var dist = (signalSignPos - refLinePositions[i]).magnitude;
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestIdx = i;
                }
            }

            return closestIdx;
        }

        orientation GetOrientation(List<Vector3> refLinePositions, Vector3 signalSignPos, Vector3 signalSignDir)
        {
            var closestIdx = GetClosestPointIdx(refLinePositions, signalSignPos);
            Vector3 refLineDir;
            if (closestIdx == 0) refLineDir = refLinePositions[1] - refLinePositions[0];
            else refLineDir = refLinePositions[closestIdx] - refLinePositions[closestIdx - 1];
            var orien = orientation.Item;
            if (Vector3.Dot(signalSignDir, refLineDir) < 0)
            {
                orien = orientation.Item1;
            }

            return orien;
        }

        OpenDRIVERoadSignalsSignal CreateSignalFromMapSignal(uint nearestRoadId, MapSignal mapSignal, bool isOnRoad)
        {
            var signalPosition = mapSignal.transform.position;
            var positions = RoadId2RefLinePositions[nearestRoadId];

            // Raycast to compute the height of the signal
            RaycastHit hit = new RaycastHit();
            int mapLayerMask = LayerMask.GetMask("Default");
            var boundOffsets = Vector3.zero;
            var zOffset = signalPosition.y;
            if (Physics.Raycast(signalPosition, Vector3.down, out hit, 1000.0f, mapLayerMask))
            {
                zOffset = hit.distance;
            }

            // Compute orientation
            var orien = GetOrientation(positions, signalPosition, mapSignal.transform.forward);
            var s = GetSAndT(signalPosition, orien, nearestRoadId, isOnRoad, out double t);
            var height = mapSignal.boundScale.y;
            var width = mapSignal.boundScale.x;

            var signal = new OpenDRIVERoadSignalsSignal()
            {
                s = s,
                sSpecified = true,
                t = t,
                tSpecified = true,
                id = (UniqueId++).ToString(),
                name = mapSignal.name,
                dynamic = dynamic.yes,
                dynamicSpecified = true,
                orientation = orien,
                orientationSpecified = true,
                zOffset = zOffset,
                zOffsetSpecified = true,
                country = "OpenDRIVE",
                type = "1000001",
                subtype = "-1",
                value = -1,
                valueSpecified = true,
                height = height,
                heightSpecified = true,
                width = width,
                widthSpecified = true,
            };

            return signal;
        }

        OpenDRIVERoadSignalsSignal CreateSignalFromSign(uint nearestRoadId, MapSign mapSign)
        {
            var signPosition = mapSign.transform.position; // note sign is on the ground
            var positions = RoadId2RefLinePositions[nearestRoadId];
            var orien = GetOrientation(positions, signPosition, mapSign.transform.forward);
            var s = GetSAndT(signPosition, orien, nearestRoadId, true, out double t);

            var zOffset = mapSign.boundOffsets.y;
            var height = mapSign.boundScale.y;
            var width = mapSign.boundScale.x;

            var signal = new OpenDRIVERoadSignalsSignal()
            {
                s = s,
                sSpecified = true,
                t = t,
                tSpecified = true,
                id = (UniqueId++).ToString(),
                name = mapSign.name,
                dynamic = dynamic.no,
                dynamicSpecified = true,
                orientation = orien,
                orientationSpecified = true,
                zOffset = zOffset,
                zOffsetSpecified = true,
                country = "OpenDRIVE",
                type = "206",
                subtype = "-1",
                value = -1,
                valueSpecified = true,
                height = height,
                heightSpecified = true,
                width = width,
                widthSpecified = true,
            };

            return signal;
        }

        void UpdateRoadLink(HashSet<LaneData> roadBeforeLanesData, HashSet<LaneData> roadAfterLanesData, uint curRoadId)
        {
            // note: before == predecessor, after == successor
            var preRoadIds = new HashSet<uint>();
            LaneData curBeforeLaneData = null, curAfterLaneData = null;
            foreach (var beforeLane in roadBeforeLanesData)
            {
                curBeforeLaneData = beforeLane;
                preRoadIds.Add(LaneData2RoadId[beforeLane]);
            }
            var sucRoadIds = new HashSet<uint>();
            foreach (var afterLane in roadAfterLanesData)
            {
                curAfterLaneData = afterLane;
                sucRoadIds.Add(LaneData2RoadId[afterLane]);
            }

            uint? preRoadId = null;
            uint? sucRoadId = null;
            var roadPredecessor = new OpenDRIVERoadLinkPredecessor();
            var roadSuccessor = new OpenDRIVERoadLinkSuccessor();

            var curRefLinePoints = RoadId2RefLinePositions[curRoadId];
            var roadLink = new OpenDRIVERoadLink();
            if (preRoadIds.Count > 0)
            {
                if (preRoadIds.Count > 1 || JunctionRoadIds.Contains(GetOnlyItemFromSet(preRoadIds)))
                {
                    // junction
                    roadPredecessor = new OpenDRIVERoadLinkPredecessor()
                    {
                        elementType = elementType.junction,
                        elementId = GetJunctionId(preRoadIds),
                        elementTypeSpecified = true,
                    };
                    // Remove predecessors from each lane in this road
                    RemoveLaneLinks(curRoadId, true);
                }
                else
                {
                    preRoadId = LaneData2RoadId[curBeforeLaneData];

                    roadPredecessor = new OpenDRIVERoadLinkPredecessor()
                    {
                        elementType = elementType.road,
                        elementId = preRoadId.ToString(),
                        contactPoint = GetContactPoint(curRoadId, curRefLinePoints.First(), curBeforeLaneData),
                        elementTypeSpecified = true,
                        contactPointSpecified = true,
                    };
                }

                roadLink = new OpenDRIVERoadLink()
                {
                    predecessor = roadPredecessor,
                };
            }

            if (sucRoadIds.Count > 0)
            {
                if (sucRoadIds.Count > 1 || JunctionRoadIds.Contains(GetOnlyItemFromSet(sucRoadIds)))
                {
                    roadSuccessor = new OpenDRIVERoadLinkSuccessor()
                    {
                        elementType = elementType.junction,
                        elementId = GetJunctionId(sucRoadIds),
                        elementTypeSpecified = true,
                    };
                    // Remove successors from each lane for this road
                    RemoveLaneLinks(curRoadId, false);
                }
                else
                {
                    sucRoadId = LaneData2RoadId[curAfterLaneData];

                    roadSuccessor = new OpenDRIVERoadLinkSuccessor()
                    {
                        elementType = elementType.road,
                        elementId = sucRoadId.ToString(),
                        contactPoint = GetContactPoint(curRoadId, curRefLinePoints.Last(), curAfterLaneData),
                        elementTypeSpecified = true,
                        contactPointSpecified = true,
                    };
                }

                roadLink = new OpenDRIVERoadLink()
                {
                    successor = roadSuccessor,
                };
           }

            if (preRoadIds.Count > 0 && sucRoadIds.Count > 0)
            {
                roadLink = new OpenDRIVERoadLink()
                {
                    predecessor = roadPredecessor,
                    successor = roadSuccessor,
                };
            }
            else if (preRoadIds.Count == 0 && sucRoadIds.Count == 0)
            {
                roadLink = new OpenDRIVERoadLink();
            }

            Roads[curRoadId].link = roadLink;

            // Update road header junctionId
            if (Roads[curRoadId].junction == null)
            {
                Roads[curRoadId].junction = (-1).ToString();
            }
        }

        void RemoveLaneLinks(uint roadId, bool isPredecessor)
        {
            var allLanes = new List<lane>();
            if (Roads[roadId].lanes.laneSection[0].left != null) allLanes.AddRange(Roads[roadId].lanes.laneSection[0].left.lane);
            if (Roads[roadId].lanes.laneSection[0].right != null) allLanes.AddRange(Roads[roadId].lanes.laneSection[0].right.lane);

            bool hasMoreThanOneLinkedLanes = false;

            foreach (var mapLane in RoadId2LanesData[roadId])
            {
                if (isPredecessor)
                {
                    if ((LaneData2LaneId[mapLane] < 0 && mapLane.befores.Count > 1) || (LaneData2LaneId[mapLane] > 0 && mapLane.afters.Count > 1))
                    {
                        hasMoreThanOneLinkedLanes = true;
                        break;
                    }
                }
                else
                {
                    if ((LaneData2LaneId[mapLane] < 0 && mapLane.afters.Count > 1) || (LaneData2LaneId[mapLane] > 0 && mapLane.befores.Count > 1))
                    {
                        hasMoreThanOneLinkedLanes = true;
                        break;
                    }
                }
            }

            if (hasMoreThanOneLinkedLanes) // only delete all records if there are more than one lane has more than one links
            {
                foreach (var lane in allLanes)
                {
                    if (isPredecessor) lane.link.predecessor = null;
                    else lane.link.successor = null;
                }
            }
        }

        // Get the only item from a set of 1 item
        static uint GetOnlyItemFromSet(HashSet<uint> roadIds)
        {
            uint roadId = uint.MaxValue;
            foreach (var id in roadIds)
            {
                return id;
            }
            return roadId;
        }

        // Given current road, its closet reference line point to predecessor/successor road
        // and any lane of predecessor/successor road, get contactPoint of the predecessor/successor road
        contactPoint GetContactPoint(uint curRoadId, Vector3 closestRoadPoint, LaneData linkRoadLaneData)
        {
            var positions = linkRoadLaneData.mapWorldPositions;
            var linkedRoadLaneStartPoint = positions.First();
            var linkedRoadLaneEndPoint = positions.Last();
            if (LaneData2LaneId[linkRoadLaneData] > 0)
            {
                // if the lane is a left lane in the road, reference line is opposite with the lane
                linkedRoadLaneStartPoint = positions.Last();
                linkedRoadLaneEndPoint = positions.First();
            }
            if ((closestRoadPoint - linkedRoadLaneStartPoint).magnitude > (closestRoadPoint - linkedRoadLaneEndPoint).magnitude)
            {
                return contactPoint.end;
            }

            return contactPoint.start;
        }
        string GetJunctionId(HashSet<uint> roadIds)
        {
            var junctionIds = new HashSet<string>();
            string junctionId = null;
            foreach (var roadId in roadIds)
            {
                var curJunctionId = Roads[roadId].junction;

                if (curJunctionId == "-1")
                {
                    Debug.LogWarning("A junction should not have id as -1, roadId: " + roadId + ". It might because your intersection has no signal/sign in it.");
                    Debug.LogWarning("Creating a junction for this road.");
                    curJunctionId = CreateJunctionWithoutControllers(roadIds);
                    continue;
                }

                junctionIds.Add(curJunctionId);
                junctionId = curJunctionId;
                if (junctionId == null)
                {
                    var msg = $"Road: {roadId} junction id is null, please double check the corresponding lane.";
                    Debug.LogWarning(msg, RoadId2LanesData[roadId][0].go);
                    msg = "\tIf this lane belongs to a laneSection, please make sure all adjacent lanes are sharing correct boundary lines!";
                    msg += "Otherwise, you can ignore this message.";
                    Debug.LogWarning(msg);
                }
            }

            if (junctionIds.Count == 0) Debug.LogError("No junctionId found!");
            else if (junctionIds.Count > 1) Debug.LogError("Multiple junctionId found for one road predecessor/successor!");

            return junctionId;
        }

        string CreateJunctionWithoutControllers(HashSet<uint> roadIds)
        {
            var junctionId = UniqueId + 1;
            var junction = new OpenDRIVEJunction()
            {
                id = junctionId.ToString(),
                name = "",
            };

            var junctionLanesData = new List<LaneData>();
            foreach (var roadId in roadIds)
            {
                junctionLanesData.AddRange(RoadId2LanesData[roadId]);
            }

            var connections2LaneLink = new Dictionary<Tuple<uint, uint, contactPoint>, List<OpenDRIVEJunctionConnectionLaneLink>>();
            foreach (var laneData in junctionLanesData)
            {
                LaneData2JunctionId[laneData] = junctionId;
                var incomingLanesData = laneData.befores;
                UpdateConnections2LaneLink(connections2LaneLink, laneData, incomingLanesData);

                // Update corresponding road header's junctionId
                uint roadId = LaneData2RoadId[laneData];
                Roads[roadId].junction = junctionId.ToString();
            }

            junction.connection = CreateConnections(connections2LaneLink);
            Junctions.Add(junction);
            return junctionId.ToString();
        }

        void UpdateLaneLink(HashSet<LaneData> roadBeforeLanesData, HashSet<LaneData> roadAfterLanesData, List<LaneData> lanesData)
        {
            var isLeft = false;
            if (LaneData2LaneId[lanesData[0]] > 0) isLeft = true; // left lanes always have positive Ids

            foreach (var lane in lanesData)
            {
                var befores = lane.befores;
                var afters = lane.afters;
                if (isLeft)
                {
                    befores = lane.afters;
                    afters = lane.befores;
                }

                // Add link field for lane if only 1 before or after
                int? preLaneId = null;
                int? sucLaneId = null;
                var lanePredecessor = new laneLinkPredecessor();
                var laneSuccessor = new laneLinkSuccessor();

                laneLink laneLink = new laneLink();
                if (befores.Count == 1)
                {
                    preLaneId = LaneData2LaneId[befores[0]];
                    lanePredecessor = new laneLinkPredecessor()
                    {
                        id = preLaneId.Value,
                        idSpecified = true,
                    };

                    laneLink = new laneLink()
                    {
                        predecessor = lanePredecessor,
                    };
                }

                if (afters.Count == 1)
                {
                    sucLaneId = LaneData2LaneId[afters[0]];
                    laneSuccessor = new laneLinkSuccessor()
                    {
                        id = sucLaneId.Value,
                        idSpecified = true,
                    };

                    laneLink = new laneLink()
                    {
                        successor = laneSuccessor,
                    };
                }

                if (preLaneId != null || sucLaneId != null)
                {
                    if (preLaneId != null && sucLaneId != null)
                    {
                        laneLink = new laneLink()
                        {
                            predecessor = lanePredecessor,
                            successor = laneSuccessor,
                        };
                    }
                    var roadId = LaneData2RoadId[lane];
                    var laneId = LaneData2LaneId[lane];
                    var laneIdx = Mathf.Abs(laneId) - 1; // right lanes Id: -1, -2, -3, ...
                    if (isLeft)
                    {
                        laneIdx = lanesData.Count - laneId; // left lanes Id: ..., 4, 3, 2, 1
                        Roads[roadId].lanes.laneSection[0].left.lane[laneIdx].link = laneLink;
                    }
                    else
                    {
                        Roads[roadId].lanes.laneSection[0].right.lane[laneIdx].link = laneLink;
                    }
                }
                roadBeforeLanesData.UnionWith(befores);
                roadAfterLanesData.UnionWith(afters);
            }
        }

        void Add2Lane2RoadId(uint roadId, List<LaneData> consideredLanesData)
        {
            foreach (var laneData in consideredLanesData)
            {
               LaneData2RoadId[laneData] = roadId;
               if (RoadId2LanesData.ContainsKey(roadId)) RoadId2LanesData[roadId].Add(laneData);
               else RoadId2LanesData[roadId] = new List<LaneData>(){laneData};
            }
        }

        void GetLaneSectionLanesAfterLanes(Queue<LaneData> queue, List<LaneData> lanesData, HashSet<int> visitedNLSLIdx, Dictionary<LaneData, int> laneData2LaneSectionIdx)
        {
            foreach (var curLaneData in lanesData)
            {
                foreach (var lanedata in curLaneData.afters)
                {
                    if (!visitedNLSLIdx.Contains(laneData2LaneSectionIdx[lanedata]))
                    {
                        queue.Enqueue(lanedata);
                    }
                }
            }
        }

        void AddLanes(OpenDRIVERoad road, LineData refLineData,
            List<LaneData> leftNeighborLaneSectionLanesData,
            List<LaneData> rightNeighborLaneSectionLanesData, bool isOneWay=true)
        {
            var lanes = new OpenDRIVERoadLanes();

            // TODO Add laneOffset for complex urban Roads
            // Add laneSection

            var center = CreateCenterLane(true, rightNeighborLaneSectionLanesData[0]);
            var right = CreateRightLanes(refLineData, rightNeighborLaneSectionLanesData);
            var laneSectionArray = new OpenDRIVERoadLanesLaneSection[1];

            laneSectionArray[0] = new OpenDRIVERoadLanesLaneSection()
            {
                s = 0,
                sSpecified = true,
                center = center,
                right = right,
            };

            if (!isOneWay)
            {
                var left = CreateLeftLanes(refLineData, leftNeighborLaneSectionLanesData);
                laneSectionArray[0] = new OpenDRIVERoadLanesLaneSection()
                {
                    s = 0,
                    sSpecified = true,
                    left = left,
                    center = center,
                    right = right,
                };
            }

            lanes.laneSection = laneSectionArray;
            road.lanes = lanes;
        }

        void AddPlanViewElevationLateral(List<Vector3> positions, OpenDRIVERoad road, bool onlyLine=false)
        {
            OpenDRIVERoadGeometry[] geometryArray;
            OpenDRIVERoadElevationProfileElevation[] elevationProfileArray;
            double roadLength;
            UpdateGeometryArrayElevationProfileArray(positions, out geometryArray, out elevationProfileArray, out roadLength, onlyLine);

            road.length = roadLength;
            road.lengthSpecified = true;
            road.planView = geometryArray;
            road.elevationProfile = new OpenDRIVERoadElevationProfile()
            {
                elevation = elevationProfileArray,
            };
            road.lateralProfile = new OpenDRIVERoadLateralProfile();
        }

        void UpdateGeometryArrayElevationProfileArray(List<Vector3> positions,
            out OpenDRIVERoadGeometry[] geometryArray,
            out OpenDRIVERoadElevationProfileElevation[] elevationProfileArray,
            out double roadLength, bool onlyLine)
        {
            float[] sList;
            if (onlyLine) geometryArray = FitGeometryLine(positions, out sList);
            else geometryArray = FitGeometryParaPoly3(positions, out sList);

            elevationProfileArray = new OpenDRIVERoadElevationProfileElevation[positions.Count - 1];
            for (int i = 0; i < positions.Count - 1; i ++)
            {
                var point = positions[i];
                var x = point.x;
                var y = point.z;
                var vec = positions[i+1] - positions[i];
                var length = vec.magnitude;
                var angle = Mathf.Deg2Rad * Vector2.SignedAngle(Vector2.right, new Vector2(vec.x, vec.z));
                var line = new OpenDRIVERoadGeometryLine();

                var a = positions[i].y;
                var b = (positions[i+1].y - positions[i].y) / length; // For line type
                elevationProfileArray[i] = new OpenDRIVERoadElevationProfileElevation() // TODO: may need to fit as well
                {
                    s = sList[i],
                    a = a,
                    b = b,
                    c = 0,
                    d = 0,
                    sSpecified = true,
                    aSpecified = true,
                    bSpecified = true,
                    cSpecified = true,
                    dSpecified = true,
                };

            }
            roadLength = sList[sList.Length - 1];
        }

        OpenDRIVERoadGeometry[] FitGeometryLine(List<Vector3> positions, out float[] sList)
        {
            var geometryArray = new OpenDRIVERoadGeometry[positions.Count - 1];
            float curS = 0;
            sList = new float[positions.Count];
            Debug.Assert(positions.Count >= 2, "a line should have at least two points");

            for (int i = 0; i < positions.Count - 1; i ++)
            {
                sList[i] = curS;

                var point = positions[i];
                var x = point.x;
                var y = point.z;
                var vec = positions[i+1] - positions[i];
                var length = vec.magnitude;
                var angle = Mathf.Deg2Rad * Vector2.SignedAngle(Vector2.right, new Vector2(vec.x, vec.z));
                var line = new OpenDRIVERoadGeometryLine();
                geometryArray[i] = new OpenDRIVERoadGeometry()
                {
                    s = curS,
                    x = x,
                    y = y,
                    hdg = angle, // the angle between this lane segment and x axis (EAST)
                    length = length,
                    Items = new OpenDRIVERoadGeometryLine[1]{line},
                    sSpecified = true,
                    xSpecified = true,
                    ySpecified = true,
                    hdgSpecified = true,
                    lengthSpecified = true,
                };

                curS += length;
            }
            sList[positions.Count - 1] = curS; // last s is road length

            return geometryArray;
        }

        OpenDRIVERoadGeometry[] FitGeometryParaPoly3(List<Vector3> positions, out float[] sList)
        {
            var geometryArray = new OpenDRIVERoadGeometry[positions.Count - 1];
            float curS = 0;
            sList = new float[positions.Count];
            Debug.Assert(positions.Count >= 2, "a line should have at least two points");

            // Add 1st coefficients for the first two points
            if (positions.Count >= 2)
            {
                sList[0] = curS;
                var point = positions[0];
                var x = point.x;
                var y = point.z;
                var vec = positions[1] - positions[0];
                var length = vec.magnitude;
                var angle = Mathf.Deg2Rad * Vector2.SignedAngle(Vector2.right, new Vector2(vec.x, vec.z));
                var line = new OpenDRIVERoadGeometryLine();
                geometryArray[0] = new OpenDRIVERoadGeometry()
                {
                    s = curS,
                    x = x,
                    y = y,
                    hdg = angle, // the angle between this lane segment and x axis (EAST)
                    length = length,
                    Items = new OpenDRIVERoadGeometryLine[1]{line},
                    sSpecified = true,
                    xSpecified = true,
                    ySpecified = true,
                    hdgSpecified = true,
                    lengthSpecified = true,
                };
                curS += length;
            }

            if (positions.Count >= 4)
            {
                for (var i = 1; i < positions.Count - 2; i++)
                {
                    sList[i] = curS;
                    var point = positions[i];
                    var x = point.x;
                    var y = point.z;
                    var vec = positions[i + 1] - positions[i];

                    var tempList = new List<Vector3>{positions[i-1], positions[i], positions[i+1], positions[i+2]};
                    var controlPoints = GetControlPoints(tempList, i == 1, i == positions.Count - 3);
                    var withControlPoints = new List<Vector3>{positions[i]};
                    withControlPoints.AddRange(controlPoints);
                    withControlPoints.Add(positions[i+1]);
                    var tangentDir = withControlPoints[1] - withControlPoints[0];
                    var angle = Mathf.Deg2Rad * Vector2.SignedAngle(Vector2.right, new Vector2(tangentDir.x, tangentDir.z));
                    ConvertToUVSpace(withControlPoints, angle);

                    var xList = new List<float>{withControlPoints[0].x, withControlPoints[1].x, withControlPoints[2].x, withControlPoints[3].x};
                    var yList = new List<float>{withControlPoints[0].z, withControlPoints[1].z, withControlPoints[2].z, withControlPoints[3].z};
                    BezierFit(xList, out float aU, out float bU, out float cU, out float dU);
                    BezierFit(yList, out float aV, out float bV, out float cV, out float dV);
                    var length = GetCubicPolyLength(xList, yList);
                    geometryArray[i] = new OpenDRIVERoadGeometry()
                    {
                        s = curS,
                        x = x,
                        y = y,
                        hdg = angle, // the angle between this lane segment and x axis (EAST)
                        length = length,
                        Items = new OpenDRIVERoadGeometryParamPoly3[1]
                        {
                            new OpenDRIVERoadGeometryParamPoly3()
                            {
                                aU = aU,
                                bU = bU,
                                cU = cU,
                                dU = dU,
                                aV = aV,
                                bV = bV,
                                cV = cV,
                                dV = dV,
                                aUSpecified = true,
                                bUSpecified = true,
                                cUSpecified = true,
                                dUSpecified = true,
                                aVSpecified = true,
                                bVSpecified = true,
                                cVSpecified = true,
                                dVSpecified = true,
                            }
                        },
                        sSpecified = true,
                        xSpecified = true,
                        ySpecified = true,
                        hdgSpecified = true,
                        lengthSpecified = true,
                    };
                    curS += length;
                }
            }

            // Add last coefficients for the last two points
            if (positions.Count >= 3)
            {
                var index = positions.Count - 2;
                sList[index] = curS;
                var point = positions[index];
                var x = point.x;
                var y = point.z;
                var vec = positions[index + 1] - positions[index];
                var length = vec.magnitude;
                var angle = Mathf.Deg2Rad * Vector2.SignedAngle(Vector2.right, new Vector2(vec.x, vec.z));
                var line = new OpenDRIVERoadGeometryLine();
                geometryArray[index] = new OpenDRIVERoadGeometry()
                {
                    s = curS,
                    x = x,
                    y = y,
                    hdg = angle, // the angle between this lane segment and x axis (EAST)
                    length = length,
                    Items = new OpenDRIVERoadGeometryLine[1]{line},
                    sSpecified = true,
                    xSpecified = true,
                    ySpecified = true,
                    hdgSpecified = true,
                    lengthSpecified = true,
                };
                curS += length;
            }
            sList[positions.Count - 1] = curS; // last s is road length

            return geometryArray;
        }

        void BezierFit(List<float> pList, out float a, out float b, out float c, out float d)
        {
            // f(t, p0, p1, p2, p3) = p0 + (t*((3*p1) - (3*p0))) +
            // ((t*t)*((3*p0) + (3*p2) - (6*p1))) + ((t*t*t)*((3*p1) - (3*p2) - p0 + p3))
            a = pList[0];
            b = -3 * pList[0] + 3 * pList[1];
            c = 3 * pList[0] - 6 * pList[1] + 3 * pList[2];
            d = -pList[0] + 3 * pList[1] - 3 * pList[2] + pList[3];
        }

        List<Vector3> GetControlPoints(List<Vector3> points, bool isFirst, bool isLast)
        {
            var offset = (points[2] - points[1]).magnitude / 4; // TODO: better logic to get offset
            var controlPoints = new List<Vector3>();
            var dir01 = (points[1] - points[0]).normalized;
            var dir12 = (points[2] - points[1]).normalized;
            var dir1 = (dir01 + dir12).normalized;
            if (isFirst) dir1 = dir01;
            var control1 = points[1] + dir1 * offset;

            var dir32 = (points[2] - points[3]).normalized;
            var dir21 = (points[1] - points[2]).normalized;
            var dir2 = (dir32 + dir21).normalized;
            if (isLast) dir2 = dir32;
            var control2 = points[2] + dir2 * offset;

            return new List<Vector3>{control1, control2};
        }

        void ConvertToUVSpace(List<Vector3> points, float angle)
        {
            var point0 = points[0];
            points[0] -= point0;
            for (int i = 1; i < points.Count; ++i)
            {
                points[i] -= point0;
                points[i] = Quaternion.Euler(0f, (float)(angle * 180f / Math.PI), 0f) * points[i];
            }
        }

        static float GetCubicPolyLength(List<float> xList, List<float> yList)
        {
            int steps = 10;
            float length = 0, preX = 0, preY = 0;
            for (int i = 0; i <= steps; ++i)
            {
                float t = i / steps;
                float curX = GetCubicBezierValue(xList, t);
                float curY = GetCubicBezierValue(yList, t);
                if (i > 0)
                {
                    float diffX = curX - preX;
                    float diffY = curY - preY;
                    length += (float)Math.Sqrt(diffX * diffX + diffY * diffY);
                }
                preX = curX;
                preY = curY;
            }

            return length;
        }

        static float GetCubicBezierValue(List<float> weights, float t)
        {
            // refer https://pomax.github.io/bezierinfo/
            var t2 = t * t;
            var t3 = t2 * t;
            var mt = 1 - t;
            var mt2 = mt * mt;
            var mt3 = mt2 * mt;
            return weights[0] * mt3 + 3 * weights[1] * mt2 * t + 3 * weights[2] * mt * t2 + weights[3] * t3;
        }

        OpenDRIVERoadLanesLaneSectionCenter CreateCenterLane(bool isOneWay, LaneData laneData)
        {
            var type = roadmarkType.solidsolid;
            if (isOneWay) type = roadmarkType.solid;

            var roadMark = new centerLaneRoadMark()
            {
                sOffset = 0,
                type1 = type,
                weight = weight.standard,
                color = color.standard,
                laneChange = laneChange.none,

                sOffsetSpecified = true,
                type1Specified = true,
                weightSpecified = true,
                colorSpecified = true,
                laneChangeSpecified = true,
            };
            var center = new OpenDRIVERoadLanesLaneSectionCenter();
            center.lane = new centerLane()
            {
                type = laneData.type,
                level = singleSide.@false,
                link = new centerLaneLink(),
                roadMark = new centerLaneRoadMark[1]{roadMark},
                idSpecified = true,
                typeSpecified = true,
                levelSpecified = true,
            };

            return center;
        }

        OpenDRIVERoadLanesLaneSectionRight CreateRightLanes(LineData refLineData, List<LaneData> neighborLaneSectionLanesData)
        {
            var right = new OpenDRIVERoadLanesLaneSectionRight();
            right.lane = new lane[neighborLaneSectionLanesData.Count];
            var rightId = -1;
            var curLeftBoundaryLineData = refLineData;

            var positions = refLineData.mapWorldPositions;
            var refLineDirection = (positions.Last() - positions.First()).normalized;
            for (int i = 0; i < neighborLaneSectionLanesData.Count; i ++)
            {
                var rightLaneData = neighborLaneSectionLanesData[i];
                LaneData2LaneId[rightLaneData] = rightId;
                var curRightBoundaryLineData = rightLaneData.rightLineData;

                var laneChangeType = laneChange.both;
                var roadMarkType = roadmarkType.broken;
                if (i == neighborLaneSectionLanesData.Count - 1)
                {
                    laneChangeType = laneChange.none;
                    roadMarkType = roadmarkType.solid;
                }

                var roadMark = new laneRoadMark()
                {
                    sOffset = 0,
                    type1 = roadMarkType,
                    weight = weight.standard,
                    color = color.standard,
                    laneChange = laneChangeType,

                    sOffsetSpecified = true,
                    type1Specified = true,
                    weightSpecified = true,
                    colorSpecified = true,
                    laneChangeSpecified = true,
                };

                var widths = CreateLaneWidths(curLeftBoundaryLineData, curRightBoundaryLineData, refLineDirection, rightLaneData);
                var lane = new lane()
                {
                    id = rightId--,
                    type = rightLaneData.type,
                    level = singleSide.@false,
                    link = new laneLink(),

                    Items = widths,
                    roadMark = new laneRoadMark[1]{roadMark},

                    idSpecified = true,
                    typeSpecified = true,
                    levelSpecified = true,
                };
                if (rightLaneData.type == laneType.sidewalk)
                {
                    lane.height = GetLaneHeight();
                }

                right.lane[i] = lane;
                curLeftBoundaryLineData = curRightBoundaryLineData;
            }

            return right;
        }

        laneHeight[] GetLaneHeight()
        {
            return new laneHeight[1]
            {
                new laneHeight()
                {
                    sOffset = 0,
                    inner = SideWalkHeight,
                    outer = SideWalkHeight,

                    sOffsetSpecified = true,
                    innerSpecified = true,
                    outerSpecified = true,
                }
            };
        }

        OpenDRIVERoadLanesLaneSectionLeft CreateLeftLanes(LineData refLineData, List<LaneData> neighborLaneSectionLanesData)
        {
            var left = new OpenDRIVERoadLanesLaneSectionLeft();
            left.lane = new lane[neighborLaneSectionLanesData.Count];
            var leftId = 1;
            var curLeftBoundaryLineData = refLineData;

            var positions = refLineData.mapWorldPositions;
            var refLineDirection = (positions.Last() - positions.First()).normalized;
            for (int i = 0; i < neighborLaneSectionLanesData.Count; i ++)
            {
                var leftLaneData = neighborLaneSectionLanesData[i];
                LaneData2LaneId[leftLaneData] = leftId;
                var curRightBoundaryLineData = leftLaneData.rightLineData;

                var laneChangeType = laneChange.both;
                var roadMarkType = roadmarkType.broken;
                if (i == neighborLaneSectionLanesData.Count - 1)
                {
                    laneChangeType = laneChange.none;
                    roadMarkType = roadmarkType.solid;
                }

                var roadMark = new laneRoadMark()
                {
                    sOffset = 0,
                    type1 = roadMarkType,
                    weight = weight.standard,
                    color = color.standard,
                    laneChange = laneChangeType,

                    sOffsetSpecified = true,
                    type1Specified = true,
                    weightSpecified = true,
                    colorSpecified = true,
                    laneChangeSpecified = true,
                };

                var widths = CreateLaneWidths(curLeftBoundaryLineData, curRightBoundaryLineData, refLineDirection, leftLaneData);
                var lane = new lane()
                {
                    id = leftId++,
                    type = leftLaneData.type,
                    level = singleSide.@false,
                    link = new laneLink(),

                    Items = widths,
                    roadMark = new laneRoadMark[1]{roadMark},

                    idSpecified = true,
                    typeSpecified = true,
                    levelSpecified = true,
                };

                if (leftLaneData.type == laneType.sidewalk)
                {
                    lane.height = GetLaneHeight();
                }
                left.lane[neighborLaneSectionLanesData.Count - 1 - i] = lane;
                curLeftBoundaryLineData = curRightBoundaryLineData;
            }

            return left;
        }

        void ReverseIfOpposite(List<Vector3> positions, Vector3 direction)
        {
            if (Vector3.Dot((positions.Last() - positions.First()), direction) < 0)
            {
                positions.Reverse();
            }
        }

        // Create width array for boundaryLine based on refLine
        laneWidth[] CreateLaneWidths(LineData refLineData, LineData boundaryLineData, Vector3 refLineDirection, LaneData laneData)
        {
            var leftPositions = refLineData.mapWorldPositions;
            ReverseIfOpposite(leftPositions, refLineDirection);
            var rightPositions = boundaryLineData.mapWorldPositions;
            ReverseIfOpposite(rightPositions, refLineDirection);

            List<Vector3> splittedLeftPoints = new List<Vector3>(), splittedRightPoints = new List<Vector3>();
            SplitLeftRightLines(leftPositions, rightPositions, ref splittedLeftPoints, ref splittedRightPoints);
            if (splittedLeftPoints.Count != splittedRightPoints.Count)
            {
                var logString = $"The boundary lines of lane {laneData.go.name} might have wrong length.";
                Debug.Log(logString, laneData.go);
                Debug.Log($"Please check boundary line {boundaryLineData.go.name}", boundaryLineData.go);
                throw new Exception("Aborting exporter.");
            }

            var widths = new float[splittedLeftPoints.Count];
            for (int i = 0; i < splittedLeftPoints.Count; i++)
            {
                widths[i] = (splittedLeftPoints[i] - splittedRightPoints[i]).magnitude;
            }

            var laneWidths = new laneWidth[widths.Length - 1];
            float curS = 0;
            var sList = new List<float>();
            sList.Add(curS);
            for (int i = 0; i < widths.Length - 1; i ++)
            {
                var length = (splittedLeftPoints[i+1] - splittedLeftPoints[i]).magnitude;
                if (length < 0.01f) continue;
                curS += length;
                sList.Add(curS);
            }

            // Add 1st coefficients for the first two points
            if (widths.Length >= 2)
            {
                var a = widths[0];
                var b = (widths[1] - widths[0]) / (sList[1] - sList[0]);
                var laneWidth = new laneWidth
                {
                    sOffset = 0,
                    a = a,
                    b = b,
                    c = 0,
                    d = 0,
                    sOffsetSpecified = true,
                    aSpecified = true,
                    bSpecified = true,
                    cSpecified = true,
                    dSpecified = true,
                };
                laneWidths[0] = laneWidth;
            }

            // Add last coefficients for the last two points
            if (widths.Length >= 3)
            {
               var a = widths[widths.Length - 2];
               var b = (widths[widths.Length - 1] - widths[widths.Length - 2]) / (sList[sList.Count - 1] - sList[sList.Count - 2]);
               var laneWidth = new laneWidth
               {
                   sOffset = sList[sList.Count - 1],
                   a = a,
                   b = b,
                   c = 0,
                   d = 0,
                   sOffsetSpecified = true,
                   aSpecified = true,
                   bSpecified = true,
                   cSpecified = true,
                   dSpecified = true,
               };
               laneWidths[laneWidths.Length - 1] = laneWidth;
            }

            if (widths.Length >= 4)
            {
                for (var i = 1; i < widths.Length - 2; i++)
                {
                    var tempSList = new List<float>(){sList[i - 1], sList[i], sList[i + 1], sList[i + 2]};
                    var tempWidthList = new List<float>(){widths[i - 1], widths[i], widths[i + 1], widths[i + 2]};
                    LagrangeCubicInterpolation(tempSList, tempWidthList, out float a, out float b, out float c, out float d);
                    var laneWidth = new laneWidth
                    {
                        sOffset = sList[i],
                        a = a,
                        b = b,
                        c = c,
                        d = d,
                        sOffsetSpecified = true,
                        aSpecified = true,
                        bSpecified = true,
                        cSpecified = true,
                        dSpecified = true,
                    };
                    laneWidths[i] = laneWidth;
                }
            }

            return laneWidths.ToArray();
        }

        void LagrangeCubicInterpolation(List<float> sList, List<float> widthList, out float a, out float b, out float c, out float d)
        {
            Debug.Assert(sList.Count == 4, "The number of sList must be 4.");
            Debug.Assert(sList.Count == widthList.Count, "The number of sList and widthList should match");
            float s0 = sList[0] - sList[1], s1 = 0, s2 = sList[2] - sList[1], s3 = sList[3] - sList[1];
            float width0 = widthList[0], width1 = widthList[1], width2 = widthList[2], width3 = widthList[3];
            var denominator0 = (s0 - s1) * (s0 - s2) * (s0 - s3);
            var denominator1 = (s1 - s0) * (s1 - s2) * (s1 - s3);
            var denominator2 = (s2 - s0) * (s2 - s1) * (s2 - s3);
            var denominator3 = (s3 - s0) * (s3 - s1) * (s3 - s2);

            a = -(s1 * s2 * s3) * width0 / denominator0 - (s0 * s2 * s3) * width1 / denominator1 - (s0 * s1 * s3) * width2 / denominator2 - (s0 * s1 * s2) * width3 / denominator3;
            b = (s1*s2 + s1*s3 + s2*s3) * width0 / denominator0 + (s0*s2 + s0*s3 + s2*s3) * width1 / denominator1;
            b += (s0*s1 + s0*s3 + s1*s3) * width2 / denominator2 + (s0*s1 + s0*s2 + s1*s2) * width3 / denominator3;
            c = -(s1 + s2 + s3) * width0 / denominator0 - (s0 + s2 + s3) * width1 / denominator1;
            c += -(s0 + s1 + s3) * width2 / denominator2 - (s0 + s1 + s2) * width3 / denominator3;
            d = width0 / denominator0 + width1 / denominator1 + width2 / denominator2 + width3 / denominator3;
        }
        void SplitLeftRightLines(List<Vector3> leftPositions, List<Vector3> rightPositions, ref List<Vector3> splittedLeftPoints, ref List<Vector3> splittedRightPoints)
        {
            float resolution = 1; // 1 meter

            float GetRangedLength(List<Vector3> positions)
            {
                float len = 0;
                for (int i = 0; i < positions.Count - 1; i++)
                {
                    len += (positions[i + 1] - positions[i]).magnitude;
                }

                return len;
            }
            // Get the length of longer boundary line
            float leftLength = GetRangedLength(leftPositions);
            float rightLength = GetRangedLength(rightPositions);
            float longerDistance = (leftLength > rightLength) ? leftLength : rightLength;
            int partitions = (int)Math.Ceiling(longerDistance / resolution);
            if (partitions < 2)
            {
                // For line whose length is less than resolution
                partitions = 2; // Make sure every line has at least 2 partitions.
            }

            float leftResolution = leftLength / partitions;
            float rightResolution = rightLength / partitions;

            SplitLine(leftPositions, ref splittedLeftPoints, leftResolution, partitions);
            SplitLine(rightPositions, ref splittedRightPoints, rightResolution, partitions);
       }

        void SplitLine(List<Vector3> positions, ref List<Vector3> splittedLinePoints, float resolution, int partitions)
        {
            splittedLinePoints = new List<Vector3>();
            splittedLinePoints.Add(positions[0]); // Add first point

            float residue = 0; // Residual length from previous segment
            int last = 0;
            // loop through each segment in boundry line
            for (int i = 1; i < positions.Count; i++)
            {
                if (splittedLinePoints.Count >= partitions) break;

                Vector3 lastPoint = positions[last];
                Vector3 curPoint = positions[i];

                // Continue if no points are made within current segment
                float segmentLength = Vector3.Distance(lastPoint, curPoint);
                if (segmentLength + residue < resolution)
                {
                    residue += segmentLength;
                    last = i;
                    continue;
                }

                Vector3 direction = (curPoint - lastPoint).normalized;
                for (float length = resolution - residue; length <= segmentLength; length += resolution)
                {
                    Vector3 partitionPoint = lastPoint + direction * length;
                    splittedLinePoints.Add(partitionPoint);
                    if (splittedLinePoints.Count >= partitions) break;
                    residue = segmentLength - length;
                }

                if (splittedLinePoints.Count >= partitions) break;
                last = i;
            }

            splittedLinePoints.Add(positions[positions.Count - 1]);
        }

        HashSet<LaneData> FindStartingLanesData()
        {
            var allLanes = new HashSet<LaneData>();
            var visitedLanes = new HashSet<LaneData>();
            var startingLanes = new HashSet<LaneData>();
            var stack = new Stack<LaneData>(); // lanes to start dfs
            foreach (var laneSegment in LanesData)
            {
                if (laneSegment.befores.Count == 0
                    || laneSegment.type == laneType.sidewalk)
                {
                    startingLanes.Add(laneSegment);
                    stack.Push(laneSegment);
                }
                allLanes.Add(laneSegment);
            }

            while (stack.Count > 0)
            {
                var lane = stack.Pop();
                if (visitedLanes.Contains(lane))
                {
                    continue;
                }

                for (var i = 0; i < lane.afters.Count; i++)
                {
                    var afterLane = lane.afters[i];
                    if (!visitedLanes.Contains(afterLane))
                    {
                        stack.Push(afterLane);
                    }
                }

                visitedLanes.Add(lane);
            }

            foreach (var lane in allLanes)
            {
                if (!visitedLanes.Contains(lane))
                {
                    startingLanes.Add(lane);
                }
            }

            if (startingLanes.Count == 0)
            {
                Debug.LogError("Error, no startingLanes found!");
            }

            return startingLanes;
        }

        public void Export(string filePath)
        {
            if (Calculate())
            {
                var serializer = new XmlSerializer(typeof(OpenDRIVE));

                using (var writer = new StreamWriter(filePath))
                using (var xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings { Indent = true, IndentChars = "    " }))
                {
                    serializer.Serialize(xmlWriter, Map);
                }

                Debug.Log("Successfully generated and exported OpenDRIVE Map! If your map looks weird at some roads, you might have wrong boundary lines for some lanes.");
            }
            else
            {
                Debug.LogError("Failed to export OpenDRIVE Map!");
            }
        }

        static Vector2 ToVector2(Vector3 pt)
        {
            return new Vector2(pt.x, pt.z);
        }

        static Vector3 ToVector3(Vector2 p)
        {
            return new Vector3(p.x, 0f, p.y);
        }
    }

    public static class Helper1
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