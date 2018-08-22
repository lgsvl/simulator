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

namespace Apollo
{
    public class HDMapTool : MonoBehaviour
    {
        public List<Transform> targets;

        public float proximity = VectorMapTool.PROXIMITY;
        public float arrowSize = VectorMapTool.ARROWSIZE;

        private Map.Apollo.HDMap hdmap;

        public string foldername = "hd_map";
        public string filename = "base_map.txt";
        public float exportScaleFactor = 1.0f;

        HDMapTool()
        {
            VectorMapTool.PROXIMITY = proximity;
            VectorMapTool.ARROWSIZE = arrowSize;
        }

        void OnEnable()
        {
            if (proximity != VectorMapTool.PROXIMITY)
            {
                VectorMapTool.PROXIMITY = proximity;
            }

            if (arrowSize != VectorMapTool.ARROWSIZE)
            {
                VectorMapTool.ARROWSIZE = arrowSize;
            }
        }

        void OnValidate()
        {
            if (proximity != VectorMapTool.PROXIMITY)
            {
                VectorMapTool.PROXIMITY = proximity;
            }

            if (arrowSize != VectorMapTool.ARROWSIZE)
            {
                VectorMapTool.ARROWSIZE = arrowSize;
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
            
            //To be implemented

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