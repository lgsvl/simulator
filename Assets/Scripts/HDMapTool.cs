/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using Map.Apollo;
using static Map.Apollo.HDMapUtil;

namespace Map
{
    namespace Apollo
    {
        public class HDMapTool : MapTool
        {
            public List<Transform> targets;

            public static float PROXIMITY = 0.02f;
            public static float ARROWSIZE = 1.0f;
            public float proximity = PROXIMITY;
            public float arrowSize = ARROWSIZE;

            public string foldername = "hd_map";
            public string filename = "base_map.txt";

            public float OriginNorthing = 4140112.5f;
            public float OriginEasting = 590470.7f;

            public float exportScaleFactor = 1.0f;

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
                foreach (var t in targetList)
                {
                    if (t == null)
                    {
                        continue;
                    }

                    var segBldr = t.GetComponentsInChildren<MapSegmentBuilder>();

                    segBldrs.AddRange(segBldr);
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

                        if ((firstPt - lastPt_cmp).magnitude < PROXIMITY)
                        {
                            segment.befores.Add(segment_cmp);
                        }

                        if ((lastPt - firstPt_cmp).magnitude < PROXIMITY)
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

                //start building up vector map structures
                var lanes = new List<Lane>();

                const float laneHalfWidth = 1.75f; //temp solution

                //Assign Ids
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

                    float length = 0;
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
                            length += (curPt - worldPoses[i - 1]).magnitude;
                        }

                        Ros.PointENU enuPos = GetApolloCoordinates(curPt, OriginEasting, OriginNorthing, 0);
                        Ros.PointENU enuLPos = GetApolloCoordinates(lPoint, OriginEasting, OriginNorthing, 0);
                        Ros.PointENU enuRPos = GetApolloCoordinates(rPoint, OriginEasting, OriginNorthing, 0);

                        centerPts.Add(new Ros.PointENU((double)enuPos.x, (double)enuPos.y));
                        lBndPts.Add(new Ros.PointENU((double)enuLPos.x, (double)enuLPos.y));
                        rBndPts.Add(new Ros.PointENU((double)enuRPos.x, (double)enuRPos.y));

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
                                    length = length
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
                        length = length,
                        speed_limit = 5,
                        predecessor_id = predecessor_ids.Count > 0 ? predecessor_ids : null,
                        successor_id = successor_ids.Count > 0 ? successor_ids : null,
                        type = Lane.LaneType.CITY_DRIVING,
                        turn = Lane.LaneTurn.NO_TURN,
                        direction = Lane.LaneDirection.FORWARD,
                    });

                }

                //final setup
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