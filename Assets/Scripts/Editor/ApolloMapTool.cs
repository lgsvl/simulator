/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Google.Protobuf;

using HD = apollo.hdmap;
using ApolloCommon = apollo.common;
using Simulator.Editor.Apollo;
using Simulator.Map;

namespace Simulator.Editor
{
    public class ApolloMapTool
    {
        // The threshold between stopline and branching point. if a stopline-lane intersect is closer than this to a branching point then this stopline is a braching stopline
        const float StoplineIntersectThreshold = 1.5f;
        private float OriginNorthing;
        private float OriginEasting;
        private float AltitudeOffset;

        private HD.Map Hdmap;
        private MapManagerData MapAnnotationData;

        public enum OverlapType
        {
            Signal_Stopline_Lane,
            Stopsign_Stopline_Lane,
        }       
        
        public void ExportHDMap(string filePath)
        {
            var mapOrigin = MapOrigin.Find();
            if (mapOrigin == null)
            {
                return;
            }

            OriginEasting = mapOrigin.OriginEasting;
            OriginNorthing = mapOrigin.OriginNorthing;
            AltitudeOffset = mapOrigin.AltitudeOffset;
            
            if (Calculate())
            {
                Export(filePath);
            }
        }

        List<MapLane> laneSegments;
        List<MapSignal> signalLights;
        List<MapSign> stopSigns;
        Dictionary<string, List<HD.Id>> junctionToOverlaps;
        Dictionary<string, List<GameObject>> overlapIdToGameObjects;
        Dictionary<GameObject, HD.ObjectOverlapInfo> gameObjectToOverlapInfo;
        Dictionary<GameObject, HD.Id> gameObjectToOverlapId;

        Dictionary<string, HD.Junction> overlapIdToJunction;
        Dictionary<string, List<GameObject>> roadIdToLanes;
        Dictionary<GameObject, HD.Id> gameObjectToLane;
        Dictionary<GameObject, HD.Id> laneGameObjectToOverlapId;
        HashSet<GameObject> laneParkingSpace;
        HashSet<GameObject> laneSpeedBump;
        HashSet<MapLane> laneSegmentsSet;
        Dictionary<MapParkingSpace, GameObject> parkingSpaceToNearestLaneGameObject;
        Dictionary<MapSpeedBump, GameObject> nearestLaneGameObjectSpeedBump;
        public List<MapLaneSection> laneSections = new List<MapLaneSection>();

        bool Calculate()
        {
            MapAnnotationData = new MapManagerData();

            // Process lanes, intersections.
            MapAnnotationData.GetIntersections();
            MapAnnotationData.GetTrafficLanes();

            Hdmap = new HD.Map()
            {
                header = new HD.Header()
                {
                    version = System.Text.Encoding.UTF8.GetBytes("1.500000"),
                    date = System.Text.Encoding.UTF8.GetBytes("2018-03-23T13:27:54"),
                    projection = new HD.Projection()
                    {
                        proj = "+proj=utm +zone=10 +ellps=WGS84 +datum=WGS84 +units=m +no_defs",
                    },
                    district = System.Text.Encoding.UTF8.GetBytes("0"),
                    rev_major = System.Text.Encoding.UTF8.GetBytes("1"),
                    rev_minor = System.Text.Encoding.UTF8.GetBytes("0"),
                    left = -121.982277,
                    top = 37.398079,
                    right = -121.971998,
                    bottom = 37.398079,
                    vendor = System.Text.Encoding.UTF8.GetBytes("LGSVL"),
                },
            };

            const float laneHalfWidth = 1.75f; //temp solution
            const float stoplineWidth = 0.7f;

            // Initial collection
            laneSegments = new List<MapLane>();
            signalLights = new List<MapSignal>();
            stopSigns = new List<MapSign>();
            
            junctionToOverlaps = new Dictionary<string, List<HD.Id>>();
            overlapIdToGameObjects = new Dictionary<string, List<GameObject>>();
            gameObjectToOverlapInfo = new Dictionary<GameObject, HD.ObjectOverlapInfo>();
            gameObjectToOverlapId = new Dictionary<GameObject, HD.Id>();

            overlapIdToJunction = new Dictionary<string, HD.Junction>();
            roadIdToLanes = new Dictionary<string, List<GameObject>>();
            gameObjectToLane = new Dictionary<GameObject, HD.Id>();
            laneGameObjectToOverlapId = new Dictionary<GameObject, HD.Id>();
            laneParkingSpace = new HashSet<GameObject>();
            laneSpeedBump = new HashSet<GameObject>();

            laneSegments.AddRange(MapAnnotationData.GetData<MapLane>());
            signalLights.AddRange(MapAnnotationData.GetData<MapSignal>());
            stopSigns.AddRange(MapAnnotationData.GetData<MapSign>());
            
            MakeJunctions();
            MakeInfoOfParkingSpace(); // TODO: needs test
            MakeInfoOfSpeedBump(); // TODO: needs test

            laneSegmentsSet = new HashSet<MapLane>(); 

            // Use set instead of list to increase speed
            foreach (var laneSegment in laneSegments)
            {
                laneSegmentsSet.Add(laneSegment);
            }

            // Link before and after segment for each lane segment, convert local positions to world positions
            foreach (var laneSegment in laneSegmentsSet)
            {
                // Each segment must have at least 2 waypoints for calculation, otherwise exit
                while (laneSegment.mapLocalPositions.Count < 2)
                {
                    Debug.Log("Some segment has less than 2 waypoints. Cancelling map generation.");
                    return false;
                }

                // Link lanes
                var firstPt = laneSegment.transform.TransformPoint(laneSegment.mapLocalPositions[0]);
                var lastPt = laneSegment.transform.TransformPoint(laneSegment.mapLocalPositions[laneSegment.mapLocalPositions.Count - 1]);

                foreach (var laneSegmentCmp in laneSegmentsSet)
                {
                    if (laneSegment == laneSegmentCmp)
                    {
                        continue;
                    }

                    var firstPt_cmp = laneSegmentCmp.transform.TransformPoint(laneSegmentCmp.mapLocalPositions[0]);
                    var lastPt_cmp = laneSegmentCmp.transform.TransformPoint(laneSegmentCmp.mapLocalPositions[laneSegmentCmp.mapLocalPositions.Count - 1]);

                    if ((firstPt - lastPt_cmp).magnitude < MapAnnotationTool.PROXIMITY / MapAnnotationTool.EXPORT_SCALE_FACTOR)
                    {
                        laneSegmentCmp.mapLocalPositions[laneSegmentCmp.mapLocalPositions.Count - 1] = laneSegmentCmp.transform.InverseTransformPoint(firstPt);
                        laneSegmentCmp.mapWorldPositions[laneSegmentCmp.mapWorldPositions.Count - 1] = firstPt;
                        laneSegment.befores.Add(laneSegmentCmp);
                    }

                    if ((lastPt - firstPt_cmp).magnitude < MapAnnotationTool.PROXIMITY / MapAnnotationTool.EXPORT_SCALE_FACTOR)
                    {
                        laneSegmentCmp.mapLocalPositions[0] = laneSegmentCmp.transform.InverseTransformPoint(lastPt);
                        laneSegmentCmp.mapWorldPositions[0] = lastPt;
                        laneSegment.afters.Add(laneSegmentCmp);
                    }
                }
            }

            // Check validity of lane segment builder relationship but it won't warn you if have A's right lane to be null or B's left lane to be null
            {
                foreach (var laneSegment in laneSegmentsSet)
                {
                    if (laneSegment.leftLaneForward != null && laneSegment != laneSegment.leftLaneForward.rightLaneForward
                        ||
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
            }

            // Lanes
            // Assign ids
            int laneId = 0;
            foreach (var laneSegment in laneSegmentsSet)
            {
                laneSegment.id = $"lane_{laneId}";
                ++laneId;
            }

            // Function to get neighbor lanes in the same road
            System.Func<MapLane, bool, List<MapLane>> GetNeighborForwardRoadLanes = null;
            GetNeighborForwardRoadLanes = delegate (MapLane self, bool fromLeft)
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
                        ret.AddRange(GetNeighborForwardRoadLanes(self.leftLaneForward, true));
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
                        ret.AddRange(GetNeighborForwardRoadLanes(self.rightLaneForward, false));
                        return ret;
                    }
                }
            };

