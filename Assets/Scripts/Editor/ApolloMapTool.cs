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
using UnityEngine;

using HD = apollo.hdmap;
using ApolloCommon = apollo.common;
using Simulator.Editor.Apollo;
using Simulator.Map;

namespace Simulator.Editor
{
    interface ADMap
    {
        string Id
        {
            get;
            set;
        }
    }

    interface OverlapInfo
    {
        HD.Id id
        {
            get;
            set;
        }
    }

    public enum ApolloBoundaryType
    {
        UNKNOWN = 0,
        DOTTED_YELLOW = 1,
        DOTTED_WHITE = 2,
        SOLID_YELLOW = 3,
        SOLID_WHITE = 4,
        DOUBLE_YELLOW = 5,
        CURB = 6,
        VIRTUAL = 7,
    }

    class ADMapLane : ADMap
    {
        public string Id
        {
            get;
            set;
        }
        public bool needSelfReverseLane = false;
        public bool isSelfReverseLane = false;
        public List<ADMapLane> befores { get; set; } = new List<ADMapLane>();
        public List<ADMapLane> afters { get; set; } = new List<ADMapLane>();
        public ADMapLane leftLaneForward = null;
        public ADMapLane rightLaneForward = null;
        public ADMapLane leftLaneReverse = null;
        public ADMapLane rightLaneReverse = null;
        public List<Vector3> mapWorldPositions;
        public int laneCount;
        public int laneNumber;
        public MapData.LaneTurnType laneTurnType = MapData.LaneTurnType.NO_TURN;
        public ApolloBoundaryType leftBoundType;
        public ApolloBoundaryType rightBoundType;

        public float speedLimit = 20.0f;
        public static int laneStaticId = 0;

        public string selfReverseLaneId;

        public static Dictionary<string, ADMapLane> id2ADMapLane = new Dictionary<string, ADMapLane>();
        public static Dictionary<string, MapLane> id2MapLane = new Dictionary<string, MapLane>();
        public ADMapLane() {}

        public ADMapLane(MapLane lane)
        {
            Id = lane.id;

            if (!id2ADMapLane.ContainsKey(lane.id))
            {
                id2ADMapLane.Add(lane.id, this);
                id2MapLane.Add(lane.id, lane);
            }
            else
            {
                Debug.LogError($"You should not add same lane {lane.id} twice!");
            }

            needSelfReverseLane = lane.needSelfReverseLane;
            isSelfReverseLane = lane.isSelfReverseLane;
            laneCount = lane.laneCount;
            laneNumber = lane.laneNumber;
            laneTurnType = lane.laneTurnType;
            BoundaryLineConversion(lane, out leftBoundType, out rightBoundType);
            speedLimit = lane.speedLimit;

            mapWorldPositions = new List<Vector3>(lane.mapWorldPositions);
        }

        public void BoundaryLineConversion(MapLane lane, out ApolloBoundaryType leftBoundaryType, out ApolloBoundaryType rightBoundaryType)
        {
            if (lane.leftLineBoundry == null)
            {
                Debug.LogWarning("MapLane is missing left boundary", lane.gameObject);
            }
            leftBoundaryType = LineTypeToBoundaryType(lane.leftLineBoundry);

            if (lane.rightLineBoundry == null)
            {
                Debug.LogWarning("MapLane is missing right boundary", lane.gameObject);
            }
            rightBoundaryType = LineTypeToBoundaryType(lane.rightLineBoundry);
        }

        public ApolloBoundaryType LineTypeToBoundaryType(MapLine line)
        {
            if (line != null)
            {
                var lineType = line.lineType;
                if (lineType == MapData.LineType.DOTTED_YELLOW) return ApolloBoundaryType.DOTTED_YELLOW;
                else if (lineType == MapData.LineType.DOTTED_WHITE) return ApolloBoundaryType.DOTTED_WHITE;
                else if (lineType == MapData.LineType.SOLID_YELLOW) return ApolloBoundaryType.SOLID_YELLOW;
                else if (lineType == MapData.LineType.SOLID_WHITE) return ApolloBoundaryType.SOLID_WHITE;
                else if (lineType == MapData.LineType.DOUBLE_YELLOW) return ApolloBoundaryType.DOUBLE_YELLOW;
                else if (lineType == MapData.LineType.CURB) return ApolloBoundaryType.CURB;
            }

            return ApolloBoundaryType.UNKNOWN;
        }
    }

    class ADMapParkingSpace : ADMap
    {
        public string Id
        {
            get;
            set;
        }
        public List<Vector3> mapWorldPositions;
        public static int parkingStaticId = 0;
        public ADMapParkingSpace() {}
        public ADMapParkingSpace(MapParkingSpace parkingSpace)
        {
            Id = parkingSpace.id;
            mapWorldPositions = new List<Vector3>(parkingSpace.mapWorldPositions);
        }
        public ADMapParkingSpace(MapParkingSpace parkingSpace, string id)
        {
            Id = id;
            mapWorldPositions = new List<Vector3>(parkingSpace.mapWorldPositions);
        }
    }

    class ADMapSpeedBump : ADMap
    {
        public string Id
        {
            get;
            set;
        }
        public List<Vector3> mapWorldPositions;
        public static int speedBumpStaticId = 0;
        public ADMapSpeedBump() {}
        public ADMapSpeedBump(MapSpeedBump speedBump)
        {
            Id = speedBump.id;
            mapWorldPositions = new List<Vector3>(speedBump.mapWorldPositions);
        }

        public ADMapSpeedBump(MapSpeedBump speedBump, string id)
        {
            Id = id;
            mapWorldPositions = new List<Vector3>(speedBump.mapWorldPositions);
        }
    }

    class ADMapClearArea : ADMap
    {
        public string Id
        {
            get;
            set;
        }
        public List<Vector3> mapWorldPositions;
        public static int clearAreaStaticId = 0;
        public ADMapClearArea() {}
        public ADMapClearArea(MapClearArea clearArea)
        {
            Id = clearArea.id;
            mapWorldPositions = new List<Vector3>(clearArea.mapWorldPositions);
        }

        public ADMapClearArea(MapClearArea clearArea, string id)
        {
            Id = id;
            mapWorldPositions = new List<Vector3>(clearArea.mapWorldPositions);
        }
    }

    class ADMapCrossWalk : ADMap
    {
        public string Id
        {
            get;
            set;
        }
        public List<Vector3> mapWorldPositions;
        public static int crossWalkStaticId = 0;
        public ADMapCrossWalk() {}
        public ADMapCrossWalk(MapCrossWalk crossWalk)
        {
            Id = crossWalk.id;
            mapWorldPositions = new List<Vector3>(crossWalk.mapWorldPositions);
        }

        public ADMapCrossWalk(MapCrossWalk crossWalk, string id)
        {
            Id = id;
            mapWorldPositions = new List<Vector3>(crossWalk.mapWorldPositions);
        }
    }

    class ADMapJunction : ADMap
    {
        public string Id
        {
            get;
            set;
        }
        public List<Vector3> mapWorldPositions;
        public static int junctionStaticId = 0;
        public ADMapJunction() {}
        public ADMapJunction(MapJunction junction)
        {
            Id = junction.id;
            mapWorldPositions = new List<Vector3>(junction.mapWorldPositions);
        }

        public ADMapJunction(MapJunction junction, string id)
        {
            Id = id;
            mapWorldPositions = new List<Vector3>(junction.mapWorldPositions);
        }
    }

    class ADMapSign : ADMap
    {
        public string Id
        {
            get;
            set;
        }
        public ADMapLine adMapLine;
        public List<Vector3> mapWorldPositions;

        public static int signStaticId = 0;
        public ADMapSign() {}
        public ADMapSign(MapSign stopSign)
        {
            Id = stopSign.id;
            adMapLine = new ADMapLine(stopSign.stopLine);
            mapWorldPositions = new List<Vector3>(stopSign.stopLine.mapWorldPositions);
        }

        public ADMapSign(MapSign stopSign, string id)
        {
            Id = id;
            adMapLine = new ADMapLine(stopSign.stopLine);
            mapWorldPositions = new List<Vector3>(stopSign.stopLine.mapWorldPositions);
        }
    }

    class ADMapSignal : ADMap
    {
        public string Id
        {
            get;
            set;
        }
        public ADMapLine adMapLine;
        public List<Vector3> mapWorldPositions;

        public static int signalStaticId = 0;
        public ADMapSignal() {}
        public ADMapSignal(MapSignal signal)
        {
            Id = signal.id;
            adMapLine = new ADMapLine(signal.stopLine);
            mapWorldPositions = new List<Vector3>(signal.stopLine.mapWorldPositions);
        }

        public ADMapSignal(MapSignal signal, string id)
        {
            Id = id;
            adMapLine = new ADMapLine(signal.stopLine);
            mapWorldPositions = new List<Vector3>(signal.stopLine.mapWorldPositions);
        }
    }

    class ADMapLine : ADMap
    {
        public string Id
        {
            get;
            set;
        }
        public List<Vector3> mapWorldPositions;
        public static int lineStaticId = 0;

        public ADMapLine(MapLine line)
        {
            Id = line.id;
            mapWorldPositions = new List<Vector3>(line.mapWorldPositions);
        }

        public ADMapLine(MapLine line, string id)
        {
            Id = id;
            mapWorldPositions = new List<Vector3>(line.mapWorldPositions);
        }
    }

    public class LaneOverlapInfo : OverlapInfo
    {
        public HD.Id id
        {
            get;
            set;
        }
        public Dictionary<string, HD.Id> signalOverlapIds = new Dictionary<string, HD.Id>();
        public Dictionary<string, HD.Id> stopSignOverlapIds = new Dictionary<string, HD.Id>();
        public Dictionary<string, HD.Id> parkingSpaceOverlapIds = new Dictionary<string, HD.Id>();
        public Dictionary<string, HD.Id> clearAreaOverlapIds = new Dictionary<string, HD.Id>();
        public Dictionary<string, HD.Id> speedBumpOverlapIds = new Dictionary<string, HD.Id>();
        public Dictionary<string, HD.Id> crossWalkOverlapIds = new Dictionary<string, HD.Id>();
        public Dictionary<string, HD.Id> yieldSignOverlapIds = new Dictionary<string, HD.Id>();
        public Dictionary<string, HD.Id> junctionOverlapIds = new Dictionary<string, HD.Id>();
        public Dictionary<string, HD.Id> laneOverlapIds = new Dictionary<string, HD.Id>();

