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

            public string foldername = "hd_map";
            public string filename = "base_map.txt";

            public float OriginNorthing = 4140112.5f;
            public float OriginEasting = 590470.7f;

            private Map.Apollo.HDMap hdmap;

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
                var signals = new List<Signal>();
                var stopsigns = new List<StopSign>();
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
                foreach (var t in targetList)
                {
                    if (t == null)
                    {
                        continue;
                    }

                    segBldrs.AddRange(t.GetComponentsInChildren<MapSegmentBuilder>());
                    signalLights.AddRange(t.GetComponentsInChildren<HDMapSignalLight>());
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
                    //Make sure clear everything that might have data left over by previous generation
                    segment.befores.Clear();
                    segment.afters.Clear();
                    segment.targetWorldPositions.Clear();

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
                        if (segment_cmp.builder.GetType() != segment.builder.GetType())
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
                    Debug.Log("Some segment has less than 2 waypoints, complement it to 2");
                }

                var allLnSegs = new HashSet<MapSegment>();

                foreach (var segment in allSegs)
                {
                    if (segment.builder.GetType() == typeof(MapLaneSegmentBuilder))
                    {
                        allLnSegs.Add(segment);
                    }
                }

                //New sets for newly converted(to world space) segments
                var allConvertedLnSeg = new HashSet<MapSegment>();

                //Filter and convert all lane segments
                if (allLnSegs.Count > 0)
                {
                    var startLnSegs = new HashSet<MapSegment>(); //The lane segments that are at merging or forking or starting position
                    var visitedLnSegs = new HashSet<MapSegment>(); //tracking for record

                    foreach (var lnSeg in allLnSegs)
                    {
                        if (lnSeg.befores.Count != 1 || (lnSeg.befores.Count == 1 && lnSeg.befores[0].afters.Count > 1)) //no any before segments
                        {
                            startLnSegs.Add(lnSeg);
                        }
                    }

                    foreach (var startLnSeg in startLnSegs)
                    {
                        ConvertAndJointSingleConnectedSegments<HDMapTool>(startLnSeg, allLnSegs, ref allConvertedLnSeg, visitedLnSegs);
                    }

                    while (allLnSegs.Count > 0)//Remaining should be isolated loops
                    {
                        MapSegment pickedSeg = null;
                        foreach (var lnSeg in allLnSegs)
                        {
                            pickedSeg = lnSeg;
                            break;
                        }
                        if (pickedSeg != null)
                        {
                            ConvertAndJointSingleConnectedSegments<HDMapTool>(pickedSeg, allLnSegs, ref allConvertedLnSeg, visitedLnSegs);
                        }
                    }
                }

                //build virtual connection lanes
                var virtualBridgeLnSegs = new List<MapSegment>();
                foreach (var lnSeg in allConvertedLnSeg)
                {
                    if (lnSeg.afters.Count > 0)
                    {
                        foreach (var aftrLn in lnSeg.afters)
                        {
                            virtualBridgeLnSegs.Add(new MapSegment()
                            {
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
                foreach (var lnSeg in allConvertedLnSeg)
                {
                    ((MapLaneSegment)lnSeg).id = $"lane_{laneId}";
                    ++laneId;
                }

                foreach (var lnSeg in allConvertedLnSeg)
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
                        predecessor_ids.Add(new Id(((MapLaneSegment)ls).id));
                    }
                    foreach (var ls in lnSeg.afters)
                    {
                        successor_ids.Add(new Id(((MapLaneSegment)ls).id));
                    }

                    lanes.Add(new Lane()
                    {
                        id = new Id(((MapLaneSegment)lnSeg).id),
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
                                        LaneBoundaryType.Type.DOTTED_WHITE,
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
                                        LaneBoundaryType.Type.DOTTED_WHITE,
                                    }
                                }
                            }
                        },
                        length = mLength,
                        speed_limit = 5,
                        predecessor_id = predecessor_ids.Count > 0 ? predecessor_ids : null,
                        successor_id = successor_ids.Count > 0 ? successor_ids : null,
                        type = Lane.LaneType.CITY_DRIVING,
                        turn = Lane.LaneTurn.NO_TURN,
                        direction = Lane.LaneDirection.FORWARD,
                        left_sample = new List<LaneSampleAssociation>()
                        {
                            new LaneSampleAssociation()
                            {
                                s = 0,
                                width = laneHalfWidth
                            },
                            new LaneSampleAssociation()
                            {
                                s = mLength,
                                width = laneHalfWidth
                            }
                        },
                        right_sample = new List<LaneSampleAssociation>()
                        {
                            new LaneSampleAssociation()
                            {
                                s = 0,
                                width = laneHalfWidth
                            },
                            new LaneSampleAssociation()
                            {
                                s = mLength,
                                width = laneHalfWidth
                            }
                        }
                    });
                }

                //for backtracking
                var laneId2OverlapIdsMapping = new Dictionary<Id, List<Id>>();

                //signals and lane_signal overlaps
                int signalId = 0;
                foreach (var signalLight in signalLights)
                {
                    var bounds = signalLight.Get2DBounds();
                    List<Subsignal> subsignals = null;
                    var stopLine = signalLight.hintStopline;
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

                    var stoplinePts = (stopLine == null || stopLine.segment.targetLocalPositions.Count == 0) ? null : new List<Ros.PointENU>();
                    stopLine.segment.targetWorldPositions.Clear();
                    stopLine.segment.targetWorldPositions = new List<Vector3>(stopLine.segment.targetLocalPositions.Count);
                    for (int i = 0; i < stopLine.segment.targetLocalPositions.Count; i++)
                    {
                        var worldPos = stopLine.segment.builder.transform.TransformPoint(stopLine.segment.targetLocalPositions[i]);
                        stopLine.segment.targetWorldPositions.Add(worldPos); //translate space here
                        stoplinePts.Add(GetApolloCoordinates(worldPos, OriginEasting, OriginNorthing, false));
                    }

                    //for filling the reference the other way
                    var signal_lane_overlapIds = new List<Id>();

                    if (stoplinePts != null)
                    {
                        var considered = new HashSet<MapSegment>();
                        var stopline2D = stopLine.segment.targetWorldPositions.Select(p => new Vector2(p.x, p.z)).ToList();

                        for (int i = 0; i < virtualBridgeLnSegs.Count; i++)
                        {
                            var vSeg = virtualBridgeLnSegs[i];
                            List<Vector2> intersects;                            
                            var virtualLane2D = vSeg.targetWorldPositions.Select(p => new Vector2(p.x, p.z)).ToList();
                            bool isIntersected = Utils.CurveSegmentsIntersect(stopline2D, virtualLane2D, out intersects);
                            if (isIntersected)
                            {
                                if (intersects.Count > 1)
                                {
                                    //UnityEditor.Selection.activeGameObject = stopLine.gameObject;
                                    Debug.LogError("stopline should not have more than one intersect point with a virtual lane, abort calculation.");
                                    return false;
                                }

                                Vector2 intersect = intersects[0];

                                var afterLane = vSeg.afters[0] as MapLaneSegment;

                                if (!considered.Contains(afterLane))
                                {
                                    considered.Add(afterLane);

                                    float s;
                                    float totalLength = Utils.GetNearestSCoordinate(intersect, stopline2D, out s);
                                    float ln_start_s = s - stoplineWidth * 0.5f;
                                    float ln_end_s = s + stoplineWidth * 0.5f;
                                    if (ln_start_s < 0)
                                    {
                                        var diff = -ln_start_s;
                                        ln_start_s += diff;
                                        ln_end_s += diff;
                                    }
                                    else if (ln_end_s > totalLength)
                                    {
                                        var diff = ln_end_s - totalLength;
                                        ln_start_s -= diff;
                                        ln_end_s -= diff;
                                    }

                                    //Create overlap
                                    var overlap_id = $"lane_signal_overlap_{overlaps.Count}";
                                    Id lane_id = afterLane.id;

                                    if (!laneId2OverlapIdsMapping.ContainsKey(lane_id))
                                    {
                                        laneId2OverlapIdsMapping.Add(lane_id, new List<Id>());
                                    }
                                    laneId2OverlapIdsMapping[lane_id].Add(overlap_id);

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
                                            new ObjectOverlapInfo()
                                            {
                                                id = $"signal_{signalId}",
                                                overlap_info = new ObjectOverlapInfo.OverlapInfo_OneOf()
                                                {
                                                    signal_overlap_info = new SignalOverlapInfo(),
                                                },
                                            },
                                        },
                                    });

                                    signal_lane_overlapIds.Add(overlap_id);
                                }
                            }
                        }

                        foreach (var seg in allConvertedLnSeg)
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
                                        Debug.Log("stopline have multiple intersect points with a lane within a cluster, pick one");
                                    }
                                    else
                                    {
                                        //UnityEditor.Selection.activeGameObject = stopLine.gameObject;
                                        Debug.LogWarning("stopline is not common to have more than one non-cluster intersect point with a lane, abort calculation");
                                        return false;
                                    }                                    
                                }

                                var lnSeg = seg as MapLaneSegment;

                                if (!considered.Contains(seg))
                                {
                                    considered.Add(seg);

                                    float s;
                                    float totalLength = Utils.GetNearestSCoordinate(intersect, stopline2D, out s);
                                    float ln_start_s = s - stoplineWidth * 0.5f;
                                    float ln_end_s = s + stoplineWidth * 0.5f;
                                    if (ln_start_s < 0)
                                    {
                                        var diff = -ln_start_s;
                                        ln_start_s += diff;
                                        ln_end_s += diff;
                                    }
                                    else if (ln_end_s > totalLength)
                                    {
                                        var diff = ln_end_s - totalLength;
                                        ln_start_s -= diff;
                                        ln_end_s -= diff;
                                    }

                                    //Create overlap
                                    var overlap_id = $"lane_signal_overlap_{overlaps.Count}";
                                    var lane_id = lnSeg.id;

                                    if (!laneId2OverlapIdsMapping.ContainsKey(lane_id))
                                    {
                                        laneId2OverlapIdsMapping.Add(lane_id, new List<Id>());
                                    }
                                    laneId2OverlapIdsMapping[lane_id].Add(overlap_id);

                                    overlaps.Add(new Overlap()
                                    {
                                        id = $"lane_signal_overlap_{overlaps.Count}",
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
                                            new ObjectOverlapInfo()
                                            {
                                                id = $"signal_{signalId}",
                                                overlap_info = new ObjectOverlapInfo.OverlapInfo_OneOf()
                                                {
                                                    signal_overlap_info = new SignalOverlapInfo(),
                                                },
                                            },
                                        },
                                    });

                                    signal_lane_overlapIds.Add(overlap_id);
                                }
                            }
                        }
                    }

                    signals.Add(new Signal()
                    {
                        id = new Id($"signal_{signalId}"),
                        boundary = new Polygon()
                        {
                            point = new List<Ros.PointENU>()
                            {
                                GetApolloCoordinates(bounds.Item1, OriginEasting, OriginNorthing),
                                GetApolloCoordinates(bounds.Item2, OriginEasting, OriginNorthing),
                                GetApolloCoordinates(bounds.Item3, OriginEasting, OriginNorthing),
                                GetApolloCoordinates(bounds.Item4, OriginEasting, OriginNorthing)
                            }
                        },
                        subsignal = subsignals,
                        overlap_id = signal_lane_overlapIds.Count > 1 ? signal_lane_overlapIds : null, //backtrack and fill reverse link
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
                    ++signalId;
                }

                //backtrack and fill missing information for lanes
                for (int i = 0; i < lanes.Count; i++)
                {
                    Id land_id = (Id)(lanes[i].id);
                    lanes[i] = new Lane()
                    {
                        id = lanes[i].id,
                        central_curve = lanes[i].central_curve,
                        left_boundary = lanes[i].left_boundary,
                        right_boundary = lanes[i].right_boundary,
                        length = lanes[i].length,
                        speed_limit = lanes[i].speed_limit,
                        overlap_id = laneId2OverlapIdsMapping.ContainsKey(land_id) ? laneId2OverlapIdsMapping[(Id)(lanes[i].id)] : null,
                        predecessor_id = lanes[i].predecessor_id,
                        successor_id = lanes[i].successor_id,
                        type = lanes[i].type,
                        turn = lanes[i].turn,
                        direction = lanes[i].direction,
                        left_sample = lanes[i].left_sample,
                        right_sample = lanes[i].right_sample
                    };
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
                    lane = lanes,
                    signal = signals,
                    overlap = overlaps,
                };

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