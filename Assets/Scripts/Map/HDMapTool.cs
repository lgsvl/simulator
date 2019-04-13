/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using static Map.Apollo.HDMapUtil;
using Google.Protobuf;
using Google.Protobuf.Collections;

using HD = global::Apollo.Hdmap;
using ApolloCommon = global::Apollo.Common;

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

            bool Calculate()
            {
                hdmap = new HD.Map()
                {
                    Header = new HD.Header()
                    {
                        Version = ByteString.CopyFromUtf8("1.500000"),
                        Date = ByteString.CopyFromUtf8("2018-03-23T13:27:54"),
                        Projection = new HD.Projection()
                        {
                            Proj = "+proj=utm +zone=10 +ellps=WGS84 +datum=WGS84 +units=m +no_defs",
                        },
                        District = ByteString.CopyFromUtf8("0"),
                        RevMajor = ByteString.CopyFromUtf8("1"),
                        RevMinor = ByteString.CopyFromUtf8("0"),
                        Left = -121.982277,
                        Top = 37.398079,
                        Right = -121.971998,
                        Bottom = 37.398079,
                        Vendor = ByteString.CopyFromUtf8("LGSVL"),
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

                MakeJunctions();
                MakeInfoOfParkingSpace();
                segBldrs.AddRange(mapManager.transform.GetComponentsInChildren<MapSegmentBuilder>());
                signalLights.AddRange(mapManager.transform.GetComponentsInChildren<HDMapSignalLightBuilder>());
                stopSigns.AddRange(mapManager.transform.GetComponentsInChildren<HDMapStopSignBuilder>());
                
                bool missingPoints = false;

                var allSegs = new HashSet<MapSegment>(); //All segments regardless of segment actual type

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
                            segment.befores.Add(segment_cmp);
                        }

                        if ((lastPt - firstPt_cmp).magnitude < PROXIMITY / exportScaleFactor)
                        {
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
                        
                        var lane_ids = new List<HD.Id>();
                        foreach(var mapSegment in roadLanes)
                        {
                            lane_ids.Add(new HD.Id() 
                            {
                                Id_ = mapSegment.hdmapInfo.id
                            });
                        };
                        
                        var roadSection = new HD.RoadSection()
                        {
                            Id = HdId($"1"),
                            Boundary = null,
                        };
                        roadSection.LaneId.Add(lane_ids);

                        var road = new HD.Road()
                        {
                            Id = HdId($"road_{roadSet.Count}"),
                            JunctionId = null,
                        };
                        road.Section.Add(roadSection);

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
                        roadIdToLanes.Add(road.Id,gameObjectsOfLanes);
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
                        S = 0,
                        Width = laneHalfWidth,
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
                                S = mLength,
                                Width = laneHalfWidth,
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
                        Id = HdId(lnSeg.hdmapInfo.id),

                        CentralCurve = new HD.Curve(),
                        LeftBoundary = new HD.LaneBoundary(),
                        RightBoundary = new HD.LaneBoundary(),
                        Length = mLength,
                        SpeedLimit = lnSeg.hdmapInfo.speedLimit,
                        Type = HD.Lane.Types.LaneType.CityDriving,
                        Turn = lnSeg.hdmapInfo.laneTurn,
                        Direction = HD.Lane.Types.LaneDirection.Forward,
                    };

                    // Make dictionary for parking space and laneId
                    gameObjectToLane.Add(lnSeg.builder.gameObject, lane.Id);

                    if (gameObjectToOverlapId.ContainsKey(lnSeg.builder.gameObject)) 
                    {
                        var overlap_id = gameObjectToOverlapId[lnSeg.builder.gameObject];
                        lane.OverlapId.Add(overlap_id);
                        
                        var objectId = HdId(lnSeg.hdmapInfo.id);

                        gameObjectToOverlapInfo[lnSeg.builder.gameObject].Id = objectId;
                        gameObjectToOverlapInfo[lnSeg.builder.gameObject].LaneOverlapInfo.StartS = 0.1;
                        gameObjectToOverlapInfo[lnSeg.builder.gameObject].LaneOverlapInfo.EndS = mLength;
                        gameObjectToOverlapInfo[lnSeg.builder.gameObject].LaneOverlapInfo.IsMerge = false;
                    }

                    if (laneParkingSpace.Contains(lnSeg.builder.gameObject))
                    {
                        var overlap_id = laneGameObjectToOverlapId[lnSeg.builder.gameObject];
                        lane.OverlapId.Add(overlap_id);
                    }

                    hdmap.Lane.Add(lane);

                    // CentralCurve
                    var lineSegment = new HD.LineSegment();
                    lineSegment.Point.Add(centerPts);
                    
                    var central_curve_segment = new List<HD.CurveSegment>()
                    {
                        new HD.CurveSegment()
                        {
                            LineSegment = new HD.LineSegment(lineSegment),
                            S = 0,
                            StartPosition = new ApolloCommon.PointENU()
                            {
                                X = centerPts[0].X,
                                Y = centerPts[0].Y,
                                Z = centerPts[0].Z,
                            },
                            Length = mLength,
                        },
                    };
                    lane.CentralCurve.Segment.AddRange(central_curve_segment);
                    // /CentralCurve

                    // LeftBoundary
                    var curveSegment = new HD.CurveSegment()
                    {
                        LineSegment = new HD.LineSegment(),
                        S = 0,
                        StartPosition = lBndPts[0],
                        Length = lLength,
                    };

                    curveSegment.LineSegment.Point.Add(lBndPts);

                    var leftBoundaryType = new List<HD.LaneBoundaryType>();
                    var leftLaneBoundaryType = new HD.LaneBoundaryType()
                    {
                        S = 0,
                    };

                    leftLaneBoundaryType.Types_.Add(lnSeg.hdmapInfo.leftBoundType);
                    leftBoundaryType.Add(leftLaneBoundaryType);

                    var left_boundary_segment = new HD.LaneBoundary()
                    {
                        Curve = new HD.Curve(),
                        Length = lLength,
                        Virtual = true,
                    };
                    left_boundary_segment.BoundaryType.Add(leftBoundaryType);
                    left_boundary_segment.Curve.Segment.Add(curveSegment);
                    lane.LeftBoundary = left_boundary_segment;
                    // /LeftBoundary
                    
                    // RightBoundary
                    curveSegment = new HD.CurveSegment()
                    {
                        LineSegment = new HD.LineSegment(),
                        S = 0,
                        StartPosition = lBndPts[0],
                        Length = lLength,
                    };

                    curveSegment.LineSegment.Point.Add(rBndPts);

                    //
                    var rightBoundaryType = new List<HD.LaneBoundaryType>();
                    var rightLaneBoundaryType = new HD.LaneBoundaryType();

                    rightLaneBoundaryType.Types_.Add(lnSeg.hdmapInfo.rightBoundType);
                    rightBoundaryType.Add(rightLaneBoundaryType);
                    //


                    var right_boundary_segment = new HD.LaneBoundary()
                    {
                        Curve = new HD.Curve(),
                        Length = rLength,
                        Virtual = true,
                    };
                    right_boundary_segment.BoundaryType.Add(rightBoundaryType);
                    right_boundary_segment.Curve.Segment.Add(curveSegment);
                    lane.RightBoundary = right_boundary_segment;
                    // /RightBoundary

                    if (predecessor_ids.Count > 0)
                        lane.PredecessorId.Add(predecessor_ids);

                    if (successor_ids.Count > 0)
                        lane.SuccessorId.Add(successor_ids);

                    lane.LeftSample.Add(associations);
                    lane.LeftRoadSample.Add(associations);
                    lane.RightSample.Add(associations);
                    lane.RightRoadSample.Add(associations);
                    if (lnSeg.hdmapInfo.leftNeighborSegmentForward != null)
                        lane.LeftNeighborForwardLaneId.Add(new List<HD.Id>() { HdId(lnSeg.hdmapInfo.leftNeighborSegmentForward.hdmapInfo.id), } );
                    if (lnSeg.hdmapInfo.rightNeighborSegmentForward != null)
                        lane.RightNeighborForwardLaneId.Add(new List<HD.Id>() { HdId(lnSeg.hdmapInfo.rightNeighborSegmentForward.hdmapInfo.id), } );
                    if (lnSeg.hdmapInfo.leftNeighborSegmentReverse != null)
                        lane.LeftNeighborReverseLaneId.Add(new List<HD.Id>() { HdId(lnSeg.hdmapInfo.leftNeighborSegmentReverse.hdmapInfo.id), } );
                    if (lnSeg.hdmapInfo.rightNeighborSegmentReverse != null)
                        lane.RightNeighborReverseLaneId.Add(new List<HD.Id>() { HdId(lnSeg.hdmapInfo.rightNeighborSegmentReverse.hdmapInfo.id), } );
                    
                    if (lnSeg.hdmapInfo.leftNeighborSegmentForward == null || lnSeg.hdmapInfo.rightNeighborSegmentForward == null)
                    {
                        var road = visitedLanes[lnSeg];
                        roadSet.Remove(road);

                        var section = road.Section[0];

                        lineSegment = new HD.LineSegment();
                        if (lnSeg.hdmapInfo.leftNeighborSegmentForward == null) 
                            lineSegment.Point.Add(lBndPts);
                        else
                            lineSegment.Point.Add(rBndPts);

                        var edges = new List<HD.BoundaryEdge>();
                        if (section.Boundary?.OuterPolygon?.Edge == null)
                        {
                            var boundaryEdge = new HD.BoundaryEdge()
                            {
                                Curve = new HD.Curve(),
                                Type = lnSeg.hdmapInfo.leftNeighborSegmentForward == null ? HD.BoundaryEdge.Types.Type.LeftBoundary : HD.BoundaryEdge.Types.Type.RightBoundary,
                            };
                            boundaryEdge.Curve.Segment.Add(new HD.CurveSegment()
                            {
                                LineSegment = new HD.LineSegment(lineSegment),
                            });
                            edges.Add(boundaryEdge);
                        }

                        section.Boundary = new HD.RoadBoundary()
                        {
                            OuterPolygon = new HD.BoundaryPolygon(),
                        };
                        section.Boundary.OuterPolygon.Edge.Add(edges);
                        road.Section[0] = section;
                        roadSet.Add(road);
                    }
                }

                foreach(var road in roadSet)
                {
                    foreach(var lane in roadIdToLanes[road.Id])
                    {
                        if (gameObjectToOverlapId.ContainsKey(lane))
                        {
                            var overlap_id = gameObjectToOverlapId[lane];
                            var junction = overlapIdToJunction[overlap_id];
                            road.JunctionId = junction.Id;
                        }
                    }
                }

                hdmap.Road.Add(roadSet);

                //for backtracking what overlaps are related to a specific lane
                var laneIds2OverlapIdsMapping = new Dictionary<HD.Id, List<HD.Id>>();

                //setup signals and lane_signal overlaps
                foreach (var signalLight in signalLights)
                {
                    //signal id
                    int signal_Id = hdmap.Signal.Count;

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
                                Id = HdId(i.ToString()),
                                Type = HD.Subsignal.Types.Type.Circle,
                                Location = GetApolloCoordinates(signalLight.transform.TransformPoint(lightData.localPosition), OriginEasting, OriginNorthing, AltitudeOffset, Angle),
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
                        boundary.Point.AddRange(signalBoundPts);

                        var signalId = HdId($"signal_{signal_Id}");
                        var signal = new HD.Signal()
                        {
                            Id = signalId,
                            Type = HD.Signal.Types.Type.Mix3Vertical,
                            Boundary = boundary,
                        };

                        var subSignal = new RepeatedField<HD.Subsignal>();
                        subSignal.Add(subsignals);
                        signal.Subsignal.Add(subsignals);

                        var overlapIds = new RepeatedField<HD.Id>();
                        if (overlap_ids.Count >= 1)
                            overlapIds.Add(overlap_ids);

                        if (gameObjectToOverlapId.ContainsKey(signalLight.gameObject))
                        {
                            var signalOverlapInfo = new HD.ObjectOverlapInfo()
                            {
                                Id = signalId,
                                SignalOverlapInfo = new HD.SignalOverlapInfo(),
                            };
                            gameObjectToOverlapInfo[signalLight.gameObject] = signalOverlapInfo; 
                            overlapIds.Add(gameObjectToOverlapId[signalLight.gameObject]);
                        }
                        signal.OverlapId.Add(overlapIds);

                        var curveSegment = new List<HD.CurveSegment>();
                        var lineSegment = new HD.LineSegment();
                        lineSegment.Point.AddRange(stoplinePts);
                        curveSegment.Add(new HD.CurveSegment()
                        {
                            LineSegment = new HD.LineSegment(lineSegment)
                        });

                        var stopLine = new HD.Curve();
                        stopLine.Segment.Add(curveSegment);
                        signal.StopLine.Add(stopLine);
                        hdmap.Signal.Add(signal);
                    }
                }

                //setup stopsigns and lane_stopsign overlaps
                foreach (var stopSign in stopSigns)
                {
                    //stopsign id
                    int stopsign_Id = hdmap.StopSign.Count;

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

                        var overlapIds = new RepeatedField<HD.Id>();
                        if (overlap_ids.Count >= 1)
                            overlapIds.Add(overlap_ids);
                        
                        var overlapId = new HD.Id();
                        // stop sign and junction overlap.
                        if (gameObjectToOverlapId.ContainsKey(stopSign.gameObject))
                        {
                            var stopOverlapInfo = new HD.ObjectOverlapInfo()
                            {
                                Id = stopId,
                                StopSignOverlapInfo = new HD.StopSignOverlapInfo(),
                            };
                            gameObjectToOverlapInfo[stopSign.gameObject] = stopOverlapInfo; 
                            overlapIds.Add(gameObjectToOverlapId[stopSign.gameObject]);
                        }

                        var curveSegment = new List<HD.CurveSegment>();
                        var lineSegment = new HD.LineSegment();

                        lineSegment.Point.AddRange(stoplinePts);

                        curveSegment.Add(new HD.CurveSegment()
                        {
                            LineSegment = new HD.LineSegment(lineSegment),
                        });

                        var stopLine = new HD.Curve();
                        stopLine.Segment.Add(curveSegment);

                        var hdStopSign = new HD.StopSign()
                        {
                            Id = stopId,
                            Type = HD.StopSign.Types.StopType.Unknown,
                        };
                        hdStopSign.OverlapId.Add(overlapIds);

                        hdStopSign.StopLine.Add(stopLine);
                        hdmap.StopSign.Add(hdStopSign);
                    }
                }

                //backtrack and fill missing information for lanes
                for (int i = 0; i < hdmap.Lane.Count; i++)
                {
                    HD.Id land_id = (HD.Id)(hdmap.Lane[i].Id);
                    var oldLane = hdmap.Lane[i];
                    if (laneIds2OverlapIdsMapping.ContainsKey(land_id))
                        oldLane.OverlapId.Add(laneIds2OverlapIdsMapping[(HD.Id)(hdmap.Lane[i].Id)]);
                    hdmap.Lane[i] = oldLane;
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
                            Id = objHdMapId,
                        };

                        overlap.Object.Add(objectOverlapInfo);
                        var objectOverlapInfo1 = new HD.ObjectOverlapInfo()
                        {

                            Id = overlapIdToJunction[overlap_id].Id,
                            JunctionOverlapInfo = new HD.JunctionOverlapInfo(),
                        };

                        overlap.Object.Add(objectOverlapInfo1);
                        hdmap.Overlap.Add(overlap);
                    }
                }

                MakeParkingSpaceAnnotation();

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
                            var overlap_id = HdId($"{overlap_id_prefix}{hdmap.Overlap.Count}");
                            var lane_id = HdId(segment.hdmapInfo.id);

                            laneId2OverlapIdsMapping.GetOrCreate(lane_id).Add(overlap_id);

                            HD.ObjectOverlapInfo objOverlapInfo = new HD.ObjectOverlapInfo();

                            if (overlapType == OverlapType.Signal_Stopline_Lane)
                            {
                                objOverlapInfo = new HD.ObjectOverlapInfo()
                                {
                                    Id = HdId($"signal_{overlapInfoId}"),
                                    SignalOverlapInfo = new HD.SignalOverlapInfo(),
                                };
                            }
                            else if (overlapType == OverlapType.Stopsign_Stopline_Lane)
                            {
                                objOverlapInfo = new HD.ObjectOverlapInfo()
                                {
                                    Id = HdId($"stopsign_{overlapInfoId}"),
                                    StopSignOverlapInfo = new HD.StopSignOverlapInfo(),
                                };
                            }

                            var object_overlap = new List<HD.ObjectOverlapInfo>()
                            {
                                new HD.ObjectOverlapInfo()
                                {
                                    Id = lane_id,
                                    LaneOverlapInfo = new HD.LaneOverlapInfo()
                                    {
                                        StartS = ln_start_s,
                                        EndS = ln_end_s,
                                        IsMerge = false,
                                    },
                                },
                                objOverlapInfo,
                            };

                            var overlap = new HD.Overlap()
                            {
                                Id = overlap_id,
                            };
                            overlap.Object.Add(object_overlap);
                            hdmap.Overlap.Add(overlap);
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
                            polygon.Point.Add(new ApolloCommon.PointENU()
                            {
                                X = ptInApollo.X,
                                Y = ptInApollo.Y,
                                Z = ptInApollo.Z,
                            });
                        }

                        var junctionId = HdId($"junction_{intersections.IndexOf(intersection)}_{junctionList.IndexOf(junction)}");
                        var junctionOverlapIds = new RepeatedField<HD.Id>();

                        // LaneSegment
                        var laneList = intersection.transform.GetComponentsInChildren<MapLaneSegmentBuilder>().ToList();
                        foreach(var lane in laneList)
                        {
                            var overlapId = HdId($"overlap_junction_{intersections.IndexOf(intersection)}_{junctionList.IndexOf(junction)}_lane_{intersections.IndexOf(intersection)}_{laneList.IndexOf(lane)}");
                            var objectOverlapInfo = new HD.ObjectOverlapInfo()
                            {
                                LaneOverlapInfo = new HD.LaneOverlapInfo(),
                            };

                            junctionToOverlaps.GetOrCreate(junctionId).Add(overlapId);
                            overlapIdToGameObjects.GetOrCreate(overlapId).Add(lane.gameObject);
                            gameObjectToOverlapInfo.GetOrCreate(lane.gameObject).LaneOverlapInfo = new HD.LaneOverlapInfo();
                            gameObjectToOverlapId.Add(lane.gameObject, overlapId);
                            
                            var junctionOverlapInfo = new HD.ObjectOverlapInfo()
                            {
                                Id = junctionId,
                                JunctionOverlapInfo = new HD.JunctionOverlapInfo(),
                            };

                            if (!gameObjectToOverlapInfo.ContainsKey(junction.gameObject))
                                gameObjectToOverlapInfo.Add(junction.gameObject, junctionOverlapInfo);

                            var j = new HD.Junction()
                            {   
                                Id = junctionId,
                                Polygon = polygon,
                            };
                            j.OverlapId.Add(overlapId);
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
                                StopSignOverlapInfo = new HD.StopSignOverlapInfo(),
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
                                Id = junctionId,
                                JunctionOverlapInfo = new HD.JunctionOverlapInfo(),
                            };

                            gameObjectToOverlapInfo[junction.gameObject] = junctionOverlapInfo;

                            var j = new HD.Junction()
                            {   
                                Id = new HD.Id(junctionId),
                                Polygon = polygon,
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
                                SignalOverlapInfo = new HD.SignalOverlapInfo(),
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
                                Id = junctionId,
                                JunctionOverlapInfo = new HD.JunctionOverlapInfo(),
                            };

                            gameObjectToOverlapInfo[junction.gameObject] = junctionOverlapInfo;

                            var j = new HD.Junction()
                            {   
                                Id = junctionId,
                                Polygon = polygon,
                            };

                            if (!overlapIdToJunction.ContainsKey(overlapId))
                                overlapIdToJunction.Add(overlapId, j);
                            overlapIdToJunction[overlapId] = j;
                            
                            junctionOverlapIds.Add(overlapId);
                        }

                        hdmap.Junction.Add(new HD.Junction()
                        {
                            Id = junctionId,
                            Polygon = polygon,
                        });
                        var hdJunction = new HD.Junction();
                        hdJunction.OverlapId.Add(junctionOverlapIds);
                    }
                }
            }

            void MakeInfoOfParkingSpace()
            {
                var parkingSpaceList = new List<MapParkingSpaceBuilder>();
                parkingSpaceList.AddRange(mapManager.transform.GetComponentsInChildren<MapParkingSpaceBuilder>());

                var laneList = new List<MapLaneSegmentBuilder>();
                laneList.AddRange(mapManager.transform.GetComponentsInChildren<MapLaneSegmentBuilder>());

                // lane's target world positions
                foreach (var lane in laneList)
                {
                    foreach (var localPos in lane.segment.targetLocalPositions)
                    {
                        lane.segment.targetWorldPositions.Add(lane.transform.TransformPoint(localPos));
                    }
                }
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
                        polygon.Point.Add(new ApolloCommon.PointENU()
                        {
                            X = ptInApollo.X,
                            Y = ptInApollo.Y,
                            Z = ptInApollo.Z,
                        });
                    }
                    var parkingSpaceId = HdId($"parking_space_{parkingSpaceList.IndexOf(parkingSpace)}");

                    // Overlap
                    var overlapId = HdId($"overlap_parking_space_{parkingSpaceList.IndexOf(parkingSpace)}");

                    // Overlap lane
                    var laneOverlapInfo = new HD.LaneOverlapInfo()
                    {
                        StartS = 0,
                        EndS = 0,
                        IsMerge = false,
                    };

                    var objectLane = new HD.ObjectOverlapInfo()
                    {
                        Id = gameObjectToLane[parkingSpace.nearestLaneGameObject],
                        LaneOverlapInfo = laneOverlapInfo,
                    };

                    var objectParkingSpace = new HD.ObjectOverlapInfo()
                    {
                        Id = parkingSpaceId,
                        ParkingSpaceOverlapInfo = new HD.ParkingSpaceOverlapInfo(),
                    };

                    var overlap = new HD.Overlap()
                    {
                        Id = overlapId,
                    };
                    overlap.Object.Add(objectLane);
                    overlap.Object.Add(objectParkingSpace);
                    hdmap.Overlap.Add(overlap);

                    var ParkingSpaceAnnotation = new HD.ParkingSpace()
                    {
                        Id = parkingSpaceId,
                        Polygon = polygon,
                        Heading = heading,
                    };
                    ParkingSpaceAnnotation.OverlapId.Add(overlapId);
                    hdmap.ParkingSpace.Add(ParkingSpaceAnnotation);
                }
            }

            double FindDistanceToSegment(Vector2 pt, Vector2 p1, Vector2 p2, out Vector2 closest)
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

            void Export()
            {
                var filepath_txt = $"{foldername}{Path.DirectorySeparatorChar}{filename}.txt";
                var filepath_bin = $"{foldername}{Path.DirectorySeparatorChar}{filename}.bin";

                System.IO.Directory.CreateDirectory(foldername);
                System.IO.File.Delete(filepath_txt);
                System.IO.File.Delete(filepath_bin);

                using (var fs = File.Create(filepath_bin))
                {
                    hdmap.WriteTo(fs);
                }

                Debug.Log("Successfully generated and exported Apollo HD Map!");
            }
            static HD.Id HdId(string id) => new HD.Id() { Id_ = id };
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