            HashSet<HD.Road> roadSet = new HashSet<HD.Road>();

            var visitedLanes = new Dictionary<MapLane, HD.Road>();

            {
                foreach (var laneSegment in laneSegmentsSet)
                {
                    if (visitedLanes.ContainsKey(laneSegment))
                    {
                        continue;
                    }

                    var lefts = GetNeighborForwardRoadLanes(laneSegment, true);  // Left forward lanes from furthest to nearest
                    var rights = GetNeighborForwardRoadLanes(laneSegment, false);  // Right forward lanes from nearest to furthest

                    
                    var roadLanes = new List<MapLane>();
                    roadLanes.AddRange(lefts);
                    roadLanes.Add(laneSegment);
                    roadLanes.AddRange(rights);
                    
                    var roadSection = new HD.RoadSection()
                    {
                        id = HdId($"1"),
                        boundary = null,
                    };

                    foreach(var roadLaneSegment in roadLanes)
                    {
                        roadSection.lane_id.Add(new HD.Id()
                        {
                            id = roadLaneSegment.id
                        });
                    };
                    
                    var road = new HD.Road()
                    {
                        id = HdId($"road_{roadSet.Count}"),
                        junction_id = null,
                    };
                    road.section.Add(roadSection);

                    roadSet.Add(road);

                    foreach (var l in roadLanes)
                    {
                        if (!visitedLanes.ContainsKey(l))
                        {
                            visitedLanes.Add(l, road);
                        }
                    }

                    var gameObjectsOfLanes = new List<GameObject>();
                    foreach(var lane in roadLanes)
                    {
                        gameObjectsOfLanes.Add(lane.gameObject);
                    }
                    roadIdToLanes.Add(road.id.id, gameObjectsOfLanes);
                }
            }

