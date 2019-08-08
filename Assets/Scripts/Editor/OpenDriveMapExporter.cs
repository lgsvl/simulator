/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */
 
using UnityEngine;
using Simulator.Map;
using System.IO;
using System.Xml.Serialization;
using Schemas;
using System.Collections.Generic;


namespace Simulator.Editor
{
    public class OpenDriveMapExporter
    {
        MapOrigin MapOrigin;
        MapManagerData MapAnnotationData;
        OpenDRIVE Map;

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
            var mapOrigin = MapOrigin.Find();
            if (mapOrigin == null)
            {
                return false;
            }

            MapAnnotationData = new MapManagerData();
            MapAnnotationData.GetIntersections();
            MapAnnotationData.GetTrafficLanes();
            
            // Initial collection // TODO validate are all of them used?
            var laneSegments = new HashSet<MapLane>(MapAnnotationData.GetData<MapLane>());
            var lineSegments = new HashSet<MapLine>(MapAnnotationData.GetData<MapLine>());
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

            foreach (var laneSegment in laneSegments)
            {
                if (laneSegment.stopLine != null)
                {
                    stopLineLanes.GetOrCreate(laneSegment.stopLine).Add(laneSegment);
                }
            }

            LinkSegments(laneSegments);
            LinkSegments(lineSegments);

            CheckNeighborLanes(laneSegments);


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








            // // Lanes
            // // Assign ids
            // int laneId = 0;
            // foreach (var laneSegment in laneSegmentsSet)
            // {
            //     laneSegment.id = $"lane_{laneId}";
            //     ++laneId;

            //     laneOverlapsInfo.GetOrCreate(laneSegment.gameObject).id = HdId(laneSegment.id);
            // }

            // // Function to get neighbor lanes in the same road
            // System.Func<MapLane, bool, List<MapLane>> GetNeighborForwardRoadLanes = null;
            // GetNeighborForwardRoadLanes = delegate (MapLane self, bool fromLeft)
            // {
            //     if (self == null)
            //     {
            //         return new List<MapLane>();
            //     }
                
            //     if (fromLeft)
            //     {
            //         if (self.leftLaneForward == null)
            //         {
            //             return new List<MapLane>();
            //         }
            //         else
            //         {
            //             var ret = new List<MapLane>();
            //             ret.AddRange(GetNeighborForwardRoadLanes(self.leftLaneForward, true));
            //             ret.Add(self.leftLaneForward);
            //             return ret;
            //         }
            //     }
            //     else
            //     {
            //         if (self.rightLaneForward == null)
            //         {
            //             return new List<MapLane>();
            //         }
            //         else
            //         {
            //             var ret = new List<MapLane>();
            //             ret.Add(self.rightLaneForward);
            //             ret.AddRange(GetNeighborForwardRoadLanes(self.rightLaneForward, false));
            //             return ret;
            //         }
            //     }
            // };

            // HashSet<HD.Road> roadSet = new HashSet<HD.Road>();

            // var visitedLanes = new Dictionary<MapLane, HD.Road>();

            // {
            //     foreach (var laneSegment in laneSegmentsSet)
            //     {
            //         if (visitedLanes.ContainsKey(laneSegment))
            //         {
            //             continue;
            //         }

            //         var lefts = GetNeighborForwardRoadLanes(laneSegment, true);  // Left forward lanes from furthest to nearest
            //         var rights = GetNeighborForwardRoadLanes(laneSegment, false);  // Right forward lanes from nearest to furthest

                    
            //         var roadLanes = new List<MapLane>();
            //         roadLanes.AddRange(lefts);
            //         roadLanes.Add(laneSegment);
            //         roadLanes.AddRange(rights);
                    
            //         var roadSection = new HD.RoadSection()
            //         {
            //             id = HdId($"1"),
            //             boundary = null,
            //         };

            //         foreach (var roadLaneSegment in roadLanes)
            //         {
            //             roadSection.lane_id.Add(new HD.Id()
            //             {
            //                 id = roadLaneSegment.id
            //             });
            //         };
                    
            //         var road = new HD.Road()
            //         {
            //             id = HdId($"road_{roadSet.Count}"),
            //             junction_id = null,
            //         };
            //         road.section.Add(roadSection);

            //         roadSet.Add(road);

            //         foreach (var l in roadLanes)
            //         {
            //             if (!visitedLanes.ContainsKey(l))
            //             {
            //                 visitedLanes.Add(l, road);
            //             }
            //         }

