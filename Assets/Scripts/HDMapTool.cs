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

namespace Map
{
    namespace Apollo
    {
        public class HDMapTool : MapTool
        {
            public List<Transform> targets;

            public float proximity = PROXIMITY;
            public float arrowSize = ARROWSIZE;
            
            //the threshold between stopline and branching point. if a stopline-lane intersect is closer than this to a branching point then this stopline is a braching stopline
            const float stoplineIntersectThreshold = 1.5f;

            public string foldername = "hd_map";
            public string filename = "base_map.txt";

            public float OriginNorthing = 4182486.0f;
            public float OriginEasting = 552874.0f;

            private Map.Apollo.HDMap hdmap;

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
                if (Calculate())
                {
                    Export();
                }
            }
            
            bool Calculate()
            {
                //BuildTestMap();
                hdmap = new Map.Apollo.HDMap();

                //HD map top level elements
                var lanes = new List<Lane>();
                var roads = new List<Road>();
                var signals = new List<Signal>();
                var stop_signs = new List<StopSign>();
                var overlaps = new List<Overlap>();

                const float laneHalfWidth = 1.75f; //temp solution
                const float stoplineWidth = 0.7f;

                //list of target transforms
                var targetList = new List<Transform>();
                var noTarget = true;
                foreach (var t in targets)
                {
                    if (t != null)
                    {
                        noTarget = false;
                        targetList.Add(t);
                    }
                }
                if (noTarget)
                {
                    targetList.Add(transform);
                }

                //initial collection
                var segBldrs = new List<MapSegmentBuilder>();
                var signalLights = new List<HDMapSignalLight>();
                var stopSigns = new List<HDMapStopSign>();

                foreach (var t in targetList)
                {
                    if (t == null)
                    {
                        continue;
                    }

                    segBldrs.AddRange(t.GetComponentsInChildren<MapSegmentBuilder>());
                    signalLights.AddRange(t.GetComponentsInChildren<HDMapSignalLight>());
                    stopSigns.AddRange(t.GetComponentsInChildren<HDMapStopSign>());
                }

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

                //check validity of lane segment builder relationship
                {
                    bool valid = true;
                    var visited = new HashSet<MapLaneSegmentBuilder>();
                    foreach (var seg in allLnSegs)
                    {
                        var lnSegBldr = (MapLaneSegmentBuilder)(seg.builder);
                        if (visited.Contains(lnSegBldr))
                        {
                            continue;
                        }
                        visited.Add(lnSegBldr);
                        if (lnSegBldr.leftNeighborForward == null)
                        {
                            continue;
                        }
                        if (lnSegBldr == lnSegBldr.leftNeighborForward.rightNeighborForward)
                        {
                            visited.Add(lnSegBldr.leftNeighborForward);
                        }
                        else
                        {
                            valid = false;
                            break;
                        }
                    }

                    if (!valid)
                    {
                        Debug.Log("Some lane segments neighbor relationships are wrong, map generation aborts");
                        return false;
                    }
                }

                foreach (var lnSeg in allLnSegs)
                {
                    foreach (var localPos in lnSeg.targetLocalPositions)
                    {
                        lnSeg.targetWorldPositions.Add(lnSeg.builder.transform.TransformPoint(localPos)); //Convert to world position
                    }
                    var lnBuilder = (MapLaneSegmentBuilder)(lnSeg.builder);

                    lnSeg.hdmapInfo.leftNeighborSegmentForward = lnBuilder.leftNeighborForward?.segment;
                    lnSeg.hdmapInfo.rightNeighborSegmentForward = lnBuilder.rightNeighborForward?.segment;
                    lnSeg.hdmapInfo.leftNeighborSegmentReverse = lnBuilder.leftNeighborReverse?.segment;
                    lnSeg.hdmapInfo.rightNeighborSegmentReverse = lnBuilder.rightNeighborReverse?.segment;
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

                HashSet<Road> roadSet = new HashSet<Road>();

                var visitedLanes = new Dictionary<MapSegment, Road>();
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

                        var road = new Road()
                        {
                            id = $"road_{roadSet.Count}",
                            section = new List<RoadSection>()
                            {
                                new RoadSection()
                                {
                                     id = 1,
                                     lane_id = roadLanes.Select(l => new Id(l.hdmapInfo.id)).ToList(),
                                     boundary = null,
                                }
                            },
                            junction_id = null,
                        };

                        roadSet.Add(road);

                        foreach (var l in roadLanes)
                        {
                            visitedLanes.Add(l, road);
                        }
                    }
                }

