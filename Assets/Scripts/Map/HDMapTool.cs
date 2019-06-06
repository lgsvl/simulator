/**
 * Copyright (c) 2018 LG Electronics, Inc.
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
using static Map.Apollo.HDMapUtil;
using Google.Protobuf;
using HD = global::apollo.hdmap;
using ApolloCommon = global::apollo.common;

namespace apollo.hdmap
{
    public partial class Id
    {
        public override int GetHashCode()
        {
            return id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return id.Equals((obj as Id).id);
        }
    }
}

namespace Map
{
    namespace Apollo
    {
        public class HDMapTool : MapTool
        {
            private MapManager mapManager;

            public float proximity = PROXIMITY;
            public float arrowSize = ARROWSIZE;
            
            //the threshold between stopline and branching point. if a stopline-lane intersect is closer than this to a branching point then this stopline is a braching stopline
            const float stoplineIntersectThreshold = 1.5f;

            public string foldername = "hd_map";
            public string filename = "base_map";

            private float OriginNorthing;
            private float OriginEasting;
            private float Angle;
            private float AltitudeOffset;

            private HD.Map hdmap;

            public enum OverlapType
            {
                Signal_Stopline_Lane,
                Stopsign_Stopline_Lane,
            }

            HDMapTool()
            {
                PROXIMITY = proximity;
                ARROWSIZE = arrowSize;
            }

            void OnEnable()
            {
                if (proximity != PROXIMITY)
                {
                    PROXIMITY = proximity;
                }
                if (arrowSize != ARROWSIZE)
                {
                    ARROWSIZE = arrowSize;
                }
            }

            void OnValidate()
            {
                if (proximity != PROXIMITY)
                {
                    PROXIMITY = proximity;
                }
                if (arrowSize != ARROWSIZE)
                {
                    ARROWSIZE = arrowSize;
                }
            }
            
            public void ExportHDMap()
            {
                mapManager = FindObjectOfType<MapManager>();
                if (mapManager == null)
                {
                    Debug.LogError("Error! No MapManager.cs in scene!");
                    return;
                }

                mapManager.SetMapForExport();

                //use the settings from current tool
                PROXIMITY = proximity;
                ARROWSIZE = arrowSize;

                MapOrigin mapOrigin = FindObjectOfType<MapOrigin>();
                if (mapOrigin == null)
                {
                    Debug.LogError("Error! No MapOrigin.cs in scene!");
                    return;
                }

                OriginEasting = mapOrigin.OriginEasting;
                OriginNorthing = mapOrigin.OriginNorthing;
                Angle = mapOrigin.Angle;
                AltitudeOffset = mapOrigin.AltitudeOffset;
                
                if (Calculate())
                {
                    Export();
                }
            }

            List<MapSegmentBuilder> segBldrs;
            List<HDMapSignalLightBuilder> signalLights;
            List<HDMapStopSignBuilder> stopSigns;
            Dictionary<HD.Id, List<HD.Id>> junctionToOverlaps;
            Dictionary<HD.Id, List<GameObject>> overlapIdToGameObjects;
            Dictionary<GameObject, HD.ObjectOverlapInfo> gameObjectToOverlapInfo;
            Dictionary<GameObject, HD.Id> gameObjectToOverlapId;

            Dictionary<HD.Id, HD.Junction> overlapIdToJunction;
            Dictionary<HD.Id, List<GameObject>> roadIdToLanes;
            Dictionary<GameObject, HD.Id> gameObjectToLane;
            Dictionary<GameObject, HD.Id> laneGameObjectToOverlapId;
            HashSet<GameObject> laneParkingSpace;
            HashSet<GameObject> laneSpeedBump;
            HashSet<MapSegment> allSegs;

            bool Calculate()
            {
                hdmap = new HD.Map()
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

                //initial collection
                segBldrs = new List<MapSegmentBuilder>();
                signalLights = new List<HDMapSignalLightBuilder>();
                stopSigns = new List<HDMapStopSignBuilder>();
                
                junctionToOverlaps = new Dictionary<HD.Id, List<HD.Id>>();
                overlapIdToGameObjects = new Dictionary<HD.Id, List<GameObject>>();
                gameObjectToOverlapInfo = new Dictionary<GameObject, HD.ObjectOverlapInfo>();
                gameObjectToOverlapId = new Dictionary<GameObject, HD.Id>();

                overlapIdToJunction = new Dictionary<HD.Id, HD.Junction>();
                roadIdToLanes = new Dictionary<HD.Id, List<GameObject>>();
                gameObjectToLane = new Dictionary<GameObject, HD.Id>();
                laneGameObjectToOverlapId = new Dictionary<GameObject, HD.Id>();
                laneParkingSpace = new HashSet<GameObject>();
                laneSpeedBump = new HashSet<GameObject>();

                segBldrs.AddRange(mapManager.transform.GetComponentsInChildren<MapSegmentBuilder>());
                signalLights.AddRange(mapManager.transform.GetComponentsInChildren<HDMapSignalLightBuilder>());
                stopSigns.AddRange(mapManager.transform.GetComponentsInChildren<HDMapStopSignBuilder>());
                
                foreach (var segBldr in segBldrs)
                {
                    foreach (var localPos in segBldr.segment.targetLocalPositions)
                    {
                        segBldr.segment.targetWorldPositions.Add(segBldr.transform.TransformPoint(localPos));
                    }
                }

                MakeJunctions();
                MakeInfoOfParkingSpace();
                MakeInfoOfSpeedBump();

                bool missingPoints = false;

                allSegs = new HashSet<MapSegment>(); //All segments regardless of segment actual type

                //connect builder reference for each segment
                foreach (var segBldr in segBldrs)
                {
                    segBldr.segment.builder = segBldr;
                    allSegs.Add(segBldr.segment);
                }

                //Link before and after segment for each segment
                foreach (var segment in allSegs)
                {
                    //Make sure to clear unwanted leftover data from previous generation
                    segment.Clear();
                    segment.hdmapInfo = new HDMapSegmentInfo();

                    if ((segment.builder as MapLaneSegmentBuilder) == null) //consider only lane for now
                    {
                        continue;
                    }

                    //this is to avoid accidentally connect two nearby stoplines
                    if ((segment.builder as MapStopLineSegmentBuilder) != null)
                    {
                        continue;
                    }

                    //each segment must have at least 2 waypoints for calculation so complement to 2 waypoints as needed
                    while (segment.targetLocalPositions.Count < 2)
                    {
                        segment.targetLocalPositions.Add(Vector3.zero);
                        missingPoints = true;
                    }

                    var firstPt = segment.builder.transform.TransformPoint(segment.targetLocalPositions[0]);
                    var lastPt = segment.builder.transform.TransformPoint(segment.targetLocalPositions[segment.targetLocalPositions.Count - 1]);

                    foreach (var segment_cmp in allSegs)
                    {
                        if (segment_cmp.builder.GetType() != segment.builder.GetType()) //only connect same actual type
                        {
                            continue;
                        }

                        var firstPt_cmp = segment_cmp.builder.transform.TransformPoint(segment_cmp.targetLocalPositions[0]);
                        var lastPt_cmp = segment_cmp.builder.transform.TransformPoint(segment_cmp.targetLocalPositions[segment_cmp.targetLocalPositions.Count - 1]);

                        if ((firstPt - lastPt_cmp).magnitude < PROXIMITY / exportScaleFactor)
                        {
                            segment_cmp.targetLocalPositions[segment_cmp.targetLocalPositions.Count - 1] = segment_cmp.builder.transform.InverseTransformPoint(firstPt);
                            segment.befores.Add(segment_cmp);
                        }

                        if ((lastPt - firstPt_cmp).magnitude < PROXIMITY / exportScaleFactor)
                        {
                            segment_cmp.targetLocalPositions[0] = segment_cmp.builder.transform.InverseTransformPoint(lastPt);
                            segment.afters.Add(segment_cmp);
                        }
                    }
                }

                if (missingPoints)
                {
                    Debug.Log("Some segment has less than 2 waypoints, map generation aborts");
                    return false;
                }

                var allLnSegs = new HashSet<MapSegment>();

                foreach (var seg in allSegs)
                {
                    var type = seg.builder.GetType();
                    if (type == typeof(MapLaneSegmentBuilder))
                    {
                        allLnSegs.Add(seg);
                    }
                }

                //check validity of lane segment builder relationship but it won't warn you if have A's right lane to be null and B's left lane to be null
                {
                    foreach (var seg in allLnSegs)
                    {
                        var lnSegBldr = (MapLaneSegmentBuilder)(seg.builder);
                        if (lnSegBldr.leftForward == null)
                        {
                            continue;
                        }
                        if (lnSegBldr.leftForward != null && lnSegBldr != lnSegBldr.leftForward?.rightForward
                            ||
                            lnSegBldr.rightForward != null && lnSegBldr != lnSegBldr.rightForward?.leftForward)
                        {
                            Debug.Log("Some lane segments neighbor relationships are wrong, map generation aborts.");
#if UNITY_EDITOR
                            UnityEditor.Selection.activeObject = lnSegBldr.gameObject;
                            Debug.Log("One probelmatic lane was selected.");
#endif
                            return false;
                        }
                    }
                }

                foreach (var lnSeg in allLnSegs)
                {
                    foreach (var localPos in lnSeg.targetLocalPositions)
                    {
                        lnSeg.targetWorldPositions.Add(lnSeg.builder.transform.TransformPoint(localPos)); //Convert to world position
                    }

                    var lnBuilder = (MapLaneSegmentBuilder)(lnSeg.builder);

                    lnSeg.hdmapInfo.speedLimit = lnBuilder.speedLimit;
                    lnSeg.hdmapInfo.leftNeighborSegmentForward = lnBuilder.leftForward?.segment;
                    lnSeg.hdmapInfo.rightNeighborSegmentForward = lnBuilder.rightForward?.segment;
                    lnSeg.hdmapInfo.leftNeighborSegmentReverse = lnBuilder.leftReverse?.segment;
                    lnSeg.hdmapInfo.rightNeighborSegmentReverse = lnBuilder.rightReverse?.segment;
                    lnSeg.hdmapInfo.leftBoundType = lnBuilder.leftBoundType;
                    lnSeg.hdmapInfo.rightBoundType = lnBuilder.rightBoundType;
                    lnSeg.hdmapInfo.laneTurn = lnBuilder.laneTurn;
                }

                //build virtual connection lanes
                var bridgeVirtualLnSegs = new List<MapSegment>();
                foreach (var lnSeg in allLnSegs)
                {
                    if (lnSeg.afters.Count > 0)
                    {
                        foreach (var aftrLn in lnSeg.afters)
                        {
                            bridgeVirtualLnSegs.Add(new MapSegment()
                            {                   
                                hdmapInfo = new HDMapSegmentInfo() { id = null },
                                builder = null,
                                targetLocalPositions = null,
                                befores = new List<MapSegment>() { lnSeg },
                                afters = new List<MapSegment>() { aftrLn },
                                targetWorldPositions = new List<Vector3>()
                                {
                                    lnSeg.targetWorldPositions[lnSeg.targetWorldPositions.Count - 1],
                                    aftrLn.targetWorldPositions[0]
                                }
                            });
                        }
                    }
                }

                //lanes
                //assign ids
                int laneId = 0;
                foreach (var lnSeg in allLnSegs)
                {
                    lnSeg.hdmapInfo.id = $"lane_{laneId}";
                    ++laneId;
                }

                //function to get neighbor lanes in the same road
                System.Func<MapSegment, bool, List<MapSegment>> GetNeighborForwardRoadLanes = null;
                GetNeighborForwardRoadLanes = delegate (MapSegment self, bool fromLeft)
                {
                    if (self == null)
                    {
                        return new List<MapSegment>();
                    }
                    
                    if (fromLeft)
                    {
                        if (self.hdmapInfo.leftNeighborSegmentForward == null)
                        {
                            return new List<MapSegment>();
                        }
                        else
                        {
                            var ret = new List<MapSegment>();
                            ret.AddRange(GetNeighborForwardRoadLanes(self.hdmapInfo.leftNeighborSegmentForward, true));
                            ret.Add(self.hdmapInfo.leftNeighborSegmentForward);
                            return ret;
                        }
                    }
                    else
                    {
                        if (self.hdmapInfo.rightNeighborSegmentForward == null)
                        {
                            return new List<MapSegment>();
                        }
                        else
                        {
                            var ret = new List<MapSegment>();
                            ret.AddRange(GetNeighborForwardRoadLanes(self.hdmapInfo.rightNeighborSegmentForward, false));
                            ret.Add(self.hdmapInfo.rightNeighborSegmentForward);
                            return ret;
                        }
                    }
                };

                HashSet<HD.Road> roadSet = new HashSet<HD.Road>();

                var visitedLanes = new Dictionary<MapSegment, HD.Road>();

                {
                    foreach (var lnSeg in allLnSegs)
                    {
                        if (visitedLanes.ContainsKey(lnSeg))
                        {
                            continue;
                        }

                        var lefts = GetNeighborForwardRoadLanes(lnSeg, true);
                        var rights = GetNeighborForwardRoadLanes(lnSeg, false);

                        var roadLanes = new List<MapSegment>();
                        roadLanes.AddRange(lefts);
                        roadLanes.Add(lnSeg);
                        rights.Reverse();
                        roadLanes.AddRange(rights);

                        var roadSection = new HD.RoadSection()
                        {
                            id = HdId($"1"),
                            boundary = null,
                        };
                        
                        foreach(var mapSegment in roadLanes)
                        {
                            roadSection.lane_id.Add(new HD.Id()
                            {
                                id = mapSegment.hdmapInfo.id
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
                            gameObjectsOfLanes.Add(lane.builder.gameObject);
                        }
                        roadIdToLanes.Add(road.id,gameObjectsOfLanes);
                    }
                }

                //config lanes
                foreach (var lnSeg in allLnSegs)
                {
                    var centerPts = new List<ApolloCommon.PointENU>();
                    var lBndPts = new List<ApolloCommon.PointENU>();
                    var rBndPts = new List<ApolloCommon.PointENU>();

                    var worldPoses = lnSeg.targetWorldPositions;
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
                        }

                        centerPts.Add(GetApolloCoordinates(curPt, OriginEasting, OriginNorthing, Angle, false));
                        lBndPts.Add(GetApolloCoordinates(lPoint, OriginEasting, OriginNorthing, Angle, false));
                        rBndPts.Add(GetApolloCoordinates(rPoint, OriginEasting, OriginNorthing, Angle, false));

                    }
                    for (int i = 0; i < worldPoses.Count; i++)
                    {
                        if (i > 0)
                        {
                            lLength += (leftBoundPoses[i] - leftBoundPoses[i - 1]).magnitude;
                            rLength += (rightBoundPoses[i] - rightBoundPoses[i - 1]).magnitude;
                        }
                    }

                    var predecessor_ids = new List<HD.Id>();
                    var successor_ids = new List<HD.Id>();
                    predecessor_ids.AddRange(lnSeg.befores.Select(seg => HdId(seg.hdmapInfo.id)));
                    successor_ids.AddRange(lnSeg.afters.Select(seg => HdId(seg.hdmapInfo.id)));

                    var lane = new HD.Lane()
                    {
                        id = HdId(lnSeg.hdmapInfo.id),
                        central_curve = new HD.Curve(),
                        left_boundary = new HD.LaneBoundary(),
                        right_boundary = new HD.LaneBoundary(),
                        length = mLength,
                        speed_limit = lnSeg.hdmapInfo.speedLimit,
                        type = HD.Lane.LaneType.CITY_DRIVING,
                        turn = lnSeg.hdmapInfo.laneTurn,
                        direction = HD.Lane.LaneDirection.FORWARD,
                    };

                    // Make dictionary for parking space and laneId
                    gameObjectToLane.Add(lnSeg.builder.gameObject, lane.id);

                    if (gameObjectToOverlapId.ContainsKey(lnSeg.builder.gameObject)) 
                    {
                        var overlap_id = gameObjectToOverlapId[lnSeg.builder.gameObject];
                        lane.overlap_id.Add(overlap_id);
                        
                        var objectId = HdId(lnSeg.hdmapInfo.id);

                        gameObjectToOverlapInfo[lnSeg.builder.gameObject].id = objectId;
                        gameObjectToOverlapInfo[lnSeg.builder.gameObject].lane_overlap_info.start_s = 0.1;
                        gameObjectToOverlapInfo[lnSeg.builder.gameObject].lane_overlap_info.end_s = mLength;
                        gameObjectToOverlapInfo[lnSeg.builder.gameObject].lane_overlap_info.is_merge = false;
                    }

                    if (laneParkingSpace.Contains(lnSeg.builder.gameObject))
                    {
                        // Todo: needs multiple objects which has same key.
                        var overlap_id = laneGameObjectToOverlapId[lnSeg.builder.gameObject];
                        lane.overlap_id.Add(overlap_id);
                    }

                    if (laneSpeedBump.Contains(lnSeg.builder.gameObject))
                    {
                        // Todo: needs multiple objects which has same key.
                        var overlap_id = laneGameObjectToOverlapId[lnSeg.builder.gameObject];
                        lane.overlap_id.Add(overlap_id);
                    }

                    hdmap.lane.Add(lane);

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

                    leftLaneBoundaryType.types.Add(lnSeg.hdmapInfo.leftBoundType);

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

                    //
                    var rightBoundaryType = new List<HD.LaneBoundaryType>();
                    var rightLaneBoundaryType = new HD.LaneBoundaryType();

                    rightLaneBoundaryType.types.Add(lnSeg.hdmapInfo.rightBoundType);
                    rightBoundaryType.Add(rightLaneBoundaryType);
                    //


                    var right_boundary_segment = new HD.LaneBoundary()
                    {
                        curve = new HD.Curve(),
                        length = rLength,
                        @virtual = true,
                    };
                    right_boundary_segment.boundary_type.AddRange(rightBoundaryType);
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
                    if (lnSeg.hdmapInfo.leftNeighborSegmentForward != null)
                        lane.left_neighbor_forward_lane_id.AddRange(new List<HD.Id>() { HdId(lnSeg.hdmapInfo.leftNeighborSegmentForward.hdmapInfo.id), } );
                    if (lnSeg.hdmapInfo.rightNeighborSegmentForward != null)
                        lane.right_neighbor_forward_lane_id.AddRange(new List<HD.Id>() { HdId(lnSeg.hdmapInfo.rightNeighborSegmentForward.hdmapInfo.id), } );
                    if (lnSeg.hdmapInfo.leftNeighborSegmentReverse != null)
                        lane.left_neighbor_reverse_lane_id.AddRange(new List<HD.Id>() { HdId(lnSeg.hdmapInfo.leftNeighborSegmentReverse.hdmapInfo.id), } );
                    if (lnSeg.hdmapInfo.rightNeighborSegmentReverse != null)
                        lane.right_neighbor_reverse_lane_id.AddRange(new List<HD.Id>() { HdId(lnSeg.hdmapInfo.rightNeighborSegmentReverse.hdmapInfo.id), } );
                    
                    if (lnSeg.hdmapInfo.leftNeighborSegmentForward == null || lnSeg.hdmapInfo.rightNeighborSegmentForward == null)
                    {
                        var road = visitedLanes[lnSeg];
                        roadSet.Remove(road);

                        var section = road.section[0];

                        lineSegment = new HD.LineSegment();
                        if (lnSeg.hdmapInfo.leftNeighborSegmentForward == null) 
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
                                type = lnSeg.hdmapInfo.leftNeighborSegmentForward == null ? HD.BoundaryEdge.Type.LEFT_BOUNDARY : HD.BoundaryEdge.Type.RIGHT_BOUNDARY,
                            };
                            boundaryEdge.curve.segment.Add(new HD.CurveSegment()
                            {
                                line_segment = lineSegment,
                            });
                            edges.Add(boundaryEdge);
                        }

                        lineSegment = new HD.LineSegment();
                        // Cases that a Road only has one lane, adds rightBoundary
                        if (lnSeg.hdmapInfo.leftNeighborSegmentForward == null && lnSeg.hdmapInfo.rightNeighborSegmentForward == null)
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

                    foreach(var lane in roadIdToLanes[road.id])
                    {
                        if (gameObjectToOverlapId.ContainsKey(lane))
                        {
                            var overlap_id = gameObjectToOverlapId[lane];
                            var junction = overlapIdToJunction[overlap_id];
                            road.junction_id = junction.id;
                        }
                    }
                }

                hdmap.road.AddRange(roadSet);

                //for backtracking what overlaps are related to a specific lane
                var laneIds2OverlapIdsMapping = new Dictionary<HD.Id, List<HD.Id>>();

                //setup signals and lane_signal overlaps
                foreach (var signalLight in signalLights)
                {
                    //signal id
                    int signal_Id = hdmap.signal.Count;

                    //construct boundry points
                    var bounds = signalLight.Get2DBounds();
                    List<ApolloCommon.PointENU> signalBoundPts = new List<ApolloCommon.PointENU>()
                    {
                        GetApolloCoordinates(bounds.Item1, OriginEasting, OriginNorthing, AltitudeOffset, Angle),
                        GetApolloCoordinates(bounds.Item2, OriginEasting, OriginNorthing, AltitudeOffset, Angle),
                        GetApolloCoordinates(bounds.Item3, OriginEasting, OriginNorthing, AltitudeOffset, Angle),
                        GetApolloCoordinates(bounds.Item4, OriginEasting, OriginNorthing, AltitudeOffset, Angle)
                    };

                    //sub signals
                    List<HD.Subsignal> subsignals = null;
                    if (signalLight.signalDatas.Count > 0)
                    {
                        subsignals = new List<HD.Subsignal>();
                        for (int i = 0; i < signalLight.signalDatas.Count; i++)
                        {
                            var lightData = signalLight.signalDatas[i];
                            subsignals.Add( new HD.Subsignal()
                            {
                                id = HdId(i.ToString()),
                                type = HD.Subsignal.Type.CIRCLE,
                                location = GetApolloCoordinates(signalLight.transform.TransformPoint(lightData.localPosition), OriginEasting, OriginNorthing, AltitudeOffset, Angle),
                            });
                        }           
                    }

                    //keep track of all overlaps this signal created
                    List<HD.Id> overlap_ids = new List<HD.Id>();

                    //stopline points
                    List<ApolloCommon.PointENU> stoplinePts = null;
                    var stopline = signalLight.hintStopline;
                    if (stopline != null && stopline.segment.targetLocalPositions.Count > 1)
                    {
                        stoplinePts = new List<ApolloCommon.PointENU>();
                        List<MapSegment> lanesToInspec = new List<MapSegment>();
                        lanesToInspec.AddRange(allLnSegs);
                        lanesToInspec.AddRange(bridgeVirtualLnSegs);

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
                            type = HD.Signal.Type.MIX_3_VERTICAL,
                            boundary = boundary,
                        };

                        signal.subsignal.AddRange(subsignals);

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
                        hdmap.signal.Add(signal);
                    }
                }

                //setup stopsigns and lane_stopsign overlaps
                foreach (var stopSign in stopSigns)
                {
                    //stopsign id
                    int stopsign_Id = hdmap.stop_sign.Count;

                    //keep track of all overlaps this stopsign created
                    List<HD.Id> overlap_ids = new List<HD.Id>();

                    //stopline points
                    List<ApolloCommon.PointENU> stoplinePts = null;
                    var stopline = stopSign.stopline;
                    if (stopline != null && stopline.segment.targetLocalPositions.Count > 1)
                    {
                        stoplinePts = new List<ApolloCommon.PointENU>();
                        List<MapSegment> lanesToInspec = new List<MapSegment>();
                        lanesToInspec.AddRange(allLnSegs);
                        lanesToInspec.AddRange(bridgeVirtualLnSegs);

                        if (!MakeStoplineLaneOverlaps(stopline, lanesToInspec, stoplineWidth, stopsign_Id, OverlapType.Stopsign_Stopline_Lane, stoplinePts, laneIds2OverlapIdsMapping, overlap_ids))
                        {
                            return false;
                        } 
                    }

                    if (stoplinePts != null && stoplinePts.Count > 2)
                    {
                        var stopId = HdId($"stopsign_{stopsign_Id}");

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
                        hdmap.stop_sign.Add(hdStopSign);
                    }
                }

                //backtrack and fill missing information for lanes
                for (int i = 0; i < hdmap.lane.Count; i++)
                {
                    HD.Id land_id = (HD.Id)(hdmap.lane[i].id);
                    var oldLane = hdmap.lane[i];
                    if (laneIds2OverlapIdsMapping.ContainsKey(land_id))
                        oldLane.overlap_id.AddRange(laneIds2OverlapIdsMapping[(HD.Id)(hdmap.lane[i].id)]);
                    hdmap.lane[i] = oldLane;
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
                        hdmap.overlap.Add(overlap);
                    }
                }

                MakeParkingSpaceAnnotation();
                MakeSpeedBumpAnnotation();

                return true;
            }

            bool MakeStoplineLaneOverlaps(MapStopLineSegmentBuilder stopline, List<MapSegment> lanesToInspec, float stoplineWidth, int overlapInfoId, OverlapType overlapType, List<ApolloCommon.PointENU> stoplinePts, Dictionary<HD.Id, List<HD.Id>> laneId2OverlapIdsMapping, List<HD.Id> overlap_ids)
            {
                stopline.segment.targetWorldPositions = new List<Vector3>(stopline.segment.targetLocalPositions.Count);
                List<Vector2> stopline2D = new List<Vector2>();

                for (int i = 0; i < stopline.segment.targetLocalPositions.Count; i++)
                {
                    var worldPos = stopline.segment.builder.transform.TransformPoint(stopline.segment.targetLocalPositions[i]);
                    stopline.segment.targetWorldPositions.Add(worldPos); //to worldspace here
                    stopline2D.Add(new Vector2(worldPos.x, worldPos.z));
                    stoplinePts.Add(GetApolloCoordinates(worldPos, OriginEasting, OriginNorthing, Angle, false));
                }

                var considered = new HashSet<MapSegment>(); //This is to prevent conceptually or practically duplicated overlaps

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
                    var lane2D = seg.targetWorldPositions.Select(p => new Vector2(p.x, p.z)).ToList();
                    bool isIntersected = Utils.CurveSegmentsIntersect(stopline2D, lane2D, out intersects);
                    if (isIntersected)
                    {
                        Vector2 intersect = intersects[0];

                        if (intersects.Count > 1)
                        {
                            //determin if is cluster
                            Vector2 avgPt = Vector2.zero;
                            float maxRadius = proximity;
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
                                Debug.LogWarning("stopline is not common to have more than one non-cluster intersect point with a lane, abort calculation");
                                return false;
                            }
                        }

                        float totalLength;
                        float s = Utils.GetNearestSCoordinate(intersect, lane2D, out totalLength);

                        var segments = new List<MapSegment>();
                        var lengths = new List<float>();

                        if (totalLength - s < stoplineIntersectThreshold && seg.afters.Count > 0)
                        {
                            s = 0;
                            foreach (var afterSeg in seg.afters)
                            {
                                segments.Add(afterSeg);
                                lengths.Add(Utils.GetCurveLength(afterSeg.targetWorldPositions.Select(p => new Vector2(p.x, p.z)).ToList()));
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
                            var overlap_id = HdId($"{overlap_id_prefix}{hdmap.overlap.Count}");
                            var lane_id = HdId(segment.hdmapInfo.id);

                            laneId2OverlapIdsMapping.GetOrCreate(lane_id).Add(overlap_id);

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
                            hdmap.overlap.Add(overlap);
                            overlap_ids.Add(overlap_id);
                        }
                    }
                }
                return true;
            }

            void MakeJunctions()
            {
                var intersections = new List<MapIntersectionBuilder>();
                intersections.AddRange(mapManager.transform.GetComponentsInChildren<MapIntersectionBuilder>());
                foreach(var intersection in intersections)
                {
                    var junctionList = intersection.transform.GetComponentsInChildren<MapJunctionBuilder>().ToList();
                    foreach(var junction in junctionList)
                    {
                        var polygon = new HD.Polygon();

                        foreach (var localPos in junction.segment.targetLocalPositions)
                            junction.segment.targetWorldPositions.Add(junction.transform.TransformPoint(localPos));

                        foreach (var pt in junction.segment.targetWorldPositions)
                        {
                            var ptInApollo = GetApolloCoordinates(pt, OriginEasting, OriginNorthing, Angle, false);
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
                        var laneList = intersection.transform.GetComponentsInChildren<MapLaneSegmentBuilder>().ToList();
                        foreach(var lane in laneList)
                        {
                            var overlapId = HdId($"overlap_junction_{intersections.IndexOf(intersection)}_{junctionList.IndexOf(junction)}_lane_{intersections.IndexOf(intersection)}_{laneList.IndexOf(lane)}");
                            var objectOverlapInfo = new HD.ObjectOverlapInfo()
                            {
                                lane_overlap_info = new HD.LaneOverlapInfo(),
                            };

                            junctionToOverlaps.GetOrCreate(junctionId).Add(overlapId);
                            overlapIdToGameObjects.GetOrCreate(overlapId).Add(lane.gameObject);
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
                            overlapIdToJunction.Add(overlapId, j);
                            
                            junctionOverlapIds.Add(overlapId);
                        }

                        // StopSign
                        var stopSignList = intersection.transform.GetComponentsInChildren<MapStopLineSegmentBuilder>().ToList();
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
                            if (!junctionToOverlaps.ContainsKey(junctionId))
                                junctionToOverlaps.Add(junctionId, overlapIds);
                            else
                                junctionToOverlaps[junctionId].Add(overlapId);

                            var stopSignObjList = new List<GameObject>();
                            stopSignObjList.Add(stopSign.gameObject);
                            if (!overlapIdToGameObjects.ContainsKey(overlapId))
                                overlapIdToGameObjects.Add(overlapId, stopSignObjList);
                            else
                                overlapIdToGameObjects[overlapId].Add(stopSign.gameObject);

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

                            if (!overlapIdToJunction.ContainsKey(overlapId))
                                overlapIdToJunction.Add(overlapId, j);
                            overlapIdToJunction[overlapId] = j;

                            junctionOverlapIds.Add(overlapId);
                        }
                        
                        // SignalLight
                        var signalList = intersection.transform.GetComponentsInChildren<HDMapSignalLightBuilder>().ToList();
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
                            if (!junctionToOverlaps.ContainsKey(junctionId))
                                junctionToOverlaps.Add(junctionId, overlapIds);
                            else
                                junctionToOverlaps[junctionId].Add(overlapId);

                            var signalObjList = new List<GameObject>();
                            signalObjList.Add(signal.gameObject);
                            if (!overlapIdToGameObjects.ContainsKey(overlapId))
                                overlapIdToGameObjects.Add(overlapId, signalObjList);
                            else
                                overlapIdToGameObjects[overlapId].Add(signal.gameObject);

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

                            if (!overlapIdToJunction.ContainsKey(overlapId))
                                overlapIdToJunction.Add(overlapId, j);
                            overlapIdToJunction[overlapId] = j;
                            
                            junctionOverlapIds.Add(overlapId);
                        }

                        hdmap.junction.Add(new HD.Junction()
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
                var parkingSpaceList = new List<MapParkingSpaceBuilder>();
                parkingSpaceList.AddRange(mapManager.transform.GetComponentsInChildren<MapParkingSpaceBuilder>());

                var laneList = new List<MapLaneSegmentBuilder>();
                laneList.AddRange(mapManager.transform.GetComponentsInChildren<MapLaneSegmentBuilder>());

                double dist = double.MaxValue;
                foreach (var parkingSpace in parkingSpaceList)
                {
                    foreach (var localPos in parkingSpace.segment.targetLocalPositions)
                    {
                        parkingSpace.segment.targetWorldPositions.Add(parkingSpace.transform.TransformPoint(localPos));
                    }

                    dist = double.MaxValue;

                    GameObject nearestLaneGameObject = null;
                    foreach (var lane in laneList)
                    {       
                        var p1 = new Vector2(lane.segment.targetWorldPositions.First().x, lane.segment.targetWorldPositions.First().z);
                        var p2 = new Vector2(lane.segment.targetWorldPositions.Last().x, lane.segment.targetWorldPositions.Last().z);
                        var pt = new Vector2(parkingSpace.transform.position.x, parkingSpace.transform.position.z);
                        var closestPt = new Vector2();
                        double d = FindDistanceToSegment(pt, p1, p2, out closestPt);

                        if (dist > d)
                        {
                            dist = d;
                            nearestLaneGameObject = lane.segment.builder.gameObject;
                        }
                    }
                    var overlapId = HdId($"overlap_parking_space_{parkingSpaceList.IndexOf(parkingSpace)}");
                    parkingSpace.nearestLaneGameObject = nearestLaneGameObject;
                    laneGameObjectToOverlapId.Add(nearestLaneGameObject, overlapId);
                    laneParkingSpace.Add(nearestLaneGameObject);
                }
            }

            void MakeParkingSpaceAnnotation()
            {
                var parkingSpaceList = new List<MapParkingSpaceBuilder>();
                parkingSpaceList.AddRange(mapManager.transform.GetComponentsInChildren<MapParkingSpaceBuilder>());

                foreach (var parkingSpace in parkingSpaceList)
                {
                    var polygon = new HD.Polygon();
                    foreach (var localPos in parkingSpace.segment.targetLocalPositions)
                       parkingSpace.segment.targetWorldPositions.Add(parkingSpace.transform.TransformPoint(localPos));
                    var vector = (parkingSpace.segment.targetWorldPositions[1] - parkingSpace.segment.targetWorldPositions[2]).normalized;
                    var heading = Mathf.Atan(vector.z / vector.x);
                    heading = (heading < 0) ? (float)(heading + Mathf.PI * 2) : heading;

                    foreach (var pt in parkingSpace.segment.targetWorldPositions)
                    {
                        var ptInApollo = GetApolloCoordinates(pt, OriginEasting, OriginNorthing, Angle, false);
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
                        id = gameObjectToLane[parkingSpace.nearestLaneGameObject],
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
                    hdmap.overlap.Add(overlap);

                    var ParkingSpaceAnnotation = new HD.ParkingSpace()
                    {
                        id = parkingSpaceId,
                        polygon = polygon,
                        heading = heading,
                    };
                    ParkingSpaceAnnotation.overlap_id.Add(overlapId);
                    hdmap.parking_space.Add(ParkingSpaceAnnotation);
                }
            }

            void MakeInfoOfSpeedBump()
            {
                var speedBumpList = new List<MapSpeedBumpBuilder>();
                speedBumpList.AddRange(mapManager.transform.GetComponentsInChildren<MapSpeedBumpBuilder>());

                var laneList = new List<MapLaneSegmentBuilder>();
                laneList.AddRange(mapManager.transform.GetComponentsInChildren<MapLaneSegmentBuilder>());

                foreach (var speedBump in speedBumpList)
                {
                    foreach (var localPos in speedBump.segment.targetLocalPositions)
                    {
                        speedBump.segment.targetWorldPositions.Add(speedBump.transform.TransformPoint(localPos));
                    }

                    foreach (var lane in laneList)
                    {       
                        var p1 = new Vector2(lane.segment.targetWorldPositions.First().x, lane.segment.targetWorldPositions.First().z);
                        var p2 = new Vector2(lane.segment.targetWorldPositions.Last().x, lane.segment.targetWorldPositions.Last().z);
                        var q1 = new Vector2(speedBump.segment.targetWorldPositions[0].x, speedBump.segment.targetWorldPositions[0].z);
                        var q2 = new Vector2(speedBump.segment.targetWorldPositions[2].x, speedBump.segment.targetWorldPositions[2].z);

                        if (doIntersect(p1, p2, q1, q2))
                        {
                            speedBump.nearestLaneGameObject = lane.segment.builder.gameObject;
                        }
                    }
                    var overlapId = HdId($"overlap_speed_bump_{speedBumpList.IndexOf(speedBump)}");
                    laneGameObjectToOverlapId.Add(speedBump.nearestLaneGameObject, overlapId);
                    laneSpeedBump.Add(speedBump.nearestLaneGameObject);
                }
            }

            void MakeSpeedBumpAnnotation()
            {
                var speedBumpList = new List<MapSpeedBumpBuilder>();
                speedBumpList.AddRange(mapManager.transform.GetComponentsInChildren<MapSpeedBumpBuilder>());

                foreach (var speedBump in speedBumpList)
                {
                    var lineSegment = new HD.LineSegment();
                    foreach(var localPos in speedBump.segment.targetLocalPositions)
                        speedBump.segment.targetWorldPositions.Add(speedBump.transform.TransformPoint(localPos));

                    foreach (var pt in speedBump.segment.targetWorldPositions)
                    {
                        var ptInApollo = GetApolloCoordinates(pt, OriginEasting, OriginNorthing, Angle, false);
                        lineSegment.point.Add(ptInApollo);
                    }
                    var speedBumpId = HdId($"speed_bump_{speedBumpList.IndexOf(speedBump)}");
                    var overlapId = HdId($"overlap_speed_bump_{speedBumpList.IndexOf(speedBump)}");
                    var s = FindSegmentDistOnLane(speedBump.segment, speedBump.nearestLaneGameObject);

                    var laneOverlapInfo = new HD.LaneOverlapInfo()
                    {
                        start_s = s.Item1, // Todo:
                        end_s = s.Item2, // Todo:
                        is_merge = false,
                    };

                    var objectLane = new HD.ObjectOverlapInfo()
                    {
                        id = gameObjectToLane[speedBump.nearestLaneGameObject],
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
                    hdmap.overlap.Add(overlap);

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

                    hdmap.speed_bump.Add(speedBumpAnnotation);
                }
            }

            float FindDistanceToSegment(Vector2 pt, Vector2 p1, Vector2 p2, out Vector2 closest)
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

            Tuple<float, float> FindSegmentDistOnLane(MapSegment segment, GameObject lane)
            {
                var laneList = new List<MapLaneSegmentBuilder>();
                laneList.AddRange(mapManager.transform.GetComponentsInChildren<MapLaneSegmentBuilder>());

                var firstPtSegment = new Vector2()
                {
                    x = segment.targetWorldPositions[0].x,
                    y = segment.targetWorldPositions[0].z,
                };
                var secondPtSegment = new Vector2()
                {
                    x = segment.targetWorldPositions[2].x,
                    y = segment.targetWorldPositions[2].z,
                };
                var midPtSegment = new Vector2()
                {
                    x = segment.targetWorldPositions[1].x,
                    y = segment.targetWorldPositions[1].z,
                };

                float startS = 0;
                float totalS = 0;

                foreach (var seg in laneList.Where(seg => seg.gameObject == lane))
                {
                    for (int i = 0; i < seg.segment.targetWorldPositions.Count - 1; i++)
                    {
                        var firstPtLane = ToVector2(seg.segment.targetWorldPositions[i]);
                        var secondPtLane = ToVector2(seg.segment.targetWorldPositions[i+1]);
                        totalS += dist(firstPtLane, secondPtLane);
                    }

                    for (int i = 0; i < seg.segment.targetWorldPositions.Count - 1; i++)
                    {
                        var firstPtLane = ToVector2(seg.segment.targetWorldPositions[i]);
                        var secondPtLane = ToVector2(seg.segment.targetWorldPositions[i+1]);

                        if (doIntersect(firstPtSegment, secondPtSegment, firstPtLane, secondPtLane))
                        {
                            var ptOnLane = new Vector2();
                            FindDistanceToSegment(firstPtSegment, firstPtLane, secondPtLane, out ptOnLane);

                            float d1 = dist(firstPtLane, ptOnLane);
                            float d2 = dist(secondPtLane, ptOnLane);
                            startS += d1;

                            break;
                        }
                        else
                        {
                            startS += dist(firstPtLane, secondPtLane);
                        }
                    }
                    totalS = 0;
                }
                startS = (float)(startS - 0.5);

                return Tuple.Create(startS, startS + 1);
            }

            Vector2 ToVector2(Vector3 pt)
            {
                return new Vector2(pt.x, pt.z);
            }
            Vector2 pointENUToVec2(ApolloCommon.PointENU pt)
            {
                return new Vector2((float)pt.x, (float)pt.y);
            }

            float dist(Vector2 pt1, Vector2 pt2)
            {
                return Mathf.Sqrt(Mathf.Pow(pt1.x - pt2.x, 2) + Mathf.Pow(pt1.y - pt2.y, 2));
            }
            float dist(Vector3 pt1, Vector3 pt2)
            {
                return Mathf.Sqrt(Mathf.Pow(pt1.x - pt2.x, 2) + Mathf.Pow(pt1.z - pt2.z, 2));
            }

            bool onSegment(Vector2 p, Vector2 q, Vector2 r)
            {
                if (q.x <= Mathf.Max(p.x, r.x) && q.x >= Mathf.Min(p.x, r.x) &&
                    q.y <= Mathf.Max(p.y, r.y) && q.y >= Mathf.Min(p.y, r.y))
                    return true;

                return false;
            }

            int orientation (Vector2 p, Vector2 q, Vector2 r)
            {
                float val = (q.y - p.y) * (r.x - q.x) -
                            (q.x - p.x) * (r.y - q.y);
                if (val == 0) return 0;

                return (val > 0) ? 1 : 2;
            }

            bool doIntersect(Vector2 p1, Vector2 q1, Vector2 p2, Vector2 q2)
            {
                int o1 = orientation(p1, q1, p2);
                int o2 = orientation(p1, q1, q2);
                int o3 = orientation(p2, q2, p1);
                int o4 = orientation(p2, q2, q1);

                if (o1 != o2 && o3 != o4)
                    return true;

                if (o1 == 0 && onSegment(p1, p2, q1)) return true;

                if (o2 == 0 && onSegment(p1, q2, q1)) return true;

                if (o3 == 0 && onSegment(p2, p1, q2)) return true;

                if (o4 == 0 && onSegment(p2, q1, q2)) return true;

                return false;
            }
            void Export()
            {
                var filepath_txt = $"{foldername}{Path.DirectorySeparatorChar}{filename}.txt";
                var filepath_bin = $"{foldername}{Path.DirectorySeparatorChar}{filename}.bin";

                System.IO.Directory.CreateDirectory(foldername);
                System.IO.File.Delete(filepath_txt);
                System.IO.File.Delete(filepath_bin);

                using (var fs = File.Create(filepath_bin))
                {
                    ProtoBuf.Serializer.Serialize(fs, hdmap);
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
}