            // Config lanes
            foreach (var laneSegment in laneSegmentsSet)
            {
                var centerPts = new List<ApolloCommon.PointENU>();
                var lBndPts = new List<ApolloCommon.PointENU>();
                var rBndPts = new List<ApolloCommon.PointENU>();

                var worldPoses = laneSegment.mapWorldPositions;
                var leftBoundPoses = new List<Vector3>();
                var rightBoundPoses = new List<Vector3>();

                float mLength = 0;
                float lLength = 0;
                float rLength = 0;

                List<HD.LaneSampleAssociation> associations = new List<HD.LaneSampleAssociation>();
                associations.Add(new HD.LaneSampleAssociation()
                {
                    s = 0,
                    width = laneHalfWidth,
                });

                for (int i = 0; i < worldPoses.Count; i++)
                {
                    Vector3 curPt = worldPoses[i];
                    Vector3 tangFwd;

                    if (i == 0)
                    {
                        tangFwd = (worldPoses[1] - curPt).normalized;
                    }
                    else if (i == worldPoses.Count - 1)
                    {
                        tangFwd = (curPt - worldPoses[worldPoses.Count - 2]).normalized;
                    }
                    else
                    {
                        tangFwd = (((curPt - worldPoses[i - 1]) + (worldPoses[i + 1] - curPt)) * 0.5f).normalized;
                    }

                    Vector3 lPoint = Vector3.Cross(tangFwd, Vector3.up) * laneHalfWidth + curPt;
                    Vector3 rPoint = -Vector3.Cross(tangFwd, Vector3.up) * laneHalfWidth + curPt;

                    leftBoundPoses.Add(lPoint);
                    rightBoundPoses.Add(rPoint);

                    if (i > 0)
                    {
                        mLength += (curPt - worldPoses[i - 1]).magnitude;
                        associations.Add(new HD.LaneSampleAssociation()
                        {
                            s = mLength,
                            width = laneHalfWidth,
                        });

                        lLength += (leftBoundPoses[i] - leftBoundPoses[i - 1]).magnitude;
                        rLength += (rightBoundPoses[i] - rightBoundPoses[i - 1]).magnitude;
                    }

                    centerPts.Add(HDMapUtil.GetApolloCoordinates(curPt, OriginEasting, OriginNorthing, false));
                    lBndPts.Add(HDMapUtil.GetApolloCoordinates(lPoint, OriginEasting, OriginNorthing, false));
                    rBndPts.Add(HDMapUtil.GetApolloCoordinates(rPoint, OriginEasting, OriginNorthing, false));

                }

                var predecessor_ids = new List<HD.Id>();
                var successor_ids = new List<HD.Id>();
                predecessor_ids.AddRange(laneSegment.befores.Select(seg => HdId(seg.id)));
                successor_ids.AddRange(laneSegment.afters.Select(seg => HdId(seg.id)));

                var lane = new HD.Lane()
                {
                    id = HdId(laneSegment.id),

                    central_curve = new HD.Curve(),
                    left_boundary = new HD.LaneBoundary(),
                    right_boundary = new HD.LaneBoundary(),
                    length = mLength,
                    speed_limit = laneSegment.speedLimit,
                    type = HD.Lane.LaneType.CITY_DRIVING,
                    turn = laneSegment.laneTurn,
                    direction = HD.Lane.LaneDirection.FORWARD,
                };

                // Make dictionary for parking space and laneId
                gameObjectToLane.Add(laneSegment.gameObject, lane.id);

                if (gameObjectToOverlapId.ContainsKey(laneSegment.gameObject)) 
                {
                    var overlap_id = gameObjectToOverlapId[laneSegment.gameObject];
                    lane.overlap_id.Add(overlap_id);
                    
                    var objectId = HdId(laneSegment.id);

                    gameObjectToOverlapInfo[laneSegment.gameObject].id = objectId;
                    gameObjectToOverlapInfo[laneSegment.gameObject].lane_overlap_info.start_s = 0.1;
                    gameObjectToOverlapInfo[laneSegment.gameObject].lane_overlap_info.end_s = mLength;
                    gameObjectToOverlapInfo[laneSegment.gameObject].lane_overlap_info.is_merge = false;
                }

                if (laneParkingSpace.Contains(laneSegment.gameObject))
                {
                    // Todo: needs multiple objects which has same key.
                    var overlap_id = laneGameObjectToOverlapId[laneSegment.gameObject];
                    lane.overlap_id.Add(overlap_id);
                }

                if (laneSpeedBump.Contains(laneSegment.gameObject))
                {
                    // Todo: needs multiple objects which has same key.
                    var overlap_id = laneGameObjectToOverlapId[laneSegment.gameObject];
                    lane.overlap_id.Add(overlap_id);
                }

                Hdmap.lane.Add(lane);

                // CentralCurve
                var lineSegment = new HD.LineSegment();
                lineSegment.point.AddRange(centerPts);
                
                var central_curve_segment = new List<HD.CurveSegment>()
                {
                    new HD.CurveSegment()
                    {
                        line_segment = lineSegment,
                        s = 0,
                        start_position = new ApolloCommon.PointENU()
                        {
                            x = centerPts[0].x,
                            y = centerPts[0].y,
                            z = centerPts[0].z,
                        },
                        length = mLength,
                    },
                };
                lane.central_curve.segment.AddRange(central_curve_segment);
                // /CentralCurve

                // LeftBoundary
                var curveSegment = new HD.CurveSegment()
                {
                    line_segment = new HD.LineSegment(),
                    s = 0,
                    start_position = lBndPts[0],
                    length = lLength,
                };

                curveSegment.line_segment.point.AddRange(lBndPts);

                var leftLaneBoundaryType = new HD.LaneBoundaryType()
                {
                    s = 0,
                };

                leftLaneBoundaryType.types.Add((HD.LaneBoundaryType.Type)laneSegment.leftBoundType);

                var left_boundary_segment = new HD.LaneBoundary()
                {
                    curve = new HD.Curve(),
                    length = lLength,
                    @virtual = true,
                };
                left_boundary_segment.boundary_type.Add(leftLaneBoundaryType);
                left_boundary_segment.curve.segment.Add(curveSegment);
                lane.left_boundary = left_boundary_segment;
                // /LeftBoundary
                
                // RightBoundary
                curveSegment = new HD.CurveSegment()
                {
                    line_segment = new HD.LineSegment(),
                    s = 0,
                    start_position = lBndPts[0],
                    length = lLength,
                };

                curveSegment.line_segment.point.AddRange(rBndPts);

                var rightLaneBoundaryType = new HD.LaneBoundaryType();

                rightLaneBoundaryType.types.Add((HD.LaneBoundaryType.Type)laneSegment.rightBoundType);

                var right_boundary_segment = new HD.LaneBoundary()
                {
                    curve = new HD.Curve(),
                    length = rLength,
                    @virtual = true,
                };
                right_boundary_segment.boundary_type.Add(rightLaneBoundaryType);
                right_boundary_segment.curve.segment.Add(curveSegment);
                lane.right_boundary = right_boundary_segment;
                // /RightBoundary

                if (predecessor_ids.Count > 0)
                    lane.predecessor_id.AddRange(predecessor_ids);

                if (successor_ids.Count > 0)
                    lane.successor_id.AddRange(successor_ids);

                lane.left_sample.AddRange(associations);
                lane.left_road_sample.AddRange(associations);
                lane.right_sample.AddRange(associations);
                lane.right_road_sample.AddRange(associations);
                if (laneSegment.leftLaneForward != null)
                    lane.left_neighbor_forward_lane_id.AddRange(new List<HD.Id>() { HdId(laneSegment.leftLaneForward.id), } );
                if (laneSegment.rightLaneForward != null)
                    lane.right_neighbor_forward_lane_id.AddRange(new List<HD.Id>() { HdId(laneSegment.rightLaneForward.id), } );
                if (laneSegment.leftLaneReverse != null)
                    lane.left_neighbor_reverse_lane_id.AddRange(new List<HD.Id>() { HdId(laneSegment.leftLaneReverse.id), } );
                if (laneSegment.rightLaneReverse != null)
                    lane.right_neighbor_reverse_lane_id.AddRange(new List<HD.Id>() { HdId(laneSegment.rightLaneReverse.id), } );
                
                // Add boundary to road
                if (laneSegment.leftLaneForward == null || laneSegment.rightLaneForward == null)
                {
                    var road = visitedLanes[laneSegment];
                    roadSet.Remove(road);

                    var section = road.section[0];

                    lineSegment = new HD.LineSegment();
                    if (laneSegment.leftLaneForward == null) 
                        lineSegment.point.AddRange(lBndPts);
                    else
                        lineSegment.point.AddRange(rBndPts);

                    var edges = new List<HD.BoundaryEdge>();
                    if (section.boundary?.outer_polygon?.edge != null)
                    {
                        edges.AddRange(section.boundary.outer_polygon.edge);
                    }

                    {
                        var boundaryEdge = new HD.BoundaryEdge()
                        {
                            curve = new HD.Curve(),
                            type = laneSegment.leftLaneForward == null ? HD.BoundaryEdge.Type.LEFT_BOUNDARY : HD.BoundaryEdge.Type.RIGHT_BOUNDARY,
                        };
                        boundaryEdge.curve.segment.Add(new HD.CurveSegment()
                        {
                            line_segment = lineSegment,
                        });
                        edges.Add(boundaryEdge);
                    }

                    lineSegment = new HD.LineSegment();
                    // Cases that a Road only has one lane, adds rightBoundary
                    if (laneSegment.leftLaneForward == null && laneSegment.rightLaneForward == null)
                    {
                        lineSegment.point.Clear();
                        lineSegment.point.AddRange(rBndPts);
                        var boundaryEdge = new HD.BoundaryEdge()
                        {
                            curve = new HD.Curve(),
                            type = HD.BoundaryEdge.Type.RIGHT_BOUNDARY,
                        };
                        boundaryEdge.curve.segment.Add(new HD.CurveSegment()
                        {
                            line_segment = lineSegment,
                        });
                        edges.Add(boundaryEdge);
                    }

                    section.boundary = new HD.RoadBoundary()
                    {
                        outer_polygon = new HD.BoundaryPolygon(),
                    };
                    section.boundary.outer_polygon.edge.AddRange(edges);
                    road.section[0] = section;
                    roadSet.Add(road);
                }
            }

            foreach(var road in roadSet)
            {
                if (road.section[0].boundary.outer_polygon.edge.Count == 0)
                {
                    Debug.Log("You have no boundary edges in some roads, please check!!!");
                    return false;
                }

                foreach(var lane in roadIdToLanes[road.id.id])
                {
                    if (gameObjectToOverlapId.ContainsKey(lane))
                    {
                        var overlap_id = gameObjectToOverlapId[lane];
                        var junction = overlapIdToJunction[overlap_id.id];
                        road.junction_id = junction.id;
                    }
                }
            }

            Hdmap.road.AddRange(roadSet);

            //for backtracking what overlaps are related to a specific lane
            var laneIds2OverlapIdsMapping = new Dictionary<string, List<HD.Id>>();