                //config lanes
                foreach (var lnSeg in allLnSegs)
                {
                    var centerPts = new List<Ros.PointENU>();
                    var lBndPts = new List<Ros.PointENU>();
                    var rBndPts = new List<Ros.PointENU>();

                    var worldPoses = lnSeg.targetWorldPositions;
                    var leftBoundPoses = new List<Vector3>();
                    var rightBoundPoses = new List<Vector3>();

                    float mLength = 0;
                    float lLength = 0;
                    float rLength = 0;

                    List<LaneSampleAssociation> associations = new List<LaneSampleAssociation>();
                    associations.Add(new LaneSampleAssociation()
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

                        Vector3 lPoint = -Vector3.Cross(tangFwd, Vector3.up) * laneHalfWidth + curPt;
                        Vector3 rPoint = Vector3.Cross(tangFwd, Vector3.up) * laneHalfWidth + curPt;

                        leftBoundPoses.Add(lPoint);
                        rightBoundPoses.Add(rPoint);

                        if (i > 0)
                        {
                            mLength += (curPt - worldPoses[i - 1]).magnitude;
                            associations.Add(new LaneSampleAssociation()
                            {
                                s = mLength,
                                width = laneHalfWidth,
                            });
                        }

                        centerPts.Add(GetApolloCoordinates(curPt, OriginEasting, OriginNorthing, false));
                        lBndPts.Add(GetApolloCoordinates(lPoint, OriginEasting, OriginNorthing, false));
                        rBndPts.Add(GetApolloCoordinates(rPoint, OriginEasting, OriginNorthing, false));

                    }
                    for (int i = 0; i < worldPoses.Count; i++)
                    {
                        if (i > 0)
                        {
                            lLength += (leftBoundPoses[i] - leftBoundPoses[i - 1]).magnitude;
                            rLength += (rightBoundPoses[i] - rightBoundPoses[i - 1]).magnitude;
                        }
                    }

                    var predecessor_ids = new List<Id>();
                    var successor_ids = new List<Id>();
                    foreach (var ls in lnSeg.befores)
                    {
                        predecessor_ids.Add(new Id(ls.hdmapInfo.id));
                    }
                    foreach (var ls in lnSeg.afters)
                    {
                        successor_ids.Add(new Id(ls.hdmapInfo.id));
                    }

                    lanes.Add(new Lane()
                    {
                        id = new Id(lnSeg.hdmapInfo.id),
                        central_curve = new Curve()
                        {
                            segment = new List<CurveSegment>()
                            {
                                new CurveSegment()
                                {
                                    curve_type = new CurveSegment.CurveType_OneOf()
                                    {
                                        line_segment = new LineSegment()
                                        {
                                            point = centerPts
                                        }
                                    },
                                    s = 0,
                                    start_position = centerPts[0],
                                    length = mLength
                                }
                            }
                        },
                        left_boundary = new LaneBoundary()
                        {
                            curve = new Curve()
                            {
                                segment = new List<CurveSegment>()
                                {
                                    new CurveSegment()
                                    {
                                        curve_type = new CurveSegment.CurveType_OneOf()
                                        {
                                            line_segment = new LineSegment()
                                            {
                                                point = lBndPts
                                            }
                                        },
                                        s = 0,
                                        start_position = lBndPts[0],
                                        length = lLength
                                    },
                                },
                            },
                            length = lLength,
                            @virtual = true,
                            boundary_type = new List<LaneBoundaryType>()
                            {
                                new LaneBoundaryType()
                                {
                                    s = 0,
                                    types = new List<LaneBoundaryType.Type>()
                                    {
                                        lnSeg.hdmapInfo.leftBoundType,
                                    }
                                }
                            }
                        },
                        right_boundary = new LaneBoundary()
                        {
                            curve = new Curve()
                            {
                                segment = new List<CurveSegment>()
                                {
                                    new CurveSegment()
                                    {
                                        curve_type = new CurveSegment.CurveType_OneOf()
                                        {
                                            line_segment = new LineSegment()
                                            {
                                                point = rBndPts
                                            }
                                        },
                                        s = 0,
                                        start_position = rBndPts[0],
                                        length = rLength
                                    },
                                },
                            },
                            length = rLength,
                            @virtual = true,
                            boundary_type = new List<LaneBoundaryType>()
                            {
                                new LaneBoundaryType()
                                {
                                    s = 0,
                                    types = new List<LaneBoundaryType.Type>()
                                    {
                                       lnSeg.hdmapInfo.rightBoundType,
                                    }
                                }
                            }
                        },
                        length = mLength,
                        speed_limit = 20,
                        predecessor_id = predecessor_ids.Count > 0 ? predecessor_ids : null,
                        successor_id = successor_ids.Count > 0 ? successor_ids : null,
                        type = Lane.LaneType.CITY_DRIVING,
                        turn = lnSeg.hdmapInfo.laneTurn,
                        direction = Lane.LaneDirection.FORWARD,
                        left_sample = associations,
                        right_sample = associations,
                        left_neighbor_forward_lane_id = lnSeg.hdmapInfo.leftNeighborSegmentForward == null ? null : new List<Id>() { lnSeg.hdmapInfo.leftNeighborSegmentForward.hdmapInfo.id },
                        right_neighbor_forward_lane_id = lnSeg.hdmapInfo.rightNeighborSegmentForward == null ? null : new List<Id>() { lnSeg.hdmapInfo.rightNeighborSegmentForward.hdmapInfo.id },
                        left_neighbor_reverse_lane_id = lnSeg.hdmapInfo.leftNeighborSegmentReverse == null ? null : new List<Id>() { lnSeg.hdmapInfo.leftNeighborSegmentReverse.hdmapInfo.id },
                        right_neighbor_reverse_lane_id = lnSeg.hdmapInfo.rightNeighborSegmentReverse == null ? null : new List<Id>() { lnSeg.hdmapInfo.rightNeighborSegmentReverse.hdmapInfo.id }
                    });

                    if (lnSeg.hdmapInfo.leftNeighborSegmentForward == null || lnSeg.hdmapInfo.rightNeighborSegmentForward == null)
                    {
                        var road = visitedLanes[lnSeg];
                        roadSet.Remove(road);

                        var section = road.section[0];

                        var edges = new List<BoundaryEdge>();
                        edges.AddRange(section.boundary?.outer_polygon?.edge ?? new List<BoundaryEdge>());                        
                        edges.Add(new BoundaryEdge()
                        {
                            curve = new Curve()
                            {
                                segment = new List<CurveSegment>()
                                {
                                    new CurveSegment()
                                    {
                                        curve_type = new CurveSegment.CurveType_OneOf()
                                        {
                                            line_segment = new LineSegment()
                                            {
                                                point = lnSeg.hdmapInfo.leftNeighborSegmentForward == null ? lBndPts : rBndPts
                                            },
                                        },
                                    },
                                },
                            },
                            type = lnSeg.hdmapInfo.leftNeighborSegmentForward == null ? BoundaryEdge.Type.LEFT_BOUNDARY : BoundaryEdge.Type.RIGHT_BOUNDARY,
                        });

                        section.boundary = new RoadBoundary()
                        {
                            outer_polygon = new BoundaryPolygon()
                            {
                                edge = edges,
                            },
                            hole = null,
                        };

                        road.section[0] = section;

                        roadSet.Add(road);
                    }
                }