        public float mLength;
        public string selfReverseLaneId;
    }

    public class SignalOverlapInfo : OverlapInfo
    {
        public HD.Id id
        {
            get;
            set;
        }
        public Dictionary<string, HD.Id> laneOverlapIds = new Dictionary<string, HD.Id>();
        public Dictionary<string, HD.Id> junctionOverlapIds = new Dictionary<string, HD.Id>();
        public Dictionary<string, List<HD.ObjectOverlapInfo>> laneOverlapInfos= new Dictionary<string, List<HD.ObjectOverlapInfo>>();
    }

    public class StopSignOverlapInfo : OverlapInfo
    {
        public HD.Id id
        {
            get;
            set;
        }
        public Dictionary<string, HD.Id> laneOverlapIds = new Dictionary<string, HD.Id>();
        public Dictionary<string, HD.Id> junctionOverlapIds = new Dictionary<string, HD.Id>();
        public Dictionary<GameObject, List<HD.ObjectOverlapInfo>> laneOverlapInfos= new Dictionary<GameObject, List<HD.ObjectOverlapInfo>>();
    }

    public class ClearAreaOverlapInfo : OverlapInfo
    {
        public HD.Id id
        {
            get;
            set;
        }
        public Dictionary<string, HD.Id> laneOverlapIds = new Dictionary<string, HD.Id>();
        public Dictionary<string, HD.Id> junctionOverlapIds = new Dictionary<string, HD.Id>();
        public HD.Polygon polygon;
    }

    public class CrossWalkOverlapInfo : OverlapInfo
    {
        public HD.Id id
        {
            get;
            set;
        }
        public Dictionary<string, HD.Id> laneOverlapIds = new Dictionary<string, HD.Id>();
        public Dictionary<string, HD.Id> junctionOverlapIds = new Dictionary<string, HD.Id>();
    }

    public class SpeedBumpOverlapInfo : OverlapInfo
    {
        public HD.Id id
        {
            get;
            set;
        }
        public Dictionary<string, HD.Id> laneOverlapIds = new Dictionary<string, HD.Id>();
    }

    public class ParkingSpaceOverlapInfo : OverlapInfo
    {
        public HD.Id id
        {
            get;
            set;
        }
        public Dictionary<string, HD.Id> laneOverlapIds_string = new Dictionary<string, HD.Id>();
    }

    public class JunctionOverlapInfo : OverlapInfo
    {
        public HD.Id id
        {
            get;
            set;
        }
        public HD.Polygon polygon;
        public Dictionary<string, HD.Id> laneOverlapIds = new Dictionary<string, HD.Id>();

        public Dictionary<string, HD.Id> signalOverlapIds = new Dictionary<string, HD.Id>();
        public Dictionary<string, HD.Id> stopSignOverlapIds = new Dictionary<string, HD.Id>();
        public Dictionary<string, HD.Id> parkingOverlapIds = new Dictionary<string, HD.Id>();
        public Dictionary<string, HD.Id> clearAreaOverlapIds = new Dictionary<string, HD.Id>();
        public Dictionary<string, HD.Id> crossWalkOverlapIds = new Dictionary<string, HD.Id>();
    }

    public class ApolloMapTool
    {
        public enum ApolloVersion
        {
            Apollo_3_0,
            Apollo_5_0
        }

        ApolloVersion Version;

        // The threshold between stopline and branching point. if a stopline-lane intersect is closer than this to a branching point then this stopline is a braching stopline
        const float StoplineIntersectThreshold = 1.5f;
        private double OriginNorthing;
        private double OriginEasting;
        int OriginZone;
        private float AltitudeOffset;
        MapOrigin mapOrigin;

        private HD.Map Hdmap;
        private MapManagerData MapAnnotationData;

        public enum OverlapType
        {
            Signal_Stopline_Lane,
            Stopsign_Stopline_Lane,
        }

        public ApolloMapTool(ApolloVersion version)
        {
            Version = version;
        }

        List<ADMapLane> laneSegments;
        List<MapSignal> signalLights;
        List<MapSign> stopSigns;
        List<MapIntersection> intersections;
        List<MapLaneSection> laneSections;
        List<MapLine> lineSegments;
        Dictionary<string, List<GameObject>> overlapIdToGameObjects;
        Dictionary<GameObject, HD.ObjectOverlapInfo> gameObjectToOverlapInfo;
        Dictionary<GameObject, HD.Id> gameObjectToOverlapId;
        Dictionary<string, HD.Id> objectIdToOverlapId;

        Dictionary<string, HD.Junction> overlapIdToJunction;
        Dictionary<string, List<string>> roadIdToLanes;

        // Data Structures for AD Stack Map
        List<ADMapLane> adMapLanes;
        List<ADMapParkingSpace> adMapParkingSpaces;
        List<ADMapSpeedBump> adMapSpeedBumps;
        List<ADMapClearArea> adMapClearAreas;
        List<ADMapCrossWalk> adMapCrossWalks;
        List<ADMapJunction> adMapJunctions;
        List<ADMapSign> adMapSigns;
        List<ADMapSignal> adMapSignals;
        List<ADMapLine> adMapLines;

        // Lane
        HashSet<ADMapLane> laneSegmentsSet;
        HashSet<MapLine> lineSegmentsSet;
        Dictionary<string, LaneOverlapInfo> laneOverlapsInfo;
        HashSet<string> laneHasParkingSpace;
        HashSet<string> laneHasSpeedBump;
        HashSet<string> laneHasJunction;
        HashSet<string> laneHasClearArea;
        HashSet<string> laneHasCrossWalk;
        // Signal
        Dictionary<string, SignalOverlapInfo> signalOverlapsInfo;
        // Stop
        Dictionary<string, StopSignOverlapInfo> stopSignOverlapsInfo;
        // Clear Area
        Dictionary<string, ClearAreaOverlapInfo> clearAreaOverlapsInfo;
        // Cross Walk
        Dictionary<string, CrossWalkOverlapInfo> crossWalkOverlapsInfo;
        // Junction
        Dictionary<string, JunctionOverlapInfo> junctionOverlapsInfo;
        // Speedbump
        Dictionary<string, SpeedBumpOverlapInfo> speedBumpOverlapsInfo;
        // Parking Space
        Dictionary<string, ParkingSpaceOverlapInfo> parkingSpaceOverlapsInfo;

        void CopyLaneRelations()
        {    
            var id2MapLane = ADMapLane.id2MapLane;
            var id2ADMapLane = ADMapLane.id2ADMapLane;
            foreach (var entry in id2ADMapLane)
            {
                var id = entry.Key;
                var lane = id2MapLane[id];
                var ADMapLane = entry.Value;
                if (lane.leftLaneForward != null) ADMapLane.leftLaneForward = id2ADMapLane[lane.leftLaneForward.id];
                if (lane.rightLaneForward != null) ADMapLane.rightLaneForward = id2ADMapLane[lane.rightLaneForward.id];
                if (lane.leftLaneReverse != null) ADMapLane.leftLaneReverse = id2ADMapLane[lane.leftLaneReverse.id];
                if (lane.rightLaneReverse != null) ADMapLane.rightLaneReverse = id2ADMapLane[lane.rightLaneReverse.id];
            }
        }