            //setup signals and lane_signal overlaps
            foreach (var signalLight in signalLights)
            {
                //signal id
                int signal_Id = Hdmap.signal.Count;

                //construct boundry points
                var bounds = signalLight.Get2DBounds();
                List<ApolloCommon.PointENU> signalBoundPts = new List<ApolloCommon.PointENU>()
                {
                    HDMapUtil.GetApolloCoordinates(bounds.Item1, OriginEasting, OriginNorthing, AltitudeOffset),
                    HDMapUtil.GetApolloCoordinates(bounds.Item2, OriginEasting, OriginNorthing, AltitudeOffset),
                    HDMapUtil.GetApolloCoordinates(bounds.Item3, OriginEasting, OriginNorthing, AltitudeOffset),
                    HDMapUtil.GetApolloCoordinates(bounds.Item4, OriginEasting, OriginNorthing, AltitudeOffset)
                };

                //sub signals
                List<HD.Subsignal> subsignals = null;
                if (signalLight.signalData.Count > 0)
                {
                    subsignals = new List<HD.Subsignal>();
                    for (int i = 0; i < signalLight.signalData.Count; i++)
                    {
                        var lightData = signalLight.signalData[i];
                        subsignals.Add( new HD.Subsignal()
                        {
                            id = HdId(i.ToString()),
                            type = HD.Subsignal.Type.CIRCLE,
                            location = HDMapUtil.GetApolloCoordinates(signalLight.transform.TransformPoint(lightData.localPosition), OriginEasting, OriginNorthing, AltitudeOffset),
                        });
                    }           
                }

                //keep track of all overlaps this signal created
                List<HD.Id> overlap_ids = new List<HD.Id>();

                //stopline points
                List<ApolloCommon.PointENU> stoplinePts = null;
                var stopline = signalLight.stopLine;
                if (stopline != null && stopline.mapLocalPositions.Count > 1)
                {
                    stoplinePts = new List<ApolloCommon.PointENU>();
                    List<MapLane> lanesToInspec = new List<MapLane>();
                    lanesToInspec.AddRange(laneSegmentsSet);

                    if (!MakeStoplineLaneOverlaps(stopline, lanesToInspec, stoplineWidth, signal_Id, OverlapType.Signal_Stopline_Lane, stoplinePts, laneIds2OverlapIdsMapping, overlap_ids))
                    {
                        return false;
                    }                  
                }

                if (stoplinePts != null && stoplinePts.Count > 2)
                {
                    var boundary = new HD.Polygon();
                    boundary.point.AddRange(signalBoundPts);

                    var signalId = HdId($"signal_{signal_Id}");
                    var signal = new HD.Signal()
                    {
                        id = signalId,
                        type = (HD.Signal.Type)signalLight.signalType, // TODO converted from LGSVL signal type to apollo need to check autoware type?
                        boundary = boundary,
                    };

                    // var subSignal = new RepeatedField<HD.Subsignal>();
                    // subSignal.Add(subsignals);
                    signal.subsignal.AddRange(subsignals);

                    // var overlapIds = new RepeatedField<HD.Id>();
                    // if (overlap_ids.Count >= 1)
                    //     overlapIds.Add(overlap_ids);

                    if (gameObjectToOverlapId.ContainsKey(signalLight.gameObject))
                    {
                        var signalOverlapInfo = new HD.ObjectOverlapInfo()
                        {
                            id = signalId,
                            signal_overlap_info = new HD.SignalOverlapInfo(),
                        };
                        gameObjectToOverlapInfo[signalLight.gameObject] = signalOverlapInfo; 
                        overlap_ids.Add(gameObjectToOverlapId[signalLight.gameObject]);
                    }
                    signal.overlap_id.AddRange(overlap_ids);

                    var curveSegment = new List<HD.CurveSegment>();
                    var lineSegment = new HD.LineSegment();
                    lineSegment.point.AddRange(stoplinePts);
                    curveSegment.Add(new HD.CurveSegment()
                    {
                        line_segment = lineSegment
                    });

                    var stopLine = new HD.Curve();
                    stopLine.segment.AddRange(curveSegment);
                    signal.stop_line.Add(stopLine);
                    Hdmap.signal.Add(signal);
                }
            }

            //setup stopsigns and lane_stopsign overlaps
            foreach (var stopSign in stopSigns)
            {
                //stopsign id
                int stopsign_Id = Hdmap.stop_sign.Count;

                //keep track of all overlaps this stopsign created
                List<HD.Id> overlap_ids = new List<HD.Id>();

                //stopline points
                List<ApolloCommon.PointENU> stoplinePts = null;
                var stopline = stopSign.stopLine;
                if (stopline != null && stopline.mapLocalPositions.Count > 1)
                {
                    stoplinePts = new List<ApolloCommon.PointENU>();
                    List<MapLane> lanesToInspec = new List<MapLane>();
                    lanesToInspec.AddRange(laneSegmentsSet);

                    if (!MakeStoplineLaneOverlaps(stopline, lanesToInspec, stoplineWidth, stopsign_Id, OverlapType.Stopsign_Stopline_Lane, stoplinePts, laneIds2OverlapIdsMapping, overlap_ids))
                    {
                        return false;
                    } 
                }

                if (stoplinePts != null && stoplinePts.Count > 2)
                {
                    var stopId = HdId($"stopsign_{stopsign_Id}");

                    // var overlapIds = new RepeatedField<HD.Id>();
                    // if (overlap_ids.Count >= 1)
                    //     overlapIds.Add(overlap_ids);
                    
                    var overlapId = new HD.Id();
                    // stop sign and junction overlap.
                    if (gameObjectToOverlapId.ContainsKey(stopSign.gameObject))
                    {
                        var stopOverlapInfo = new HD.ObjectOverlapInfo()
                        {
                            id = stopId,
                            stop_sign_overlap_info = new HD.StopSignOverlapInfo(),
                        };
                        gameObjectToOverlapInfo[stopSign.gameObject] = stopOverlapInfo; 
                        overlap_ids.Add(gameObjectToOverlapId[stopSign.gameObject]);
                    }

                    var curveSegment = new List<HD.CurveSegment>();
                    var lineSegment = new HD.LineSegment();

                    lineSegment.point.AddRange(stoplinePts);

                    curveSegment.Add(new HD.CurveSegment()
                    {
                        line_segment = lineSegment,
                    });

                    var stopLine = new HD.Curve();
                    stopLine.segment.AddRange(curveSegment);

                    var hdStopSign = new HD.StopSign()
                    {
                        id = stopId,
                        type = HD.StopSign.StopType.UNKNOWN,
                    };
                    hdStopSign.overlap_id.AddRange(overlap_ids);

                    hdStopSign.stop_line.Add(stopLine);
                    Hdmap.stop_sign.Add(hdStopSign);
                }
            }

            //backtrack and fill missing information for lanes
            for (int i = 0; i < Hdmap.lane.Count; i++)
            {
                HD.Id land_id = (HD.Id)(Hdmap.lane[i].id);
                var oldLane = Hdmap.lane[i];
                if (laneIds2OverlapIdsMapping.ContainsKey(land_id.id))
                    oldLane.overlap_id.AddRange(laneIds2OverlapIdsMapping[Hdmap.lane[i].id.id]);
                Hdmap.lane[i] = oldLane;
            }

