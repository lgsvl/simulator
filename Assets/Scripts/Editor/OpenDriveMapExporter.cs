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

            // Add Roads
                // Add link
                // Add elevationProfile
                // Add lateralProfile
                // Add lanes
                    // Add laneSection
            // Add controllers
            // Add junctions
            // Add JunctionGroups
            // Add stations
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
                    // One Way lane section
                    if (neighborLaneSectionLanesIdx[curNLSLIdx].Count == 1)
                    {
                        var neighborLaneSectionLanes = neighborForwardLaneSectionLanes[neighborLaneSectionLanesIdx[curNLSLIdx][0]];

                        List<Vector3> positions;
                        var refLine = GetRefLineAndPositions(neighborLaneSectionLanes, out positions);
                        // Add road link?
                        // Add type?

                        AddPlanViewElevationLateral(positions, road);

                        // Add lanes
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

                        List<Vector3> positions;
                        MapLine refLine;
                        List<MapLane> leftNeighborLaneSectionLanes, rightNeighborLaneSectionLanes;
                        if (Vector3.Dot(direction1, curDirection) > 0)
                        {
                            refLine = GetRefLineAndPositions(neighborLaneSectionLanes1, out positions);
                            rightNeighborLaneSectionLanes = neighborLaneSectionLanes1;
                            leftNeighborLaneSectionLanes = neighborLaneSectionLanes2;
                        }
                        else
                        {
                            refLine = GetRefLineAndPositions(neighborLaneSectionLanes2, out positions);
                            rightNeighborLaneSectionLanes = neighborLaneSectionLanes2;
                            leftNeighborLaneSectionLanes = neighborLaneSectionLanes1;
                        }
                        // Add road link?
                        // Add type?
                        AddPlanViewElevationLateral(positions, road);

                        AddLanes(road, refLine, leftNeighborLaneSectionLanes, rightNeighborLaneSectionLanes, false);
                        
                        visitedNLSLIdx.Add(lane2LaneSectionIdx[neighborLaneSectionLanes1[0]]);
                        visitedNLSLIdx.Add(lane2LaneSectionIdx[neighborLaneSectionLanes2[0]]);
                        consideredLanes.AddRange(neighborLaneSectionLanes1);
                        consideredLanes.AddRange(neighborLaneSectionLanes2);
                    }
                    
                    GetLaneSectionLanesAfterLanes(ref queue, consideredLanes, visitedNLSLIdx, lane2LaneSectionIdx);
                    Add2Lane2RoadId(roadId, consideredLanes);
                    Roads[roadId] = road;
                    Debug.Log(consideredLanes[0].gameObject.GetInstanceID() + "    road Id " + roadId + "   lane Id " + Lane2LaneId[consideredLanes[0]]);
                    roadId += 1;
                } 
            }
            
            Map.junction = AddJunctions(roadId);

            // Update road links and lane links
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
        }

        OpenDRIVEJunction[] AddJunctions(uint roadId)
        {
            uint firstJunctionId = roadId;
            uint junctionId = firstJunctionId;
            var junctions = new OpenDRIVEJunction[Intersections.Count];
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
                // Tuple: (incomingRoadId, connectingRoadId, contactPoint)
                var connections2LaneLink = new Dictionary<Tuple<uint, uint, contactPoint>, List<OpenDRIVEJunctionConnectionLaneLink>>();
                foreach (var lane in intersectionLanes)
                {
                    Lane2JunctionId[lane] = junctionId;
                    var connectingRoadId = Lane2RoadId[lane];
                    var connectingLaneId = Lane2LaneId[lane];
                    var incomingLanes = lane.befores;
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

                junction.connection = connections;
                junctions[junctionId-firstJunctionId] = junction;
                junctionId += 1;
            }

            return junctions;
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
    }
}