            //         var gameObjectsOfLanes = new List<GameObject>();
            //         foreach (var lane in roadLanes)
            //         {
            //             gameObjectsOfLanes.Add(lane.gameObject);
            //         }
            //         roadIdToLanes.Add(road.id.id, gameObjectsOfLanes);
            //     }
            // }

            // // Config lanes
            // foreach (var laneSegment in laneSegmentsSet)
            // {
            //     var centerPts = new List<ApolloCommon.PointENU>();
            //     var lBndPts = new List<ApolloCommon.PointENU>();
            //     var rBndPts = new List<ApolloCommon.PointENU>();

            //     var worldPoses = laneSegment.mapWorldPositions;
            //     var leftBoundPoses = new List<Vector3>();
            //     var rightBoundPoses = new List<Vector3>();

            //     float mLength = 0;
            //     float lLength = 0;
            //     float rLength = 0;

            //     List<HD.LaneSampleAssociation> associations = new List<HD.LaneSampleAssociation>();
            //     associations.Add(new HD.LaneSampleAssociation()
            //     {
            //         s = 0,
            //         width = laneHalfWidth,
            //     });

            //     for (int i = 0; i < worldPoses.Count; i++)
            //     {
            //         Vector3 curPt = worldPoses[i];
            //         Vector3 tangFwd;

            //         if (i == 0)
            //         {
            //             tangFwd = (worldPoses[1] - curPt).normalized;
            //         }
            //         else if (i == worldPoses.Count - 1)
            //         {
            //             tangFwd = (curPt - worldPoses[worldPoses.Count - 2]).normalized;
            //         }
            //         else
            //         {
            //             tangFwd = (((curPt - worldPoses[i - 1]) + (worldPoses[i + 1] - curPt)) * 0.5f).normalized;
            //         }

            //         Vector3 lPoint = Vector3.Cross(tangFwd, Vector3.up) * laneHalfWidth + curPt;
            //         Vector3 rPoint = -Vector3.Cross(tangFwd, Vector3.up) * laneHalfWidth + curPt;

            //         leftBoundPoses.Add(lPoint);
            //         rightBoundPoses.Add(rPoint);

            //         if (i > 0)
            //         {
            //             mLength += (curPt - worldPoses[i - 1]).magnitude;
            //             associations.Add(new HD.LaneSampleAssociation()
            //             {
            //                 s = mLength,
            //                 width = laneHalfWidth,
            //             });

            //             lLength += (leftBoundPoses[i] - leftBoundPoses[i - 1]).magnitude;
            //             rLength += (rightBoundPoses[i] - rightBoundPoses[i - 1]).magnitude;
            //         }

            //         centerPts.Add(HDMapUtil.GetApolloCoordinates(curPt, OriginEasting, OriginNorthing, false));
            //         lBndPts.Add(HDMapUtil.GetApolloCoordinates(lPoint, OriginEasting, OriginNorthing, false));
            //         rBndPts.Add(HDMapUtil.GetApolloCoordinates(rPoint, OriginEasting, OriginNorthing, false));

            //     }

            //     var predecessor_ids = new List<HD.Id>();
            //     var successor_ids = new List<HD.Id>();
            //     predecessor_ids.AddRange(laneSegment.befores.Select(seg => HdId(seg.id)));
            //     successor_ids.AddRange(laneSegment.afters.Select(seg => HdId(seg.id)));

            //     var lane = new HD.Lane()
            //     {
            //         id = HdId(laneSegment.id),

            //         central_curve = new HD.Curve(),
            //         left_boundary = new HD.LaneBoundary(),
            //         right_boundary = new HD.LaneBoundary(),
            //         length = mLength,
            //         speed_limit = laneSegment.speedLimit,
            //         type = HD.Lane.LaneType.CITY_DRIVING,
            //         turn = laneSegment.laneTurn,
            //         direction = HD.Lane.LaneDirection.FORWARD,
            //     };

            //     // Record lane's length
            //     laneOverlapsInfo[laneSegment.gameObject].mLength = mLength;


            //     if (laneHasJunction.Contains(laneSegment.gameObject))
            //     {
            //         foreach (var junctionOverlapId in laneOverlapsInfo[laneSegment.gameObject].junctionOverlapIds)
            //         {
            //             lane.overlap_id.Add(junctionOverlapId.Value);
            //         }
            //     }