            //Overlap
            foreach(var overlapIdToGameObject in overlapIdToGameObjects)
            {
                var overlap_id = overlapIdToGameObject.Key;
                
                foreach(var objId in overlapIdToGameObject.Value)
                {
                    var objHdMapId = gameObjectToOverlapId[objId];
                    var objectOverlapInfo = gameObjectToOverlapInfo[objId];

                    var overlap = new HD.Overlap()
                    {
                        id = objHdMapId,
                    };

                    overlap.@object.Add(objectOverlapInfo);
                    var objectOverlapInfo1 = new HD.ObjectOverlapInfo()
                    {

                        id = overlapIdToJunction[overlap_id].id,
                        junction_overlap_info = new HD.JunctionOverlapInfo(),
                    };

                    overlap.@object.Add(objectOverlapInfo1);
                    Hdmap.overlap.Add(overlap);
                }
            }

            MakeParkingSpaceAnnotation();
            MakeSpeedBumpAnnotation();

            return true;
        }

        bool MakeStoplineLaneOverlaps(MapLine stopline, List<MapLane> lanesToInspec, float stoplineWidth, int overlapInfoId, OverlapType overlapType, List<ApolloCommon.PointENU> stoplinePts, Dictionary<string, List<HD.Id>> laneId2OverlapIdsMapping, List<HD.Id> overlap_ids)
        {
            stopline.mapWorldPositions = new List<Vector3>(stopline.mapLocalPositions.Count);
            List<Vector2> stopline2D = new List<Vector2>();

            for (int i = 0; i < stopline.mapLocalPositions.Count; i++)
            {
                var worldPos = stopline.transform.TransformPoint(stopline.mapLocalPositions[i]);
                stopline.mapWorldPositions.Add(worldPos); //to worldspace here
                stopline2D.Add(new Vector2(worldPos.x, worldPos.z));
                stoplinePts.Add(HDMapUtil.GetApolloCoordinates(worldPos, OriginEasting, OriginNorthing, false));
            }

            var considered = new HashSet<MapLane>(); //This is to prevent conceptually or practically duplicated overlaps

            string overlap_id_prefix = "";
            if (overlapType == OverlapType.Signal_Stopline_Lane)
            {
                overlap_id_prefix = "signal_lane_overlap_";
            }
            else if (overlapType == OverlapType.Stopsign_Stopline_Lane)
            {
                overlap_id_prefix = "stopsign_lane_overlap_";
            }

            foreach (var seg in lanesToInspec)
            {
                List<Vector2> intersects;
                var lane2D = seg.mapWorldPositions.Select(p => new Vector2(p.x, p.z)).ToList();
                bool isIntersected = Utilities.Utility.CurveSegmentsIntersect(stopline2D, lane2D, out intersects);
                if (isIntersected)
                {
                    Vector2 intersect = intersects[0];

                    if (intersects.Count > 1)
                    {
                        //determin if is cluster
                        Vector2 avgPt = Vector2.zero;
                        float maxRadius = MapAnnotationTool.PROXIMITY;
                        bool isCluster = true;
                        for (int i = 0; i < intersects.Count; i++)
                        {
                            avgPt += intersects[i];
                        }
                        avgPt /= intersects.Count;
                        for (int i = 0; i < intersects.Count; i++)
                        {
                            if ((avgPt - intersects[i]).magnitude > maxRadius)
                            {
                                isCluster = false;
                            }
                        }

                        if (isCluster)
                        {
                            //Debug.Log("stopline have multiple intersect points with a lane within a cluster, pick one");
                        }
                        else
                        {
                            //UnityEditor.Selection.activeGameObject = stopLine.gameObject;
                            Debug.LogWarning("Stopline has more than one non-cluster intersect point with a lane. Cancelling map export.");
                            return false;
                        }
                    }

                    float totalLength;
                    float s = Utilities.Utility.GetNearestSCoordinate(intersect, lane2D, out totalLength);

                    var segments = new List<MapLane>();
                    var lengths = new List<float>();

                    if (totalLength - s < StoplineIntersectThreshold && seg.afters.Count > 0)
                    {
                        s = 0;
                        foreach (var afterSeg in seg.afters)
                        {
                            segments.Add(afterSeg);
                            lengths.Add(Utilities.Utility.GetCurveLength(afterSeg.mapWorldPositions.Select(p => new Vector2(p.x, p.z)).ToList()));
                        }
                    }
                    else
                    {
                        segments.Add(seg);
                        lengths.Add(totalLength);
                    }

                    for (int i = 0; i < segments.Count; i++)
                    {
                        var segment = segments[i];
                        var segLen = lengths[i];
                        if (considered.Contains(segment))
                        {
                            continue;
                        }

                        considered.Add(segment);

                        float ln_start_s = s - stoplineWidth * 0.5f;
                        float ln_end_s = s + stoplineWidth * 0.5f;

                        if (ln_start_s < 0)
                        {
                            var diff = -ln_start_s;
                            ln_start_s += diff;
                            ln_end_s += diff;
                            if (ln_end_s > segLen)
                            {
                                ln_end_s = segLen;
                            }
                        }
                        else if (ln_end_s > segLen)
                        {
                            var diff = ln_end_s - segLen;
                            ln_start_s -= diff;
                            ln_end_s -= diff;
                            if (ln_start_s < 0)
                            {
                                ln_start_s = 0;
                            }
                        }

                        //Create overlap
                        var overlap_id = HdId($"{overlap_id_prefix}{Hdmap.overlap.Count}");
                        var lane_id = HdId(segment.id);

                        laneId2OverlapIdsMapping.GetOrCreate(lane_id.id).Add(overlap_id);

                        HD.ObjectOverlapInfo objOverlapInfo = new HD.ObjectOverlapInfo();

                        if (overlapType == OverlapType.Signal_Stopline_Lane)
                        {
                            objOverlapInfo = new HD.ObjectOverlapInfo()
                            {
                                id = HdId($"signal_{overlapInfoId}"),
                                signal_overlap_info = new HD.SignalOverlapInfo(),
                            };
                        }
                        else if (overlapType == OverlapType.Stopsign_Stopline_Lane)
                        {
                            objOverlapInfo = new HD.ObjectOverlapInfo()
                            {
                                id = HdId($"stopsign_{overlapInfoId}"),
                                stop_sign_overlap_info = new HD.StopSignOverlapInfo(),
                            };
                        }

                        var object_overlap = new List<HD.ObjectOverlapInfo>()
                        {
                            new HD.ObjectOverlapInfo()
                            {
                                id = lane_id,
                                lane_overlap_info = new HD.LaneOverlapInfo()
                                {
                                    start_s = ln_start_s,
                                    end_s = ln_end_s,
                                    is_merge = false,
                                },
                            },
                            objOverlapInfo,
                        };

                        var overlap = new HD.Overlap()
                        {
                            id = overlap_id,
                        };
                        overlap.@object.AddRange(object_overlap);
                        Hdmap.overlap.Add(overlap);
                        overlap_ids.Add(overlap_id);
                    }
                }
            }
            return true;
        }