        bool Calculate()
        {
            MapAnnotationData = new MapManagerData();

            var allLanes = new HashSet<MapLane>(MapAnnotationData.GetData<MapLane>());
            var areAllLanesWithBoundaries = Lanelet2MapExporter.AreAllLanesWithBoundaries(allLanes, true);
            if (!areAllLanesWithBoundaries) return false;

            // Process lanes, intersections.
            MapAnnotationData.GetIntersections();
            MapAnnotationData.GetTrafficLanes();
            MapAnnotationData.GetNonLaneObjects();

            Hdmap = new HD.Map();

            const float laneHalfWidth = 1.75f; //temp solution
            const float stoplineWidth = 0.7f;

            // Initial collection
            laneSegments = new List<ADMapLane>();
            signalLights = new List<MapSignal>();
            stopSigns = new List<MapSign>();
            intersections = new List<MapIntersection>();
            laneSections = new List<MapLaneSection>();
            lineSegments = new List<MapLine>();

            overlapIdToGameObjects = new Dictionary<string, List<GameObject>>();
            gameObjectToOverlapInfo = new Dictionary<GameObject, HD.ObjectOverlapInfo>();
            gameObjectToOverlapId = new Dictionary<GameObject, HD.Id>();

            overlapIdToJunction = new Dictionary<string, HD.Junction>();
            roadIdToLanes = new Dictionary<string, List<string>>();

            // Lane
            laneOverlapsInfo = new Dictionary<string, LaneOverlapInfo>();
            laneHasParkingSpace = new HashSet<string>();
            laneHasSpeedBump = new HashSet<string>();
            laneHasJunction = new HashSet<string>();
            laneHasClearArea = new HashSet<string>();
            laneHasCrossWalk = new HashSet<string>();

            // Signal
            signalOverlapsInfo = new Dictionary<string, SignalOverlapInfo>();
            // Stop
            stopSignOverlapsInfo = new Dictionary<string, StopSignOverlapInfo>();
            // Clear Area
            clearAreaOverlapsInfo = new Dictionary<string, ClearAreaOverlapInfo>();
            // Cross Walk
            crossWalkOverlapsInfo = new Dictionary<string, CrossWalkOverlapInfo>();
            // Junction
            junctionOverlapsInfo = new Dictionary<string, JunctionOverlapInfo>();
            // Speed Bump
            speedBumpOverlapsInfo = new Dictionary<string, SpeedBumpOverlapInfo>();
            // Parking Space
            parkingSpaceOverlapsInfo = new Dictionary<string, ParkingSpaceOverlapInfo>();

            // Not sure if this dynamic allocation is needed?
            adMapLanes = new List<ADMapLane>();
            adMapParkingSpaces = new List<ADMapParkingSpace>();
            adMapSpeedBumps = new List<ADMapSpeedBump>();
            adMapClearAreas = new List<ADMapClearArea>();
            adMapCrossWalks = new List<ADMapCrossWalk>();
            adMapJunctions = new List<ADMapJunction>();
            adMapSignals = new List<ADMapSignal>();
            adMapSigns = new List<ADMapSign>();

            ADMapLane.id2MapLane.Clear();
            ADMapLane.id2ADMapLane.Clear();
            // Copy annotation(eg. lane, parking space, etc.) into ad container with id, which can be key.
            GetMapData<MapLane, ADMapLane, LaneOverlapInfo>("lane", MapAnnotationData, adMapLanes, laneOverlapsInfo, mapLane => new ADMapLane(mapLane));
            CopyLaneRelations();

            GetMapData<MapParkingSpace, ADMapParkingSpace, ParkingSpaceOverlapInfo>("PS", MapAnnotationData, adMapParkingSpaces, parkingSpaceOverlapsInfo, mapParkingSpace => new ADMapParkingSpace(mapParkingSpace));
            GetMapData<MapSpeedBump, ADMapSpeedBump, SpeedBumpOverlapInfo>("SB", MapAnnotationData, adMapSpeedBumps, speedBumpOverlapsInfo, mapSpeedBump => new ADMapSpeedBump(mapSpeedBump));
            GetMapData<MapClearArea, ADMapClearArea, ClearAreaOverlapInfo>("CA", MapAnnotationData, adMapClearAreas, clearAreaOverlapsInfo, mapClearArea => new ADMapClearArea(mapClearArea));
            GetMapData<MapCrossWalk, ADMapCrossWalk, CrossWalkOverlapInfo>("CW", MapAnnotationData, adMapCrossWalks, crossWalkOverlapsInfo, mapCrossWalk => new ADMapCrossWalk(mapCrossWalk));
            GetMapData<MapJunction, ADMapJunction, JunctionOverlapInfo>("J", MapAnnotationData, adMapJunctions, junctionOverlapsInfo, mapJunction => new ADMapJunction(mapJunction));
            GetMapData<MapSignal, ADMapSignal, SignalOverlapInfo>("signal", MapAnnotationData, adMapSignals, signalOverlapsInfo, mapSignal => new ADMapSignal(mapSignal));
            GetMapData<MapSign, ADMapSign, SignalOverlapInfo>("stopsign", MapAnnotationData, adMapSigns, signalOverlapsInfo, mapSign => new ADMapSign(mapSign));

            signalLights.AddRange(MapAnnotationData.GetData<MapSignal>());
            stopSigns.AddRange(MapAnnotationData.GetData<MapSign>());
            intersections.AddRange(MapAnnotationData.GetData<MapIntersection>());
            laneSections.AddRange(MapAnnotationData.GetData<MapLaneSection>());
            lineSegments.AddRange(MapAnnotationData.GetData<MapLine>());

            laneSegmentsSet = new HashSet<ADMapLane>();
            lineSegmentsSet = new HashSet<MapLine>();

            foreach (var line in lineSegments)
            {
                lineSegmentsSet.Add(line);
            }

            if (Version == ApolloVersion.Apollo_5_0)
            {
                MakeSelfReverseLane();
            }

            laneSegments.AddRange(adMapLanes);

            // Use set instead of list to increase speed
            foreach (var laneSegment in laneSegments)
            {
                laneSegmentsSet.Add(laneSegment);
            }

            if (Version == ApolloVersion.Apollo_5_0)
            {
                MakeInfoOfLane();
                MakeInfoOfClearArea();
                MakeInfoOfJunction();
                MakeInfoOfParkingSpace();
                MakeInfoOfSpeedBump();
                MakeInfoOfCrossWalk();
            }
            else if (Version == ApolloVersion.Apollo_3_0)
            {
                MakeInfoOfLane();
                MakeInfoOfClearArea();
                MakeInfoOfSpeedBump();
                MakeInfoOfCrossWalk();
            }

            // Clear before and after of lane
            foreach (var laneSegment in laneSegmentsSet)
            {
                laneSegment.befores.Clear();
                laneSegment.afters.Clear();
            }

            // Link before and after segment for each lane segment
            foreach (var laneSegment in laneSegmentsSet)
            {
                // Each segment must have at least 2 waypoints for calculation, otherwise exit
                while (laneSegment.mapWorldPositions.Count < 2)
                {
                    Debug.Log("Some segment has less than 2 waypoints. Cancelling map generation.");
                    return false;
                }

                // Link lanes
                var firstPt = laneSegment.mapWorldPositions[0];
                var lastPt = laneSegment.mapWorldPositions[laneSegment.mapWorldPositions.Count - 1];

                foreach (var laneSegmentCmp in laneSegmentsSet)
                {
                    if (laneSegment == laneSegmentCmp)
                    {
                        continue;
                    }

                    if (laneSegment.laneTurnType == MapData.LaneTurnType.LEFT_TURN || laneSegment.laneTurnType == MapData.LaneTurnType.RIGHT_TURN)
                    {
                        if (laneSegmentCmp.laneTurnType == MapData.LaneTurnType.LEFT_TURN || laneSegmentCmp.laneTurnType == MapData.LaneTurnType.RIGHT_TURN)
                            continue;
                    }

                    // Make connection before or after with current lane by checking proximity.
                    var firstPt_cmp = laneSegmentCmp.mapWorldPositions[0];
                    var lastPt_cmp = laneSegmentCmp.mapWorldPositions[laneSegmentCmp.mapWorldPositions.Count - 1];

                    if ((firstPt - lastPt_cmp).magnitude < MapAnnotationTool.PROXIMITY / MapAnnotationTool.EXPORT_SCALE_FACTOR)
                    {
                            laneSegmentCmp.mapWorldPositions[laneSegmentCmp.mapWorldPositions.Count - 1] = firstPt;
                            laneSegment.befores.Add(laneSegmentCmp);
                    }

                    if ((lastPt - firstPt_cmp).magnitude < MapAnnotationTool.PROXIMITY / MapAnnotationTool.EXPORT_SCALE_FACTOR)
                    {
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
                        // UnityEditor.Selection.activeObject = laneSegment.gameObject;
                        Debug.Log("Please fix the selected lane.");
#endif
                        return false;
                    }
                }
            }

            // Function to get neighbor lanes in the same road
            System.Func<ADMapLane, bool, List<ADMapLane>> GetNeighborForwardRoadLanes = null;
            GetNeighborForwardRoadLanes = delegate (ADMapLane self, bool fromLeft)
            {
                if (self == null)
                {
                    return new List<ADMapLane>();
                }
                
                if (fromLeft)
                {
                    if (self.leftLaneForward == null)
                    {
                        return new List<ADMapLane>();
                    }
                    else
                    {
                        var ret = new List<ADMapLane>();
                        ret.AddRange(GetNeighborForwardRoadLanes(self.leftLaneForward, true));
                        ret.Add(self.leftLaneForward);
                        return ret;
                    }
                }
                else
                {
                    if (self.rightLaneForward == null)
                    {
                        return new List<ADMapLane>();
                    }
                    else
                    {
                        var ret = new List<ADMapLane>();
                        ret.Add(self.rightLaneForward);
                        ret.AddRange(GetNeighborForwardRoadLanes(self.rightLaneForward, false));
                        return ret;
                    }
                }
            };

            HashSet<HD.Road> roadSet = new HashSet<HD.Road>();
            var visitedLanes2Road = new Dictionary<string, HD.Road>();
            var visitedLanes = new HashSet<string>();
            {
                var allRoadLanes = new List<List<ADMapLane>>();
                foreach (var adMapLane in adMapLanes)
                {
                    if (visitedLanes.Contains(adMapLane.Id))
                    {
                        continue;
                    }

                    var roadLanes = new List<ADMapLane>();

                    if (adMapLane.isSelfReverseLane)
                    {
                        roadLanes.Add(adMapLane);
                    }
                    else
                    {
                        var lefts = GetNeighborForwardRoadLanes(adMapLane, true);  // Left forward lanes from furthest to nearest
                        var rights = GetNeighborForwardRoadLanes(adMapLane, false);  // Right forward lanes from nearest to furthest

                        roadLanes.AddRange(lefts);
                        roadLanes.Add(adMapLane);
                        roadLanes.AddRange(rights);
                    }
                    foreach (var lane in roadLanes)
                    {
                        visitedLanes.Add(lane.Id);
                    }
                    allRoadLanes.Add(roadLanes);
                }

                var mergedRoadLanes = new List<List<ADMapLane>>();
                var visited = new bool[allRoadLanes.Count];
                for (int i = 0; i < allRoadLanes.Count; ++i)
                {
                    if (visited[i]) continue;
                    var roadLanes = allRoadLanes[i];
                    var leftMostLane = roadLanes.First();
                    var rightMostLane = roadLanes.Last();
                    var found = false;
                    for (int j = 0; j < allRoadLanes.Count; ++j)
                    {
                        if (i == j || visited[j]) continue;
                        var otherRoadLanes = allRoadLanes[j];
                        var otherLeftMostLane = otherRoadLanes.First();
                        var otherRightMostLane = otherRoadLanes.Last();
                        if (leftMostLane.leftLaneReverse == otherLeftMostLane && otherLeftMostLane.leftLaneReverse == leftMostLane) // left hand traffic
                        {
                            roadLanes.Reverse();
                            roadLanes.AddRange(otherRoadLanes);
                            mergedRoadLanes.Add(roadLanes);
                            visited[j] = true;
                            found = true;
                        }
                        else if (rightMostLane.rightLaneReverse == otherRightMostLane && otherRightMostLane.rightLaneReverse == rightMostLane) // right hand traffic
                        {
                            otherRoadLanes.Reverse();
                            roadLanes.AddRange(otherRoadLanes);
                            mergedRoadLanes.Add(roadLanes);
                            visited[j] = true;
                            found = true;
                        }
                    }

                    if (!found) mergedRoadLanes.Add(roadLanes);
                }

                foreach (var roadLanes in mergedRoadLanes)
                {
                    var roadSection = new HD.RoadSection()
                    {
                        id = HdId($"1"),
                        boundary = null,
                    };

                    foreach (var roadLaneSegment in roadLanes)
                    {
                        roadSection.lane_id.Add(new HD.Id()
                        {
                            id = roadLaneSegment.Id
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
                        if (!visitedLanes2Road.ContainsKey(l.Id))
                        {
                            visitedLanes2Road.Add(l.Id, road);
                        }
                    }

                    var LanesIds = new List<string>();

                    foreach (var lane in roadLanes)
                    {
                        string lane_id = null;
                        for (int i = 0; i < adMapLanes.Count; i++)
                        {
                            var lane_ = adMapLanes[i];

                            if (lane_ == lane)
                            {
                                lane_id = lane_.Id;
                                break;
                            }
                        }

                        LanesIds.Add(lane_id);
                    }
                    roadIdToLanes.Add(road.id.id, LanesIds);
                }
            }

            // Config lanes
            foreach (var adMapLane in adMapLanes)
            {
                var centerPts = new List<ApolloCommon.PointENU>();
                var lBndPts = new List<ApolloCommon.PointENU>();
                var rBndPts = new List<ApolloCommon.PointENU>();

                var worldPoses = adMapLane.mapWorldPositions;
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
                predecessor_ids.AddRange(adMapLane.befores.Select(seg => HdId(seg.Id)));
                successor_ids.AddRange(adMapLane.afters.Select(seg => HdId(seg.Id)));

                var lane = new HD.Lane()
                {
                    id = HdId(adMapLane.Id),

                    central_curve = new HD.Curve(),
                    left_boundary = new HD.LaneBoundary(),
                    right_boundary = new HD.LaneBoundary(),
                    length = mLength,
                    speed_limit = adMapLane.speedLimit,
                    type = HD.Lane.LaneType.CityDriving,
                    turn = (HD.Lane.LaneTurn)adMapLane.laneTurnType,
                    direction = HD.Lane.LaneDirection.Forward,
                };

                // Record lane's length
                laneOverlapsInfo[adMapLane.Id].mLength = mLength;

                if (laneHasJunction.Contains(adMapLane.Id))
                {
                    foreach (var junctionOverlapId in laneOverlapsInfo[adMapLane.Id].junctionOverlapIds)
                    {
                        lane.overlap_id.Add(junctionOverlapId.Value);
                    }
                }

                if (laneHasParkingSpace.Contains(adMapLane.Id))
                {
                    foreach (var parkingSpaceOverlapId in laneOverlapsInfo[adMapLane.Id].parkingSpaceOverlapIds)
                    {
                        lane.overlap_id.Add(parkingSpaceOverlapId.Value);
                    }
                }

                if (laneHasSpeedBump.Contains(adMapLane.Id))
                {
                    foreach (var speedBumpOverlapId in laneOverlapsInfo[adMapLane.Id].speedBumpOverlapIds)
                    {
                        lane.overlap_id.Add(speedBumpOverlapId.Value);
                    }
                }

                if (laneHasClearArea.Contains(adMapLane.Id))
                {
                    foreach (var clearAreaOverlapId in laneOverlapsInfo[adMapLane.Id].clearAreaOverlapIds)
                    {
                        lane.overlap_id.Add(clearAreaOverlapId.Value);
                    }
                }

                if (laneHasCrossWalk.Contains(adMapLane.Id))
                {
                    foreach (var crossWalkOverlapId in laneOverlapsInfo[adMapLane.Id].crossWalkOverlapIds)
                    {
                        lane.overlap_id.Add(crossWalkOverlapId.Value);
                    }
                }

                if (laneOverlapsInfo.ContainsKey(adMapLane.Id) && adMapLane.isSelfReverseLane)
                {
                    var self_reverse_lane_id = laneOverlapsInfo[adMapLane.Id].selfReverseLaneId;

                    // Set reverse lane id.
                    lane.self_reverse_lane_id.Add(laneOverlapsInfo[self_reverse_lane_id].id);

                    // Set lane's lane overlap info.
                    var otherLaneId = laneOverlapsInfo[self_reverse_lane_id].id;
                    var id = HdId($"overlap_{adMapLane.Id}_{otherLaneId.id}");

                    if (!laneOverlapsInfo.GetOrCreate(adMapLane.Id).laneOverlapIds.ContainsKey(self_reverse_lane_id))
                    {
                        laneOverlapsInfo.GetOrCreate(adMapLane.Id).laneOverlapIds.Add(self_reverse_lane_id, id);
                        laneOverlapsInfo.GetOrCreate(self_reverse_lane_id).laneOverlapIds.Add(adMapLane.Id, id);

                        var laneOverlapInfo1 = new HD.ObjectOverlapInfo()
                        {
                            id = laneOverlapsInfo[adMapLane.Id].id,
                            lane_overlap_info = new HD.LaneOverlapInfo()
                            {
                                start_s = 0,
                                end_s = laneOverlapsInfo[adMapLane.Id].mLength,
                                is_merge = true,
                            }
                        };
                        var laneOverlapInfo2 = new HD.ObjectOverlapInfo()
                        {
                            id = laneOverlapsInfo[self_reverse_lane_id].id,
                            lane_overlap_info = new HD.LaneOverlapInfo()
                            {
                                start_s = 0,
                                end_s = laneOverlapsInfo[adMapLane.Id].mLength,
                                is_merge = true,
                            }
                        };
                        var overlap = new HD.Overlap()
                        {
                            id = HdId(laneOverlapsInfo[adMapLane.Id].laneOverlapIds[self_reverse_lane_id].id),
                        };
                        overlap.@object.Add(laneOverlapInfo1);
                        overlap.@object.Add(laneOverlapInfo2);
                        Hdmap.overlap.Add(overlap);
                    }
                    lane.overlap_id.Add(id);
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

                leftLaneBoundaryType.types.Add((HD.LaneBoundaryType.Type)adMapLane.leftBoundType);

                var left_boundary_segment = new HD.LaneBoundary()
                {
                    curve = new HD.Curve(),
                    length = lLength,
                };
                if (adMapLane.leftBoundType == ApolloBoundaryType.VIRTUAL) left_boundary_segment.@virtual = true;
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

                rightLaneBoundaryType.types.Add((HD.LaneBoundaryType.Type)adMapLane.rightBoundType);

                var right_boundary_segment = new HD.LaneBoundary()
                {
                    curve = new HD.Curve(),
                    length = rLength,
                };
                if (adMapLane.rightBoundType == ApolloBoundaryType.VIRTUAL) right_boundary_segment.@virtual = true;
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
                if (adMapLane.leftLaneForward != null)
                    lane.left_neighbor_forward_lane_id.AddRange(new List<HD.Id>() { HdId(adMapLane.leftLaneForward.Id), } );
                if (adMapLane.rightLaneForward != null)
                    lane.right_neighbor_forward_lane_id.AddRange(new List<HD.Id>() { HdId(adMapLane.rightLaneForward.Id), } );
                if (adMapLane.leftLaneReverse != null)
                    lane.left_neighbor_reverse_lane_id.AddRange(new List<HD.Id>() { HdId(adMapLane.leftLaneReverse.Id), } );
                if (adMapLane.rightLaneReverse != null)
                    lane.right_neighbor_reverse_lane_id.AddRange(new List<HD.Id>() { HdId(adMapLane.rightLaneReverse.Id), } );
                
                // Add boundary to road
                if (adMapLane.leftLaneForward == null || adMapLane.rightLaneForward == null)
                {
                    var road = visitedLanes2Road[adMapLane.Id];

                    roadSet.Remove(road);

                    var section = road.section[0];

                    lineSegment = new HD.LineSegment();
                    if (adMapLane.leftLaneForward == null) 
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
                            type = adMapLane.leftLaneForward == null ? HD.BoundaryEdge.Type.LeftBoundary : HD.BoundaryEdge.Type.RightBoundary,
                        };
                        boundaryEdge.curve.segment.Add(new HD.CurveSegment()
                        {
                            line_segment = lineSegment,
                        });
                        edges.Add(boundaryEdge);
                    }

                    lineSegment = new HD.LineSegment();
                    // Cases that a Road only has one lane, adds rightBoundary
                    if (adMapLane.leftLaneForward == null && adMapLane.rightLaneForward == null)
                    {
                        lineSegment.point.Clear();
                        lineSegment.point.AddRange(rBndPts);
                        var boundaryEdge = new HD.BoundaryEdge()
                        {
                            curve = new HD.Curve(),
                            type = HD.BoundaryEdge.Type.RightBoundary,
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
                    if (lane == null || laneOverlapsInfo[lane] == null || laneOverlapsInfo[lane].junctionOverlapIds == null)
                    {
                        continue;
                    }
                    // Need to check example of road and junction.
                    if (laneOverlapsInfo[lane].junctionOverlapIds.Count() > 0)
                    {
                        road.junction_id = HdId(laneOverlapsInfo[lane].junctionOverlapIds.Keys.First());
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
                            type = HD.Subsignal.Type.Circle,
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
                    List<ADMapLane> lanesToInspec = new List<ADMapLane>();
                    lanesToInspec.AddRange(laneSegmentsSet);

                    if (!MakeStoplineLaneOverlaps(stopline, lanesToInspec, stoplineWidth, signal_Id, OverlapType.Signal_Stopline_Lane, stoplinePts, laneIds2OverlapIdsMapping, overlap_ids, signalLight.id))
                    {
                        return false;
                    }
                }

                if (stoplinePts != null && stoplinePts.Count >= 2)
                {
                    var boundary = new HD.Polygon();
                    boundary.point.AddRange(signalBoundPts);
                    var signalId = HdId(signalLight.id);

                    var signal = new HD.Signal()
                    {
                        id = signalId,
                        type = (HD.Signal.Type)signalLight.signalType, // TODO converted from LGSVL signal type to apollo need to check autoware type?
                        boundary = boundary,
                    };

                    signal.subsignal.AddRange(subsignals);
                    signalOverlapsInfo.GetOrCreate(signalLight.id).id = signalId;

                    if (signalOverlapsInfo.ContainsKey(signalLight.id))
                    {
                        var signalOverlapInfo = new HD.ObjectOverlapInfo()
                        {
                            id = signalId,
                            signal_overlap_info = new HD.SignalOverlapInfo(),
                        };

                        foreach (var overlapId in signalOverlapsInfo[signalLight.id].laneOverlapIds.Values)
                        {
                            overlap_ids.Add(overlapId);
                        }

                        foreach (var overlapId in signalOverlapsInfo[signalLight.id].junctionOverlapIds.Values)
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
                    List<ADMapLane> lanesToInspec = new List<ADMapLane>();
                    lanesToInspec.AddRange(laneSegmentsSet);

                    if (!MakeStoplineLaneOverlaps(stopline, lanesToInspec, stoplineWidth, stopsign_Id, OverlapType.Stopsign_Stopline_Lane, stoplinePts, laneIds2OverlapIdsMapping, overlap_ids, stopSign.id))
                    {
                        return false;
                    } 
                }

                if (stoplinePts != null && stoplinePts.Count >= 2)
                {
                    stopSign.id = $"stopsign_{stopsign_Id}";
                    var stopId = HdId(stopSign.id);

                    if (stopSignOverlapsInfo.ContainsKey(stopSign.id))
                    {
                        var stopOverlapInfo = new HD.ObjectOverlapInfo()
                        {
                            id = stopId,
                            stop_sign_overlap_info = new HD.StopSignOverlapInfo(),
                        };

                        foreach (var overlapId in stopSignOverlapsInfo[stopSign.id].laneOverlapIds.Values)
                        {
                            overlap_ids.Add(overlapId);
                        }

                        foreach (var overlapId in stopSignOverlapsInfo[stopSign.id].junctionOverlapIds.Values)
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
                        type = HD.StopSign.StopType.Unknown,
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

            if (Version == ApolloVersion.Apollo_5_0)
            {
                MakeJunctionAnnotation();
                MakeParkingSpaceAnnotation();
                MakeSpeedBumpAnnotation();
                MakeClearAreaAnnotation();
                MakeCrossWalkAnnotation();
            }
            else if (Version == ApolloVersion.Apollo_3_0)
            {
                MakeSpeedBumpAnnotation();
                MakeClearAreaAnnotation();
                MakeCrossWalkAnnotation();
            }

            double originLatitude, originLongitude;
            mapOrigin.GetLatitudeLongitude(mapOrigin.OriginNorthing, mapOrigin.OriginEasting, out originLatitude, out originLongitude);
            var LeftLongitude = originLongitude;
            var RightLongitude = originLongitude;
            var TopLatitude = originLatitude;
            var BottomLatitude = originLatitude;

            Hdmap.header = new HD.Header()
            {
                version = System.Text.Encoding.UTF8.GetBytes("1.500000"),
                date = System.Text.Encoding.UTF8.GetBytes("2018-03-23T13:27:54"),
                projection = new HD.Projection()
                {
                    proj = $"+proj=utm +zone={OriginZone} +ellps=WGS84 +datum=WGS84 +units=m +no_defs",
                },
                district = System.Text.Encoding.UTF8.GetBytes("0"),
                rev_major = System.Text.Encoding.UTF8.GetBytes("1"),
                rev_minor = System.Text.Encoding.UTF8.GetBytes("0"),
                left = LeftLongitude,
                top = TopLatitude,
                right = RightLongitude,
                bottom = BottomLatitude,
                vendor = System.Text.Encoding.UTF8.GetBytes("LGSVL"),
            };

            return true;
        }

        bool MakeStoplineLaneOverlaps(MapLine stopline, List<ADMapLane> lanesToInspec, float stoplineWidth, int overlapInfoId, OverlapType overlapType, List<ApolloCommon.PointENU> stoplinePts, Dictionary<string, List<HD.Id>> laneId2OverlapIdsMapping, List<HD.Id> overlap_ids, string signalOrSignId)
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

            var considered = new HashSet<ADMapLane>(); //This is to prevent conceptually or practically duplicated overlaps

            string overlap_id_prefix = "";
            if (overlapType == OverlapType.Signal_Stopline_Lane)
            {
                overlap_id_prefix = "overlap_";
            }
            else if (overlapType == OverlapType.Stopsign_Stopline_Lane)
            {
                overlap_id_prefix = "overlap_";
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

                    var segments = new List<ADMapLane>();
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
                        var lane_id = segment.Id;
                        var overlap_id = HdId($"{overlap_id_prefix}{signalOrSignId}_{lane_id}");

                        laneId2OverlapIdsMapping.GetOrCreate(lane_id).Add(overlap_id);

                        HD.ObjectOverlapInfo objOverlapInfo = new HD.ObjectOverlapInfo();

                        if (overlapType == OverlapType.Signal_Stopline_Lane)
                        {
                            var id = HdId(signalOrSignId);
                            objOverlapInfo = new HD.ObjectOverlapInfo()
                            {
                                id = id,
                                signal_overlap_info = new HD.SignalOverlapInfo(),
                            };

                            signalOverlapsInfo.GetOrCreate(signalOrSignId).id = id;
                        }
                        else if (overlapType == OverlapType.Stopsign_Stopline_Lane)
                        {
                            var id = HdId(signalOrSignId);
                            objOverlapInfo = new HD.ObjectOverlapInfo()
                            {
                                id = id,
                                stop_sign_overlap_info = new HD.StopSignOverlapInfo(),
                            };

                            stopSignOverlapsInfo.GetOrCreate(signalOrSignId).id = id;
                        }

                        var object_overlap = new List<HD.ObjectOverlapInfo>()
                        {
                            new HD.ObjectOverlapInfo()
                            {
                                id = HdId(lane_id),
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
                    string junctionId = null;
                    foreach (var j in adMapJunctions.Where(j => j.Id == junction.id))
                    {
                        junctionId = j.Id;
                    }

                    // LaneSegment
                    var laneList = intersection.transform.GetComponentsInChildren<MapLane>().ToList();
                    foreach (var lane in laneList)
                    {
                        string laneId = null;
                        foreach (var l in adMapLanes.Where(l => l.Id == lane.id))
                        {
                            laneId = l.Id;
                        }

                        var overlapId = HdId($"overlap_junction_I{intersections.IndexOf(intersection)}_J{junctionList.IndexOf(junction)}_{laneId}");
                        junctionOverlapsInfo.GetOrCreate(junctionId).laneOverlapIds.Add(laneId, overlapId);
                        laneOverlapsInfo.GetOrCreate(laneId).junctionOverlapIds.Add(junctionId, overlapId);

                        if (!laneHasJunction.Contains(laneId))
                            laneHasJunction.Add(laneId);
                    }

                    // StopSign
                    var stopSignList = intersection.transform.GetComponentsInChildren<MapSign>().ToList();
                    foreach (var stopSign in stopSignList)
                    {
                        var overlapId = HdId($"overlap_junction_I{intersections.IndexOf(intersection)}_J{junctionList.IndexOf(junction)}_stopsign{stopSignList.IndexOf(stopSign)}");
                        junctionOverlapsInfo.GetOrCreate(junctionId).stopSignOverlapIds.Add(stopSign.id, overlapId);
                        stopSignOverlapsInfo.GetOrCreate(stopSign.id).junctionOverlapIds.Add(junctionId, overlapId);
                    }

                    // Signal
                    var signalList = intersection.transform.GetComponentsInChildren<MapSignal>().ToList();
                    foreach (var signal in signalList)
                    {
                        var overlapId = HdId($"overlap_junction_I{intersections.IndexOf(intersection)}_J{junctionList.IndexOf(junction)}_signal_{intersections.IndexOf(intersection)}_{signalList.IndexOf(signal)}");
                        junctionOverlapsInfo.GetOrCreate(junctionId).signalOverlapIds.Add(signal.id, overlapId);
                        signalOverlapsInfo.GetOrCreate(signal.id).junctionOverlapIds.Add(junctionId, overlapId);
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

                    string junctionId = null;
                    foreach (var j in adMapJunctions.Where(j => j.Id == junction.id))
                    {
                        junctionId = j.Id;
                    }

                    var junctionOverlapIds_string = new List<HD.Id>();

                    // LaneSegment
                    var laneList = intersection.transform.GetComponentsInChildren<MapLane>().ToList();
                    foreach (var lane in laneList)
                    {
                        string laneId = null;
                        foreach (var l in adMapLanes.Where(l => l.Id == lane.id))
                        {
                            laneId = l.Id;
                        }

                        var overlapId = junctionOverlapsInfo[junctionId].laneOverlapIds[lane.id];
                        junctionOverlapIds_string.Add(overlapId);

                        // Overlap Annotation
                        var objectLane = new HD.ObjectOverlapInfo()
                        {
                            id = laneOverlapsInfo[laneId].id,
                            lane_overlap_info = new HD.LaneOverlapInfo()
                            {
                                start_s = 0,        // TODO
                                end_s = laneOverlapsInfo[laneId].mLength,  // TODO
                                is_merge =false,
                            }
                        };

                        var objectJunction = new HD.ObjectOverlapInfo()
                        {
                            id = junctionOverlapsInfo[junctionId].id,
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
                        var overlapId = junctionOverlapsInfo[junctionId].stopSignOverlapIds[stopSign.id];

                        junctionOverlapIds_string.Add(overlapId);

                        var objectStopSign = new HD.ObjectOverlapInfo()
                        {
                            id = stopSignOverlapsInfo[stopSign.id].id,
                            stop_sign_overlap_info = new HD.StopSignOverlapInfo(),
                        };

                        var objectJunction = new HD.ObjectOverlapInfo()
                        {
                            id = junctionOverlapsInfo[junctionId].id,
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
                        var overlapId = junctionOverlapsInfo[junctionId].signalOverlapIds[signal.id];
                        junctionOverlapIds_string.Add(overlapId);

                        var objectSignalLight = new HD.ObjectOverlapInfo()
                        {
                            id = signalOverlapsInfo[signal.id].id,
                            signal_overlap_info = new HD.SignalOverlapInfo(),
                        };

                        var objectJunction = new HD.ObjectOverlapInfo()
                        {
                            id = junctionOverlapsInfo[junctionId].id,
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
                        id = HdId(junctionId),
                        polygon = polygon,
                    };
                    junctionAnnotation.overlap_id.AddRange(junctionOverlapIds_string);
                    Hdmap.junction.Add(junctionAnnotation);
               }
            }
        }

        void MakeInfoOfParkingSpace()
        {
            double dist = double.MaxValue;
            foreach (var adMapParkingSpace in adMapParkingSpaces)
            {
                ADMapLane refLane = null;
                var parkingSpaceInWorld = new List<Vector3>();

                dist = double.MaxValue;
                // Find nearest lane to parking space
                string nearestLaneId = null;
                bool isSelfReverse = false;

                foreach (var adMapLane in adMapLanes)
                {
                    var p1 = new Vector2(adMapLane.mapWorldPositions.First().x, adMapLane.mapWorldPositions.First().z);
                    var p2 = new Vector2(adMapLane.mapWorldPositions.Last().x, adMapLane.mapWorldPositions.Last().z);
                    var pt = new Vector2((adMapParkingSpace.mapWorldPositions[0].x + adMapParkingSpace.mapWorldPositions[2].x) / 2, 
                        (adMapParkingSpace.mapWorldPositions[0].z + adMapParkingSpace.mapWorldPositions[2].z) / 2);

                    var closestPt = new Vector2();
                    double d = FindDistanceToSegment(pt, p1, p2, out closestPt);

                    if (dist > d)
                    {
                        dist = d;
                        nearestLaneId = adMapLane.Id;
                        isSelfReverse = adMapLane.isSelfReverseLane;
                        refLane = adMapLane;
                    }
                }

                if (laneOverlapsInfo.ContainsKey(nearestLaneId) && laneOverlapsInfo[nearestLaneId].parkingSpaceOverlapIds.ContainsKey(adMapParkingSpace.Id))
                    continue;

                var overlapId = HdId($"overlap_{adMapParkingSpace.Id}_{laneOverlapsInfo[nearestLaneId].id.id}");

                laneHasParkingSpace.Add(nearestLaneId);
                parkingSpaceOverlapsInfo.GetOrCreate(adMapParkingSpace.Id).laneOverlapIds_string.Add(nearestLaneId, overlapId);
                laneOverlapsInfo.GetOrCreate(nearestLaneId).parkingSpaceOverlapIds.Add(adMapParkingSpace.Id, overlapId);

                // self-reverse lane
                if (isSelfReverse)
                {
                    var selfReverseLaneId = laneOverlapsInfo[nearestLaneId].selfReverseLaneId;
                    var overlapReverseLaneId = HdId($"overlap_{adMapParkingSpace.Id}_{selfReverseLaneId}");

                    laneHasParkingSpace.Add(selfReverseLaneId);
                    parkingSpaceOverlapsInfo.GetOrCreate(adMapParkingSpace.Id).laneOverlapIds_string.Add(selfReverseLaneId, overlapReverseLaneId);
                    laneOverlapsInfo.GetOrCreate(selfReverseLaneId).parkingSpaceOverlapIds.Add(adMapParkingSpace.Id, overlapReverseLaneId);
                }
            }
        }

        void MakeParkingSpaceAnnotation()
        {
            foreach (var adMapParkingSpace in adMapParkingSpaces)
            {
                var polygon = new HD.Polygon();
                var parkingSpaceInWorld = new List<Vector3>();

                foreach (var worldPos in adMapParkingSpace.mapWorldPositions)
                    parkingSpaceInWorld.Add(worldPos);

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

                var parkingSpaceId = HdId(adMapParkingSpace.Id);
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

                foreach (var lane in parkingSpaceOverlapsInfo[adMapParkingSpace.Id].laneOverlapIds_string.Keys)
                {
                    var first_s = FindSegmentDistNotOnLane(frontSegment, lane);
                    var second_s = FindSegmentDistNotOnLane(backSegment, lane);
                    var start_s = Mathf.Min(first_s, second_s);
                    var end_s = Mathf.Max(first_s, second_s);

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
                        id = laneOverlapsInfo[lane].id,
                        lane_overlap_info = laneOverlapInfo,
                    };

                    var overlap = new HD.Overlap()
                    {
                        id = parkingSpaceOverlapsInfo[adMapParkingSpace.Id].laneOverlapIds_string[lane],
                    };

                    var objectParkingSpace = new HD.ObjectOverlapInfo()
                    {
                        id = parkingSpaceId,
                        parking_space_overlap_info = new HD.ParkingSpaceOverlapInfo(),
                    };

                    overlap.@object.Add(objectLane);
                    overlap.@object.Add(objectParkingSpace);
                    Hdmap.overlap.Add(overlap);

                    parkingSpaceAnnotation.overlap_id.Add(parkingSpaceOverlapsInfo[adMapParkingSpace.Id].laneOverlapIds_string[lane]);
                }
                Hdmap.parking_space.Add(parkingSpaceAnnotation);
            }
        }

        void MakeInfoOfSpeedBump()
        {
            foreach (var adMapSpeedBump in adMapSpeedBumps)
            {
                var speedBumpInWorld = new List<Vector3>();
                foreach (var worldPos in adMapSpeedBump.mapWorldPositions)
                {
                    speedBumpInWorld.Add(worldPos);
                }

                var speedBumpId = adMapSpeedBump.Id;

                foreach (var lane in adMapLanes)
                {
                    for (int i = 0; i < lane.mapWorldPositions.Count - 1; i++)
                    {
                        var p1 = new Vector2(lane.mapWorldPositions[i].x, lane.mapWorldPositions[i].z);
                        var p2 = new Vector2(lane.mapWorldPositions[i+1].x, lane.mapWorldPositions[i+1].z);
                        var q1 = new Vector2(speedBumpInWorld[0].x, speedBumpInWorld[0].z);
                        var q2 = new Vector2(speedBumpInWorld[1].x, speedBumpInWorld[1].z);

                        if (DoIntersect(p1, p2, q1, q2))
                        {
                            if (speedBumpOverlapsInfo.GetOrCreate(adMapSpeedBump.Id).id == null)
                                speedBumpOverlapsInfo.GetOrCreate(adMapSpeedBump.Id).id = HdId(speedBumpId);

                            var overlapId = HdId($"overlap_{adMapSpeedBump.Id}_{lane.Id}");
                            laneOverlapsInfo.GetOrCreate(lane.Id).speedBumpOverlapIds.Add(adMapSpeedBump.Id, overlapId);
                            speedBumpOverlapsInfo.GetOrCreate(adMapSpeedBump.Id).laneOverlapIds.Add(lane.Id, overlapId);
                            laneHasSpeedBump.Add(lane.Id);
                            break;
                        }
                    }       
                }
            }
        }

        void MakeSpeedBumpAnnotation()
        {
            foreach (var adMapSpeedBump in adMapSpeedBumps)
            {
                var speedBumpInWorld = new List<Vector3>();
                var lineSegment = new HD.LineSegment();
                speedBumpInWorld.Add(adMapSpeedBump.mapWorldPositions[0]);
                speedBumpInWorld.Add(adMapSpeedBump.mapWorldPositions[1]);

                foreach (var pt in speedBumpInWorld)
                {
                    var ptInApollo = HDMapUtil.GetApolloCoordinates(pt, OriginEasting, OriginNorthing, false);
                    lineSegment.point.Add(ptInApollo);
                }

                var speedBumpAnnotation = new HD.SpeedBump()
                {
                    id = speedBumpOverlapsInfo[adMapSpeedBump.Id].id,
                };

                foreach (var lane in speedBumpOverlapsInfo[adMapSpeedBump.Id].laneOverlapIds.Keys)
                {
                    var s = FindSegmentDistOnLane(speedBumpInWorld, lane);

                    var laneOverlapInfo = new HD.LaneOverlapInfo()
                    {
                        start_s = s - 0.5, // Todo:
                        end_s = s + 1.0, // Todo:
                        is_merge = false,
                    };

                    // lane
                    var objectLane = new HD.ObjectOverlapInfo()
                    {
                        id = laneOverlapsInfo[lane].id,
                        lane_overlap_info = laneOverlapInfo,
                    };

                    var objectSpeedBump = new HD.ObjectOverlapInfo()
                    {
                        id = HdId(adMapSpeedBump.Id),
                        speed_bump_overlap_info = new HD.SpeedBumpOverlapInfo(),
                    };

                    var overlap = new HD.Overlap()
                    {
                        id = speedBumpOverlapsInfo[adMapSpeedBump.Id].laneOverlapIds[lane],
                    };
                    overlap.@object.Add(objectLane);
                    overlap.@object.Add(objectSpeedBump);
                    Hdmap.overlap.Add(overlap);

                    speedBumpAnnotation.overlap_id.Add(speedBumpOverlapsInfo[adMapSpeedBump.Id].laneOverlapIds[lane]);

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
            foreach (var adMapClearArea in adMapClearAreas)
            {
                var clearAreaInWorld = new List<Vector3>();
                foreach (var worldPos in adMapClearArea.mapWorldPositions)
                {
                    clearAreaInWorld.Add(worldPos);
                }

                // Sequence of vertices in Rectangle
                // [0]   [1]
                // --------- lane
                // [3]   [2]
                foreach (var lane in adMapLanes)
                {
                    for (int i = 0; i < lane.mapWorldPositions.Count - 1; i++)
                    {
                        var p1 = new Vector2(lane.mapWorldPositions[i].x, lane.mapWorldPositions[i].z);
                        var p2 = new Vector2(lane.mapWorldPositions[i+1].x, lane.mapWorldPositions[i+1].z);
                    
                        var q0 = new Vector2(clearAreaInWorld[0].x, clearAreaInWorld[0].z);
                        var q3 = new Vector2(clearAreaInWorld[3].x, clearAreaInWorld[3].z);
                        
                        if (DoIntersect(p1, p2, q0, q3))
                        {
                            var clearAreaId = HdId(adMapClearArea.Id);
                            if (clearAreaOverlapsInfo.GetOrCreate(clearAreaId.id).id == null)
                                clearAreaOverlapsInfo.GetOrCreate(clearAreaId.id).id = clearAreaId;

                            var overlapId = HdId($"overlap_{clearAreaId.id}_{lane.Id}");
                            laneOverlapsInfo.GetOrCreate(lane.Id).clearAreaOverlapIds.Add(clearAreaId.id, overlapId);
                            clearAreaOverlapsInfo.GetOrCreate(clearAreaId.id).laneOverlapIds.Add(lane.Id, overlapId);
                            laneHasClearArea.Add(lane.Id);
                        }
                    }
                }
            }
        }

        void MakeClearAreaAnnotation()
        {
            foreach (var adMapClearArea in adMapClearAreas)
            {
                var clearAreaInWorld = new List<Vector3>();
                var frontSegment = new List<Vector3>();
                var backSegment = new List<Vector3>();

                var lineSegment = new HD.LineSegment();
                var polygon = new HD.Polygon();

                foreach (var worldPos in adMapClearArea.mapWorldPositions)
                    clearAreaInWorld.Add(worldPos);
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

                foreach (var laneOverlap in clearAreaOverlapsInfo[adMapClearArea.Id].laneOverlapIds)
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
                        id = clearAreaOverlapsInfo[adMapClearArea.Id].id,
                        clear_area_overlap_info = new HD.ClearAreaOverlapInfo(),
                    };

                    var overlap = new HD.Overlap()
                    {
                        id = clearAreaOverlapsInfo[adMapClearArea.Id].laneOverlapIds[laneOverlap.Key],
                    };

                    overlap.@object.Add(objectLane);
                    overlap.@object.Add(objectClearArea);
                    Hdmap.overlap.Add(overlap);

                    clearAreaAnnotation.id = clearAreaOverlapsInfo[adMapClearArea.Id].id;
                    clearAreaAnnotation.overlap_id.Add(clearAreaOverlapsInfo.GetOrCreate(adMapClearArea.Id).laneOverlapIds[laneOverlap.Key]);
                }

                clearAreaAnnotation.polygon = polygon;
                Hdmap.clear_area.Add(clearAreaAnnotation);
            }
        }

        void MakeInfoOfCrossWalk()
        {
            foreach (var adMapCrossWalk in adMapCrossWalks)
            {
                var crossWalkInWorld = new List<Vector3>();
                foreach (var worldPos in adMapCrossWalk.mapWorldPositions)
                {
                    crossWalkInWorld.Add(worldPos);
                }

                // Sequence of vertices in Rectangle
                // [0]   [1]
                // |       |
                // --------- lane
                // |       |
                // [3]   [2]
                var crossWalkId = adMapCrossWalk.Id;

                if (crossWalkOverlapsInfo.GetOrCreate(adMapCrossWalk.Id).id == null)
                    crossWalkOverlapsInfo.GetOrCreate(adMapCrossWalk.Id).id = HdId(crossWalkId);

                foreach (var lane in adMapLanes)
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
                            var overlapId = HdId($"overlap_{crossWalkId}_{lane.Id}");

                            laneOverlapsInfo.GetOrCreate(lane.Id).crossWalkOverlapIds.Add(crossWalkId, overlapId);
                            crossWalkOverlapsInfo.GetOrCreate(crossWalkId).laneOverlapIds.Add(lane.Id, overlapId);
                            laneHasCrossWalk.Add(lane.Id);
                        }
                    }
                }
            }
        }

        void MakeInfoOfLane()
        {
            // iterate over lanes to set lane id of self-reverse lane.
            foreach (var adMapLane in adMapLanes)
            {
                laneOverlapsInfo.GetOrCreate(adMapLane.Id).id = HdId(adMapLane.Id);
            }

            // Iterate over all adMapLanes
            for (int i = 0; i < adMapLanes.Count(); i++)
            {
                for (int j = i+1; j < adMapLanes.Count(); j++)
                {
                    var lane = adMapLanes[i];
                    var laneCmp = adMapLanes[j];

                    if (!lane.isSelfReverseLane || !laneCmp.isSelfReverseLane)
                        continue;

                    if (lane.selfReverseLaneId == laneCmp.Id)
                    {
                        laneOverlapsInfo.GetOrCreate(lane.Id).selfReverseLaneId = laneCmp.Id;
                        laneOverlapsInfo.GetOrCreate(laneCmp.Id).selfReverseLaneId = lane.Id;

                        laneOverlapsInfo.GetOrCreate(lane.Id).id = HdId(lane.Id);
                        laneOverlapsInfo.GetOrCreate(laneCmp.Id).id = HdId(laneCmp.Id);
                    }
                }
            }
        }

        // Apollo 5 method
        void MakeSelfReverseLane()
        {
            var laneSections = MapAnnotationData.GetLaneSections();
            foreach(var laneSection in laneSections)
            {
                var lanes = laneSection.GetComponentsInChildren<MapLane>();

                if (lanes.Count() != 2)
                    continue;

                if (!lanes[0].needSelfReverseLane || !lanes[1].needSelfReverseLane)
                    continue;

                List<Vector3> centerLinePoints = ComputeCenterLine(lanes[0].mapWorldPositions, lanes[1].mapWorldPositions);
                List<Vector3> firstSelfReversePts = new List<Vector3>(centerLinePoints);
                List<Vector3> secondSelfReversePts = new List<Vector3>(centerLinePoints);
                secondSelfReversePts.Reverse();

                // Make small difference between forward and backward self-reverse lanes to solve the problem.
                // Problem: Apollo routing may make longer route if it choose wrong waypoint among two ones.
                List<Vector3> dirVector = new List<Vector3>();
                for (int i = 0; i < secondSelfReversePts.Count; i++)
                {
                    var dir = Vector3.Scale(GetNormalDir(secondSelfReversePts, i), new Vector3(0.5f, 0.5f, 0.5f));
                    dirVector.Add(dir);
                }
                for (int i = 0; i < secondSelfReversePts.Count; i++)
                {
                    secondSelfReversePts[i] += dirVector[i];
                }

                // Direction
                var directionLane0 = (lanes[0].mapWorldPositions.Last() - lanes[0].mapWorldPositions[0]).normalized;
                var directionLane1 = (lanes[1].mapWorldPositions.Last() - lanes[1].mapWorldPositions[0]).normalized;
                var directionFirstSelfReverse = (firstSelfReversePts.Last() - firstSelfReversePts[0]).normalized;
                var directionSecondSelfReverse = (secondSelfReversePts.Last() - secondSelfReversePts[0]).normalized;

                if (Vector3.Dot(directionFirstSelfReverse, directionLane0) > 0)
                {
                    firstSelfReversePts[0] = lanes[0].mapWorldPositions[0];
                    firstSelfReversePts[firstSelfReversePts.Count - 1] = lanes[0].mapWorldPositions.Last();
                }
                else if (Vector3.Dot(directionFirstSelfReverse, directionLane1) > 0)
                {
                    firstSelfReversePts[0] = lanes[1].mapWorldPositions[0];
                    firstSelfReversePts[firstSelfReversePts.Count - 1] = lanes[1].mapWorldPositions.Last();
                }

                if (Vector3.Dot(directionSecondSelfReverse, directionLane0) > 0)
                {
                    secondSelfReversePts[0] = lanes[0].mapWorldPositions[0];
                    secondSelfReversePts[secondSelfReversePts.Count - 1] = lanes[0].mapWorldPositions.Last();
                }
                else if (Vector3.Dot(directionSecondSelfReverse, directionLane1) > 0)
                {
                    secondSelfReversePts[0] = lanes[1].mapWorldPositions[0];
                    secondSelfReversePts[secondSelfReversePts.Count - 1] = lanes[1].mapWorldPositions.Last();
                }

                // Create MapLane based on center line, MapLine based on two ways
                // Generate first self-rerverse lane
                ADMapLane firstSrADLane = new ADMapLane();
                firstSrADLane.isSelfReverseLane = true;
                firstSrADLane.mapWorldPositions = new List<Vector3>(firstSelfReversePts);

                // Generate second self-rerverse lane
                ADMapLane secondSrADLane = new ADMapLane();
                secondSrADLane.isSelfReverseLane = true;
                secondSrADLane.mapWorldPositions = new List<Vector3>(secondSelfReversePts);

                // Replace lane with self-reverse lane.
                string firstNonSrLaneId = null;
                string secondNonSrLaneId = null;

                for (int i = 0; i < adMapLanes.Count; i++)
                {
                    var lane = adMapLanes[i];

                    if (lane.Id == lanes[0].id)
                    {
                        firstNonSrLaneId = lane.Id;
                        firstSrADLane.Id = lane.Id;
                        break;
                    }
                }

                for (int i = 0; i < adMapLanes.Count; i++)
                {
                    var lane = adMapLanes[i];

                    if (lane.Id == lanes[1].id)
                    {
                        secondNonSrLaneId = lane.Id;
                        secondSrADLane.Id = lane.Id;
                        break;
                    }
                }

                for (int i = 0; i < adMapLanes.Count; i++)
                {
                    if (adMapLanes[i].Id == firstNonSrLaneId)
                    {
                        adMapLanes[i] = firstSrADLane;
                        adMapLanes[i].selfReverseLaneId = lanes[1].id;
                        break;
                    }
                }

                for (int i = 0; i < adMapLanes.Count; i++)
                {
                    if (adMapLanes[i].Id == secondNonSrLaneId)
                    {
                        adMapLanes[i] = secondSrADLane;
                        adMapLanes[i].selfReverseLaneId = lanes[0].id;
                        break;
                    }
                }

                // Made Lane Overlap info for self reverse lane
                laneOverlapsInfo.GetOrCreate(firstSrADLane.Id).selfReverseLaneId = secondSrADLane.Id;
                laneOverlapsInfo.GetOrCreate(secondSrADLane.Id).selfReverseLaneId = firstSrADLane.Id;
            }
        }

        public static List<Vector3> ComputeCenterLine(List<Vector3> leftLinePoints, List<Vector3> rightLinePoints)
        {
            List<Vector3> centerLinePoints = new List<Vector3>();
            var leftFirstPoint = leftLinePoints[0];
            var leftLastPoint = leftLinePoints[leftLinePoints.Count-1];
            var rightFirstPoint = rightLinePoints[0];
            var rightLastPoint = rightLinePoints[rightLinePoints.Count-1];
            var leftDirection = (leftLastPoint - leftFirstPoint).normalized;
            var rightDirection = (rightLastPoint - rightFirstPoint).normalized;
            float resolution = 5; // 5 meter
            var sameDirection = true;

            if (Vector3.Dot(leftDirection, rightDirection) < 0)
            {
                sameDirection = false;
            }

            float GetRangedLength(List<Vector3> positions)
            {
                float len = 0;
                for (int i = 0; i < positions.Count - 1; i++)
                {
                    len += (positions[i + 1] - positions[i]).magnitude;
                }

                return len;
            }
            // Get the length of longer boundary line
            float leftLength = GetRangedLength(leftLinePoints);
            float rightLength = GetRangedLength(rightLinePoints);
            float longerDistance = (leftLength > rightLength) ? leftLength : rightLength;
            int partitions = (int)Math.Ceiling(longerDistance / resolution);
            if (partitions < 2)
            {
                // For line whose length is less than resolution
                partitions = 2; // Make sure every line has at least 2 partitions.
            }

            float leftResolution = leftLength / partitions;
            float rightResolution = rightLength / partitions;


            List<Vector3> splittedLeftPoints = new List<Vector3>(), splittedRightPoints = new List<Vector3>();
            SplitLine(leftLinePoints, ref splittedLeftPoints, leftResolution, partitions);
            // If left and right lines have opposite direction, reverse right line
            if (!sameDirection)
                SplitLine(rightLinePoints, ref splittedRightPoints, rightResolution, partitions, true);
            else
                SplitLine(rightLinePoints, ref splittedRightPoints, rightResolution, partitions);

            if (splittedLeftPoints.Count != partitions + 1 || splittedRightPoints.Count != partitions + 1)
            {
                Debug.LogError("Something wrong with number of points. (left, right, partitions): (" + leftLinePoints.Count + ", " + rightLinePoints.Count + ", " + partitions);
                return new List<Vector3>();
            }

            for (int i = 0; i < partitions+1; i++)
            {
                Vector3 centerPoint = (splittedRightPoints[i] + splittedLeftPoints[i]) / 2;
                centerLinePoints.Add(centerPoint);
            }

            // Compare temp centerLine with left line, determine direction
            var centerDirection = (centerLinePoints[centerLinePoints.Count-1] - centerLinePoints[0]).normalized;
            var centerToLeftDir = (leftFirstPoint - centerLinePoints[0]).normalized;

            if (Vector3.Cross(centerDirection, centerToLeftDir).y > 0)
            {
                // Left line is on right of centerLine, we need to reverse the center points
                centerLinePoints.Reverse();
            }

            return centerLinePoints;
        }

        Vector3 GetRightNormalDir(Vector3 p1, Vector3 p2)
        {
            var dir = p2 - p1;
            return Vector3.Cross(Vector3.up, dir).normalized;
        }

        Vector3 GetNormalDir(List<Vector3> points, int index)
        {
            Vector3 normalDir;
            if (index == 0) normalDir = GetRightNormalDir(points[0], points[1]);
            else if (index == points.Count - 1) normalDir = GetRightNormalDir(points[points.Count - 2], points.Last());
            else
            {
                var normalDir1 = GetRightNormalDir(points[index - 1], points[index]);
                var normalDir2 = GetRightNormalDir(points[index], points[index + 1]);
                normalDir = (normalDir1 + normalDir2).normalized;
            }

            // if (isLeft) return -normalDir;
            return normalDir;
        }

        static void SplitLine(List<Vector3> positions, ref List<Vector3> splittedLinePoints, float resolution, int partitions, bool reverse=false)
        {
            splittedLinePoints = new List<Vector3>();
            splittedLinePoints.Add(positions[0]); // Add first point

            float residue = 0; // Residual length from previous segment
            int last = 0;
            // loop through each segment in boundry line
            for (int i = 1; i < positions.Count; i++)
            {
                if (splittedLinePoints.Count >= partitions) break;

                Vector3 lastPoint = positions[last];
                Vector3 curPoint = positions[i];

                // Continue if no points are made within current segment
                float segmentLength = Vector3.Distance(lastPoint, curPoint);
                if (segmentLength + residue < resolution)
                {
                    residue += segmentLength;
                    last = i;
                    continue;
                }

                Vector3 direction = (curPoint - lastPoint).normalized;
                for (float length = resolution - residue; length <= segmentLength; length += resolution)
                {
                    Vector3 partitionPoint = lastPoint + direction * length;
                    splittedLinePoints.Add(partitionPoint);
                    if (splittedLinePoints.Count >= partitions) break;
                    residue = segmentLength - length;
                }

                if (splittedLinePoints.Count >= partitions) break;
                last = i;
            }

            splittedLinePoints.Add(positions[positions.Count - 1]);

            if (reverse)
                splittedLinePoints.Reverse();
        }

        void MakeCrossWalkAnnotation()
        {
            foreach (var adMapCrossWalk in adMapCrossWalks)
            {
                var crossWalkInWorld = new List<Vector3>();
                var frontSegment = new List<Vector3>();
                var backSegment = new List<Vector3>();

                var polygon = new HD.Polygon();

                foreach (var worldPos in adMapCrossWalk.mapWorldPositions)
                {
                    crossWalkInWorld.Add(worldPos);
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

                foreach (var lane in crossWalkOverlapsInfo[adMapCrossWalk.Id].laneOverlapIds.Keys)
                {
                    var start_s = FindSegmentDistOnLane(frontSegment, lane);
                    var end_s = FindSegmentDistOnLane(backSegment, lane);

                    var objectLane = new HD.ObjectOverlapInfo()
                    {
                        id = laneOverlapsInfo[lane].id,
                        lane_overlap_info = new HD.LaneOverlapInfo()
                        {
                            start_s = Mathf.Min(start_s, end_s),
                            end_s = Mathf.Max(start_s, end_s),
                            is_merge = false,
                        }
                    };

                    var objectCrossWalk = new HD.ObjectOverlapInfo()
                    {
                        id = crossWalkOverlapsInfo[adMapCrossWalk.Id].id,
                        crosswalk_overlap_info = new HD.CrosswalkOverlapInfo(),
                    };

                    var overlap = new HD.Overlap()
                    {
                        id = crossWalkOverlapsInfo[adMapCrossWalk.Id].laneOverlapIds[lane],
                    };

                    overlap.@object.Add(objectLane);
                    overlap.@object.Add(objectCrossWalk);
                    Hdmap.overlap.Add(overlap);

                    crossWalkAnnotation.id = crossWalkOverlapsInfo[adMapCrossWalk.Id].id;
                    crossWalkAnnotation.overlap_id.Add(crossWalkOverlapsInfo.GetOrCreate(adMapCrossWalk.Id).laneOverlapIds[lane]);
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
        float FindSegmentDistOnLane(List<Vector3> mapWorldPositions, string lane)
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

            foreach (var seg in adMapLanes.Where(seg => seg.Id == lane))
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
        float FindSegmentDistNotOnLane(List<Vector3> mapWorldPositions, string lane)
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

            float s = 0;
            float totalS = 0;

            foreach (var seg in adMapLanes.Where(seg => seg.Id == lane))
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
                        s += d1;

                        break;
                    }
                    else
                    {
                        s += Vector2.Distance(firstPtLane, secondPtLane);
                    }
                }
                totalS = 0;
            }

            return s;
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

        public void Export(string filePath)
        {
            mapOrigin = MapOrigin.Find();

            OriginEasting = mapOrigin.OriginEasting;
            OriginNorthing = mapOrigin.OriginNorthing;
            AltitudeOffset = mapOrigin.AltitudeOffset;
            OriginZone = mapOrigin.UTMZoneId;

            if (Calculate())
            {
                using (var fs = File.Create(filePath))
                {
                    ProtoBuf.Serializer.Serialize(fs, Hdmap);
                }

                Debug.Log("Successfully generated and exported Apollo HD Map!");
            }
        }

        static HD.Id HdId(string id) => new HD.Id() { id = id };

        void GetMapData<MapType, ADType, OverlapType>(string name, MapManagerData annotations, List<ADType> ad, Dictionary<string, OverlapType> overlaps, Func<MapType, ADType> create)
            where MapType : IMapType
            where ADType : ADMap, new()
            where OverlapType : OverlapInfo, new()
        {
            int count = 0;
            foreach (var item in MapAnnotationData.GetData<MapType>())
            {
                string id;
                if (string.IsNullOrEmpty(item.id))
                {
                    id = $"{name}_{count++}";
                    item.id = id;
                }
                else
                {
                    id = item.id;
                }

                ad.Add(create(item));
                overlaps.GetOrCreate(id).id = HdId(id);
            }
        }
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