            //     if (laneHasParkingSpace.Contains(laneSegment.gameObject))
            //     {
            //         foreach (var parkingSpaceOverlapId in laneOverlapsInfo[laneSegment.gameObject].parkingSpaceOverlapIds)
            //         {
            //             lane.overlap_id.Add(parkingSpaceOverlapId.Value);
            //         }
            //     }

            //     if (laneHasSpeedBump.Contains(laneSegment.gameObject))
            //     {
            //         foreach (var speedBumpOverlapId in laneOverlapsInfo[laneSegment.gameObject].speedBumpOverlapIds)
            //         {
            //             lane.overlap_id.Add(speedBumpOverlapId.Value);
            //         }
            //     }

            //     if (laneHasClearArea.Contains(laneSegment.gameObject))
            //     {
            //         foreach (var clearAreaOverlapId in laneOverlapsInfo[laneSegment.gameObject].clearAreaOverlapIds)
            //         {
            //             lane.overlap_id.Add(clearAreaOverlapId.Value);
            //         }
            //     }

            //     if (laneHasCrossWalk.Contains(laneSegment.gameObject))
            //     {
            //         foreach (var crossWalkOverlapId in laneOverlapsInfo[laneSegment.gameObject].crossWalkOverlapIds)
            //         {
            //             lane.overlap_id.Add(crossWalkOverlapId.Value);
            //         }
            //     }

            //     Hdmap.lane.Add(lane);

            //     // CentralCurve
            //     var segment = new HD.segment();
            //     segment.point.AddRange(centerPts);
                
            //     var central_curve_segment = new List<HD.CurveSegment>()
            //     {
            //         new HD.CurveSegment()
            //         {
            //             line_segment = segment,
            //             s = 0,
            //             start_position = new ApolloCommon.PointENU()
            //             {
            //                 x = centerPts[0].x,
            //                 y = centerPts[0].y,
            //                 z = centerPts[0].z,
            //             },
            //             length = mLength,
            //         },
            //     };
            //     lane.central_curve.segment.AddRange(central_curve_segment);
            //     // /CentralCurve

            //     // LeftBoundary
            //     var curveSegment = new HD.CurveSegment()
            //     {
            //         line_segment = new HD.segment(),
            //         s = 0,
            //         start_position = lBndPts[0],
            //         length = lLength,
            //     };

            //     curveSegment.line_segment.point.AddRange(lBndPts);

            //     var leftLaneBoundaryType = new HD.LaneBoundaryType()
            //     {
            //         s = 0,
            //     };

            //     leftLaneBoundaryType.types.Add((HD.LaneBoundaryType.Type)laneSegment.leftBoundType);

            //     var left_boundary_segment = new HD.LaneBoundary()
            //     {
            //         curve = new HD.Curve(),
            //         length = lLength,
            //         @virtual = true,
            //     };
            //     left_boundary_segment.boundary_type.Add(leftLaneBoundaryType);
            //     left_boundary_segment.curve.segment.Add(curveSegment);
            //     lane.left_boundary = left_boundary_segment;
            //     // /LeftBoundary
                
            //     // RightBoundary
            //     curveSegment = new HD.CurveSegment()
            //     {
            //         line_segment = new HD.segment(),
            //         s = 0,
            //         start_position = lBndPts[0],
            //         length = lLength,
            //     };

            //     curveSegment.line_segment.point.AddRange(rBndPts);

            //     var rightLaneBoundaryType = new HD.LaneBoundaryType();

            //     rightLaneBoundaryType.types.Add((HD.LaneBoundaryType.Type)laneSegment.rightBoundType);

            //     var right_boundary_segment = new HD.LaneBoundary()
            //     {
            //         curve = new HD.Curve(),
            //         length = rLength,
            //         @virtual = true,
            //     };
            //     right_boundary_segment.boundary_type.Add(rightLaneBoundaryType);
            //     right_boundary_segment.curve.segment.Add(curveSegment);
            //     lane.right_boundary = right_boundary_segment;
            //     // /RightBoundary

            //     if (predecessor_ids.Count > 0)
            //         lane.predecessor_id.AddRange(predecessor_ids);

            //     if (successor_ids.Count > 0)
            //         lane.successor_id.AddRange(successor_ids);