        void MakeJunctions()
        {
            var intersections = new List<MapIntersection>();
            intersections.AddRange(MapAnnotationData.GetData<MapIntersection>());
            foreach(var intersection in intersections)
            {
                var junctionList = intersection.transform.GetComponentsInChildren<MapJunction>().ToList();
                foreach(var junction in junctionList)
                {
                    var polygon = new HD.Polygon();

                    foreach (var localPos in junction.mapLocalPositions)
                        junction.mapWorldPositions.Add(junction.transform.TransformPoint(localPos));

                    foreach (var pt in junction.mapWorldPositions)
                    {
                        var ptInApollo = HDMapUtil.GetApolloCoordinates(pt, OriginEasting, OriginNorthing, false);
                        polygon.point.Add(new ApolloCommon.PointENU()
                        {
                            x = ptInApollo.x,
                            y = ptInApollo.y,
                            z = ptInApollo.z,
                        });
                    }

                    var junctionId = HdId($"junction_{intersections.IndexOf(intersection)}_{junctionList.IndexOf(junction)}");
                    var junctionOverlapIds = new List<HD.Id>();

                    // LaneSegment
                    var laneList = intersection.transform.GetComponentsInChildren<MapLane>().ToList();
                    foreach(var lane in laneList)
                    {
                        var overlapId = HdId($"overlap_junction_{intersections.IndexOf(intersection)}_{junctionList.IndexOf(junction)}_lane_{intersections.IndexOf(intersection)}_{laneList.IndexOf(lane)}");
                        var objectOverlapInfo = new HD.ObjectOverlapInfo()
                        {
                            lane_overlap_info = new HD.LaneOverlapInfo(),
                        };

                        junctionToOverlaps.GetOrCreate(junctionId.id).Add(overlapId);
                        overlapIdToGameObjects.GetOrCreate(overlapId.id).Add(lane.gameObject);
                        gameObjectToOverlapInfo.GetOrCreate(lane.gameObject).lane_overlap_info = new HD.LaneOverlapInfo();
                        gameObjectToOverlapId.Add(lane.gameObject, overlapId);
                        
                        var junctionOverlapInfo = new HD.ObjectOverlapInfo()
                        {
                            id = junctionId,
                            junction_overlap_info = new HD.JunctionOverlapInfo(),
                        };

                        if (!gameObjectToOverlapInfo.ContainsKey(junction.gameObject))
                            gameObjectToOverlapInfo.Add(junction.gameObject, junctionOverlapInfo);

                        var j = new HD.Junction()
                        {   
                            id = junctionId,
                            polygon = polygon,
                        };
                        j.overlap_id.Add(overlapId);
                        overlapIdToJunction.Add(overlapId.id, j);
                        
                        junctionOverlapIds.Add(overlapId);
                    }

                    // StopSign
                    var stopSignList = intersection.transform.GetComponentsInChildren<MapLine>().ToList();
                    foreach(var stopSign in stopSignList)
                    {
                        var overlapId = HdId($"overlap_junction{junctionList.IndexOf(junction)}_stopsign{stopSignList.IndexOf(stopSign)}");                            
                        var objectOverlapInfo = new HD.ObjectOverlapInfo()
                        {
                            stop_sign_overlap_info = new HD.StopSignOverlapInfo(),
                        };

                        var overlapIds = new List<HD.Id>()
                        {
                            HdId(overlapId.ToString()),
                        };
                        if (!junctionToOverlaps.ContainsKey(junctionId.id))
                            junctionToOverlaps.Add(junctionId.id, overlapIds);
                        else
                            junctionToOverlaps[junctionId.id].Add(overlapId);

                        var stopSignObjList = new List<GameObject>();
                        stopSignObjList.Add(stopSign.gameObject);
                        if (!overlapIdToGameObjects.ContainsKey(overlapId.id))
                            overlapIdToGameObjects.Add(overlapId.id, stopSignObjList);
                        else
                            overlapIdToGameObjects[overlapId.id].Add(stopSign.gameObject);

                        if (!gameObjectToOverlapInfo.ContainsKey(stopSign.gameObject))
                            gameObjectToOverlapInfo.Add(stopSign.gameObject, objectOverlapInfo);
                        else
                            gameObjectToOverlapInfo[stopSign.gameObject] = objectOverlapInfo;

                        if (!gameObjectToOverlapId.ContainsKey(stopSign.gameObject))
                            gameObjectToOverlapId.Add(stopSign.gameObject, overlapId);
                        else
                            gameObjectToOverlapId[stopSign.gameObject] = overlapId;

                        
                        var junctionOverlapInfo = new HD.ObjectOverlapInfo()
                        {
                            id = junctionId,
                            junction_overlap_info = new HD.JunctionOverlapInfo(),
                        };

                        gameObjectToOverlapInfo[junction.gameObject] = junctionOverlapInfo;

                        var j = new HD.Junction()
                        {   
                            id = junctionId,
                            polygon = polygon,
                        };

                        if (!overlapIdToJunction.ContainsKey(overlapId.id))
                            overlapIdToJunction.Add(overlapId.id, j);
                        overlapIdToJunction[overlapId.id] = j;

                        junctionOverlapIds.Add(overlapId);
                    }
                    
                    // SignalLight
                    var signalList = intersection.transform.GetComponentsInChildren<MapSignal>().ToList();
                    foreach(var signal in signalList)
                    {
                        var overlapId = HdId($"overlap_junction_{intersections.IndexOf(intersection)}_{junctionList.IndexOf(junction)}_signal_{intersections.IndexOf(intersection)}_{signalList.IndexOf(signal)}");
                        var objectOverlapInfo = new HD.ObjectOverlapInfo()
                        {
                            signal_overlap_info = new HD.SignalOverlapInfo(),
                        };

                        var overlapIds = new List<HD.Id>()
                        {
                            HdId(overlapId.ToString()),
                        };
                        if (!junctionToOverlaps.ContainsKey(junctionId.id))
                            junctionToOverlaps.Add(junctionId.id, overlapIds);
                        else
                            junctionToOverlaps[junctionId.id].Add(overlapId);

                        var signalObjList = new List<GameObject>();
                        signalObjList.Add(signal.gameObject);
                        if (!overlapIdToGameObjects.ContainsKey(overlapId.id))
                            overlapIdToGameObjects.Add(overlapId.id, signalObjList);
                        else
                            overlapIdToGameObjects[overlapId.id].Add(signal.gameObject);

                        if (!gameObjectToOverlapInfo.ContainsKey(signal.gameObject))
                            gameObjectToOverlapInfo.Add(signal.gameObject, objectOverlapInfo);
                        else
                            gameObjectToOverlapInfo[signal.gameObject] = objectOverlapInfo;

                        if (!gameObjectToOverlapId.ContainsKey(signal.gameObject))
                            gameObjectToOverlapId.Add(signal.gameObject, overlapId);
                        else
                            gameObjectToOverlapId[signal.gameObject] = overlapId;

                        var junctionOverlapInfo = new HD.ObjectOverlapInfo()
                        {
                            id = junctionId,
                            junction_overlap_info = new HD.JunctionOverlapInfo(),
                        };

                        gameObjectToOverlapInfo[junction.gameObject] = junctionOverlapInfo;

                        var j = new HD.Junction()
                        {   
                            id = junctionId,
                            polygon = polygon,
                        };

                        if (!overlapIdToJunction.ContainsKey(overlapId.id))
                            overlapIdToJunction.Add(overlapId.id, j);
                        overlapIdToJunction[overlapId.id] = j;
                        
                        junctionOverlapIds.Add(overlapId);
                    }

                    Hdmap.junction.Add(new HD.Junction()
                    {
                        id = junctionId,
                        polygon = polygon,
                    });
                    var hdJunction = new HD.Junction();
                    hdJunction.overlap_id.AddRange(junctionOverlapIds);
                }
            }
        }

