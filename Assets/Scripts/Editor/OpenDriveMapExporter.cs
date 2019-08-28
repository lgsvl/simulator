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
using Utility = Simulator.Utilities.Utility;


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
        Dictionary<MapLane, int> Lane2LaneId = new Dictionary<MapLane, int>(); // lane to its laneId inside OpenDRIVE road
        Dictionary<MapLane, uint> Lane2JunctionId = new Dictionary<MapLane, uint>();
        Dictionary<uint, List<Vector3>> RoadId2RefLinePositions = new Dictionary<uint, List<Vector3>>(); // roadId to corresponding reference MapLine positions with correct order
        uint UniqueId;
        OpenDRIVERoad[] Roads;
        public void ExportOpenDRIVEMap(string filePath)
        { 
            if (Calculate())
            {
                Export(filePath);
                Debug.Log("Successfully generated and exported OpenDRIVE Map!");
            }
            else
            {
                Debug.LogError("Failed to export OpenDRIVE Map!");
            }
        }

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
            
            // Initial collection // TODO validate are all of them used?
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


            Map = new OpenDRIVE()
            {
                header = new OpenDRIVEHeader()
                {
                    revMajor = (ushort)1,
                    revMinor = (ushort)4,
                    name = "",
                    version = 1.00f,
                    date = System.DateTime.Now.ToString("ddd, MMM dd HH':'mm':'ss yyy"),
                    vendor = "LGSVL",
                    // TODO geoReference?
                    // geoReference = "+proj=utm +zone=10 +ellps=WGS84 +datum=WGS84 +units=m +no_defs"
                }
            };

            ComputeRoads();

            return true;
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

        void ComputeRoads()
        {
            // Function to get neighbor lanes in the same road
            System.Func<MapLane, bool, List<MapLane>> GetNeighborForwardLaneSectionLanes = null;
            GetNeighborForwardLaneSectionLanes = delegate (MapLane self, bool fromLeft)
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
            };

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
            Debug.Log($"We got {neighborForwardLaneSectionLanes.Count} laneSections, {LaneSegments.Count} lanes");

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
                        Debug.Log($"{leftMostLane1.name} == {leftMostLane2.name}");
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

            Debug.Log($"We got {neighborLaneSectionLanesIdx.Count} laneSections");

            // Find out starting lanes and start from them and go through all laneSections
            var startingLanes = FindStartingLanes();
            var visitedNLSLIdx = new HashSet<int>(); // visited indices of NLSL(neighborLaneSectionLanesIdx) list
            
            // Return MapLine from neighborLaneSectionLanes and return correct order of positions for reference line
            MapLine GetRefLineAndPositions(List<MapLane> neighborLaneSectionLanes, out List<Vector3> positions)
            {
                // Lanes are stored from left most to right most
                // Reference line is the left boundary line of the left most lane
                var refLine = neighborLaneSectionLanes[0].leftLineBoundry;
                positions = refLine.mapWorldPositions;
                // Make sure refLine positions has same direction as the road lanes
                var leftLanePositions = neighborLaneSectionLanes[0].mapWorldPositions; // pick any lane
                if (Vector3.Dot((positions.Last() - positions.First()), (leftLanePositions.Last() - leftLanePositions.First())) < 0)
                {
                    positions.Reverse();
                }

                return refLine;
            }

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
                    
                    GetLaneSectionLanesAfterLanes(ref queue, consideredLanes, visitedNLSLIdx, lane2LaneSectionIdx);
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
                    Roads[roadId] = road;
                    RoadId2RefLinePositions[roadId] = refLinePositions;
                    Debug.Log(consideredLanes[0].gameObject.GetInstanceID() + "    road Id " + roadId + "   lane Id " + Lane2LaneId[consideredLanes[0]]);
                    roadId += 1;
                } 
            }
            // For each intersection, get all related roads, then find out the nearest roads to each signal

            // Create controller for pairs of signals in each intersection
            var controllers = new List<OpenDRIVEController>();
            // Add controller to each junction
            // Update road links and lane links
 
            UniqueId = roadId;
            var junctions = AddJunctions(UniqueId, ref controllers);

            foreach (var NLSLIdxList in neighborLaneSectionLanesIdx)
            {
                var roadBeforeLanes = new HashSet<MapLane>(); // lanes before current road
                var roadAfterLanes = new HashSet<MapLane>();

                // One way road 
                if (NLSLIdxList.Count == 1)
                {
                    UpdateLaneLink(ref roadBeforeLanes, ref roadAfterLanes, neighborForwardLaneSectionLanes[NLSLIdxList[0]]);
                }
                // Two way Roads
                else
                {
                    UpdateLaneLink(ref roadBeforeLanes, ref roadAfterLanes, neighborForwardLaneSectionLanes[NLSLIdxList[0]]);
                    UpdateLaneLink(ref roadBeforeLanes, ref roadAfterLanes, neighborForwardLaneSectionLanes[NLSLIdxList[1]]);
                }

                var curRoadId = Lane2RoadId[neighborForwardLaneSectionLanes[NLSLIdxList[0]][0]];
                UpdateRoadLink(roadBeforeLanes, roadAfterLanes, curRoadId);
            }


            Map.road = Roads;
            Map.controller = controllers.ToArray();
            Map.junction = junctions;
        }

        OpenDRIVEJunction[] AddJunctions(uint roadId, ref List<OpenDRIVEController> controllers)
        {
            uint firstJunctionId = roadId;
            uint junctionId = firstJunctionId;
            var junctions = new OpenDRIVEJunction[Intersections.Count];
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
                Debug.LogWarning($"intersectionLanes length {intersectionLanes.Length}");

                var roadId2MidPoint = new Dictionary<uint, Vector3>(); // roadId to the middle point of its reference line for intersection roads and roads connecting to the intersection
                // Tuple: (incomingRoadId, connectingRoadId, contactPoint)
                var connections2LaneLink = new Dictionary<Tuple<uint, uint, contactPoint>, List<OpenDRIVEJunctionConnectionLaneLink>>();
                var allBeforeAndAfterLanes = new List<MapLane>();
                foreach (var lane in intersectionLanes)
                {
                    Lane2JunctionId[lane] = junctionId;
                    var connectingRoadId = Lane2RoadId[lane];
                    var connectingLaneId = Lane2LaneId[lane];
                    var incomingLanes = lane.befores;
                    var beforeAfterLanes = new List<MapLane>(incomingLanes);
                    beforeAfterLanes.AddRange(lane.afters);
                    allBeforeAndAfterLanes.AddRange(beforeAfterLanes);
                    // Check whether lane has same direction with its road
                    if (connectingLaneId > 0)
                    {
                        incomingLanes = lane.afters;
                    }

                    // Get connected roads with this intersection and roads within it
                    foreach (var beforeAfterLane in beforeAfterLanes)
                    {
                        var id = Lane2RoadId[beforeAfterLane];
                        if (!roadId2MidPoint.ContainsKey(id))
                        {
                            var refLinePositions = RoadId2RefLinePositions[id]; 
                            roadId2MidPoint[id] = (refLinePositions.First() + refLinePositions.Last()) / 2;
                        }
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

                    // Update corresponding road header's junctionId
                    roadId = Lane2RoadId[lane];
                    if (updatedRoadIds.Contains(roadId)) continue;
                    Roads[roadId].junction = junctionId.ToString();
                }

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
                    // Find the nearesst approaching road to this signal
                    var pairedRoadId = GetPairedRoadId(stopLine2RoadId, mapSignal);
                    if (mapIntersection.facingGroup.Contains(mapSignal)) facingGroupSignalIds.Add(UniqueId);
                    if (mapIntersection.oppFacingGroup.Contains(mapSignal)) oppFacingGroupSignalIds.Add(UniqueId);

                    var roadIdStopLine = stopLine2RoadId[mapSignal.stopLine]; 
                    // Create signal
                    var isOnRoad = pairedRoadId == roadIdStopLine; // Check if the signal created will on the intended road
                    var signal = CreateSignalFromMapSignal(pairedRoadId, mapSignal, isOnRoad);
                    Debug.Log("CreateSignal   " + mapSignal.name + " " + pairedRoadId + "   " + isOnRoad);
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

                Debug.LogError(mapSigns.Length);
                foreach (var mapSign in mapSigns)
                {
                    // Find the nearesst road to this sign
                    var nearestRoadId = stopLine2RoadId[mapSign.stopLine];

                    // Create signal from sign
                    var signal = CreateSignalFromSign(nearestRoadId, mapSign);
                    roadId2Signals.CreateOrAdd(nearestRoadId, signal);
                }

                // Add signals and signalReferences
                foreach (var pair in roadId2Signals)
                {
                    Debug.LogError("road ID " + pair.Key + "  signals number " + pair.Value.Count);
                    var signals = new OpenDRIVERoadSignals();
                    signals.signal = pair.Value.ToArray();
                    Roads[pair.Key].signals = signals;
                }
                foreach (var pair in roadId2SignalReferences)
                {
                    Debug.LogError("road Id " + pair.Key + "  signalReference number " + pair.Value.Count);
                    var id = pair.Key;
                    if (Roads[id].signals == null)
                    {
                        Roads[id].signals = new OpenDRIVERoadSignals();
                    }

                    Roads[id].signals.signalReference = pair.Value.ToArray();
                }

                junction.connection = connections;
                junctions[junctionId - firstJunctionId] = junction;
                junctionId += 1;
            }

            return junctions;
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

            float x1 = p0.x, y1 = p0.z, x2 = p1.x, y2 = p1.z, x0 = signalSignPos.x, y0 = signalSignPos.z;
            t = Math.Abs((y2 - y1) * x0 - (x2 - x1) * y0 + x2 * y1 - y2 * x1) / Math.Sqrt((y2 - y1)*(y2 - y1) + (x2 - x1) * (x2 - x1));

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
            Debug.LogWarning("  nearestRoadId "+ nearestRoadId);
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

            Debug.LogWarning($"orientation {orien} s {s} zOffset {zOffset}");
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
            Debug.LogWarning("CreateSignalFromSign  nearestRoadId "+ nearestRoadId);
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
                if (preRoadIds.Count > 1)
                {
                    // junction
                    roadPredecessor = new OpenDRIVERoadLinkPredecessor()
                    {
                        elementType = elementType.junction,
                        elementId = GetJunctionId(preRoadIds),
                        elementTypeSpecified = true,
                    };
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
                if (sucRoadIds.Count > 1)
                {
                    roadSuccessor = new OpenDRIVERoadLinkSuccessor()
                    {
                        elementType = elementType.junction,
                        elementId = GetJunctionId(sucRoadIds),
                        elementTypeSpecified = true,
                    };
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
            else
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

        // Given current road and any lane of predecessor/successor road, get contactPoint of  the predecessor/successor road
        contactPoint GetContactPoint(uint curRoadId, MapLane linkRoadLane)
        {
            var roadStartPoint = new Vector3((float)Roads[curRoadId].planView[0].x, (float)Roads[curRoadId].elevationProfile.elevation[0].a, (float)Roads[curRoadId].planView[0].y);
            var positions = linkRoadLane.mapWorldPositions;
            if (Lane2LaneId[linkRoadLane] > 0)
            {
                // if the lane is a left lane in the road, reference line is opposite with the lane
                positions.Reverse();
            }
            if ((roadStartPoint - positions.First()).magnitude > (roadStartPoint - positions.Last()).magnitude)
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
                junctionIds.Add(curJunctionId);
                junctionId = curJunctionId;
                if (junctionId == "-1") Debug.LogError("A junction should not have id as -1, roadId: " + roadId);
            }

            if (junctionIds.Count == 0) Debug.LogError("No junctionId found!");
            else if (junctionIds.Count > 1) Debug.LogError("Multiple junctionId found for one road predecessor/successor!");
            
            return junctionId; 
        }
        void UpdateLaneLink(ref HashSet<MapLane> roadBeforeLanes, ref HashSet<MapLane> roadAfterLanes, List<MapLane> lanes)
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
            }
        }

        void GetLaneSectionLanesAfterLanes(ref Queue<MapLane> queue, List<MapLane> lanes, HashSet<int> visitedNLSLIdx, Dictionary<MapLane, int> lane2LaneSectionIdx)
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
                center = center,
                right = right,
            };

            if (!isOneWay)
            {
                var left = CreateLeftLanes(refLine, leftNeighborLaneSectionLanes);
                laneSectionArray[0] = new OpenDRIVERoadLanesLaneSection()
                {
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
                var x = location.Easting;
                var y = location.Northing;
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

                var widths = CreateLaneWidths(curLeftBoundaryLine, curRightBoundaryLine, refLineDirection);
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

                var widths = CreateLaneWidths(curLeftBoundaryLine, curRightBoundaryLine, refLineDirection);
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

        void ReverseIfOpposite(ref List<Vector3> positions, Vector3 direction)
        {
            if (Vector3.Dot((positions.Last() - positions.First()), direction) < 0)
            {
                positions.Reverse();
            }
        }

        // Create width array for boundaryLine based on refLine
        laneWidth[] CreateLaneWidths(MapLine refLine, MapLine boundaryLine, Vector3 refLineDirection)
        {
            var leftPositions = refLine.mapWorldPositions;
            ReverseIfOpposite(ref leftPositions, refLineDirection);
            var rightPositions = boundaryLine.mapWorldPositions;
            ReverseIfOpposite(ref rightPositions, refLineDirection);
            var laneWidths = new laneWidth[rightPositions.Count - 1];
            var widths = new float[rightPositions.Count];

            for (int idx = 0; idx < rightPositions.Count; idx ++)
            {
                var pos = rightPositions[idx];
                var lastDist = float.MaxValue;
                for (int i = 0; i < leftPositions.Count - 1; i ++)
                {
                    Vector3 p0 = leftPositions[i], p1 = leftPositions[i+1];

                    var curDist = Utility.SqrDistanceToSegment(p0, p1, pos);
                    if (curDist > lastDist) break; // distance should not increase

                    lastDist = curDist;
                }
                widths[idx] = Mathf.Sqrt(lastDist);
            }
            
            float curS = 0;
            for (int i = 0; i < widths.Length - 1; i ++)
            {
                var length = (rightPositions[i+1] - rightPositions[i]).magnitude;
                var a = widths[i];
                var b = (widths[i+1] - widths[i]) / length;
                laneWidths[i] = new laneWidth
                {
                    sOffset = curS,
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
                curS += length;
            }

            return laneWidths;
        }

        List<MapLane> FindStartingLanes()
        {
            var startingLanes = new List<MapLane>();
            foreach (var laneSegment in LaneSegments)
            {
                if (laneSegment.befores.Count == 0)
                {
                    startingLanes.Add(laneSegment);
                }
            }
            if (startingLanes.Count == 0)
            {
                Debug.LogError("Error, no startingLanes found!");
            }
            // Debug.Log($"We got {startingLanes.Count} startingLanes");
            return startingLanes;
        }

        void Export(string filePath)
        {
            var serializer = new XmlSerializer(typeof(OpenDRIVE));

            using (var writer = new StreamWriter(filePath))
            using (var xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings {Indent = true, IndentChars = "    "}))
            {
                serializer.Serialize(xmlWriter, Map);
            }
        }
        
        Vector2 ToVector2(Vector3 pt)
        {
            return new Vector2(pt.x, pt.z);
        }
        Vector3 ToVector3(Vector2 p)
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