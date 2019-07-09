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
    public class LaneOverlapInfo
    {
        public HD.Id id;
        public Dictionary<GameObject, HD.Id> signalOverlapIds = new Dictionary<GameObject, HD.Id>();
        public Dictionary<GameObject, HD.Id> stopSignOverlapIds = new Dictionary<GameObject, HD.Id>();
        public Dictionary<GameObject, HD.Id> parkingSpaceOverlapIds = new Dictionary<GameObject, HD.Id>();
        public Dictionary<GameObject, HD.Id> clearAreaOverlapIds = new Dictionary<GameObject, HD.Id>();
        public Dictionary<GameObject, HD.Id> speedBumpOverlapIds = new Dictionary<GameObject, HD.Id>();
        public Dictionary<GameObject, HD.Id> crossWalkOverlapIds = new Dictionary<GameObject, HD.Id>();
        public Dictionary<GameObject, HD.Id> yieldSignOverlapIds = new Dictionary<GameObject, HD.Id>();
        public Dictionary<GameObject, HD.Id> junctionOverlapIds = new Dictionary<GameObject, HD.Id>();
        public float mLength;
    }

    public class SignalOverlapInfo
    {
        public HD.Id id;
        public Dictionary<GameObject, HD.Id> laneOverlapIds = new Dictionary<GameObject, HD.Id>(); 
        public Dictionary<GameObject, HD.Id> junctionOverlapIds = new Dictionary<GameObject, HD.Id>();
        public Dictionary<GameObject, List<HD.ObjectOverlapInfo>> laneOverlapInfos= new Dictionary<GameObject, List<HD.ObjectOverlapInfo>>();
    }

    public class StopSignOverlapInfo
    {
        public HD.Id id;
        public Dictionary<GameObject, HD.Id> laneOverlapIds = new Dictionary<GameObject, HD.Id>();
        public Dictionary<GameObject, HD.Id> junctionOverlapIds = new Dictionary<GameObject, HD.Id>();
        public Dictionary<GameObject, List<HD.ObjectOverlapInfo>> laneOverlapInfos= new Dictionary<GameObject, List<HD.ObjectOverlapInfo>>();
    }

    public class ClearAreaOverlapInfo
    {
        public HD.Id id;
        public Dictionary<GameObject, HD.Id> laneOverlapIds = new Dictionary<GameObject, HD.Id>();
        public Dictionary<GameObject, HD.Id> junctionOverlapIds = new Dictionary<GameObject, HD.Id>();
        public HD.Polygon polygon;
    }

    public class CrossWalkOverlapInfo
    {
        public HD.Id id;
        public Dictionary<GameObject, HD.Id> laneOverlapIds = new Dictionary<GameObject, HD.Id>();
        public Dictionary<GameObject, HD.Id> junctionOverlapIds = new Dictionary<GameObject, HD.Id>();
    }

    public class SpeedBumpOverlapInfo
    {
        public HD.Id id;
        public Dictionary<GameObject, HD.Id> laneOverlapIds = new Dictionary<GameObject, HD.Id>();
    }

    public class ParkingSpaceOverlapInfo
    {
        public HD.Id id;
        public Dictionary<GameObject, HD.Id> laneOverlapIds = new Dictionary<GameObject, HD.Id>();
    }

    public class JunctionOverlapInfo
    {
        public HD.Id id;
        public HD.Polygon polygon;
        public Dictionary<GameObject, HD.Id> laneOverlapIds = new Dictionary<GameObject, HD.Id>();
        public Dictionary<GameObject, HD.Id> signalOverlapIds = new Dictionary<GameObject, HD.Id>();
        public Dictionary<GameObject, HD.Id> stopSignOverlapIds = new Dictionary<GameObject, HD.Id>();
        public Dictionary<GameObject, HD.Id> parkingOverlapIds = new Dictionary<GameObject, HD.Id>();
        public Dictionary<GameObject, HD.Id> clearAreaOverlapIds = new Dictionary<GameObject, HD.Id>();
        public Dictionary<GameObject, HD.Id> crossWalkOverlapIds = new Dictionary<GameObject, HD.Id>();
    }
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
        List<MapIntersection> intersections;
        Dictionary<string, List<GameObject>> overlapIdToGameObjects;
        Dictionary<GameObject, HD.ObjectOverlapInfo> gameObjectToOverlapInfo;
        Dictionary<GameObject, HD.Id> gameObjectToOverlapId;

        Dictionary<string, HD.Junction> overlapIdToJunction;
        Dictionary<string, List<GameObject>> roadIdToLanes;

        // Lane
        HashSet<MapLane> laneSegmentsSet;
        Dictionary<GameObject, LaneOverlapInfo> laneOverlapsInfo;
        HashSet<GameObject> laneHasParkingSpace;
        HashSet<GameObject> laneHasSpeedBump;
        HashSet<GameObject> laneHasJunction;
        HashSet<GameObject> laneHasClearArea;
        HashSet<GameObject> laneHasCrossWalk;
        // Signal
        Dictionary<GameObject, SignalOverlapInfo> signalOverlapsInfo;
        // Stop
        Dictionary<GameObject, StopSignOverlapInfo> stopSignOverlapsInfo;
        // Clear Area
        Dictionary<GameObject, ClearAreaOverlapInfo> clearAreaOverlapsInfo;
        // Cross Walk
        Dictionary<GameObject, CrossWalkOverlapInfo> crossWalkOverlapsInfo;
        // Junction
        Dictionary<GameObject, JunctionOverlapInfo> junctionOverlapsInfo;
        // Speedbump
        Dictionary<GameObject, SpeedBumpOverlapInfo> speedBumpOverlapsInfo;
        // Parking Space
        Dictionary<GameObject, ParkingSpaceOverlapInfo> parkingSpaceOverlapsInfo;

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
            intersections = new List<MapIntersection>();
            
            overlapIdToGameObjects = new Dictionary<string, List<GameObject>>();
            gameObjectToOverlapInfo = new Dictionary<GameObject, HD.ObjectOverlapInfo>();
            gameObjectToOverlapId = new Dictionary<GameObject, HD.Id>();

            overlapIdToJunction = new Dictionary<string, HD.Junction>();
            roadIdToLanes = new Dictionary<string, List<GameObject>>();

            // Lane
            laneOverlapsInfo = new Dictionary<GameObject, LaneOverlapInfo>();
            laneHasParkingSpace = new HashSet<GameObject>();
            laneHasSpeedBump = new HashSet<GameObject>();
            laneHasJunction = new HashSet<GameObject>();
            laneHasClearArea = new HashSet<GameObject>();
            laneHasCrossWalk = new HashSet<GameObject>();
            // Signal
            signalOverlapsInfo = new Dictionary<GameObject, SignalOverlapInfo>();

            // Stop
            stopSignOverlapsInfo = new Dictionary<GameObject, StopSignOverlapInfo>();
            
            // Clear Area
            clearAreaOverlapsInfo = new Dictionary<GameObject, ClearAreaOverlapInfo>();
            // Cross Walk
            crossWalkOverlapsInfo = new Dictionary<GameObject, CrossWalkOverlapInfo>(); 
            // Junction
            junctionOverlapsInfo = new Dictionary<GameObject, JunctionOverlapInfo>();
            // Speed Bump
            speedBumpOverlapsInfo = new Dictionary<GameObject, SpeedBumpOverlapInfo>();
            // Parking Space
            parkingSpaceOverlapsInfo = new Dictionary<GameObject, ParkingSpaceOverlapInfo>();

            laneSegments.AddRange(MapAnnotationData.GetData<MapLane>());
            signalLights.AddRange(MapAnnotationData.GetData<MapSignal>());
            stopSigns.AddRange(MapAnnotationData.GetData<MapSign>());
            intersections.AddRange(MapAnnotationData.GetData<MapIntersection>());

            laneSegmentsSet = new HashSet<MapLane>(); 

            // Use set instead of list to increase speed
            foreach (var laneSegment in laneSegments)
            {
                laneSegmentsSet.Add(laneSegment);
            }

            MakeInfoOfClearArea();

            MakeInfoOfJunction();
            MakeInfoOfParkingSpace(); // TODO: needs test
            MakeInfoOfSpeedBump(); // TODO: needs test
            MakeInfoOfCrossWalk();

            // Link before and after segment for each lane segment
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

                laneOverlapsInfo.GetOrCreate(laneSegment.gameObject).id = HdId(laneSegment.id);
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

                    foreach (var roadLaneSegment in roadLanes)
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
                    foreach (var lane in roadLanes)
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

                // Record lane's length
                laneOverlapsInfo[laneSegment.gameObject].mLength = mLength;


                if (laneHasJunction.Contains(laneSegment.gameObject))
                {
                    foreach (var junctionOverlapId in laneOverlapsInfo[laneSegment.gameObject].junctionOverlapIds)
                    {
                        lane.overlap_id.Add(junctionOverlapId.Value);
                    }
                }

                if (laneHasParkingSpace.Contains(laneSegment.gameObject))
                {
                    foreach (var parkingSpaceOverlapId in laneOverlapsInfo[laneSegment.gameObject].parkingSpaceOverlapIds)
                    {
                        lane.overlap_id.Add(parkingSpaceOverlapId.Value);
                    }
                }

                if (laneHasSpeedBump.Contains(laneSegment.gameObject))
                {
                    foreach (var speedBumpOverlapId in laneOverlapsInfo[laneSegment.gameObject].speedBumpOverlapIds)
                    {
                        lane.overlap_id.Add(speedBumpOverlapId.Value);
                    }
                }

                if (laneHasClearArea.Contains(laneSegment.gameObject))
                {
                    foreach (var clearAreaOverlapId in laneOverlapsInfo[laneSegment.gameObject].clearAreaOverlapIds)
                    {
                        lane.overlap_id.Add(clearAreaOverlapId.Value);
                    }
                }

                if (laneHasCrossWalk.Contains(laneSegment.gameObject))
                {
                    foreach (var crossWalkOverlapId in laneOverlapsInfo[laneSegment.gameObject].crossWalkOverlapIds)
                    {
                        lane.overlap_id.Add(crossWalkOverlapId.Value);
                    }
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

            foreach (var road in roadSet)
            {
                if (road.section[0].boundary.outer_polygon.edge.Count == 0)
                {
                    Debug.Log("You have no boundary edges in some roads, please check!!!");
                    return false;
                }

                foreach (var lane in roadIdToLanes[road.id.id])
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

                    if (!MakeStoplineLaneOverlaps(stopline, lanesToInspec, stoplineWidth, signal_Id, OverlapType.Signal_Stopline_Lane, stoplinePts, laneIds2OverlapIdsMapping, overlap_ids, signalLight.gameObject))
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

                    signal.subsignal.AddRange(subsignals);
                    signalOverlapsInfo.GetOrCreate(signalLight.gameObject).id = signalId;

                    if (signalOverlapsInfo.ContainsKey(signalLight.gameObject))
                    {
                        var signalOverlapInfo = new HD.ObjectOverlapInfo()
                        {
                            id = signalId,
                            signal_overlap_info = new HD.SignalOverlapInfo(),
                        };

                        foreach (var overlapId in signalOverlapsInfo[signalLight.gameObject].laneOverlapIds.Values)
                        {
                            overlap_ids.Add(overlapId);
                        }
                        foreach (var overlapId in signalOverlapsInfo[signalLight.gameObject].junctionOverlapIds.Values)
                        {
                            overlap_ids.Add(overlapId);
                        }
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

                    if (!MakeStoplineLaneOverlaps(stopline, lanesToInspec, stoplineWidth, stopsign_Id, OverlapType.Stopsign_Stopline_Lane, stoplinePts, laneIds2OverlapIdsMapping, overlap_ids, stopSign.gameObject))
                    {
                        return false;
                    } 
                }

                if (stoplinePts != null && stoplinePts.Count > 2)
                {
                    var stopId = HdId($"stopsign_{stopsign_Id}");

                    if (stopSignOverlapsInfo.ContainsKey(stopSign.gameObject))
                    {
                        var stopOverlapInfo = new HD.ObjectOverlapInfo()
                        {
                            id = stopId,
                            stop_sign_overlap_info = new HD.StopSignOverlapInfo(),
                        };

                        foreach (var overlapId in stopSignOverlapsInfo[stopSign.gameObject].laneOverlapIds.Values)
                        {
                            overlap_ids.Add(overlapId);
                        }
                        foreach (var overlapId in stopSignOverlapsInfo[stopSign.gameObject].junctionOverlapIds.Values)
                        {
                            overlap_ids.Add(overlapId);
                        }
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

            MakeJunctionAnnotation();
            MakeParkingSpaceAnnotation();
            MakeSpeedBumpAnnotation();
            MakeClearAreaAnnotation();
            MakeCrossWalkAnnotation();

            return true;
        }

        bool MakeStoplineLaneOverlaps(MapLine stopline, List<MapLane> lanesToInspec, float stoplineWidth, int overlapInfoId, OverlapType overlapType, List<ApolloCommon.PointENU> stoplinePts, Dictionary<string, List<HD.Id>> laneId2OverlapIdsMapping, List<HD.Id> overlap_ids, GameObject gameObject)
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
                            var id = HdId($"signal_{overlapInfoId}");
                            objOverlapInfo = new HD.ObjectOverlapInfo()
                            {
                                id = id,
                                signal_overlap_info = new HD.SignalOverlapInfo(),
                            };

                            signalOverlapsInfo.GetOrCreate(gameObject).id = id;
                        }
                        else if (overlapType == OverlapType.Stopsign_Stopline_Lane)
                        {
                            var id = HdId($"stopsign_{overlapInfoId}");
                            objOverlapInfo = new HD.ObjectOverlapInfo()
                            {
                                id = id,
                                stop_sign_overlap_info = new HD.StopSignOverlapInfo(),
                            };

                            stopSignOverlapsInfo.GetOrCreate(gameObject).id = id;
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

        void MakeInfoOfJunction()
        {
            foreach (var intersection in intersections)
            {
                var junctionList = intersection.transform.GetComponentsInChildren<MapJunction>().ToList();

                foreach (var junction in junctionList)
                {
                    var junctionId = HdId($"junction_{intersections.IndexOf(intersection)}_{junctionList.IndexOf(junction)}");
                    var junctionOverlapIds = new List<HD.Id>();
                    junctionOverlapsInfo.GetOrCreate(junction.gameObject).id = junctionId;

                    // LaneSegment
                    var laneList = intersection.transform.GetComponentsInChildren<MapLane>().ToList();
                    foreach (var lane in laneList)
                    {
                        var overlapId = HdId($"overlap_junction_{intersections.IndexOf(intersection)}_{junctionList.IndexOf(junction)}_lane_{intersections.IndexOf(intersection)}_{laneList.IndexOf(lane)}");
                        junctionOverlapsInfo.GetOrCreate(junction.gameObject).laneOverlapIds.Add(lane.gameObject, overlapId);
                        laneOverlapsInfo.GetOrCreate(lane.gameObject).junctionOverlapIds.Add(junction.gameObject, overlapId);

                        if (!laneHasJunction.Contains(lane.gameObject))
                            laneHasJunction.Add(lane.gameObject);
                    }

                    // StopSign
                    var stopSignList = intersection.transform.GetComponentsInChildren<MapSign>().ToList();
                    foreach (var stopSign in stopSignList)
                    {
                        var overlapId = HdId($"overlap_junction{junctionList.IndexOf(junction)}_stopsign{stopSignList.IndexOf(stopSign)}");                        
                        junctionOverlapsInfo.GetOrCreate(junction.gameObject).stopSignOverlapIds.Add(stopSign.gameObject, overlapId);
                        stopSignOverlapsInfo.GetOrCreate(stopSign.gameObject).junctionOverlapIds.Add(junction.gameObject, overlapId);
                    }

                    // Signal
                    var signalList = intersection.transform.GetComponentsInChildren<MapSignal>().ToList();
                    foreach (var signal in signalList)
                    {
                        var overlapId = HdId($"overlap_junction_{intersections.IndexOf(intersection)}_{junctionList.IndexOf(junction)}_signal_{intersections.IndexOf(intersection)}_{signalList.IndexOf(signal)}");
                        junctionOverlapsInfo.GetOrCreate(junction.gameObject).signalOverlapIds.Add(signal.gameObject, overlapId);
                        signalOverlapsInfo.GetOrCreate(signal.gameObject).junctionOverlapIds.Add(junction.gameObject, overlapId);
                    }
                }
            }
        }
        void MakeJunctionAnnotation()
        {
            foreach (var intersection in intersections)
            {
                var junctionList = intersection.transform.GetComponentsInChildren<MapJunction>().ToList();
                foreach (var junction in junctionList)
                {
                    var junctionInWorld = new List<Vector3>();
                    var polygon = new HD.Polygon();

                    foreach (var localPos in junction.mapLocalPositions)
                        junctionInWorld.Add(junction.transform.TransformPoint(localPos));

                    foreach (var pt in junctionInWorld)
                    {
                        var ptInApollo = HDMapUtil.GetApolloCoordinates(pt, OriginEasting, OriginNorthing, false);
                        polygon.point.Add(new ApolloCommon.PointENU()
                        {
                            x = ptInApollo.x,
                            y = ptInApollo.y,
                            z = ptInApollo.z,
                        });
                    }

                    var junctionId = junctionOverlapsInfo[junction.gameObject].id;
                    var junctionOverlapIds = new List<HD.Id>();

                    // LaneSegment
                    var laneList = intersection.transform.GetComponentsInChildren<MapLane>().ToList();
                    foreach (var lane in laneList)
                    {
                        var overlapId = junctionOverlapsInfo[junction.gameObject].laneOverlapIds[lane.gameObject];                        
                        junctionOverlapIds.Add(overlapId);

                        // Overlap Annotation
                        var objectLane = new HD.ObjectOverlapInfo()
                        {
                            id = laneOverlapsInfo[lane.gameObject].id,
                            lane_overlap_info = new HD.LaneOverlapInfo()
                            {
                                start_s = 0,        // TODO
                                end_s = laneOverlapsInfo[lane.gameObject].mLength,  // TODO
                                is_merge =false,
                            }
                        };

                        var objectJunction = new HD.ObjectOverlapInfo()
                        {
                            id = junctionOverlapsInfo[junction.gameObject].id,
                            junction_overlap_info = new HD.JunctionOverlapInfo(),
                        };

                        var overlap = new HD.Overlap()
                        {
                            id = overlapId,
                        };
                        overlap.@object.Add(objectLane);
                        overlap.@object.Add(objectJunction);
                        Hdmap.overlap.Add(overlap);
                    }

                    // StopSign
                    var stopSignList = intersection.transform.GetComponentsInChildren<MapSign>().ToList();
                    foreach (var stopSign in stopSignList)
                    {
                        var overlapId = junctionOverlapsInfo[junction.gameObject].stopSignOverlapIds[stopSign.gameObject];

                        junctionOverlapIds.Add(overlapId);

                        var objectStopSign = new HD.ObjectOverlapInfo()
                        {
                            id = stopSignOverlapsInfo[stopSign.gameObject].id,
                            stop_sign_overlap_info = new HD.StopSignOverlapInfo(),
                        };

                        var objectJunction = new HD.ObjectOverlapInfo()
                        {
                            id = junctionOverlapsInfo[junction.gameObject].id,
                            junction_overlap_info = new HD.JunctionOverlapInfo(),
                        };

                        var overlap = new HD.Overlap()
                        {
                            id = overlapId,
                        };
                        overlap.@object.Add(objectStopSign);
                        overlap.@object.Add(objectJunction);
                        Hdmap.overlap.Add(overlap);
                    }
                    
                    // SignalLight
                    var signalList = intersection.transform.GetComponentsInChildren<MapSignal>().ToList();
                    foreach (var signal in signalList)
                    {
                        var overlapId = junctionOverlapsInfo[junction.gameObject].signalOverlapIds[signal.gameObject];
                        junctionOverlapIds.Add(overlapId);

                        var objectSignalLight = new HD.ObjectOverlapInfo()
                        {
                            id = signalOverlapsInfo[signal.gameObject].id,
                            signal_overlap_info = new HD.SignalOverlapInfo(),
                        };

                        var objectJunction = new HD.ObjectOverlapInfo()
                        {
                            id = junctionOverlapsInfo[junction.gameObject].id,
                            junction_overlap_info = new HD.JunctionOverlapInfo(),
                        };

                        var overlap = new HD.Overlap()
                        {
                            id = overlapId,
                        };
                        overlap.@object.Add(objectSignalLight);
                        overlap.@object.Add(objectJunction);
                        Hdmap.overlap.Add(overlap);
                    }

                    // Junction Annotation
                    var junctionAnnotation = new HD.Junction()
                    {
                        id = junctionId,
                        polygon = polygon,
                    };
                    junctionAnnotation.overlap_id.AddRange(junctionOverlapIds);
                    Hdmap.junction.Add(junctionAnnotation);
               }
            }
        }

        void MakeInfoOfParkingSpace()
        {
            var parkingSpaceList = new List<MapParkingSpace>();
            parkingSpaceList.AddRange(MapAnnotationData.GetData<MapParkingSpace>());

            double dist = double.MaxValue;
            foreach (var parkingSpace in parkingSpaceList)
            {
                var parkingSpaceInWorld = new List<Vector3>();
                foreach (var localPos in parkingSpace.mapLocalPositions)
                {
                    parkingSpaceInWorld.Add(parkingSpace.transform.TransformPoint(localPos));
                }

                dist = double.MaxValue;

                // Find nearest lane to parking space
                GameObject nearestLaneGameObject = null;

                foreach (var lane in laneSegments)
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

                laneHasParkingSpace.Add(nearestLaneGameObject);
                parkingSpaceOverlapsInfo.GetOrCreate(parkingSpace.gameObject).laneOverlapIds.Add(nearestLaneGameObject, overlapId);
                laneOverlapsInfo.GetOrCreate(nearestLaneGameObject).parkingSpaceOverlapIds.Add(parkingSpace.gameObject, overlapId);
            }
        }

        void MakeParkingSpaceAnnotation()
        {
            var parkingSpaceList = new List<MapParkingSpace>();
            parkingSpaceList.AddRange(MapAnnotationData.GetData<MapParkingSpace>());

            foreach (var parkingSpace in parkingSpaceList)
            {
                var polygon = new HD.Polygon();
                var parkingSpaceInWorld = new List<Vector3>();
                foreach (var localPos in parkingSpace.mapLocalPositions)
                    parkingSpaceInWorld.Add(parkingSpace.transform.TransformPoint(localPos));

                var vector = new Vector2((parkingSpaceInWorld[1] - parkingSpaceInWorld[2]).x, (parkingSpaceInWorld[1] - parkingSpaceInWorld[2]).z);

                var heading = Mathf.Atan2(vector.y, vector.x);
                heading = (heading < 0) ? (float)(heading + Mathf.PI * 2) : heading;

                foreach (var pt in parkingSpaceInWorld)
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
                var parkingSpaceAnnotation = new HD.ParkingSpace()
                {
                    id = parkingSpaceId,
                    polygon = polygon,
                    heading = heading,
                };

                var frontSegment = new List<Vector3>();
                frontSegment.Add(parkingSpaceInWorld[0]);
                frontSegment.Add(parkingSpaceInWorld[3]);

                var backSegment = new List<Vector3>();
                backSegment.Add(parkingSpaceInWorld[1]);
                backSegment.Add(parkingSpaceInWorld[2]);

                foreach (var lane in parkingSpaceOverlapsInfo[parkingSpace.gameObject].laneOverlapIds.Keys)
                {
                    var start_s = FindSegmentDistNotOnLane(frontSegment, lane);
                    var end_s = FindSegmentDistNotOnLane(backSegment, lane);
                    // Overlap lane

                    var laneOverlapInfo = new HD.LaneOverlapInfo()
                    {
                        start_s = start_s,
                        end_s = end_s,
                        is_merge = false,
                    };

                    // lane which has parking space overlap
                    var objectLane = new HD.ObjectOverlapInfo()
                    {
                        id = laneOverlapsInfo[lane.gameObject].id,
                        lane_overlap_info = laneOverlapInfo,
                    };

                    var overlap = new HD.Overlap()
                    {
                        id = parkingSpaceOverlapsInfo[parkingSpace.gameObject].laneOverlapIds[lane.gameObject],
                    };

                    var objectParkingSpace = new HD.ObjectOverlapInfo()
                    {
                        id = parkingSpaceId,
                        parking_space_overlap_info = new HD.ParkingSpaceOverlapInfo(),
                    };

                    overlap.@object.Add(objectLane);
                    overlap.@object.Add(objectParkingSpace);
                    Hdmap.overlap.Add(overlap);

                    parkingSpaceAnnotation.overlap_id.Add(parkingSpaceOverlapsInfo[parkingSpace.gameObject].laneOverlapIds[lane.gameObject]);
                }
                Hdmap.parking_space.Add(parkingSpaceAnnotation);
            }
        }

        void MakeInfoOfSpeedBump()
        {
            var speedBumpList = new List<MapSpeedBump>();
            speedBumpList.AddRange(MapAnnotationData.GetData<MapSpeedBump>());

            foreach (var speedBump in speedBumpList)
            {
                var speedBumpInWorld = new List<Vector3>();
                foreach (var localPos in speedBump.mapLocalPositions)
                {
                    speedBumpInWorld.Add(speedBump.transform.TransformPoint(localPos));
                }

                var speedBumpId = HdId($"speed_bump_{speedBumpList.IndexOf(speedBump)}");

                foreach (var lane in laneSegments)
                {
                    for (int i = 0; i < lane.mapWorldPositions.Count - 1; i++)
                    {
                        var p1 = new Vector2(lane.mapWorldPositions[i].x, lane.mapWorldPositions[i].z);
                        var p2 = new Vector2(lane.mapWorldPositions[i+1].x, lane.mapWorldPositions[i+1].z);
                        var q1 = new Vector2(speedBumpInWorld[0].x, speedBumpInWorld[0].z);
                        var q2 = new Vector2(speedBumpInWorld[1].x, speedBumpInWorld[1].z);

                        if (DoIntersect(p1, p2, q1, q2))
                        {
                            if (speedBumpOverlapsInfo.GetOrCreate(speedBump.gameObject).id == null)
                                speedBumpOverlapsInfo.GetOrCreate(speedBump.gameObject).id = speedBumpId;

                            var overlapId = HdId($"overlap_speed_bump_{speedBumpList.IndexOf(speedBump)}_lane_{laneSegments.IndexOf(lane)}");
                            laneOverlapsInfo.GetOrCreate(lane.gameObject).speedBumpOverlapIds.Add(speedBump.gameObject, overlapId);
                            speedBumpOverlapsInfo.GetOrCreate(speedBump.gameObject).laneOverlapIds.Add(lane.gameObject, overlapId);
                            laneHasSpeedBump.Add(lane.gameObject);
                            break;
                        }
                    }       
                }
            }
        }

        void MakeSpeedBumpAnnotation()
        {
            var speedBumpList = new List<MapSpeedBump>();
            speedBumpList.AddRange(MapAnnotationData.GetData<MapSpeedBump>());

            foreach (var speedBump in speedBumpList)
            {
                var speedBumpInWorld = new List<Vector3>();
                var lineSegment = new HD.LineSegment();
                var localPos = speedBump.mapLocalPositions[0];
                speedBumpInWorld.Add(speedBump.transform.TransformPoint(localPos));
                localPos = speedBump.mapLocalPositions[1];
                speedBumpInWorld.Add(speedBump.transform.TransformPoint(localPos));

                foreach (var pt in speedBumpInWorld)
                {
                    var ptInApollo = HDMapUtil.GetApolloCoordinates(pt, OriginEasting, OriginNorthing, false);
                    lineSegment.point.Add(ptInApollo);
                }

                var speedBumpAnnotation = new HD.SpeedBump()
                {
                    id = speedBumpOverlapsInfo[speedBump.gameObject].id,
                };

                foreach (var lane in speedBumpOverlapsInfo[speedBump.gameObject].laneOverlapIds.Keys)
                {
                    
                    var s = FindSegmentDistOnLane(speedBumpInWorld, lane.gameObject);

                    var laneOverlapInfo = new HD.LaneOverlapInfo()
                    {
                        start_s = s - 0.5, // Todo:
                        end_s = s + 1.0, // Todo:
                        is_merge = false,
                    };

                    // lane
                    var objectLane = new HD.ObjectOverlapInfo()
                    {
                        id = laneOverlapsInfo[lane.gameObject].id,
                        lane_overlap_info = laneOverlapInfo,
                    };

                    var objectSpeedBump = new HD.ObjectOverlapInfo()
                    {
                        id = speedBumpOverlapsInfo[speedBump.gameObject].id,
                        speed_bump_overlap_info = new HD.SpeedBumpOverlapInfo(),
                    };

                    var overlap = new HD.Overlap()
                    {
                        id = speedBumpOverlapsInfo[speedBump.gameObject].laneOverlapIds[lane.gameObject],
                    };
                    overlap.@object.Add(objectLane);
                    overlap.@object.Add(objectSpeedBump);
                    Hdmap.overlap.Add(overlap);

                    speedBumpAnnotation.overlap_id.Add(speedBumpOverlapsInfo[speedBump.gameObject].laneOverlapIds[lane]);

                    var position = new HD.Curve();
                    var segment = new HD.CurveSegment()
                    {
                        line_segment = lineSegment,
                    };
                    position.segment.Add(segment);
                    speedBumpAnnotation.position.Add(position);
                }
                Hdmap.speed_bump.Add(speedBumpAnnotation);
            }
        }

        void MakeInfoOfClearArea()
        {
            var clearAreaList = new List<MapClearArea>();
            clearAreaList.AddRange(MapAnnotationData.GetData<MapClearArea>());

            foreach (var clearArea in clearAreaList)
            {
                var clearAreaInWorld = new List<Vector3>();
                foreach (var localPos in clearArea.mapLocalPositions)
                {
                    clearAreaInWorld.Add(clearArea.transform.TransformPoint(localPos));
                }

                // Sequence of vertices in Rectangle
                // [0]   [1]
                // --------- lane
                // [3]   [2]
                foreach (var lane in laneSegments)
                {
                    for (int i = 0; i < lane.mapWorldPositions.Count - 1; i++)
                    {
                        var p1 = new Vector2(lane.mapWorldPositions[i].x, lane.mapWorldPositions[i].z);
                        var p2 = new Vector2(lane.mapWorldPositions[i+1].x, lane.mapWorldPositions[i+1].z);
                    
                        var q0 = new Vector2(clearAreaInWorld[0].x, clearAreaInWorld[0].z);
                        var q3 = new Vector2(clearAreaInWorld[3].x, clearAreaInWorld[3].z);
                        
                        if (DoIntersect(p1, p2, q0, q3))
                        {
                            var clearAreaId = HdId($"clear_area_{clearAreaList.IndexOf(clearArea)}_lane_{laneSegments.IndexOf(lane)}");
                            if (clearAreaOverlapsInfo.GetOrCreate(clearArea.gameObject).id == null)
                                clearAreaOverlapsInfo.GetOrCreate(clearArea.gameObject).id = clearAreaId;

                            var overlapId = HdId($"overlap_clear_area_{clearAreaList.IndexOf(clearArea)}_lane_{laneSegments.IndexOf(lane)}");
                            laneOverlapsInfo.GetOrCreate(lane.gameObject).clearAreaOverlapIds.Add(clearArea.gameObject, overlapId);
                            clearAreaOverlapsInfo.GetOrCreate(clearArea.gameObject).laneOverlapIds.Add(lane.gameObject, overlapId);
                            laneHasClearArea.Add(lane.gameObject);
                        }
                    }
                }
            }
        }

        void MakeClearAreaAnnotation()
        {
            var clearAreaList = new List<MapClearArea>();
            clearAreaList.AddRange(MapAnnotationData.GetData<MapClearArea>());

            foreach (var clearArea in clearAreaList)
            {
                var clearAreaInWorld = new List<Vector3>();
                var frontSegment = new List<Vector3>();
                var backSegment = new List<Vector3>();

                var lineSegment = new HD.LineSegment();
                var polygon = new HD.Polygon();
                foreach (var localPos in clearArea.mapLocalPositions)
                    clearAreaInWorld.Add(clearArea.transform.TransformPoint(localPos));
                foreach (var vertex in clearAreaInWorld)
                {
                    var vertexInApollo = HDMapUtil.GetApolloCoordinates(vertex, OriginEasting, OriginNorthing, false);
                    polygon.point.Add(new ApolloCommon.PointENU()
                    {
                        x = vertexInApollo.x,
                        y = vertexInApollo.y,
                        z = vertexInApollo.z,
                    });
                }

                frontSegment.Add(clearAreaInWorld[0]);
                frontSegment.Add(clearAreaInWorld[3]);
                backSegment.Add(clearAreaInWorld[1]);
                backSegment.Add(clearAreaInWorld[2]);

                HD.ClearArea clearAreaAnnotation = new HD.ClearArea();

                foreach (var laneOverlap in clearAreaOverlapsInfo[clearArea.gameObject].laneOverlapIds)
                {
                    var start_s = FindSegmentDistOnLane(frontSegment, laneOverlap.Key);
                    var end_s = FindSegmentDistOnLane(backSegment, laneOverlap.Key);

                    var laneOverlapInfo = new HD.LaneOverlapInfo()
                    {
                        start_s = Mathf.Min(start_s, end_s),
                        end_s = Mathf.Max(start_s, end_s),
                        is_merge = false,
                    };

                    var objectLane = new HD.ObjectOverlapInfo()
                    {
                        id = laneOverlapsInfo[laneOverlap.Key].id,
                        lane_overlap_info = laneOverlapInfo,
                    };

                    var objectClearArea = new HD.ObjectOverlapInfo()
                    {
                        id = clearAreaOverlapsInfo[clearArea.gameObject].id,
                        clear_area_overlap_info = new HD.ClearAreaOverlapInfo(),
                    };

                    var overlap = new HD.Overlap()
                    {
                        id = clearAreaOverlapsInfo[clearArea.gameObject].laneOverlapIds[laneOverlap.Key],
                    };

                    overlap.@object.Add(objectLane);
                    overlap.@object.Add(objectClearArea);
                    Hdmap.overlap.Add(overlap);

                    clearAreaAnnotation.id = clearAreaOverlapsInfo[clearArea.gameObject].id;

                    clearAreaAnnotation.overlap_id.Add(clearAreaOverlapsInfo.GetOrCreate(clearArea.gameObject).laneOverlapIds[laneOverlap.Key]);
                }

                clearAreaAnnotation.polygon = polygon;
                Hdmap.clear_area.Add(clearAreaAnnotation);
            }
        }

        void MakeInfoOfCrossWalk()
        {
            var crossWalkList = new List<MapCrossWalk>();
            crossWalkList.AddRange(MapAnnotationData.GetData<MapCrossWalk>());

            foreach (var crossWalk in crossWalkList)
            {
                var crossWalkInWorld = new List<Vector3>();
                foreach (var localPos in crossWalk.mapLocalPositions)
                {
                    crossWalkInWorld.Add(crossWalk.transform.TransformPoint(localPos));
                }

                // Sequence of vertices in Rectangle
                // [0]   [1]
                // |       |
                // --------- lane
                // |       |
                // [3]   [2]

                var crossWalkId = HdId($"crosswalk_{crossWalkList.IndexOf(crossWalk)}");

                foreach (var lane in laneSegments)
                {
                    for (int i = 0; i < lane.mapWorldPositions.Count - 1; i++)
                    {
                        var p1 = new Vector2(lane.mapWorldPositions[i].x, lane.mapWorldPositions[i].z);
                        var p2 = new Vector2(lane.mapWorldPositions[i+1].x, lane.mapWorldPositions[i+1].z);

                        var q0 = new Vector2(crossWalkInWorld[0].x, crossWalkInWorld[0].z);
                        var q1 = new Vector2(crossWalkInWorld[1].x, crossWalkInWorld[1].z);
                        var q2 = new Vector2(crossWalkInWorld[2].x, crossWalkInWorld[2].z);
                        var q3 = new Vector2(crossWalkInWorld[3].x, crossWalkInWorld[3].z);

                        if (DoIntersect(p1, p2, q0, q3))
                        {
                            if (crossWalkOverlapsInfo.GetOrCreate(crossWalk.gameObject).id == null)
                                crossWalkOverlapsInfo.GetOrCreate(crossWalk.gameObject).id = crossWalkId;

                            var overlapId = HdId($"overlap_{crossWalkId.id}_lane_{laneSegments.IndexOf(lane)}");

                            laneOverlapsInfo.GetOrCreate(lane.gameObject).crossWalkOverlapIds.Add(crossWalk.gameObject, overlapId);
                            crossWalkOverlapsInfo.GetOrCreate(crossWalk.gameObject).laneOverlapIds.Add(lane.gameObject, overlapId);
                            laneHasCrossWalk.Add(lane.gameObject);
                        }
                    }
                }
            }
        }

        void MakeCrossWalkAnnotation()
        {
            var crossWalkList = new List<MapCrossWalk>();
            crossWalkList.AddRange(MapAnnotationData.GetData<MapCrossWalk>());

            foreach (var crossWalk in crossWalkList)
            {
                var crossWalkInWorld = new List<Vector3>();
                var frontSegment = new List<Vector3>();
                var backSegment = new List<Vector3>();

                var polygon = new HD.Polygon();
                foreach (var localPos in crossWalk.mapLocalPositions)
                {
                    crossWalkInWorld.Add(crossWalk.transform.TransformPoint(localPos));
                }

                foreach (var vertex in crossWalkInWorld)
                {
                    var vertexInApollo = HDMapUtil.GetApolloCoordinates(vertex, OriginEasting, OriginNorthing, false);
                    polygon.point.Add(vertexInApollo);
                }

                frontSegment.Add(crossWalkInWorld[0]);
                frontSegment.Add(crossWalkInWorld[3]);

                backSegment.Add(crossWalkInWorld[1]);
                backSegment.Add(crossWalkInWorld[2]);

                HD.Crosswalk crossWalkAnnotation = new HD.Crosswalk();

                foreach (var lane in crossWalkOverlapsInfo[crossWalk.gameObject].laneOverlapIds.Keys)
                {
                    var start_s = FindSegmentDistOnLane(frontSegment, lane.gameObject);
                    var end_s = FindSegmentDistOnLane(backSegment, lane.gameObject);

                    var objectLane = new HD.ObjectOverlapInfo()
                    {
                        id = laneOverlapsInfo[lane.gameObject].id,
                        lane_overlap_info = new HD.LaneOverlapInfo()
                        {
                            start_s = Mathf.Min(start_s, end_s),
                            end_s = Mathf.Max(start_s, end_s),
                            is_merge = false,
                        }
                    };

                    var objectCrossWalk = new HD.ObjectOverlapInfo()
                    {
                        id = crossWalkOverlapsInfo[crossWalk.gameObject].id,
                        crosswalk_overlap_info = new HD.CrosswalkOverlapInfo(),
                    };

                    var overlap = new HD.Overlap()
                    {
                        id = crossWalkOverlapsInfo[crossWalk.gameObject].laneOverlapIds[lane.gameObject],
                    };

                    overlap.@object.Add(objectLane);
                    overlap.@object.Add(objectCrossWalk);
                    Hdmap.overlap.Add(overlap);

                    crossWalkAnnotation.id = crossWalkOverlapsInfo[crossWalk.gameObject].id;

                    crossWalkAnnotation.overlap_id.Add(crossWalkOverlapsInfo.GetOrCreate(crossWalk.gameObject).laneOverlapIds[lane.gameObject]);
                }

                crossWalkAnnotation.polygon = polygon;
                Hdmap.crosswalk.Add(crossWalkAnnotation);
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
        // Segment is intersected with lane.
        float FindSegmentDistOnLane(List<Vector3> mapWorldPositions, GameObject lane)
        {
            var firstPtSegment = new Vector2()
            {
                x = mapWorldPositions[0].x,
                y = mapWorldPositions[0].z,
            };
            var secondPtSegment = new Vector2()
            {
                x = mapWorldPositions[1].x,
                y = mapWorldPositions[1].z,
            };

            float startS = 0;
            float totalS = 0;

            foreach (var seg in laneSegments.Where(seg => seg.gameObject == lane))
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

            return startS;
        }
        // Segment is not intersected with lane.
        float FindSegmentDistNotOnLane(List<Vector3> mapWorldPositions, GameObject lane)
        {
            var onePtSegment = new Vector2()
            {
                x = mapWorldPositions[0].x,
                y = mapWorldPositions[0].z,
            };
            var otherPtSegment = new Vector2()
            {
                x = mapWorldPositions[1].x,
                y = mapWorldPositions[1].z,
            };

            float startS = 0;
            float totalS = 0;

            foreach (var seg in laneSegments.Where(seg => seg.gameObject == lane))
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

                    var closestPt = new Vector2();
                    Vector2 nearPtSegment;
                    var d1 = FindDistanceToSegment(onePtSegment, firstPtLane, secondPtLane, out closestPt);
                    var d2 = FindDistanceToSegment(otherPtSegment, firstPtLane, secondPtLane, out closestPt);

                    if (d1 > d2)
                    {
                        nearPtSegment = otherPtSegment;
                    }
                    else
                    {
                        nearPtSegment = onePtSegment;
                    }

                    if (FindDistanceToSegment(nearPtSegment, firstPtLane, secondPtLane, out closestPt) < 5.0)
                    {
                        var ptOnLane = new Vector2();
                        FindDistanceToSegment(nearPtSegment, firstPtLane, secondPtLane, out ptOnLane);

                        d1 = Vector2.Distance(firstPtLane, ptOnLane);
                        d2 = Vector2.Distance(secondPtLane, ptOnLane);
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

            return startS;
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