        void MakeInfoOfParkingSpace()
        {
            parkingSpaceToNearestLaneGameObject = new Dictionary<MapParkingSpace, GameObject>();
            var parkingSpaceList = new List<MapParkingSpace>();
            parkingSpaceList.AddRange(MapAnnotationData.GetData<MapParkingSpace>());

            var laneList = new List<MapLane>();
            laneList.AddRange(MapAnnotationData.GetData<MapLane>());

            double dist = double.MaxValue;
            foreach (var parkingSpace in parkingSpaceList)
            {
                foreach (var localPos in parkingSpace.mapLocalPositions)
                {
                    parkingSpace.mapWorldPositions.Add(parkingSpace.transform.TransformPoint(localPos));
                }

                dist = double.MaxValue;

                GameObject nearestLaneGameObject = null;
                foreach (var lane in laneList)
                {       
                    var p1 = new Vector2(lane.mapWorldPositions.First().x, lane.mapWorldPositions.First().z);
                    var p2 = new Vector2(lane.mapWorldPositions.Last().x, lane.mapWorldPositions.Last().z);
                    var pt = new Vector2(parkingSpace.transform.position.x, parkingSpace.transform.position.z);
                    var closestPt = new Vector2();
                    double d = FindDistanceToSegment(pt, p1, p2, out closestPt);

                    if (dist > d)
                    {
                        dist = d;
                        nearestLaneGameObject = lane.gameObject;
                    }
                }
                var overlapId = HdId($"overlap_parking_space_{parkingSpaceList.IndexOf(parkingSpace)}");
                parkingSpaceToNearestLaneGameObject.Add(parkingSpace, nearestLaneGameObject);
                laneGameObjectToOverlapId.Add(nearestLaneGameObject, overlapId);
                laneParkingSpace.Add(nearestLaneGameObject);
            }
        }

        void MakeParkingSpaceAnnotation()
        {
            var parkingSpaceList = new List<MapParkingSpace>();
            parkingSpaceList.AddRange(MapAnnotationData.GetData<MapParkingSpace>());

            foreach (var parkingSpace in parkingSpaceList)
            {
                var polygon = new HD.Polygon();
                foreach (var localPos in parkingSpace.mapLocalPositions) parkingSpace.mapWorldPositions.Add(parkingSpace.transform.TransformPoint(localPos));
                var vector = (parkingSpace.mapWorldPositions[1] - parkingSpace.mapWorldPositions[2]).normalized;
                var heading = Mathf.Atan(vector.z / vector.x);
                heading = (heading < 0) ? (float)(heading + Mathf.PI * 2) : heading;

                foreach (var pt in parkingSpace.mapWorldPositions)
                {
                    var ptInApollo = HDMapUtil.GetApolloCoordinates(pt, OriginEasting, OriginNorthing, false);
                    polygon.point.Add(new ApolloCommon.PointENU()
                    {
                        x = ptInApollo.x,
                        y = ptInApollo.y,
                        z = ptInApollo.z,
                    });
                }
                var parkingSpaceId = HdId($"parking_space_{parkingSpaceList.IndexOf(parkingSpace)}");

                // Overlap
                var overlapId = HdId($"overlap_parking_space_{parkingSpaceList.IndexOf(parkingSpace)}");

                // Overlap lane
                var laneOverlapInfo = new HD.LaneOverlapInfo()
                {
                    start_s = 0,
                    end_s = 0,
                    is_merge = false,
                };

                var objectLane = new HD.ObjectOverlapInfo()
                {
                    id = gameObjectToLane[parkingSpaceToNearestLaneGameObject[parkingSpace]],
                    lane_overlap_info = laneOverlapInfo,
                };

                var objectParkingSpace = new HD.ObjectOverlapInfo()
                {
                    id = parkingSpaceId,
                    parking_space_overlap_info = new HD.ParkingSpaceOverlapInfo(),
                };

                var overlap = new HD.Overlap()
                {
                    id = overlapId,
                };
                overlap.@object.Add(objectLane);
                overlap.@object.Add(objectParkingSpace);
                Hdmap.overlap.Add(overlap);

                var ParkingSpaceAnnotation = new HD.ParkingSpace()
                {
                    id = parkingSpaceId,
                    polygon = polygon,
                    heading = heading,
                };
                ParkingSpaceAnnotation.overlap_id.Add(overlapId);
                Hdmap.parking_space.Add(ParkingSpaceAnnotation);
            }
        }

        void MakeInfoOfSpeedBump()
        {
            nearestLaneGameObjectSpeedBump = new Dictionary<MapSpeedBump, GameObject>();
            var speedBumpList = new List<MapSpeedBump>();
            speedBumpList.AddRange(MapAnnotationData.GetData<MapSpeedBump>());

            var laneList = new List<MapLane>();
            laneList.AddRange(MapAnnotationData.GetData<MapLane>());

            foreach (var speedBump in speedBumpList)
            {
                foreach (var localPos in speedBump.mapLocalPositions)
                {
                    speedBump.mapWorldPositions.Add(speedBump.transform.TransformPoint(localPos));
                }

                foreach (var lane in laneList)
                {       
                    var p1 = new Vector2(lane.mapWorldPositions.First().x, lane.mapWorldPositions.First().z);
                    var p2 = new Vector2(lane.mapWorldPositions.Last().x, lane.mapWorldPositions.Last().z);
                    var q1 = new Vector2(speedBump.mapWorldPositions[0].x, speedBump.mapWorldPositions[0].z);
                    var q2 = new Vector2(speedBump.mapWorldPositions[2].x, speedBump.mapWorldPositions[2].z);

                    if (DoIntersect(p1, p2, q1, q2))
                    {
                        nearestLaneGameObjectSpeedBump[speedBump] = lane.gameObject; // TODO: maybe this is not correct? we need the nearest one?
                    }
                }
                var overlapId = HdId($"overlap_speed_bump_{speedBumpList.IndexOf(speedBump)}");
                laneGameObjectToOverlapId.Add(nearestLaneGameObjectSpeedBump[speedBump], overlapId);
                laneSpeedBump.Add(nearestLaneGameObjectSpeedBump[speedBump]);
            }
        }