                roads = roadSet.ToList();

                //for backtracking what overlaps are related to a specific lane
                var laneIds2OverlapIdsMapping = new Dictionary<Id, List<Id>>();

                //setup signals and lane_signal overlaps
                foreach (var signalLight in signalLights)
                {
                    //signal id
                    int signal_Id = signals.Count;

                    //construct boundry points
                    var bounds = signalLight.Get2DBounds();
                    List<Ros.PointENU> signalBoundPts = new List<Ros.PointENU>()
                    {
                        GetApolloCoordinates(bounds.Item1, OriginEasting, OriginNorthing),
                        GetApolloCoordinates(bounds.Item2, OriginEasting, OriginNorthing),
                        GetApolloCoordinates(bounds.Item3, OriginEasting, OriginNorthing),
                        GetApolloCoordinates(bounds.Item4, OriginEasting, OriginNorthing)
                    };

                    //sub signals
                    List<Subsignal> subsignals = null;
                    if (signalLight.signalDatas.Count > 0)
                    {
                        subsignals = new List<Subsignal>();
                        for (int i = 0; i < signalLight.signalDatas.Count; i++)
                        {
                            var lightData = signalLight.signalDatas[i];
                            subsignals.Add( new Subsignal()
                            {
                                id = i,
                                type = Subsignal.Type.CIRCLE,
                                location = GetApolloCoordinates(signalLight.transform.TransformPoint(lightData.localPosition), OriginEasting, OriginNorthing),
                            });
                        }
                    }

                    //keep track of all overlaps this signal created
                    List<Id> overlap_ids = new List<Id>();

                    //stopline points
                    List<Ros.PointENU> stoplinePts = null;
                    var stopline = signalLight.hintStopline;
                    if (stopline != null && stopline.segment.targetLocalPositions.Count > 1)
                    {
                        stoplinePts = new List<Ros.PointENU>();
                        List<MapSegment> lanesToInspec = new List<MapSegment>();
                        lanesToInspec.AddRange(allLnSegs);
                        lanesToInspec.AddRange(bridgeVirtualLnSegs);

                        if (!MakeStoplineLaneOverlaps(stopline, lanesToInspec, stoplineWidth, signal_Id, OverlapType.Signal_Stopline_Lane, ref stoplinePts, ref laneIds2OverlapIdsMapping, ref overlap_ids, ref overlaps))
                        {
                            return false;
                        }                  
                    }

                    if (stoplinePts != null && stoplinePts.Count > 2)
                    {
                        signals.Add(new Signal()
                        {
                            id = $"signal_{signal_Id}",
                            boundary = new Polygon()
                            {
                                point = signalBoundPts,
                            },
                            subsignal = subsignals,
                            overlap_id = overlap_ids.Count > 1 ? overlap_ids : null, //backtrack and fill reverse link
                            type = Signal.Type.MIX_3_VERTICAL,
                            stop_line = new List<Curve>()
                            {
                                new Curve()
                                {
                                    segment = new List<CurveSegment>()
                                    {
                                        new CurveSegment()
                                        {
                                            curve_type = new CurveSegment.CurveType_OneOf()
                                            {
                                                line_segment = new LineSegment()
                                                {
                                                    point = stoplinePts,
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        });
                    }
                }

                //setup stopsigns and lane_stopsign overlaps
                foreach (var stopSign in stopSigns)
                {
                    //stopsign id
                    int stopsign_Id = stop_signs.Count;

                    //keep track of all overlaps this stopsign created
                    List<Id> overlap_ids = new List<Id>();

                    //stopline points
                    List<Ros.PointENU> stoplinePts = null;
                    var stopline = stopSign.stopline;
                    if (stopline != null && stopline.segment.targetLocalPositions.Count > 1)
                    {
                        stoplinePts = new List<Ros.PointENU>();
                        List<MapSegment> lanesToInspec = new List<MapSegment>();
                        lanesToInspec.AddRange(allLnSegs);
                        lanesToInspec.AddRange(bridgeVirtualLnSegs);

                        if (!MakeStoplineLaneOverlaps(stopline, lanesToInspec, stoplineWidth, stopsign_Id, OverlapType.Stopsign_Stopline_Lane, ref stoplinePts, ref laneIds2OverlapIdsMapping, ref overlap_ids, ref overlaps))
                        {
                            return false;
                        } 
                    }

                    if (stoplinePts != null && stoplinePts.Count > 2)
                    {
                        stop_signs.Add(new StopSign()
                        {
                            id = $"stopsign_{stopsign_Id}",
                            overlap_id = overlap_ids.Count > 1 ? overlap_ids : null, //backtrack and fill reverse link;
                            stop_line = new List<Curve>()
                            {
                                new Curve()
                                {
                                    segment = new List<CurveSegment>()
                                    {
                                        new CurveSegment()
                                        {
                                            curve_type = new CurveSegment.CurveType_OneOf()
                                            {
                                                line_segment = new LineSegment()
                                                {
                                                    point = stoplinePts,
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        });
                    }
                }

                //backtrack and fill missing information for lanes
                for (int i = 0; i < lanes.Count; i++)
                {
                    Id land_id = (Id)(lanes[i].id);
                    var oldLane = lanes[i];
                    oldLane.overlap_id = laneIds2OverlapIdsMapping.ContainsKey(land_id) ? laneIds2OverlapIdsMapping[(Id)(lanes[i].id)] : null;
                    lanes[i] = oldLane;
                }

                //integration
                hdmap = new HDMap()
                {
                    header = new Header()
                    {
                        version = "1.500000",
                        date = "2018-03-23T13:27:54",
                        projection = new Projection()
                        {
                            proj = "+proj=utm +zone=10 +ellps=WGS84 +datum=WGS84 +units=m +no_defs",
                        },
                        district = "0",
                        rev_major = "1",
                        rev_minor = "0",
                        left = -121.982277,
                        top = 37.398079,
                        right = -121.971998,
                        bottom = 37.398079,
                        vendor = "LGSVL",
                    },
                    lane = lanes.Count == 0 ? null : lanes,
                    road = roads.Count == 0 ? null : roads,
                    signal = signals.Count == 0 ? null : signals,
                    overlap = overlaps.Count == 0 ? null : overlaps,
                    stop_sign = stop_signs.Count == 0 ? null : stop_signs,
                };

                return true;
            }

            bool MakeStoplineLaneOverlaps(MapStopLineSegmentBuilder stopline, List<MapSegment> lanesToInspec, float stoplineWidth, int overlapInfoId, OverlapType overlapType, ref List<Ros.PointENU> stoplinePts, ref Dictionary<Id, List<Id>> laneId2OverlapIdsMapping, ref List<Id> overlap_ids, ref List<Overlap> overlaps)
            {
                stopline.segment.targetWorldPositions = new List<Vector3>(stopline.segment.targetLocalPositions.Count);
                List<Vector2> stopline2D = new List<Vector2>();

                for (int i = 0; i < stopline.segment.targetLocalPositions.Count; i++)
                {
                    var worldPos = stopline.segment.builder.transform.TransformPoint(stopline.segment.targetLocalPositions[i]);
                    stopline.segment.targetWorldPositions.Add(worldPos); //to worldspace here
                    stopline2D.Add(new Vector2(worldPos.x, worldPos.z));
                    stoplinePts.Add(GetApolloCoordinates(worldPos, OriginEasting, OriginNorthing, false));
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
                            var overlap_id = $"{overlap_id_prefix}{overlaps.Count}";
                            var lane_id = segment.hdmapInfo.id;

                            if (!laneId2OverlapIdsMapping.ContainsKey(lane_id))
                            {
                                laneId2OverlapIdsMapping.Add(lane_id, new List<Id>());
                            }
                            laneId2OverlapIdsMapping[lane_id].Add(overlap_id);

                            ObjectOverlapInfo objOverlapInfo = new ObjectOverlapInfo();

                            if (overlapType == OverlapType.Signal_Stopline_Lane)
                            {
                                objOverlapInfo = new ObjectOverlapInfo()
                                {
                                    id = $"signal_{overlapInfoId}",
                                    overlap_info = new ObjectOverlapInfo.OverlapInfo_OneOf()
                                    {
                                        signal_overlap_info = new SignalOverlapInfo(),
                                    },
                                };
                            }
                            else if (overlapType == OverlapType.Stopsign_Stopline_Lane)
                            {
                                objOverlapInfo = new ObjectOverlapInfo()
                                {
                                    id = $"stopsign_{overlapInfoId}",
                                    overlap_info = new ObjectOverlapInfo.OverlapInfo_OneOf()
                                    {
                                        stop_sign_overlap_info = new StopSignOverlapInfo(),
                                    },
                                };
                            }

                            overlaps.Add(new Overlap()
                            {
                                id = overlap_id,
                                @object = new List<ObjectOverlapInfo>()
                                {
                                    new ObjectOverlapInfo()
                                    {
                                        id = lane_id,
                                        overlap_info = new ObjectOverlapInfo.OverlapInfo_OneOf()
                                        {
                                            lane_overlap_info = new LaneOverlapInfo()
                                            {
                                                start_s = ln_start_s,
                                                end_s = ln_end_s,
                                                is_merge = false,
                                            },
                                        },
                                    },
                                    objOverlapInfo,
                                },
                            });

                            overlap_ids.Add(overlap_id);
                        }
                    }
                }

                return true;
            }

            void Export()
            {
                var filepath = $"{foldername}{Path.DirectorySeparatorChar}{filename}";

                if (!System.IO.Directory.Exists(foldername))
                {
                    System.IO.Directory.CreateDirectory(foldername);
                }

                if (System.IO.File.Exists(filepath))
                {
                    System.IO.File.Delete(filepath);
                }

                using (StreamWriter sw = File.CreateText(filepath))
                {
                    StringBuilder sb;
                    Map.Apollo.HDMapUtil.SerializeHDMap(hdmap, out sb);
                    sw.Write(sb);
                }
            }

            void BuildTestMap()
            {
                var lanes = new List<Lane>();
                var overlaps = new List<Overlap>();

                lanes.Add(new Lane()
                {
                    id = new Id("2dap0_1_1"),
                    central_curve = new Curve()
                    {
                        segment = new List<CurveSegment>()
                        {
                            new CurveSegment()
                            {
                                curve_type = new CurveSegment.CurveType_OneOf()
                                {
                                    line_segment = new LineSegment()
                                    {
                                        point = new List<Ros.PointENU>()
                                        {
                                            new Ros.PointENU(590700, 4140310.24),
                                            new Ros.PointENU(590606, 4140310.24)
                                        },
                                    },
                                },
                                s = 0,
                                start_position = new Ros.PointENU(590700, 4140310.24),
                                length = 94
                            },
                        },
                    },
                    left_boundary = new LaneBoundary()
                    {
                        curve = new Curve()
                        {
                            segment = new List<CurveSegment>()
                            {
                                 new CurveSegment()
                                 {
                                      curve_type = new CurveSegment.CurveType_OneOf()
                                      {
                                          line_segment = new LineSegment()
                                          {
                                              point = new List<Ros.PointENU>()
                                              {
                                                  new Ros.PointENU(590700, 4140308.24),
                                                  new Ros.PointENU(590611, 4140308.24)
                                              },
                                          },
                                      },
                                      s = 0,
                                      start_position = new Ros.PointENU(590700, 4140308.24),
                                      length = 94,
                                 },
                            },
                        },
                        length = 94,
                        @virtual = false,
                        boundary_type = new List<LaneBoundaryType>()
                        {
                            new LaneBoundaryType()
                            {
                                 s = 0,
                                 types = new List<LaneBoundaryType.Type>()
                                 {
                                     LaneBoundaryType.Type.DOUBLE_YELLOW,
                                 },
                            },
                        },
                    },
                });

                overlaps.Add(new Overlap()
                {
                    id = new Id("overlap_1"),
                    @object = new List<ObjectOverlapInfo>()
                    {
                        new ObjectOverlapInfo()
                        {
                            id = new Id("2dap0_1_2"),
                            overlap_info = new ObjectOverlapInfo.OverlapInfo_OneOf()
                            {
                                lane_overlap_info = new LaneOverlapInfo()
                                {
                                    start_s = 0,
                                    end_s = 0.1,
                                    is_merge = false,
                                },
                            },
                        },
                        new ObjectOverlapInfo()
                        {
                            id = new Id("2505"),
                            overlap_info = new ObjectOverlapInfo.OverlapInfo_OneOf()
                            {
                                signal_overlap_info = new SignalOverlapInfo(),
                            },
                        },
                    }
                });
                overlaps.Add(new Overlap()
                {
                    id = new Id("overlap_2"),
                    @object = new List<ObjectOverlapInfo>()
                    {
                        new ObjectOverlapInfo()
                        {
                            id = new Id("2dap0_2_2"),
                            overlap_info = new ObjectOverlapInfo.OverlapInfo_OneOf()
                            {
                                lane_overlap_info = new LaneOverlapInfo()
                                {
                                    start_s = 0,
                                    end_s = 0.1,
                                    is_merge = false,
                                },
                            },
                        },
                        new ObjectOverlapInfo()
                        {
                            id = new Id("2506"),
                            overlap_info = new ObjectOverlapInfo.OverlapInfo_OneOf()
                            {
                                signal_overlap_info = new SignalOverlapInfo(),
                            },
                        },
                    }
                });

                hdmap = new HDMap()
                {
                    header = new Map.Apollo.Header()
                    {
                        version = "1.500000",
                        date = "2018-03-23T13:27:54",
                        projection = new Projection()
                        {
                            proj = "+proj=utm +zone=10 +ellps=WGS84 +datum=WGS84 +units=m +no_defs",
                        },
                        district = "0",
                        rev_major = "1",
                        rev_minor = "0",
                        left = -121.982277,
                        top = 37.398079,
                        right = -121.971998,
                        bottom = 37.398079,
                        vendor = "Baidu",
                    },
                    lane = lanes,
                    overlap = overlaps,
                };
            }
        }
    }
}
