/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */
 
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
    public class OpenDriveMapExporter
    {
        MapOrigin MapOrigin;
        MapManagerData MapAnnotationData;
        OpenDRIVE Map;
        HashSet<MapLane> LaneSegments;
        HashSet<MapLine> LineSegments;
        List<MapIntersection> Intersections;
        Dictionary<MapLane, uint> Lane2RoadId = new Dictionary<MapLane, uint>();
        Dictionary<uint, List<MapLane>> RoadId2Lanes = new Dictionary<uint, List<MapLane>>();
        Dictionary<MapLane, int> Lane2LaneId = new Dictionary<MapLane, int>(); // lane to its laneId inside OpenDRIVE road
        Dictionary<MapLane, uint> Lane2JunctionId = new Dictionary<MapLane, uint>();
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
            Intersections = MapAnnotationData.GetIntersections();
            MapAnnotationData.GetTrafficLanes();
            
            // Initial collection 
            LaneSegments = new HashSet<MapLane>(MapAnnotationData.GetData<MapLane>());
            LineSegments = new HashSet<MapLine>(MapAnnotationData.GetData<MapLine>());
            var signalLights = new List<MapSignal>(MapAnnotationData.GetData<MapSignal>());
            var crossWalkList = new List<MapCrossWalk>(MapAnnotationData.GetData<MapCrossWalk>());
            var mapSignList = new List<MapSign>(MapAnnotationData.GetData<MapSign>());
            var stopLineLanes = new Dictionary<MapLine, List<MapLane>>();

            foreach (var mapSign in mapSignList)
            {
                if (mapSign.signType == MapData.SignType.STOP && mapSign.stopLine != null)
                {
                    mapSign.stopLine.stopSign = mapSign;
                }
            }

            foreach (var laneSegment in LaneSegments)
            {
                if (laneSegment.stopLine != null)
                {
                    stopLineLanes.GetOrCreate(laneSegment.stopLine).Add(laneSegment);
                }
            }

            LinkSegments(LaneSegments);
            LinkSegments(LineSegments);

            CheckNeighborLanes(LaneSegments);
            CheckBoundaries(LaneSegments);

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

            // Check boundary line for each lane, create fake ones if necessary
            var lanelet2MapExporter = new Lanelet2MapExporter();
            if (!Lanelet2MapExporter.ExistsLaneWithBoundaries(LaneSegments))
            {
                Debug.LogWarning("There are no boundaries. Creating fake boundaries.");

                // Create fake boundary lines
                lanelet2MapExporter.CreateFakeBoundariesFromLanes(LaneSegments);

                lanelet2MapExporter.AlignPointsInLines(LaneSegments);
            }

            ComputeRoads();

            return true;
        }

        void CheckBoundaries(HashSet<MapLane> lanes)
        {
            foreach (var lane in lanes)
            {
                if (lane.leftLineBoundry == null || lane.rightLineBoundry == null)
                {
                    Debug.LogWarning($"Lane {lane.name} instance id: {lane.gameObject.GetInstanceID()} is missing boundary lines, please check.");
#if UNITY_EDITOR
                    UnityEditor.Selection.activeObject = lane.gameObject;
                    Debug.Log("Please fix the selected lane.");
#endif
                }
            }
        }

        // Link before and after lanes/lines
        bool LinkSegments<T>(HashSet<T> segments) where T : MapDataPoints, IMapLaneLineCommon<T>
        {
            foreach (var segment in segments)
            {
                // clear
                segment.befores.Clear();
                segment.afters.Clear();

                if (typeof(T) == typeof(MapLine))
                    if ((segment as MapLine).lineType == MapData.LineType.STOP) continue;

                // Each segment must have at least 2 waypoints for calculation, otherwise exit
                while (segment.mapLocalPositions.Count < 2)
                {
                    Debug.LogError("Some segment has less than 2 waypoints. Cancelling map generation.");
                    return false;
                }

                // Link lanes/lines
                var firstPt = segment.transform.TransformPoint(segment.mapLocalPositions[0]);
                var lastPt = segment.transform.TransformPoint(segment.mapLocalPositions[segment.mapLocalPositions.Count - 1]);

                foreach (var segmentCmp in segments)
                {
                    if (segment == segmentCmp)
                    {
                        continue;
                    }
                    if (typeof(T) == typeof(MapLine))
                        if ((segmentCmp as MapLine).lineType == MapData.LineType.STOP) continue;

                    var firstPt_cmp = segmentCmp.transform.TransformPoint(segmentCmp.mapLocalPositions[0]);
                    var lastPt_cmp = segmentCmp.transform.TransformPoint(segmentCmp.mapLocalPositions[segmentCmp.mapLocalPositions.Count - 1]);

                    if ((firstPt - lastPt_cmp).magnitude < MapAnnotationTool.PROXIMITY / MapAnnotationTool.EXPORT_SCALE_FACTOR)
                    {
                        segmentCmp.mapLocalPositions[segmentCmp.mapLocalPositions.Count - 1] = segmentCmp.transform.InverseTransformPoint(firstPt);
                        segmentCmp.mapWorldPositions[segmentCmp.mapWorldPositions.Count - 1] = firstPt;
                        segment.befores.Add(segmentCmp);
                    }

                    if ((lastPt - firstPt_cmp).magnitude < MapAnnotationTool.PROXIMITY / MapAnnotationTool.EXPORT_SCALE_FACTOR)
                    {
                        segmentCmp.mapLocalPositions[0] = segmentCmp.transform.InverseTransformPoint(lastPt);
                        segmentCmp.mapWorldPositions[0] = lastPt;
                        segment.afters.Add(segmentCmp);
                    }
                }
            }

            return true;
        }

        bool CheckNeighborLanes(HashSet<MapLane> LaneSegments)
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

        // Return MapLine from neighborLaneSectionLanes and return correct order of positions for reference line
        MapLine GetRefLineAndPositions(List<MapLane> neighborLaneSectionLanes, out List<Vector3> positions)
        {
            // Lanes are stored from left most to right most
            // Reference line is the left boundary line of the left most lane
            var refLine = neighborLaneSectionLanes[0].leftLineBoundry;
            if (refLine == null)
            {
                Debug.LogError($"Lane: {neighborLaneSectionLanes[0].gameObject.name} has no left boundary line, its object instance ID is {neighborLaneSectionLanes[0].gameObject.GetInstanceID()}");
            }

            positions = refLine.mapWorldPositions;
            // Make sure refLine positions has same direction as the road lanes
            var leftLanePositions = neighborLaneSectionLanes[0].mapWorldPositions; // pick any lane
            if (Vector3.Dot((positions.Last() - positions.First()), (leftLanePositions.Last() - leftLanePositions.First())) < 0)
            {
                positions.Reverse();
            }

            return refLine;
        }

        // Function to get neighbor lanes in the same road
        List<MapLane> GetNeighborForwardLaneSectionLanes(MapLane self, bool fromLeft)
        {
            if (self == null)
            {
                return new List<MapLane>();
            }
            
            if (fromLeft)
            {
                if (self.leftLaneForward == null)
                {
                    return new List<MapLane>();
                }
                else
                {
                    var ret = new List<MapLane>();
                    ret.AddRange(GetNeighborForwardLaneSectionLanes(self.leftLaneForward, true));
                    ret.Add(self.leftLaneForward);
                    return ret;
                }
            }
            else
            {
                if (self.rightLaneForward == null)
                {
                    return new List<MapLane>();
                }
                else
                {
                    var ret = new List<MapLane>();
                    ret.Add(self.rightLaneForward);
                    ret.AddRange(GetNeighborForwardLaneSectionLanes(self.rightLaneForward, false));
                    return ret;
                }
            }
        }


        void ComputeRoads()
        {
            var visitedLanes = new HashSet<MapLane>();
            var neighborForwardLaneSectionLanes = new List<List<MapLane>>();
            foreach (var laneSegment in LaneSegments)
            {
                if (visitedLanes.Contains(laneSegment))
                {
                    continue;
                }

                var lefts = GetNeighborForwardLaneSectionLanes(laneSegment, true);  // Left forward lanes from furthest to nearest
                var rights = GetNeighborForwardLaneSectionLanes(laneSegment, false);  // Right forward lanes from nearest to furthest

                var laneSectionLanes = new List<MapLane>();
                laneSectionLanes.AddRange(lefts);
                laneSectionLanes.Add(laneSegment);
                laneSectionLanes.AddRange(rights);
                
                foreach (var l in laneSectionLanes)
                {
                    if (!visitedLanes.Contains(l))
                    {
                        visitedLanes.Add(l);
                    }
                }
                neighborForwardLaneSectionLanes.Add(laneSectionLanes);
            }
            // Debug.Log($"We got {neighborForwardLaneSectionLanes.Count} laneSections, {LaneSegments.Count} lanes");

            var neighborLaneSectionLanesIdx = new List<List<int>>(); // Final laneSections idx after grouping
            visitedLanes.Clear();
            var lane2LaneSectionIdx = new Dictionary<MapLane, int>(); // MapLane to the index of laneSection in neighborLaneSectionLanes
            
            void AddToLane2Index(List<MapLane> laneSectionLanes, int index)
            {
                foreach (var lane in laneSectionLanes)
                {
                    lane2LaneSectionIdx[lane] = index;
                }
            }

            for (var i = 0; i < neighborForwardLaneSectionLanes.Count; i ++)
            {
                var laneSectionLanes1 = neighborForwardLaneSectionLanes[i];
                var leftMostLane1 = laneSectionLanes1.First();
                var rightMostLane1 = laneSectionLanes1.Last();

                if (visitedLanes.Contains(leftMostLane1) || visitedLanes.Contains(rightMostLane1)) continue;

                visitedLanes.Add(leftMostLane1);
                visitedLanes.Add(rightMostLane1);
                var found = false;
                // Find the neighbor laneSectionLanes
                for (var j = i+1; j < neighborForwardLaneSectionLanes.Count; j ++)
                {
                    var laneSectionLanes2 = neighborForwardLaneSectionLanes[j];
                    var leftMostLane2 = laneSectionLanes2.First();
                    var rightMostLane2 = laneSectionLanes2.Last();

                    if (leftMostLane1 == leftMostLane2 && rightMostLane1 == rightMostLane2) continue;
                    if (visitedLanes.Contains(leftMostLane2) || visitedLanes.Contains(rightMostLane2)) continue;
                    
                    // Consider both left-hand/right-hand traffic
                    var leftHandTraffic = (leftMostLane1.leftLineBoundry == leftMostLane2.leftLineBoundry);
                    var rightHandTraffic = (rightMostLane1.rightLineBoundry == rightMostLane2.rightLineBoundry); 
                    if (leftHandTraffic || rightHandTraffic)
                    {
                        visitedLanes.Add(leftMostLane2);
                        visitedLanes.Add(rightMostLane2);
                        neighborLaneSectionLanesIdx.Add(new List<int>() {i, j});
                        
                        AddToLane2Index(laneSectionLanes1, neighborLaneSectionLanesIdx.Count-1);
                        AddToLane2Index(laneSectionLanes2, neighborLaneSectionLanesIdx.Count-1);
                        
                        found = true;
                    }
                }

                if (!found)
                {
                    // If the neighbor laneSectionLane not found, it is a one way road
                    neighborLaneSectionLanesIdx.Add(new List<int>() {i});
                    AddToLane2Index(laneSectionLanes1, neighborLaneSectionLanesIdx.Count-1);
                }
            }

            // Debug.Log($"We got {neighborLaneSectionLanesIdx.Count} laneSections");

            // Find out starting lanes and start from them and go through all laneSections
            var startingLanes = FindStartingLanes();
            var visitedNLSLIdx = new HashSet<int>(); // visited indices of NLSL(neighborLaneSectionLanesIdx) list
            
            Vector3 GetDirection(MapLane mapLane)
            {
                var positions = mapLane.mapWorldPositions;
                return positions.Last() - positions.First();
            }
            
            Roads = new OpenDRIVERoad[neighborLaneSectionLanesIdx.Count];
            uint roadId = 0;
            foreach (var startingLane in startingLanes)
            {
                // BFS until the lane has 0 afters
                var startingLaneNLSLIdx = lane2LaneSectionIdx[startingLane];
                if (visitedNLSLIdx.Contains(startingLaneNLSLIdx)) continue;
                
                var queue = new Queue<MapLane>();
                queue.Enqueue(startingLane);

                while (queue.Any())
                {
                    var curLane = queue.Dequeue();
                    var curNLSLIdx = lane2LaneSectionIdx[curLane];
                    if (visitedNLSLIdx.Contains(curNLSLIdx)) continue;
                    // Make a road for the laneSection curLane is in
                    // Reference line should have the same direction with curLane
                    var road = new OpenDRIVERoad()
                    {
                        name = "", 
                        id = roadId.ToString(),
                    };
                    
                    List<MapLane> consideredLanes = new List<MapLane>();
                    MapLine refLine;
                    List<Vector3> refLinePositions;
                    // One Way lane section
                    if (neighborLaneSectionLanesIdx[curNLSLIdx].Count == 1)
                    {
                        var neighborLaneSectionLanes = neighborForwardLaneSectionLanes[neighborLaneSectionLanesIdx[curNLSLIdx][0]];

                        refLine = GetRefLineAndPositions(neighborLaneSectionLanes, out refLinePositions);

                        AddPlanViewElevationLateral(refLinePositions, road);
                        AddLanes(road, refLine, neighborLaneSectionLanes, neighborLaneSectionLanes);
                        
                        visitedNLSLIdx.Add(curNLSLIdx);
                        consideredLanes = neighborLaneSectionLanes;
                    }
                    // Two Way lane section
                    else
                    {
                        var neighborLaneSectionLanes1 = neighborForwardLaneSectionLanes[neighborLaneSectionLanesIdx[curNLSLIdx][0]];
                        var neighborLaneSectionLanes2 = neighborForwardLaneSectionLanes[neighborLaneSectionLanesIdx[curNLSLIdx][1]];
                        
                        // Check curLane direction, reference line should have same direction with it
                        var direction1 = GetDirection(neighborLaneSectionLanes1[0]);
                        var curDirection = GetDirection(curLane);

                        List<MapLane> leftNeighborLaneSectionLanes, rightNeighborLaneSectionLanes;
                        if (Vector3.Dot(direction1, curDirection) > 0)
                        {
                            refLine = GetRefLineAndPositions(neighborLaneSectionLanes1, out refLinePositions);
                            rightNeighborLaneSectionLanes = neighborLaneSectionLanes1;
                            leftNeighborLaneSectionLanes = neighborLaneSectionLanes2;
                        }
                        else
                        {
                            refLine = GetRefLineAndPositions(neighborLaneSectionLanes2, out refLinePositions);
                            rightNeighborLaneSectionLanes = neighborLaneSectionLanes2;
                            leftNeighborLaneSectionLanes = neighborLaneSectionLanes1;
                        }
                        
                        AddPlanViewElevationLateral(refLinePositions, road);
                        AddLanes(road, refLine, leftNeighborLaneSectionLanes, rightNeighborLaneSectionLanes, false);
                        
                        visitedNLSLIdx.Add(lane2LaneSectionIdx[neighborLaneSectionLanes1[0]]);
                        visitedNLSLIdx.Add(lane2LaneSectionIdx[neighborLaneSectionLanes2[0]]);
                        consideredLanes.AddRange(neighborLaneSectionLanes1);
                        consideredLanes.AddRange(neighborLaneSectionLanes2);
                    }
                    
                    GetLaneSectionLanesAfterLanes(queue, consideredLanes, visitedNLSLIdx, lane2LaneSectionIdx);
                    Add2Lane2RoadId(roadId, consideredLanes);
                    
                    // Add speed limit
                    var maxSpeedMPH = consideredLanes[0].speedLimit * 2.23694;
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
                    // Debug.Log(consideredLanes[0].gameObject.GetInstanceID() + "    road Id " + roadId + "   lane Id " + Lane2LaneId[consideredLanes[0]], consideredLanes[0].gameObject);
                    roadId += 1;
                } 
            }

            // Create controller for pairs of signals in each intersection
            var controllers = new List<OpenDRIVEController>();
 
            UniqueId = roadId;
            AddJunctions(UniqueId, controllers);

            foreach (var NLSLIdxList in neighborLaneSectionLanesIdx)
            {
                var roadBeforeLanes = new HashSet<MapLane>(); // lanes before current road
                var roadAfterLanes = new HashSet<MapLane>();

                // One way road 
                if (NLSLIdxList.Count == 1)
                {
                    UpdateLaneLink(roadBeforeLanes, roadAfterLanes, neighborForwardLaneSectionLanes[NLSLIdxList[0]]);
                }
                // Two way Roads
                else
                {
                    UpdateLaneLink(roadBeforeLanes, roadAfterLanes, neighborForwardLaneSectionLanes[NLSLIdxList[0]]);
                    UpdateLaneLink(roadBeforeLanes, roadAfterLanes, neighborForwardLaneSectionLanes[NLSLIdxList[1]]);
                }

                var curRoadId = Lane2RoadId[neighborForwardLaneSectionLanes[NLSLIdxList[0]][0]];
                UpdateRoadLink(roadBeforeLanes, roadAfterLanes, curRoadId);
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
                var allBeforeAndAfterLanes = new List<MapLane>();
                foreach (var lane in intersectionLanes)
                {
                    Lane2JunctionId[lane] = junctionId;

                    var incomingLanes = lane.befores;
                    var beforeAfterLanes = new List<MapLane>(incomingLanes);
                    beforeAfterLanes.AddRange(lane.afters);
                    allBeforeAndAfterLanes.AddRange(beforeAfterLanes);

                    UpdateConnections2LaneLink(connections2LaneLink, lane, incomingLanes);

                    // Update corresponding road header's junctionId
                    roadId = Lane2RoadId[lane];
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
                foreach (var lane in allBeforeAndAfterLanes)
                {
                    var roadIdOfLane = Lane2RoadId[lane];
                    var stopLine = lane.stopLine;
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
                        var orien = GetOrientation(RoadId2RefLinePositions[roadIdStopLine], mapSignal.transform.forward);
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

        void UpdateConnections2LaneLink(Dictionary<Tuple<uint, uint, contactPoint>, List<OpenDRIVEJunctionConnectionLaneLink>> connections2LaneLink, MapLane lane, List<MapLane> incomingLanes)
        {
            var connectingRoadId = Lane2RoadId[lane];
            var connectingLaneId = Lane2LaneId[lane];
            // Check whether lane has same direction with its road
            if (connectingLaneId > 0)
            {
                incomingLanes = lane.afters;
            }

            foreach (var incomingLane in incomingLanes)
            {
                var incomingRoadId = Lane2RoadId[incomingLane];
                var key = Tuple.Create(incomingRoadId, connectingRoadId, GetContactPoint(incomingRoadId, lane));
                if (connections2LaneLink.ContainsKey(key))
                {
                    connections2LaneLink[key].Add(
                        new OpenDRIVEJunctionConnectionLaneLink()
                        {
                            from = Lane2LaneId[incomingLane],
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
                                from = Lane2LaneId[incomingLane],
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

        orientation GetOrientation(List<Vector3> refLinePositions, Vector3 SignalSignDir)
        {
            var refLineDir = (refLinePositions.Last() - refLinePositions.First());
            var orien = orientation.Item;
            if (Vector3.Dot(SignalSignDir, refLineDir) < 0)
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
            var orien = GetOrientation(positions, mapSignal.transform.forward);
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
            var orien = GetOrientation(positions, mapSign.transform.forward);
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

        void UpdateRoadLink(HashSet<MapLane> roadBeforeLanes, HashSet<MapLane> roadAfterLanes, uint curRoadId)
        {
            // note: before == predecessor, after == successor
            var preRoadIds = new HashSet<uint>();
            MapLane curBeforeLane = null, curAfterLane = null; 
            foreach (var beforeLane in roadBeforeLanes)
            {
                curBeforeLane = beforeLane;
                preRoadIds.Add(Lane2RoadId[beforeLane]);
            }
            var sucRoadIds = new HashSet<uint>();
            foreach (var afterLane in roadAfterLanes)
            {
                curAfterLane = afterLane;
                sucRoadIds.Add(Lane2RoadId[afterLane]);
            }

            uint? preRoadId = null;
            uint? sucRoadId = null;
            var roadPredecessor = new OpenDRIVERoadLinkPredecessor();
            var roadSuccessor = new OpenDRIVERoadLinkSuccessor();

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
                    preRoadId = Lane2RoadId[curBeforeLane];

                    roadPredecessor = new OpenDRIVERoadLinkPredecessor()
                    {
                        elementType = elementType.road,
                        elementId = preRoadId.ToString(),
                        contactPoint = GetContactPoint(curRoadId, curBeforeLane),
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
                    sucRoadId = Lane2RoadId[curAfterLane];

                    roadSuccessor = new OpenDRIVERoadLinkSuccessor()
                    {
                        elementType = elementType.road,
                        elementId = sucRoadId.ToString(),
                        contactPoint = GetContactPoint(curRoadId, curAfterLane),
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
            
            foreach (var mapLane in RoadId2Lanes[roadId])
            {
                if (isPredecessor)  
                {
                    if ((Lane2LaneId[mapLane] < 0 && mapLane.befores.Count > 1) || (Lane2LaneId[mapLane] > 0 && mapLane.afters.Count > 1))
                    {
                        hasMoreThanOneLinkedLanes = true;
                        break;
                    }
                }
                else
                {
                    if ((Lane2LaneId[mapLane] < 0 && mapLane.afters.Count > 1) || (Lane2LaneId[mapLane] > 0 && mapLane.befores.Count > 1))
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

        // Given current road and any lane of predecessor/successor road, get contactPoint of the predecessor/successor road
        contactPoint GetContactPoint(uint curRoadId, MapLane linkRoadLane)
        {
            var roadStartPoint = new Vector3((float)Roads[curRoadId].planView[0].x, (float)Roads[curRoadId].elevationProfile.elevation[0].a, (float)Roads[curRoadId].planView[0].y);
            var roadLastPoint = new Vector3((float)Roads[curRoadId].planView.Last().x, (float)Roads[curRoadId].elevationProfile.elevation.Last().a, (float)Roads[curRoadId].planView.Last().y);
            var positions = linkRoadLane.mapWorldPositions;
            var linkedRoadLaneStartPoint = positions.First();
            var linkedRoadLaneEndPoint = positions.Last();
            if (Lane2LaneId[linkRoadLane] > 0)
            {
                // if the lane is a left lane in the road, reference line is opposite with the lane
                linkedRoadLaneStartPoint = positions.Last();
                linkedRoadLaneEndPoint = positions.First();
            }
            if ((roadStartPoint - linkedRoadLaneStartPoint).magnitude > (roadStartPoint - linkedRoadLaneEndPoint).magnitude
                && (roadLastPoint - linkedRoadLaneStartPoint).magnitude > (roadLastPoint - linkedRoadLaneEndPoint).magnitude)
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
                    Debug.LogWarning(msg, RoadId2Lanes[roadId][0].gameObject);
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

            var junctionLanes = new List<MapLane>();
            foreach (var roadId in roadIds)
            {
                junctionLanes.AddRange(RoadId2Lanes[roadId]);
            }

            var connections2LaneLink = new Dictionary<Tuple<uint, uint, contactPoint>, List<OpenDRIVEJunctionConnectionLaneLink>>();
            foreach (var lane in junctionLanes)
            {
                Lane2JunctionId[lane] = junctionId;
                var incomingLanes = lane.befores;
                UpdateConnections2LaneLink(connections2LaneLink, lane, incomingLanes);

                // Update corresponding road header's junctionId
                uint roadId = Lane2RoadId[lane];
                Roads[roadId].junction = junctionId.ToString();
            }

            junction.connection = CreateConnections(connections2LaneLink);
            Junctions.Add(junction);
            return junctionId.ToString();
        }

        void UpdateLaneLink(HashSet<MapLane> roadBeforeLanes, HashSet<MapLane> roadAfterLanes, List<MapLane> lanes)
        {
            var isLeft = false;
            if (Lane2LaneId[lanes[0]] > 0) isLeft = true; // left lanes always have positive Ids
            
            foreach (var lane in lanes)
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
                    preLaneId = Lane2LaneId[befores[0]];
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
                    sucLaneId = Lane2LaneId[afters[0]];
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
                    var roadId = Lane2RoadId[lane];
                    var laneId = Lane2LaneId[lane];
                    var laneIdx = Mathf.Abs(laneId) - 1; // right lanes Id: -1, -2, -3, ...
                    if (isLeft)
                    {
                        laneIdx = lanes.Count - laneId; // left lanes Id: ..., 4, 3, 2, 1
                        Roads[roadId].lanes.laneSection[0].left.lane[laneIdx].link = laneLink; 
                    }
                    else
                    {
                        Roads[roadId].lanes.laneSection[0].right.lane[laneIdx].link = laneLink; 
                    }
                }
                roadBeforeLanes.UnionWith(befores);
                roadAfterLanes.UnionWith(afters);
            }
        }

        void Add2Lane2RoadId(uint roadId, List<MapLane> consideredLanes)
        {
            foreach (var lane in consideredLanes)
            {
               Lane2RoadId[lane] = roadId;
               if (RoadId2Lanes.ContainsKey(roadId)) RoadId2Lanes[roadId].Add(lane);
               else RoadId2Lanes[roadId] = new List<MapLane>(){lane};
            }
        }

        void GetLaneSectionLanesAfterLanes(Queue<MapLane> queue, List<MapLane> lanes, HashSet<int> visitedNLSLIdx, Dictionary<MapLane, int> lane2LaneSectionIdx)
        {
            foreach (var curLane in lanes)
            {
                foreach (var lane in curLane.afters)
                {
                    if (!visitedNLSLIdx.Contains(lane2LaneSectionIdx[lane]))
                    {
                        queue.Enqueue(lane);
                    }
                }
            }
        }

        void AddLanes(OpenDRIVERoad road, MapLine refLine, List<MapLane> leftNeighborLaneSectionLanes, List<MapLane> rightNeighborLaneSectionLanes, bool isOneWay=true)
        {
            var lanes = new OpenDRIVERoadLanes();

            // TODO Add laneOffset for complex urban Roads
            // Add laneSection

            var center = CreateCenterLane(true);
            var right = CreateRightLanes(refLine, rightNeighborLaneSectionLanes);
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
                var left = CreateLeftLanes(refLine, leftNeighborLaneSectionLanes);
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

        void AddPlanViewElevationLateral(List<Vector3> positions, OpenDRIVERoad road)
        {
            OpenDRIVERoadGeometry[] geometryArray;
            OpenDRIVERoadElevationProfileElevation[] elevationProfileArray;
            double roadLength;
            UpdateGeometryArrayElevationProfileArray(positions, out geometryArray, out elevationProfileArray, out roadLength);
            
            road.length = roadLength;
            road.lengthSpecified = true;
            road.planView = geometryArray;
            road.elevationProfile = new OpenDRIVERoadElevationProfile()
            {
                elevation = elevationProfileArray,
            };
            road.lateralProfile = new OpenDRIVERoadLateralProfile();
        }

        void UpdateGeometryArrayElevationProfileArray(List<Vector3> positions, out OpenDRIVERoadGeometry[] geometryArray, out OpenDRIVERoadElevationProfileElevation[] elevationProfileArray, out double roadLength)
        {
            geometryArray = new OpenDRIVERoadGeometry[positions.Count - 1];
            elevationProfileArray = new OpenDRIVERoadElevationProfileElevation[positions.Count - 1];
            double curS = 0;
            for (int i = 0; i < positions.Count - 1; i ++)
            {
                var point = positions[i];
                var location = MapOrigin.GetGpsLocation(point);
                var x = point.x; 
                var y = point.z;
                var vec = positions[i+1] - positions[i];
                var length = vec.magnitude;
                var angle = Mathf.Deg2Rad * Vector2.SignedAngle(Vector2.right, new Vector2(vec.x, vec.z)); // TODO Validate
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
                
                var a = positions[i].y;
                var b = (positions[i+1].y - positions[i].y) / length; // For line type
                elevationProfileArray[i] = new OpenDRIVERoadElevationProfileElevation()
                {
                    s = curS,
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

                curS += length;
            }
            roadLength = curS;
        }

        OpenDRIVERoadLanesLaneSectionCenter CreateCenterLane(bool isOneWay)
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
                type = laneType.driving,
                level = singleSide.@false,
                link = new centerLaneLink(),
                roadMark = new centerLaneRoadMark[1]{roadMark},
                idSpecified = true,
                typeSpecified = true,
                levelSpecified = true,
            };
            
            return center;
        }

        OpenDRIVERoadLanesLaneSectionRight CreateRightLanes(MapLine refLine, List<MapLane> neighborLaneSectionLanes)
        {
            var right = new OpenDRIVERoadLanesLaneSectionRight();
            right.lane = new lane[neighborLaneSectionLanes.Count];
            var rightId = -1;
            var curLeftBoundaryLine = refLine;

            var positions = refLine.mapWorldPositions;
            var refLineDirection = (positions.Last() - positions.First()).normalized;
            for (int i = 0; i < neighborLaneSectionLanes.Count; i ++)
            {
                var rightLane = neighborLaneSectionLanes[i];
                Lane2LaneId[rightLane] = rightId;
                var curRightBoundaryLine = rightLane.rightLineBoundry;
                
                var laneChangeType = laneChange.both;
                var roadMarkType = roadmarkType.broken;
                if (i == neighborLaneSectionLanes.Count - 1)
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

                var widths = CreateLaneWidths(curLeftBoundaryLine, curRightBoundaryLine, refLineDirection, rightLane);
                var lane = new lane()
                {
                    id = rightId--,
                    type = laneType.driving,
                    level = singleSide.@false,
                    link = new laneLink(),

                    Items = widths,
                    roadMark = new laneRoadMark[1]{roadMark},

                    idSpecified = true,
                    typeSpecified = true,
                    levelSpecified = true,
                };

                right.lane[i] = lane;
                curLeftBoundaryLine = curRightBoundaryLine;
            }

            return right;
        }

        OpenDRIVERoadLanesLaneSectionLeft CreateLeftLanes(MapLine refLine, List<MapLane> neighborLaneSectionLanes)
        {
            var left = new OpenDRIVERoadLanesLaneSectionLeft();
            left.lane = new lane[neighborLaneSectionLanes.Count];
            var leftId = 1;
            var curLeftBoundaryLine = refLine;
            
            var positions = refLine.mapWorldPositions;
            var refLineDirection = (positions.Last() - positions.First()).normalized;
            for (int i = 0; i < neighborLaneSectionLanes.Count; i ++)
            {
                var leftLane = neighborLaneSectionLanes[i];
                Lane2LaneId[leftLane] = leftId;
                var curRightBoundaryLine = leftLane.rightLineBoundry;
                
                var laneChangeType = laneChange.both;
                var roadMarkType = roadmarkType.broken;
                if (i == neighborLaneSectionLanes.Count - 1)
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

                var widths = CreateLaneWidths(curLeftBoundaryLine, curRightBoundaryLine, refLineDirection, leftLane);
                var lane = new lane()
                {
                    id = leftId++,
                    type = laneType.driving,
                    level = singleSide.@false,
                    link = new laneLink(),

                    Items = widths,
                    roadMark = new laneRoadMark[1]{roadMark},

                    idSpecified = true,
                    typeSpecified = true,
                    levelSpecified = true,
                };

                left.lane[neighborLaneSectionLanes.Count - 1 - i] = lane;
                curLeftBoundaryLine = curRightBoundaryLine;
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
        laneWidth[] CreateLaneWidths(MapLine refLine, MapLine boundaryLine, Vector3 refLineDirection, MapLane lane)
        {
            var leftPositions = refLine.mapWorldPositions;
            ReverseIfOpposite(leftPositions, refLineDirection);
            var rightPositions = boundaryLine.mapWorldPositions;
            ReverseIfOpposite(rightPositions, refLineDirection);

            List<Vector3> splittedLeftPoints = new List<Vector3>(), splittedRightPoints = new List<Vector3>();
            SplitLeftRightLines(leftPositions, rightPositions, ref splittedLeftPoints, ref splittedRightPoints);
            if (splittedLeftPoints.Count != splittedRightPoints.Count)
            {
                var logString = $"The boundary lines of lane {lane.name} might have wrong length.";
                Debug.Log(logString, lane.gameObject);
                Debug.Log($"Please check boundary line {boundaryLine.name}", boundaryLine.gameObject);
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
                for (float length = resolution - residue; length < segmentLength; length += resolution)
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

        HashSet<MapLane> FindStartingLanes()
        {
            var allLanes = new HashSet<MapLane>();
            var visitedLanes = new HashSet<MapLane>();
            var startingLanes = new HashSet<MapLane>();
            var stack = new Stack<MapLane>(); // lanes to start dfs
            foreach (var laneSegment in LaneSegments)
            {
                if (laneSegment.befores.Count == 0)
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