            //     lane.left_sample.AddRange(associations);
            //     lane.left_road_sample.AddRange(associations);
            //     lane.right_sample.AddRange(associations);
            //     lane.right_road_sample.AddRange(associations);
            //     if (laneSegment.leftLaneForward != null)
            //         lane.left_neighbor_forward_lane_id.AddRange(new List<HD.Id>() { HdId(laneSegment.leftLaneForward.id), } );
            //     if (laneSegment.rightLaneForward != null)
            //         lane.right_neighbor_forward_lane_id.AddRange(new List<HD.Id>() { HdId(laneSegment.rightLaneForward.id), } );
            //     if (laneSegment.leftLaneReverse != null)
            //         lane.left_neighbor_reverse_lane_id.AddRange(new List<HD.Id>() { HdId(laneSegment.leftLaneReverse.id), } );
            //     if (laneSegment.rightLaneReverse != null)
            //         lane.right_neighbor_reverse_lane_id.AddRange(new List<HD.Id>() { HdId(laneSegment.rightLaneReverse.id), } );
                
            //     // Add boundary to road
            //     if (laneSegment.leftLaneForward == null || laneSegment.rightLaneForward == null)
            //     {
            //         var road = visitedLanes[laneSegment];
            //         roadSet.Remove(road);

            //         var section = road.section[0];

            //         segment = new HD.segment();
            //         if (laneSegment.leftLaneForward == null) 
            //             segment.point.AddRange(lBndPts);
            //         else
            //             segment.point.AddRange(rBndPts);

            //         var edges = new List<HD.BoundaryEdge>();
            //         if (section.boundary?.outer_polygon?.edge != null)
            //         {
            //             edges.AddRange(section.boundary.outer_polygon.edge);
            //         }

            //         {
            //             var boundaryEdge = new HD.BoundaryEdge()
            //             {
            //                 curve = new HD.Curve(),
            //                 type = laneSegment.leftLaneForward == null ? HD.BoundaryEdge.Type.LEFT_BOUNDARY : HD.BoundaryEdge.Type.RIGHT_BOUNDARY,
            //             };
            //             boundaryEdge.curve.segment.Add(new HD.CurveSegment()
            //             {
            //                 line_segment = segment,
            //             });
            //             edges.Add(boundaryEdge);
            //         }

            //         segment = new HD.segment();
            //         // Cases that a Road only has one lane, adds rightBoundary
            //         if (laneSegment.leftLaneForward == null && laneSegment.rightLaneForward == null)
            //         {
            //             segment.point.Clear();
            //             segment.point.AddRange(rBndPts);
            //             var boundaryEdge = new HD.BoundaryEdge()
            //             {
            //                 curve = new HD.Curve(),
            //                 type = HD.BoundaryEdge.Type.RIGHT_BOUNDARY,
            //             };
            //             boundaryEdge.curve.segment.Add(new HD.CurveSegment()
            //             {
            //                 line_segment = segment,
            //             });
            //             edges.Add(boundaryEdge);
            //         }

            //         section.boundary = new HD.RoadBoundary()
            //         {
            //             outer_polygon = new HD.BoundaryPolygon(),
            //         };
            //         section.boundary.outer_polygon.edge.AddRange(edges);
            //         road.section[0] = section;
            //         roadSet.Add(road);
            //     }
            // }

            // foreach (var road in roadSet)
            // {
            //     if (road.section[0].boundary.outer_polygon.edge.Count == 0)
            //     {
            //         Debug.Log("You have no boundary edges in some roads, please check!!!");
            //         return false;
            //     }

            //     foreach (var lane in roadIdToLanes[road.id.id])
            //     {
            //         if (gameObjectToOverlapId.ContainsKey(lane))
            //         {
            //             var overlap_id = gameObjectToOverlapId[lane];
            //             var junction = overlapIdToJunction[overlap_id.id];
            //             road.junction_id = junction.id;
            //         }
            //     }
            // }

            // Hdmap.road.AddRange(roadSet);
            // Add roads
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

        bool CheckNeighborLanes(HashSet<MapLane> laneSegments)
        {
            // Check validity of lane segment builder relationship but it won't warn you if have A's right lane to be null or B's left lane to be null
            foreach (var laneSegment in laneSegments)
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

        void Export(string filePath)
        {
            var serializer = new XmlSerializer(typeof(OpenDRIVE));

            StreamWriter writer = new StreamWriter(filePath);
            serializer.Serialize(writer, Map);
        }
    }
}