        void MakeSpeedBumpAnnotation()
        {
            var speedBumpList = new List<MapSpeedBump>();
            speedBumpList.AddRange(MapAnnotationData.GetData<MapSpeedBump>());

            foreach (var speedBump in speedBumpList)
            {
                var lineSegment = new HD.LineSegment();
                foreach(var localPos in speedBump.mapLocalPositions)
                    speedBump.mapWorldPositions.Add(speedBump.transform.TransformPoint(localPos));

                foreach (var pt in speedBump.mapWorldPositions)
                {
                    var ptInApollo = HDMapUtil.GetApolloCoordinates(pt, OriginEasting, OriginNorthing, false);
                    lineSegment.point.Add(ptInApollo);
                }
                var speedBumpId = HdId($"speed_bump_{speedBumpList.IndexOf(speedBump)}");
                var overlapId = HdId($"overlap_speed_bump_{speedBumpList.IndexOf(speedBump)}");
                var s = FindSegmentDistOnLane(speedBump.mapWorldPositions, nearestLaneGameObjectSpeedBump[speedBump]);

                var laneOverlapInfo = new HD.LaneOverlapInfo()
                {
                    start_s = s.Item1, // Todo:
                    end_s = s.Item2, // Todo:
                    is_merge = false,
                };

                var objectLane = new HD.ObjectOverlapInfo()
                {
                    id = gameObjectToLane[nearestLaneGameObjectSpeedBump[speedBump]],
                    lane_overlap_info = laneOverlapInfo,
                };

                var objectSpeedBump = new HD.ObjectOverlapInfo()
                {
                    id = speedBumpId,
                    speed_bump_overlap_info = new HD.SpeedBumpOverlapInfo(),
                };

                var overlap = new HD.Overlap()
                {
                    id = overlapId,
                };
                overlap.@object.Add(objectLane);
                overlap.@object.Add(objectSpeedBump);
                Hdmap.overlap.Add(overlap);

                var speedBumpAnnotation = new HD.SpeedBump()
                {
                    id = speedBumpId,
                    

                };
                speedBumpAnnotation.overlap_id.Add(overlapId);
                var position = new HD.Curve();
                var segment = new HD.CurveSegment()
                {
                    line_segment = lineSegment,
                };
                position.segment.Add(segment);
                speedBumpAnnotation.position.Add(position);

                Hdmap.speed_bump.Add(speedBumpAnnotation);
            }
        }

        static float FindDistanceToSegment(Vector2 pt, Vector2 p1, Vector2 p2, out Vector2 closest)
        {
            float dx = p2.x - p1.x;
            float dy = p2.y - p1.y;
            if ((dx == 0) && (dy == 0))
            {
                // It's a point not a line segment.
                closest = p1;
                dx = pt.x - p1.x;
                dy = pt.y - p1.y;
                return Mathf.Sqrt(dx * dx + dy * dy);
            }

            // Calculate the t that minimizes the distance.
            float t = ((pt.x - p1.x) * dx + (pt.y - p1.y) * dy) /
                (dx * dx + dy * dy);

            // See if this represents one of the segment's
            // end points or a point in the middle.
            if (t < 0)
            {
                closest = new Vector2(p1.x, p1.y);
                dx = pt.x - p1.x;
                dy = pt.y - p1.y;
            }
            else if (t > 1)
            {
                closest = new Vector2(p2.x, p2.y);
                dx = pt.x - p2.x;
                dy = pt.y - p2.y;
            }
            else
            {
                closest = new Vector2(p1.x + t * dx, p1.y + t * dy);
                dx = pt.x - closest.x;
                dy = pt.y - closest.y;
            }

            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        Tuple<float, float> FindSegmentDistOnLane(List<Vector3> mapWorldPositions, GameObject lane)
        {
            var laneList = new List<MapLane>();
            laneList.AddRange(MapAnnotationData.GetData<MapLane>());

            var firstPtSegment = new Vector2()
            {
                x = mapWorldPositions[0].x,
                y = mapWorldPositions[0].z,
            };
            var secondPtSegment = new Vector2()
            {
                x = mapWorldPositions[2].x,
                y = mapWorldPositions[2].z,
            };
            var midPtSegment = new Vector2()
            {
                x = mapWorldPositions[1].x,
                y = mapWorldPositions[1].z,
            };

            float startS = 0;
            float totalS = 0;

            foreach (var seg in laneList.Where(seg => seg.gameObject == lane))
            {
                for (int i = 0; i < seg.mapWorldPositions.Count - 1; i++)
                {
                    var firstPtLane = ToVector2(seg.mapWorldPositions[i]);
                    var secondPtLane = ToVector2(seg.mapWorldPositions[i+1]);
                    totalS += Vector2.Distance(firstPtLane, secondPtLane);
                }

                for (int i = 0; i < seg.mapWorldPositions.Count - 1; i++)
                {
                    var firstPtLane = ToVector2(seg.mapWorldPositions[i]);
                    var secondPtLane = ToVector2(seg.mapWorldPositions[i+1]);

                    if (DoIntersect(firstPtSegment, secondPtSegment, firstPtLane, secondPtLane))
                    {
                        var ptOnLane = new Vector2();
                        FindDistanceToSegment(firstPtSegment, firstPtLane, secondPtLane, out ptOnLane);

                        float d1 = Vector2.Distance(firstPtLane, ptOnLane);
                        float d2 = Vector2.Distance(secondPtLane, ptOnLane);
                        startS += d1;

                        break;
                    }
                    else
                    {
                        startS += Vector2.Distance(firstPtLane, secondPtLane);
                    }
                }
                totalS = 0;
            }
            startS = (float)(startS - 0.5);

            return Tuple.Create(startS, startS + 1);
        }

        static Vector2 ToVector2(Vector3 pt)
        {
            return new Vector2(pt.x, pt.z);
        }

        static bool OnSegment(Vector2 p, Vector2 q, Vector2 r)
        {
            if (q.x <= Mathf.Max(p.x, r.x) && q.x >= Mathf.Min(p.x, r.x) &&
                q.y <= Mathf.Max(p.y, r.y) && q.y >= Mathf.Min(p.y, r.y))
                return true;

            return false;
        }

        static int Orientation (Vector2 p, Vector2 q, Vector2 r)
        {
            float val = (q.y - p.y) * (r.x - q.x) -
                        (q.x - p.x) * (r.y - q.y);
            if (val == 0) return 0;

            return (val > 0) ? 1 : 2;
        }

        static bool DoIntersect(Vector2 p1, Vector2 q1, Vector2 p2, Vector2 q2)
        {
            int o1 = Orientation(p1, q1, p2);
            int o2 = Orientation(p1, q1, q2);
            int o3 = Orientation(p2, q2, p1);
            int o4 = Orientation(p2, q2, q1);

            if (o1 != o2 && o3 != o4)
                return true;

            if (o1 == 0 && OnSegment(p1, p2, q1)) return true;

            if (o2 == 0 && OnSegment(p1, q2, q1)) return true;

            if (o3 == 0 && OnSegment(p2, p1, q2)) return true;

            if (o4 == 0 && OnSegment(p2, q1, q2)) return true;

            return false;
        }
        void Export(string filePath)
        {
            using (var fs = File.Create(filePath))
            {
                ProtoBuf.Serializer.Serialize(fs, Hdmap);
            }

            Debug.Log("Successfully generated and exported Apollo HD Map!");
        }
        static HD.Id HdId(string id) => new HD.Id() { id = id };
    }
    public static class Helper
    {
        public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key) where TValue : new()
        {
            TValue val;

            if (!dict.TryGetValue(key, out val))
            {
                val = new TValue();
                dict.Add(key, val);
            }
            return val;
        }
    }
}
