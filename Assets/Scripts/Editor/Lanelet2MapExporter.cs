/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Simulator.Map;
using OsmSharp;
using OsmSharp.Streams;
using OsmSharp.Tags;
using Utility = Simulator.Utilities.Utility;

namespace Simulator.Editor
{
    public class CrossWalkData : PositionsData
    {
        public MapCrossWalk mapCrossWalk;
        public CrossWalkData(MapCrossWalk crossWalk) : base(crossWalk)
        {
            mapCrossWalk = crossWalk;
        }
    }

    public class ParkingSpaceData : PositionsData
    {
        public MapParkingSpace mapParkingSpace;
        public ParkingSpaceData(MapParkingSpace parkingSpace) : base(parkingSpace)
        {
            mapParkingSpace = parkingSpace;
        }
    }

    public class Lanelet2MapExporter
    {
        private MapManagerData MapAnnotationData;
        MapOrigin MapOrigin;
        List<OsmGeo> map = new List<OsmGeo>();
        Dictionary<int, long> LineId2StartNodeId = new Dictionary<int, long>();
        Dictionary<int, long> LineId2EndNodeId = new Dictionary<int, long>();
        Dictionary<long, Node> Id2Node = new Dictionary<long, Node>();
        HashSet<LaneData> LanesData;
        HashSet<LineData> LinesData;

        public Lanelet2MapExporter()
        {
        }

        bool Calculate()
        {
            MapAnnotationData = new MapManagerData();
            var allLanes = new HashSet<MapTrafficLane>(MapAnnotationData.GetData<MapTrafficLane>());
            var areAllLanesWithBoundaries = Lanelet2MapExporter.AreAllLanesWithBoundaries(allLanes, true);
            if (!areAllLanesWithBoundaries) return false;

            MapAnnotationData.GetIntersections();
            MapAnnotationData.GetTrafficLanes();

            MapOrigin = MapOrigin.Find();

            // Initial collection
            var laneSegments = new HashSet<MapTrafficLane>(MapAnnotationData.GetData<MapTrafficLane>());
            var lineSegments = new HashSet<MapLine>(MapAnnotationData.GetData<MapLine>());
            var signalLights = new List<MapSignal>(MapAnnotationData.GetData<MapSignal>());
            var crossWalks = new List<MapCrossWalk>(MapAnnotationData.GetData<MapCrossWalk>());
            var mapSigns = new List<MapSign>(MapAnnotationData.GetData<MapSign>());
            var parkingSpaces = new List<MapParkingSpace>(MapAnnotationData.GetData<MapParkingSpace>());

            foreach (var mapSign in mapSigns)
            {
                if (mapSign.signType == MapData.SignType.STOP && mapSign.stopLine != null)
                {
                    mapSign.stopLine.stopSign = mapSign;
                }
            }

            LinesData = OpenDriveMapExporter.GetLinesData(lineSegments);
            LanesData = OpenDriveMapExporter.GetLanesData(laneSegments);

            // Link before and after segment for each lane segment
            if (!OpenDriveMapExporter.LinkSegments(LanesData)) return false;

            if (!OpenDriveMapExporter.CheckNeighborLanes(laneSegments)) return false;

            var stopLineLanesData = new Dictionary<LineData, List<LaneData>>();

            foreach (var laneData in LanesData)
            {
                if (laneData.mapLane.stopLine != null)
                {
                    var stopLineData = LineData.Line2LineData[laneData.mapLane.stopLine];
                    stopLineLanesData.GetOrCreate(stopLineData).Add(laneData);
                }
            }

            var crossWalksData = new List<CrossWalkData>(crossWalks.Select(x => new CrossWalkData(x)));
            // Link points in each crosswalk
            AlignPointsInCrossWalk(crossWalksData);

            var parkingSpacesData = new List<ParkingSpaceData>(parkingSpaces.Select(x => new ParkingSpaceData(x)));
            // Link Points in each parking area
            AlignPointsInParkingSpace(parkingSpacesData);

            // Link before and after segment for each line segment based on lane's predecessor/successor
            AlignPointsInLines(LanesData);
            CreateLaneletsFromLanes(LanesData);

            // process stop lines - create stop lines
            foreach (var lineData in LinesData)
            {
                if (lineData.mapLine.lineType == MapData.LineType.STOP)
                {
                    Way wayStopLine = CreateWayStopLineFromLine(lineData);

                    List<Way> wayTrafficLightList = new List<Way>();
                    List<Way> wayLightBulbsList = new List<Way>();

                    if (lineData.mapLine.signals.Count > 0)
                    {
                        // create way for traffic light and light bulbs
                        foreach (var signal in lineData.mapLine.signals)
                        {
                            // create way for traffic light (lower outline)
                            Way wayTrafficLight = CreateWayTrafficLightFromSignal(signal);
                            wayTrafficLightList.Add(wayTrafficLight);

                            // create way for light_bulbs
                            Way wayLightBulbs = CreateWayLightBulbsFromSignal(signal, (long)wayTrafficLight.Id);
                            wayLightBulbsList.Add(wayLightBulbs);
                        }

                        // create relation of regulatory element
                        Relation relationRegulatoryElement = CreateRegulatoryElementFromStopLineSignals(wayStopLine, wayTrafficLightList, wayLightBulbsList);
                        map.Add(relationRegulatoryElement);

                        // asscoate with lanelet
                        foreach (var laneData in stopLineLanesData[lineData])
                        {
                            RelationMember member = new RelationMember(relationRegulatoryElement.Id.Value, "regulatory_element", OsmGeoType.Relation);
                            AddMemberToLanelet(laneData, member);
                        }
                    }

                    if (lineData.mapLine.isStopSign)
                    {
                        if (!stopLineLanesData.ContainsKey(lineData))
                        {
                            var msg = $"Stop line for {lineData.mapLine.stopSign.gameObject.name} ";
                            msg += "sign is not associated with any lane (lane probably ";
                            msg += "does not intersect stop line in last segment)";
                            Debug.LogError(msg, lineData.mapLine.stopSign.gameObject);
                            throw new Exception("Export failed");
                        }

                        // create way for stop sign
                        if (lineData.mapLine.stopSign == null)
                        {
                            var msg = $"Stop line {lineData.go.name} should be associated with a stop sign, please check and fix!";
                            Debug.LogError(msg, lineData.go);
                            continue;
                        }

                        Way wayStopSign = CreateWayFromStopSign(lineData.mapLine.stopSign);
                        Relation relationRegulatoryElement = CreateRegulatoryElementFromStopLineStopSign(wayStopLine, wayStopSign);
                        map.Add(relationRegulatoryElement);

                        // asscoate with lanelet
                        foreach (var laneData in stopLineLanesData[lineData])
                        {
                            RelationMember member = new RelationMember(relationRegulatoryElement.Id.Value, "regulatory_element", OsmGeoType.Relation);
                            AddMemberToLanelet(laneData, member);
                        }
                    }
                }
            }

            // process crosswalk
            foreach (var crossWalkData in crossWalksData)
            {
                map.Add(CreateLaneletFromCrossWalk(crossWalkData));
            }

            // process parking space
            foreach (var parkingSpaceData in parkingSpacesData)
            {
                map.Add(CreateMultiPolygonFromParkingSpace(parkingSpaceData));
            }

            return true;
        }

        private void CreateLaneletsFromLanes(HashSet<LaneData> lanesData)
        {
            foreach (var laneData in lanesData)
            {
                Relation lanelet = CreateLaneletFromLane(laneData);
                if (lanelet != null)
                {
                    map.Add(lanelet);
                }
            }
        }

        public long GetNewId()
        {
            return map.Count + 1;
        }

        public Node GetNodeById(long id)
        {
            foreach (OsmGeo element in map)
            {
                if (element == null)
                {
                    continue;
                }

                if (element.Type == OsmGeoType.Node)
                {
                    Node node = element as Node;
                    if (node.Id == id)
                    {
                        return node;
                    }
                }
            }
            return null;
        }

        public long NodeExists(Node node)
        {
            if (map == null)
            {
                return 0;
            }

            foreach (OsmGeo element in map)
            {
                if (element == null)
                {
                    continue;
                }

                if (element.Type == OsmGeoType.Node)
                {
                    Node _node = element as Node;
                    if (_node.Latitude == node.Latitude && _node.Longitude == node.Longitude && _node.Tags.Equals(node.Tags))
                    {
                        return _node.Id.Value;
                    }
                }
            }

            return 0;
        }

        public Way GetWayById(long id)
        {
            foreach (OsmGeo element in map)
            {
                if (element == null)
                {
                    continue;
                }

                if (element.Type == OsmGeoType.Way)
                {
                    Way way = element as Way;
                    if (way.Id == id)
                    {
                        return way;
                    }
                }
            }
            return null;
        }

        public long WayExists(Way way)
        {
            if (map == null)
            {
                return 0;
            }

            foreach (OsmGeo element in map)
            {
                if (element == null)
                {
                    continue;
                }

                if (element.Type == OsmGeoType.Way)
                {
                    Way _way = element as Way;
                    if (Enumerable.SequenceEqual(_way.Nodes, way.Nodes) || Enumerable.SequenceEqual(_way.Nodes, way.Nodes.Reverse()))
                    {
                        return _way.Id.Value;
                    }
                }
            }

            return 0;
        }

        public Way CreateWayFromLine(LineData lineData, TagsCollection tags, bool isBasedOnLane = false)
        {
            var startIdx = 0;
            var endIdx = lineData.mapWorldPositions.Count;
            List<Node> nodes = new List<Node>();
            Node firstNode, lastNode;
            var lineId = lineData.go.GetInstanceID();

            if (isBasedOnLane)
            {
                startIdx = 1;
                endIdx = lineData.mapWorldPositions.Count - 1;
                if (LineId2StartNodeId.ContainsKey(lineId))
                {
                    var nodeId = LineId2StartNodeId[lineId];
                    firstNode = Id2Node[nodeId];
                }
                else firstNode = CreateNodeByIndex(lineData, 0);
                nodes.Add(firstNode);
            }

            // create nodes
            for (int p = startIdx; p < endIdx; p++)
            {
                Node node = CreateNodeByIndex(lineData, p);
                nodes.Add(node);
            }

            if (isBasedOnLane)
            {
                if (LineId2EndNodeId.ContainsKey(lineId))
                {
                    var nodeId = LineId2EndNodeId[lineId];
                    lastNode = Id2Node[nodeId];
                }
                else lastNode = CreateNodeByIndex(lineData, lineData.mapWorldPositions.Count - 1);
                nodes.Add(lastNode);
            }

            return CreateWayFromNodes(nodes, tags);
        }

        private Node CreateNodeByIndex(LineData lineData, int p)
        {
            Vector3 pos = lineData.mapWorldPositions[p];
            var location = MapOrigin.PositionToGpsLocation(pos);
            Node node = CreateNodeFromPoint(pos);
            Id2Node[node.Id.Value] = node;
            return node;
        }

        public Node CreateNodeFromPoint(Vector3 point)
        {
            TagsCollection tags = new TagsCollection();
            return CreateNodeFromPoint(point, tags);
        }

        public Node CreateNodeFromPoint(Vector3 point, TagsCollection tags)
        {
            var location = MapOrigin.PositionToGpsLocation(point);
            TagsCollection tags_xyele = new TagsCollection(
                new Tag("x", point.z.ToString()),
                new Tag("y", (-point.x).ToString()),
                new Tag("ele", point.y.ToString())
            );

            // concatenate two tags
            foreach (Tag t in tags_xyele)
            {
                tags.AddOrReplace(t);
            }

            Node node = new Node()
            {
                Id = GetNewId(),
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                Tags = tags,
                Version = 1,
                Visible = true,
            };

            // check if the node already exists
            long id = NodeExists(node);

            if (id == 0)
            {
                map.Add(node);
                return node;
            }
            else
            {
                return GetNodeById(id);
            }
        }

        public Way CreateWayFromNodes(List<Node> nodeList)
        {
            TagsCollection tags = new TagsCollection();
            return CreateWayFromNodes(nodeList, tags);
        }

        public Way CreateWayFromNodes(List<Node> nodeList, TagsCollection tags)
        {
            Way way = new Way()
            {
                Id = GetNewId(),
                Version = 1,
                Visible = true,
                Nodes = nodeList.Select(n => n.Id.Value).ToArray(),
                Tags = tags
            };

            // check if the way already exists
            long id = WayExists(way);

            if (id == 0)
            {
                map.Add(way);
                return way;
            }
            else
            {
                return GetWayById(id);
            }
        }

        public Way CreateWayTrafficLightFromSignal(MapSignal signalLight)
        {
            // get width and height of the traffic light
            Vector3 boundScale = signalLight.boundScale;

            double height = boundScale.y;

            Vector3 pos_center = Vector3.zero;
            Vector3 pos_lower_left = new Vector3(pos_center.x + boundScale.x / 2.0f, pos_center.y - boundScale.y / 2.0f, pos_center.z);
            Vector3 pos_lower_right = new Vector3(pos_center.x - boundScale.x / 2.0f, pos_center.y - boundScale.y / 2.0f, pos_center.z);

            Vector3 world_pos_center = signalLight.transform.TransformPoint(pos_center);
            Vector3 world_pos_lower_left = signalLight.transform.TransformPoint(pos_lower_left);
            Vector3 world_pos_lower_right = signalLight.transform.TransformPoint(pos_lower_right);

            // create nodes
            Node nodeLowerLeft = CreateNodeFromPoint(world_pos_lower_left);
            Node nodeLowerRight = CreateNodeFromPoint(world_pos_lower_right);

            TagsCollection tags = new TagsCollection(
                new Tag("type", "traffic_light"),
                new Tag("subtype", "red_yellow_green"),
                new Tag("height", height.ToString())
            );

            // create ways
            Way signalWay = CreateWayFromNodes(new List<Node>() { nodeLowerLeft, nodeLowerRight }, tags);

            return signalWay;
        }

        public Way CreateWayLightBulbsFromSignal(MapSignal signalLight, long trafficLightId)
        {
            List<Node> nodes = new List<Node>();
            for (int p = 0; p < signalLight.signalData.Count; p++)
            {
                string color = "yellow";

                // Get global position of each lamps of one signal light
                Vector3 pos = signalLight.transform.TransformPoint(signalLight.signalData[p].localPosition);

                switch (signalLight.signalData[p].signalColor)
                {
                    case MapData.SignalColorType.Red:
                        color = "red";
                        break;
                    case MapData.SignalColorType.Yellow:
                        color = "yellow";
                        break;
                    case MapData.SignalColorType.Green:
                        color = "green";
                        break;
                }

                TagsCollection tagsColor = new TagsCollection(new Tag("color", color));
                Node node = CreateNodeFromPoint(pos, tagsColor);
                nodes.Add(node);
            }

            // After creating nodes, create ways
            var tagsLightBulbs = new TagsCollection(
                new Tag("type", "light_bulbs"),
                new Tag("traffic_light_id", trafficLightId.ToString())
            );

            return (CreateWayFromNodes(nodes, tagsLightBulbs));
        }

        public Relation CreateRelationFromMembers(RelationMember[] members, TagsCollection tags)
        {
            Relation relation = new Relation()
            {
                Id = GetNewId(),
                Version = 1,
                Visible = true,
                Tags = tags,
                Members = members
            };

            return relation;
        }

        public Relation CreateRegulatoryElementFromStopLineSignals(Way wayStopLine, List<Way> wayTrafficLightList, List<Way> wayLightBulbsList)
        {
            int num = wayTrafficLightList.Count + wayLightBulbsList.Count + 1;
            RelationMember[] members = new RelationMember[num];

            members[0] = new RelationMember(wayStopLine.Id.Value, "ref_line", OsmGeoType.Way);

            for (int i = 0; i < wayTrafficLightList.Count; i++)
            {
                members[i + 1] = new RelationMember(wayTrafficLightList[i].Id.Value, "refers", OsmGeoType.Way);
            }

            for (int i = 0; i < wayLightBulbsList.Count; i++)
            {
                members[i + wayTrafficLightList.Count + 1] = new RelationMember(wayLightBulbsList[i].Id.Value, "light_bulbs", OsmGeoType.Way);
            }

            TagsCollection tags = new TagsCollection(
                new Tag("subtype", "traffic_light"),
                new Tag("type", "regulatory_element")
            );

            return CreateRelationFromMembers(members, tags);
        }

        public Relation CreateLaneletFromLane(LaneData laneData)
        {
            var leftLine = laneData.mapLane.leftLineBoundry;
            var leftLineData = LineData.Line2LineData[leftLine];
            var rightLine = laneData.mapLane.rightLineBoundry;
            var rightLineData = LineData.Line2LineData[rightLine];

            var isSameDirectionLeft = isSameDirection(laneData, leftLineData);
            var isSameDirectionRight = isSameDirection(laneData, rightLineData);

            // check if a lane has both left and right boundary
            if (leftLine != null && rightLine != null)
            {
                TagsCollection left_way_tags = new TagsCollection();
                TagsCollection right_way_tags = new TagsCollection();
                TagsCollection lanelet_tags = new TagsCollection(
                    new Tag("location", "urban"),
                    new Tag("subtype", "road"),
                    new Tag("participant:vehicle", "yes"),
                    new Tag("type", "lanelet")
                );

                // create node and way from boundary
                Way leftWay = CreateWayFromLine(leftLineData, left_way_tags, true);
                Way rightWay = CreateWayFromLine(rightLineData, right_way_tags, true);

                UpdateLineStartEnd2NodeId(leftLineData, leftWay);
                UpdateLineStartEnd2NodeId(rightLineData, rightWay);

                var leftWayStartNodeId = leftWay.Nodes.First();
                var leftWayEndNodeId = leftWay.Nodes.Last();

                var rightWayStartNodeId = rightWay.Nodes.First();
                var rightWayEndNodeId = rightWay.Nodes.Last();

                var beforeLanesData = laneData.befores;
                var leftStartNodeBasedOnLane = isSameDirectionLeft ? leftWayStartNodeId : leftWayEndNodeId;
                var rightStartNodeBasedOnLane = isSameDirectionRight ? rightWayStartNodeId : rightWayEndNodeId;

                foreach (var beforeLaneData in beforeLanesData)
                {
                    var beforeLaneLeftLine = beforeLaneData.mapLane.leftLineBoundry;
                    var beforeLaneLeftLineData = LineData.Line2LineData[beforeLaneLeftLine];
                    UpdateBeforeLane(leftStartNodeBasedOnLane, beforeLaneData, beforeLaneLeftLineData);

                    var beforeLaneRightLine = beforeLaneData.mapLane.rightLineBoundry;
                    var beforeLaneRightLineData = LineData.Line2LineData[beforeLaneRightLine];
                    UpdateBeforeLane(rightStartNodeBasedOnLane, beforeLaneData, beforeLaneRightLineData);
                }

                var afterLanesData = laneData.afters;
                var leftEndNodeBasedOnLane = isSameDirectionLeft ? leftWayEndNodeId : leftWayStartNodeId;
                var rightEndNodeBasedOnLane = isSameDirectionRight ? rightWayEndNodeId : rightWayStartNodeId;
                foreach (var afterLaneData in afterLanesData)
                {
                    var afterLaneLeftLine = afterLaneData.mapLane.leftLineBoundry;
                    var afterLaneLeftLineData = LineData.Line2LineData[afterLaneLeftLine];
                    UpdateAfterLane(leftEndNodeBasedOnLane, afterLaneData, afterLaneLeftLineData);

                    var afterLaneRightLine = afterLaneData.mapLane.rightLineBoundry;
                    var afterLaneRightLineData = LineData.Line2LineData[afterLaneRightLine];
                    UpdateAfterLane(rightEndNodeBasedOnLane, afterLaneData, afterLaneRightLineData);
                }


                AddBoundaryTagToWay(laneData, leftWay, rightWay);

                var lane = laneData.mapLane;
                if (lane.isIntersectionLane)
                {
                    if (lane.laneTurnType == MapTrafficLane.LaneTurnType.NO_TURN)
                    {
                        lanelet_tags.Add(
                            new Tag("turn_direction", "straight")
                        );
                    }
                    if (lane.laneTurnType == MapTrafficLane.LaneTurnType.RIGHT_TURN)
                    {
                        lanelet_tags.Add(
                            new Tag("turn_direction", "right")
                        );
                    }
                    if (lane.laneTurnType == MapTrafficLane.LaneTurnType.LEFT_TURN)
                    {
                        lanelet_tags.Add(
                            new Tag("turn_direction", "left")
                        );
                    }
                }

                var members = new[]
                {
                    new RelationMember(leftWay.Id.Value, "left", OsmGeoType.Way),
                    new RelationMember(rightWay.Id.Value, "right", OsmGeoType.Way),
                };

                return CreateRelationFromMembers(members, lanelet_tags);
            }
            else
            {
                return null;
            }
        }

        private void UpdateBeforeLane(long startNodeBasedOnLane, LaneData beforeLaneData, LineData beforeLaneLineData)
        {
            if (isSameDirection(beforeLaneData, beforeLaneLineData))
            {
                LineId2EndNodeId[beforeLaneLineData.go.GetInstanceID()] = startNodeBasedOnLane;
            }
            else
            {
                LineId2StartNodeId[beforeLaneLineData.go.GetInstanceID()] = startNodeBasedOnLane;
            }
        }

        private void UpdateAfterLane(long EndNodeBasedOnLane, LaneData afterLaneData, LineData afterLaneLineData)
        {
            if (isSameDirection(afterLaneData, afterLaneLineData))
            {
                LineId2StartNodeId[afterLaneLineData.go.GetInstanceID()] = EndNodeBasedOnLane;
            }
            else
            {
                LineId2EndNodeId[afterLaneLineData.go.GetInstanceID()] = EndNodeBasedOnLane;
            }
        }
        void UpdateLineStartEnd2NodeId(LineData lineData, Way way)
        {
            var lineId = lineData.go.GetInstanceID();
            LineId2StartNodeId[lineId] = way.Nodes.First();
            LineId2EndNodeId[lineId] = way.Nodes.Last();
        }

        public Way CreateWayStopLineFromLine(LineData lineData)
        {
            if (lineData.mapLine.lineType == MapData.LineType.STOP)
            {
                var tagStopLine = new TagsCollection(
                    new Tag("type", "stop_line")
                );

                // create way for stop line
                Way wayStopLine = CreateWayFromLine(lineData, tagStopLine);

                return wayStopLine;
            }
            return null;
        }

        public Way CreateWayFromStopSign(MapSign sign)
        {
            Vector3 boundScale = sign.boundScale;
            double height = boundScale.y;

            Vector3 pos_center = Vector3.zero + sign.boundOffsets;
            Vector3 pos_lower_left = new Vector3(pos_center.x + boundScale.x / 2.0f, pos_center.y - boundScale.y / 2.0f, pos_center.z);
            Vector3 pos_lower_right = new Vector3(pos_center.x - boundScale.x / 2.0f, pos_center.y - boundScale.y / 2.0f, pos_center.z);

            Vector3 world_pos_center = sign.transform.TransformPoint(pos_center);
            Vector3 world_pos_lower_left = sign.transform.TransformPoint(pos_lower_left);
            Vector3 world_pos_lower_right = sign.transform.TransformPoint(pos_lower_right);

            // create nodes
            Node nodeLowerLeft = CreateNodeFromPoint(world_pos_lower_left);
            Node nodeLowerRight = CreateNodeFromPoint(world_pos_lower_right);

            var tags = new TagsCollection(
                new Tag("height", height.ToString()),
                new Tag("subtype", "usR1-1"),
                new Tag("type", "traffic_sign")
            );

            // create ways
            Way signWay = CreateWayFromNodes(new List<Node>() { nodeLowerLeft, nodeLowerRight }, tags);

            return signWay;
        }

        public Relation CreateRegulatoryElementFromStopLineStopSign(Way wayStopLine, Way wayStopSign)
        {
            RelationMember[] members = new RelationMember[] {
                new RelationMember(wayStopLine.Id.Value, "ref_line", OsmGeoType.Way),
                new RelationMember(wayStopSign.Id.Value, "refers", OsmGeoType.Way)
            };

            TagsCollection tags = new TagsCollection(
                new Tag("subtype", "traffic_sign"),
                new Tag("type", "regulatory_element")
            );

            return CreateRelationFromMembers(members, tags);
        }

        public Relation CreateLaneletFromCrossWalk(CrossWalkData crossWalkData)
        {
            Vector3 p0 = crossWalkData.go.transform.TransformPoint(crossWalkData.mapLocalPositions[0]);
            Vector3 p1 = crossWalkData.go.transform.TransformPoint(crossWalkData.mapLocalPositions[1]);
            Vector3 p2 = crossWalkData.go.transform.TransformPoint(crossWalkData.mapLocalPositions[2]);
            Vector3 p3 = crossWalkData.go.transform.TransformPoint(crossWalkData.mapLocalPositions[3]);

            Node n0 = CreateNodeFromPoint(p0);
            Node n1 = CreateNodeFromPoint(p1);
            Node n2 = CreateNodeFromPoint(p2);
            Node n3 = CreateNodeFromPoint(p3);

            // check the distance between each point
            double d0 = Vector3.Distance(p0, p1);
            double d1 = Vector3.Distance(p0, p3);

            Way wayLeft;
            Way wayRight;
            if (d0 >= d1) // create way 0-1, 2-3
            {
                wayLeft = CreateWayFromNodes(new List<Node>() { n0, n1 });
                wayRight = CreateWayFromNodes(new List<Node>() { n2, n3 });
            }
            else // create way 0-3, 1-2
            {
                wayLeft = CreateWayFromNodes(new List<Node>() { n0, n3 });
                wayRight = CreateWayFromNodes(new List<Node>() { n1, n2 });
            }

            wayLeft.Tags.Add(
                new Tag("type", "pedestrian_marking")
            );
            wayRight.Tags.Add(
                new Tag("type", "pedestrian_marking")
            );

            // create lanelet
            var tags = new TagsCollection(
                new Tag("subtype", "crosswalk"),
                new Tag("type", "lanelet")
            );

            var members = new[]
            {
                new RelationMember(wayLeft.Id.Value, "left", OsmGeoType.Way),
                new RelationMember(wayRight.Id.Value, "right", OsmGeoType.Way),
            };

            return CreateRelationFromMembers(members, tags);
        }

        public Relation CreateMultiPolygonFromParkingSpace(ParkingSpaceData parkingSpaceData)
        {
            Vector3 p0 = parkingSpaceData.go.transform.TransformPoint(parkingSpaceData.mapLocalPositions[0]);
            Vector3 p1 = parkingSpaceData.go.transform.TransformPoint(parkingSpaceData.mapLocalPositions[1]);
            Vector3 p2 = parkingSpaceData.go.transform.TransformPoint(parkingSpaceData.mapLocalPositions[2]);
            Vector3 p3 = parkingSpaceData.go.transform.TransformPoint(parkingSpaceData.mapLocalPositions[3]);

            Node n0 = CreateNodeFromPoint(p0);
            Node n1 = CreateNodeFromPoint(p1);
            Node n2 = CreateNodeFromPoint(p2);
            Node n3 = CreateNodeFromPoint(p3);

            Way way0 = CreateWayFromNodes(new List<Node>() { n0, n1 });
            Way way1 = CreateWayFromNodes(new List<Node>() { n1, n2 });
            Way way2 = CreateWayFromNodes(new List<Node>() { n2, n3 });
            Way way3 = CreateWayFromNodes(new List<Node>() { n3, n0 });

            AddSolidSubtype(way1);
            AddSolidSubtype(way3);
            AddVirtualType(way0);
            AddVirtualType(way2);

            // create multipolygon
            var tags = new TagsCollection(
                new Tag("location", "urban"),
                new Tag("subtype", "parking"),
                new Tag("type", "multipolygon")
                );

            var members = new[]
            {
                new RelationMember(way0.Id.Value, "outer", OsmGeoType.Way),
                new RelationMember(way1.Id.Value, "outer", OsmGeoType.Way),
                new RelationMember(way2.Id.Value, "outer", OsmGeoType.Way),
                new RelationMember(way3.Id.Value, "outer", OsmGeoType.Way)
            };

            return CreateRelationFromMembers(members, tags);
        }

        void AddSolidSubtype(Way way)
        {
            if (WayExists(way) != 0) return;
            way.Tags.Add(
                new Tag("type", "line_thin")
            );
            way.Tags.Add(
                new Tag("subtype", "solid")
            );
        }

        void AddVirtualType(Way way)
        {
            if (WayExists(way) != 0) return;
            way.Tags.Add(
                new Tag("type", "virtual")
            );
        }

        public bool IsSameLeftRight(RelationMember[] members1, RelationMember[] members2)
        {
            long rightId1 = 0;
            long leftId1 = 0;
            long rightId2 = 0;
            long leftId2 = 0;

            if (members1.Length == 0 || members1.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < members1.Length; i++)
            {
                if (members1[i].Role == "right")
                {
                    rightId1 = members1[i].Id;
                }
                if (members1[i].Role == "left")
                {
                    leftId1 = members1[i].Id;
                }
            }

            for (int i = 0; i < members2.Length; i++)
            {
                if (members2[i].Role == "right")
                {
                    rightId2 = members2[i].Id;
                }
                if (members2[i].Role == "left")
                {
                    leftId2 = members2[i].Id;
                }
            }

            return ((rightId1 == rightId2) && (leftId1 == leftId2));
        }

        public void AddMemberToLanelet(LaneData laneData, RelationMember member)
        {
            // check if a lane has both left and right boundary
            var lane = laneData.mapLane;
            if (lane.leftLineBoundry != null && lane.rightLineBoundry != null)
            {
                TagsCollection way_tags = new TagsCollection();
                TagsCollection lanelet_tags = new TagsCollection(
                    new Tag("location", "urban"),
                    new Tag("subtype", "road"),
                    new Tag("participant:vehicle", "yes"),
                    new Tag("type", "lanelet")
                );

                // create node and way from boundary
                Way leftWay = CreateWayFromLine(LineData.Line2LineData[lane.leftLineBoundry], way_tags);
                Way rightWay = CreateWayFromLine(LineData.Line2LineData[lane.rightLineBoundry], way_tags);

                var members = new[]
                {
                    new RelationMember(leftWay.Id.Value, "left", OsmGeoType.Way),
                    new RelationMember(rightWay.Id.Value, "right", OsmGeoType.Way),
                };

                for (int i = 0; i < map.Count; i++)
                {
                    if (map[i] == null)
                    {
                        continue;
                    }

                    if (map[i].Type == OsmGeoType.Relation)
                    {
                        Relation relation = map[i] as Relation;

                        if (IsSameLeftRight(members, relation.Members))
                        {
                            RelationMember[] tmp = new RelationMember[relation.Members.Length + 1];
                            Array.Copy(relation.Members, tmp, relation.Members.Length);
                            tmp[relation.Members.Length] = member;
                            relation.Members = tmp;

                            map[i] = relation;
                        }
                    }
                }
            }
        }

        static List<Vector3> ComputeBoundary(List<Vector3> leftLanePoints, List<Vector3> rightLanePoints)
        {
            // Check the directions of two boundry lines
            //    if they are not same, reverse one and get a temp centerline. Compare centerline with left line, determine direction of the centerlane
            //    if they are same, compute centerline.
            var sameDirection = true;
            var leftFirstPoint = leftLanePoints[0];
            var leftLastPoint = leftLanePoints[leftLanePoints.Count - 1];
            var rightFirstPoint = rightLanePoints[0];
            var rightLastPoint = rightLanePoints[rightLanePoints.Count - 1];
            var leftDirection = (leftLastPoint - leftFirstPoint).normalized;
            var rightDirection = (rightLastPoint - rightFirstPoint).normalized;

            if (Vector3.Dot(leftDirection, rightDirection) < 0)
            {
                sameDirection = false;
            }

            float resolution = 5; // 5 meters
            List<Vector3> centerLinePoints = new List<Vector3>();

            // Get the length of longer boundary line
            float leftLength = RangedLength(leftLanePoints);
            float rightLength = RangedLength(rightLanePoints);
            float longerDistance = (leftLength > rightLength) ? leftLength : rightLength;
            int partitions = (int)Math.Ceiling(longerDistance / resolution);
            if (partitions < 2)
            {
                // For lineStrings whose length is less than resolution
                partitions = 2; // Make sure every line has at least 2 partitions.
            }

            float leftResolution = leftLength / partitions;
            float rightResolution = rightLength / partitions;

            leftLanePoints = SplitLine(leftLanePoints, leftResolution, partitions);
            // If left and right lines have opposite direction, reverse right line
            if (!sameDirection)
            {
                rightLanePoints = SplitLine(rightLanePoints, rightResolution, partitions, true);
            }
            else
            {
                rightLanePoints = SplitLine(rightLanePoints, rightResolution, partitions);
            }

            if (leftLanePoints.Count != partitions + 1 || rightLanePoints.Count != partitions + 1)
            {
                Debug.LogError("Something wrong with number of points. (left, right, partitions): (" + leftLanePoints.Count + ", " + rightLanePoints.Count + ", " + partitions);
                return new List<Vector3>();
            }

            for (int i = 0; i < partitions + 1; i++)
            {
                Vector3 centerPoint = (leftLanePoints[i] + rightLanePoints[i]) / 2;
                centerLinePoints.Add(centerPoint);
            }

            // Compare temp centerLine with left line, determine direction
            var centerDirection = (centerLinePoints[centerLinePoints.Count - 1] - centerLinePoints[0]).normalized;
            var centerToLeftDir = (leftFirstPoint - centerLinePoints[0]).normalized;
            if (Vector3.Cross(centerDirection, centerToLeftDir).y > 0)
            {
                // Left line is on right of centerLine, we need to reverse the center points
                centerLinePoints.Reverse();
            }

            return centerLinePoints;
        }


        static List<Vector3> ComputeBoundary(List<Vector3> lanePoints, string side, double width, double pitch)
        {
            // Check the directions of two boundry lines
            //    if they are not same, reverse one and get a temp centerline. Compare centerline with left line, determine direction of the centerlane
            //    if they are same, compute centerline.
            float resolution = (float)pitch;
            List<Vector3> boundaryLinePoints = new List<Vector3>();

            // Get the length of longer boundary line
            float length = RangedLength(lanePoints);

            if (length < resolution)
            {
                resolution = length / 2;
            }


            int partitions = (int)Math.Ceiling(length / resolution);
            if (partitions < 2)
            {
                // For lineStrings whose length is less than resolution
                partitions = 2; // Make sure every line has at least 2 partitions.
            }

            lanePoints = SplitLine(lanePoints, resolution, partitions);

            for (int i = 0; i <= partitions; i++)
            {
                Vector3 orientation = new Vector3();
                if (i != partitions)
                {
                    orientation = (lanePoints[i + 1] - lanePoints[i]).normalized;
                }
                else
                {
                    orientation = (lanePoints[i] - lanePoints[i - 1]).normalized;
                }
                Vector3 boundaryPoint = new Vector3();
                if (side == "left")
                {
                    boundaryPoint = lanePoints[i] + Quaternion.Euler(0, -90, 0) * (orientation * (float)width);

                }
                else if (side == "right")
                {
                    boundaryPoint = lanePoints[i] + Quaternion.Euler(0, 90, 0) * (orientation * (float)width);
                }
                else
                {
                    boundaryPoint = new Vector3(0, 0, 0);
                }
                boundaryLinePoints.Add(boundaryPoint);
            }

            return boundaryLinePoints;
        }

        public static float RangedLength(List<Vector3> points)
        {
            float len = 0;

            for (int i = 0; i < points.Count - 1; i++)
            {
                len += Vector3.Distance(points[i], points[i + 1]);
            }

            return len;
        }

        // Connect all lines by adjusting starting/ending points
        public static void AlignPointsInLines(HashSet<LaneData> allLanesData)
        {
            var visitedLaneIdsEnd = new HashSet<int>(); // lanes whose end point has been visited
            var visitedLaneIdsStart = new HashSet<int>(); // lanes whose start point has been visited
            foreach (var mapLaneData in allLanesData)
            {
                // Handle lane start connecting point
                if (!visitedLaneIdsStart.Contains(mapLaneData.go.GetInstanceID()))
                    AlignLines(visitedLaneIdsEnd, visitedLaneIdsStart, mapLaneData, true);

                // Handle lane end connecting point
                if (!visitedLaneIdsEnd.Contains(mapLaneData.go.GetInstanceID()))
                    AlignLines(visitedLaneIdsEnd, visitedLaneIdsStart, mapLaneData, false);
            }
        }

        static void AlignLines(HashSet<int> visitedLaneIdsEnd, HashSet<int> visitedLaneIdsStart,
            LaneData mapLaneData, bool isStart)
        {
            var allConnectedLanesData2InOut = new Dictionary<LaneData, InOut>();
            if (isStart)
            {
                allConnectedLanesData2InOut[mapLaneData] = InOut.Out;
                AddInOutToDictionary(mapLaneData.befores, allConnectedLanesData2InOut, InOut.In);
                foreach (var beforeLane in mapLaneData.befores) AddInOutToDictionary(beforeLane.afters, allConnectedLanesData2InOut, InOut.Out);
            }
            else
            {
                allConnectedLanesData2InOut[mapLaneData] = InOut.In;
                AddInOutToDictionary(mapLaneData.afters, allConnectedLanesData2InOut, InOut.Out);
                foreach (var afterLane in mapLaneData.afters) AddInOutToDictionary(afterLane.befores, allConnectedLanesData2InOut, InOut.In);
            }

            if (allConnectedLanesData2InOut.Count > 1)
            {
                var boundaryLineData2InOut = GetBoundaryLine2InOut(allConnectedLanesData2InOut);
                List<Vector3> leftMergingPoints, rightMergingPoints;
                UpdateMergingPoints(allConnectedLanesData2InOut, boundaryLineData2InOut, out leftMergingPoints, out rightMergingPoints);

                var leftEndPoint = Lanelet2MapImporter.GetAverage(leftMergingPoints);
                var rightEndPoint = Lanelet2MapImporter.GetAverage(rightMergingPoints);
                SetEndPoints(allConnectedLanesData2InOut, boundaryLineData2InOut, leftEndPoint, rightEndPoint);
            }

            UpdateVisitedSets(allConnectedLanesData2InOut, visitedLaneIdsStart, visitedLaneIdsEnd);
        }

        private static void SetEndPoints(Dictionary<LaneData, InOut> allConnectedLanesData2InOut,
            Dictionary<LineData, InOut> boundaryLineData2InOut, Vector3 leftEndPoint, Vector3 rightEndPoint)
        {
            foreach (var entry in allConnectedLanesData2InOut)
            {
                var laneData = entry.Key;
                var leftLine = laneData.mapLane.leftLineBoundry;
                var leftLineData = LineData.Line2LineData[leftLine];
                var rightLine = laneData.mapLane.rightLineBoundry;
                var rightLineData = LineData.Line2LineData[rightLine];
                SetEndPoint(leftLineData, leftEndPoint, boundaryLineData2InOut[leftLineData]);
                SetEndPoint(rightLineData, rightEndPoint, boundaryLineData2InOut[rightLineData]);
            }
        }

        static void UpdateMergingPoints(Dictionary<LaneData, InOut> allConnectedLanesData2InOut,
            Dictionary<LineData, InOut> boundaryLineData2InOut,
            out List<Vector3> leftMergingPoints, out List<Vector3> rightMergingPoints)
        {
            leftMergingPoints = new List<Vector3>();
            rightMergingPoints = new List<Vector3>();
            foreach (var entry in allConnectedLanesData2InOut)
            {
                var laneData = entry.Key;
                var leftLine = laneData.mapLane.leftLineBoundry;
                var leftLineData = LineData.Line2LineData[leftLine];
                var rightLine = laneData.mapLane.rightLineBoundry;
                var rightLineData = LineData.Line2LineData[rightLine];

                var leftPoint = GetEndPoint(boundaryLineData2InOut[leftLineData], leftLineData.mapWorldPositions);
                var rightPoint = GetEndPoint(boundaryLineData2InOut[rightLineData], rightLineData.mapWorldPositions);
                leftMergingPoints.Add(leftPoint);
                rightMergingPoints.Add(rightPoint);
            }
        }

        static void SetEndPoint(LineData lineData, Vector3 endPoint, InOut inOut)
        {
            RemoveOverlappingEndPoints(lineData, inOut);
            var index = GetEndPointIndex(inOut, lineData.mapWorldPositions);
            lineData.mapWorldPositions[index] = endPoint;
            lineData.mapLocalPositions[index] = lineData.go.transform.InverseTransformPoint(endPoint);
        }

        static void RemoveOverlappingEndPoints(LineData lineData, InOut inOut)
        {
            var positions = lineData.mapWorldPositions;

            // We have less than 2 points, nothing to remove.
            if(positions.Count < 3)
                return;
            Vector3 p1 = positions.First(), p2 = positions[1];
            var changed = false;
            var distThreshold = 0.5;
            if ((p2 - p1).magnitude < distThreshold)
            {
                CheckLinePositionsSize(lineData);
                if (positions.Count > 2 && !isSameDirection3Points(p1, p2, positions[2]))
                {
                    positions.RemoveAt(1);
                    changed = true;
                }
            }
            Vector3 p3 = positions[positions.Count - 2], p4 = positions.Last();
            if ((p3 - p4).magnitude < distThreshold)
            {
                CheckLinePositionsSize(lineData);
                if (positions.Count > 2 && !isSameDirection3Points(positions[positions.Count - 3], p3, p4))
                {
                    positions.RemoveAt(positions.Count - 2);
                    changed = true;
                }
            }

            if (changed)
            {
                UpdateLocalPositions(lineData);
            }
        }

        public static void UpdateLocalPositions(PositionsData positionsData)
        {
            var localPositions = positionsData.mapLocalPositions;
            var worldPositions = positionsData.mapWorldPositions;
            localPositions.Clear();
            for (int i = 0; i < worldPositions.Count; i++)
            {
                localPositions.Add(positionsData.go.transform.InverseTransformPoint(worldPositions[i]));
            }
        }

        static bool isSameDirection3Points(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            var vec12 = p2 - p1;
            var vec23 = p3 - p2;
            return Vector3.Dot(vec12, vec23) >= 0;
        }

        static void CheckLinePositionsSize(LineData lineData)
        {
            if (lineData.mapWorldPositions.Count == 2)
            {
                var msg = $"Line: {lineData.go.name} instanceID: {lineData.go.GetInstanceID()} ";
                msg += "only has two points and overlapping with each other, ";
                msg += "please check and manually adjust it.";
                Debug.LogWarning(msg, lineData.go);
            }
        }

        static Vector3 GetEndPoint(InOut inOut, List<Vector3> positions)
        {
            return inOut == InOut.In ? positions.Last() : positions.First();
        }

        static int GetEndPointIndex(InOut inOut, List<Vector3> positions)
        {
            return inOut == InOut.In ? positions.Count - 1 : 0;
        }

        static void UpdateVisitedSets(Dictionary<LaneData, InOut> allConnectedLanesData2InOut,
            HashSet<int> visitedLaneInstanceIdsStart, HashSet<int> visitedLaneInstanceIdsEnd)
        {
            foreach (var entry in allConnectedLanesData2InOut)
            {
                var laneData = entry.Key;
                var inOut = entry.Value;
                if (inOut == InOut.In) visitedLaneInstanceIdsEnd.Add(laneData.go.GetInstanceID());
                else visitedLaneInstanceIdsStart.Add(laneData.go.GetInstanceID());
            }
        }

        static Dictionary<LineData, InOut> GetBoundaryLine2InOut(Dictionary<LaneData, InOut> laneData2InOut)
        {
            var boundaryLineData2InOut = new Dictionary<LineData, InOut>();
            foreach (var entry in laneData2InOut)
            {
                var laneData = entry.Key;
                var laneInOut = entry.Value;
                var leftBoundaryLine = laneData.mapLane.leftLineBoundry;
                var leftBoundaryLineData = LineData.Line2LineData[leftBoundaryLine];
                var rightBoundaryLine = laneData.mapLane.rightLineBoundry;
                var rightBoundaryLineData = LineData.Line2LineData[rightBoundaryLine];

                AddToLine2InOut(boundaryLineData2InOut, leftBoundaryLineData, laneData, laneInOut);
                AddToLine2InOut(boundaryLineData2InOut, rightBoundaryLineData, laneData, laneInOut);
            }

            return boundaryLineData2InOut;
        }

        static void AddToLine2InOut(Dictionary<LineData, InOut> boundaryLine2InOut,
            LineData mapLineData, LaneData mapLaneData, InOut laneInOut)
        {
            if (isSameDirection(mapLaneData, mapLineData)) boundaryLine2InOut[mapLineData] = laneInOut;
            else boundaryLine2InOut[mapLineData] = ReverseInOut(laneInOut);
        }

        static InOut ReverseInOut(InOut inOut)
        {
            if (inOut == InOut.In) return InOut.Out;
            else return InOut.In;
        }

        enum InOut {In, Out};

        static void AddInOutToDictionary(List<LaneData> lanesData, Dictionary<LaneData, InOut> allConnectedLanesData2InOut, InOut inOut)
        {
            foreach (var laneData in lanesData)
            {
                if (allConnectedLanesData2InOut.ContainsKey(laneData))
                {
                    if (allConnectedLanesData2InOut[laneData] != inOut)
                    {
                        var message = $"Lane {laneData.go.name} {laneData.go.GetInstanceID()} is added ";
                        message += $"already and with {allConnectedLanesData2InOut[laneData]} not {inOut}";
                        Debug.LogError(message, laneData.go);
                    }
                    continue;
                }
                allConnectedLanesData2InOut[laneData] = inOut;
            }
        }

        static bool isSameDirection(LaneData mapLaneData, LineData mapLineData)
        {
            var lanePositions = mapLaneData.mapWorldPositions;
            var linePositions = mapLineData.mapWorldPositions;
            var dir = lanePositions.Last() - lanePositions[0];
            var lineDir = linePositions.Last() - linePositions[0];
            return Vector3.Dot(dir, lineDir) > 0;
        }

        void ClearWorldPositions<T>(List<T> positionsData) where T : PositionsData
        {
            foreach (var positionData in positionsData)
            {
                positionData.mapWorldPositions.Clear();
            }
        }

        void AlignPointsInCrossWalk(List<CrossWalkData> crossWalksData)
        {
            ClearWorldPositions(crossWalksData);
            foreach (var crossWalkData in crossWalksData)
            {
                for (int i = 0; i < crossWalkData.mapLocalPositions.Count; i++)
                {
                    var pt = crossWalkData.go.transform.TransformPoint(crossWalkData.mapLocalPositions[i]);

                    foreach (var crossWalkDataCmp in crossWalksData)
                    {
                        if (crossWalkData == crossWalkDataCmp)
                        {
                            continue;
                        }

                        for (int j = 0; j < crossWalkDataCmp.mapLocalPositions.Count; j++)
                        {
                            var ptCmp = crossWalkDataCmp.go.transform.TransformPoint(crossWalkDataCmp.mapLocalPositions[j]);

                            if ((pt - ptCmp).magnitude < MapAnnotationTool.PROXIMITY / MapAnnotationTool.EXPORT_SCALE_FACTOR)
                            {
                                crossWalkDataCmp.mapLocalPositions[j] = crossWalkDataCmp.go.transform.InverseTransformPoint(pt);
                                crossWalkDataCmp.mapWorldPositions.Add(pt);
                            }
                        }
                    }
                }
            }
        }

        void AlignPointsInParkingSpace(List<ParkingSpaceData> parkingSpacesData)
        {
            ClearWorldPositions(parkingSpacesData);
            foreach (var parkingSpaceData in parkingSpacesData)
            {
                for (int i = 0; i < parkingSpaceData.mapLocalPositions.Count; i++)
                {
                    var pt = parkingSpaceData.go.transform.TransformPoint(parkingSpaceData.mapLocalPositions[i]);

                    foreach (var parkingSpaceDataCmp in parkingSpacesData)
                    {
                        if (parkingSpaceData == parkingSpaceDataCmp)
                        {
                            continue;
                        }

                        for (int j = 0; j < parkingSpaceDataCmp.mapLocalPositions.Count; j++)
                        {
                            var ptCmp = parkingSpaceDataCmp.go.transform.TransformPoint(parkingSpaceDataCmp.mapLocalPositions[j]);

                            if ((pt - ptCmp).magnitude < MapAnnotationTool.PROXIMITY / MapAnnotationTool.EXPORT_SCALE_FACTOR)
                            {
                                parkingSpaceDataCmp.mapLocalPositions[j] = parkingSpaceDataCmp.go.transform.InverseTransformPoint(pt);
                                parkingSpaceDataCmp.mapWorldPositions.Add(pt);
                            }
                        }
                    }
                }
            }
        }

        public static List<Vector3> SplitLine(List<Vector3> line, float resolution, int partitions, bool reverse = false)
        {
            List<Vector3> splittedLinePoints = new List<Vector3>();
            splittedLinePoints.Add(line[0]); // Add first point

            float residue = 0; // Residual length from previous segment

            // loop through each segment in boundry line
            for (int i = 1; i < line.Count; i++)
            {
                if (splittedLinePoints.Count >= partitions) break;

                Vector3 lastPoint = line[i - 1];
                Vector3 curPoint = line[i];

                // Continue if no points are made within current segment
                float segmentLength = Vector3.Distance(lastPoint, curPoint);
                if (segmentLength + residue < resolution)
                {
                    residue += segmentLength;
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
            }

            splittedLinePoints.Add(line[line.Count - 1]);

            if (reverse)
            {
                splittedLinePoints.Reverse();
            }

            return splittedLinePoints;
        }

        public static bool AreAllLanesWithBoundaries(HashSet<MapTrafficLane> lanes, bool showError=false)
        {
            var areAllLanesWithBoundaries = true;
            foreach (var lane in lanes)
            {
                if (lane.leftLineBoundry == null || lane.rightLineBoundry == null)
                {
                    Debug.LogWarning($"Lane {lane.name} has null left and/or right lines.", lane.gameObject);
                    areAllLanesWithBoundaries = false;
                    break;
                }
            }

            if (!areAllLanesWithBoundaries && showError)
            {
                var msg = "There are no boundary lines for some lanes! ";
                msg += "please annotate boundary lines for each lane OR ";
                msg += "use \"Create lines\" button in HD Map Annotation window to generate boundary lines!";
                Debug.LogError(msg);
            }

            return areAllLanesWithBoundaries;
        }

        public static List<MapLine> CreateFakeBoundariesFromLanes(HashSet<MapTrafficLane> laneSegments)
        {
            List<MapLine> fakeBoundaryLineList = new List<MapLine>();
            GameObject fakeBoundariesObj = new GameObject("FakeBoundaries");

            var mapHolder = UnityEngine.Object.FindObjectOfType<MapHolder>();
            fakeBoundariesObj.transform.parent = mapHolder.transform;

            int k = 0;

            foreach (var laneSegment in laneSegments)
            {
                float resolution = 5f;

                // Check if there is left adjacent lane
                if (laneSegment.leftLineBoundry == null)
                {
                    if (HasAdjacentLane(laneSegment, "left"))
                    {
                        if (HasAdjacentLaneBoundaryInBetween(laneSegment, GetAdjacentLane(laneSegment, "left")))
                        {
                            laneSegment.leftLineBoundry = GetAdjacentLaneBoundaryInBetween(laneSegment, GetAdjacentLane(laneSegment, "left"));
                        }
                        else
                        {
                            var fakeBoundaryLineObj = new GameObject("FakeBoundaryLine_" + k);
                            var fakeBoundaryLine = fakeBoundaryLineObj.AddComponent<MapLine>();

                            var boundaryPoints = ComputeBoundary(laneSegment.mapWorldPositions, GetAdjacentLane(laneSegment, "left").mapWorldPositions);
                            fakeBoundaryLine.mapWorldPositions = boundaryPoints;

                            // Set transform of the line as same as the first points
                            fakeBoundaryLineObj.transform.position = boundaryPoints[0];
                            fakeBoundaryLineObj.transform.parent = fakeBoundariesObj.transform;

                            for (int j = 0; j < boundaryPoints.Count; j++)
                            {
                                fakeBoundaryLine.mapLocalPositions.Add(fakeBoundaryLineObj.transform.InverseTransformPoint(boundaryPoints[j]));
                            }

                            laneSegment.leftLineBoundry = fakeBoundaryLine;

                            fakeBoundaryLineList.Add(fakeBoundaryLine);
                            k++;
                        }
                    }
                    else
                    {
                        var fakeBoundaryLineObj = new GameObject("FakeBoundaryLine_" + k);
                        var fakeBoundaryLine = fakeBoundaryLineObj.AddComponent<MapLine>();

                        if (laneSegment.laneTurnType != MapData.LaneTurnType.NO_TURN)
                        {
                            resolution = 2f;
                        }

                        var boundaryPoints = ComputeBoundary(laneSegment.mapWorldPositions, "left", laneSegment.displayLaneWidth / 2f, resolution);
                        fakeBoundaryLine.mapWorldPositions = boundaryPoints;

                        // Set transform of the line as same as the first points
                        fakeBoundaryLineObj.transform.position = boundaryPoints[0];
                        fakeBoundaryLineObj.transform.parent = fakeBoundariesObj.transform;

                        for (int j = 0; j < boundaryPoints.Count; j++)
                        {
                            fakeBoundaryLine.mapLocalPositions.Add(fakeBoundaryLineObj.transform.InverseTransformPoint(boundaryPoints[j]));
                        }

                        laneSegment.leftLineBoundry = fakeBoundaryLine;

                        fakeBoundaryLineList.Add(fakeBoundaryLine);
                        k++;
                    }
                }

                // Check if there is right adjacent lane
                if (laneSegment.rightLineBoundry == null)
                {
                    if (HasAdjacentLane(laneSegment, "right"))
                    {
                        if (HasAdjacentLaneBoundaryInBetween(laneSegment, GetAdjacentLane(laneSegment, "right")))
                        {
                            laneSegment.rightLineBoundry = GetAdjacentLaneBoundaryInBetween(laneSegment, GetAdjacentLane(laneSegment, "right"));
                        }
                        else
                        {
                            var fakeBoundaryLineObj = new GameObject("FakeBoundaryLine_" + k);
                            var fakeBoundaryLine = fakeBoundaryLineObj.AddComponent<MapLine>();

                            var boundaryPoints = ComputeBoundary(laneSegment.mapWorldPositions, GetAdjacentLane(laneSegment, "right").mapWorldPositions);
                            fakeBoundaryLine.mapWorldPositions = boundaryPoints;

                            // Set transform of the line as same as the first points
                            fakeBoundaryLineObj.transform.position = boundaryPoints[0];
                            fakeBoundaryLineObj.transform.parent = fakeBoundariesObj.transform;

                            for (int j = 0; j < boundaryPoints.Count; j++)
                            {
                                fakeBoundaryLine.mapLocalPositions.Add(fakeBoundaryLineObj.transform.InverseTransformPoint(boundaryPoints[j]));
                            }

                            laneSegment.rightLineBoundry = fakeBoundaryLine;

                            fakeBoundaryLineList.Add(fakeBoundaryLine);
                            k++;
                        }
                    }
                    else
                    {
                        var fakeBoundaryLineObj = new GameObject("FakeBoundaryLine_" + k);
                        var fakeBoundaryLine = fakeBoundaryLineObj.AddComponent<MapLine>();

                        if (laneSegment.laneTurnType != MapData.LaneTurnType.NO_TURN)
                        {
                            resolution = 2f;
                        }

                        var boundaryPoints = ComputeBoundary(laneSegment.mapWorldPositions, "right", laneSegment.displayLaneWidth / 2f, resolution);
                        fakeBoundaryLine.mapWorldPositions = boundaryPoints;

                        // Set transform of the line as same as the first points
                        fakeBoundaryLineObj.transform.position = boundaryPoints[0];
                        fakeBoundaryLineObj.transform.parent = fakeBoundariesObj.transform;

                        for (int j = 0; j < boundaryPoints.Count; j++)
                        {
                            fakeBoundaryLine.mapLocalPositions.Add(fakeBoundaryLineObj.transform.InverseTransformPoint(boundaryPoints[j]));
                        }

                        laneSegment.rightLineBoundry = fakeBoundaryLine;

                        fakeBoundaryLineList.Add(fakeBoundaryLine);
                        k++;
                    }
                }
            }

            if (fakeBoundaryLineList.Count == 0) GameObject.DestroyImmediate(fakeBoundariesObj);
            return fakeBoundaryLineList;
        }

        static bool HasAdjacentLane(MapTrafficLane lane, string side)
        {
            if (side == "left")
            {
                return (lane.leftLaneForward != null) || (lane.leftLaneReverse != null);
            }
            if (side == "right")
            {
                return (lane.rightLaneForward != null) || (lane.rightLaneReverse != null);
            }

            return false;
        }

        static MapTrafficLane GetAdjacentLane(MapTrafficLane lane, string side)
        {
            if (side == "left")
            {
                if (lane.leftLaneForward != null)
                {
                    return lane.leftLaneForward;
                }
                else if(lane.leftLaneReverse != null)
                {
                    return lane.leftLaneReverse;
                }
            }
            if (side == "right")
            {
                if (lane.rightLaneForward != null)
                {
                    return lane.rightLaneForward;
                }
                else if (lane.rightLaneReverse != null)
                {
                    return lane.rightLaneReverse;
                }
            }

            return null;
        }

        static bool HasAdjacentLaneBoundaryInBetween(MapTrafficLane lane1, MapTrafficLane lane2)
        {
            if(lane1.leftLaneForward == lane2)
            {
                return lane2.rightLineBoundry != null;
            }
            else if (lane1.leftLaneReverse == lane2)
            {
                return lane2.leftLineBoundry != null;
            }
            else if (lane1.rightLaneForward == lane2)
            {
                return lane2.leftLineBoundry != null;
            }
            else if (lane1.rightLaneReverse == lane2)
            {
                return lane2.rightLineBoundry != null;
            }

            return false;
        }

        static MapLine GetAdjacentLaneBoundaryInBetween(MapTrafficLane lane1, MapTrafficLane lane2)
        {
            if (lane1.leftLaneForward == lane2)
            {
                return lane2.rightLineBoundry;
            }
            else if (lane1.leftLaneReverse == lane2)
            {
                return lane2.leftLineBoundry;
            }
            else if (lane1.rightLaneForward == lane2)
            {
                return lane2.leftLineBoundry;
            }
            else if (lane1.rightLaneReverse == lane2)
            {
                return lane2.rightLineBoundry;
            }

            return null;
        }

        public bool Export(string filePath)
        {
            try
            {
                if (Calculate())
                {
                    using (var file = File.Create(filePath))
                    using (var target = new XmlOsmStreamTarget(file))
                    {
                        target.Generator = "LGSVL Simulator";
                        target.Initialize();

                        foreach (OsmGeo element in map)
                        {
                            if (element == null)
                            {
                                continue;
                            }

                            if (element.Type == OsmGeoType.Node)
                            {
                                Node node = element as Node;
                                target.AddNode(node);
                            }
                            else if (element.Type == OsmGeoType.Way)
                            {
                                Way way = element as Way;
                                target.AddWay(way);
                            }
                            else if (element.Type == OsmGeoType.Relation)
                            {
                                Relation relation = element as Relation;
                                target.AddRelation(relation);
                            }
                        }
                        target.Close();
                    }
                    Debug.Log("Successfully generated and exported Lanelet2 HD Map!");
                    return true;
                }
                Debug.LogError("Failed to export Lanelet2 HD Map!");
            }
            catch (Exception exc)
            {
                Debug.LogError($"Lanelet2 HD Map export unexpected error: {exc.Message}");
            }
            return false;
        }

        public void AddBoundaryTagToWay(LaneData laneData, Way leftWay, Way rightWay)
        {
            // set boundary type
            if (leftWay.Tags.ContainsKey("type")) {}
            else if (laneData.mapLane.leftLineBoundry.lineType == MapData.LineType.DOTTED_WHITE)
            {
                leftWay.Tags.Add(
                    new Tag("type", "line_thin")
                );
                leftWay.Tags.Add(
                    new Tag("subtype", "dashed")
                );
                leftWay.Tags.Add(
                    new Tag("color", "white")
                );
            }
            else if (laneData.mapLane.leftLineBoundry.lineType == MapData.LineType.DOTTED_YELLOW)
            {
                leftWay.Tags.Add(
                    new Tag("type", "line_thin")
                );
                leftWay.Tags.Add(
                    new Tag("subtype", "dashed")
                );
                leftWay.Tags.Add(
                    new Tag("color", "yellow")
                );
            }
            else if (laneData.mapLane.leftLineBoundry.lineType == MapData.LineType.SOLID_WHITE)
            {
                leftWay.Tags.Add(
                    new Tag("type", "line_thin")
                );
                leftWay.Tags.Add(
                    new Tag("subtype", "solid")
                );
                leftWay.Tags.Add(
                    new Tag("color", "white")
                );
            }
            else if (laneData.mapLane.leftLineBoundry.lineType == MapData.LineType.SOLID_YELLOW)
            {
                leftWay.Tags.Add(
                    new Tag("type", "line_thin")
                );
                leftWay.Tags.Add(
                    new Tag("subtype", "solid")
                );
                leftWay.Tags.Add(
                    new Tag("color", "yellow")
                );
            }
            else if (laneData.mapLane.leftLineBoundry.lineType == MapData.LineType.DOUBLE_YELLOW)
            {
                leftWay.Tags.Add(
                    new Tag("type", "line_thin")
                );
                leftWay.Tags.Add(
                    new Tag("subtype", "solid_solid")
                );
                leftWay.Tags.Add(
                    new Tag("color", "yellow")
                );
            }
            else if (laneData.mapLane.leftLineBoundry.lineType == MapData.LineType.CURB)
            {
                leftWay.Tags.Add(
                    new Tag("type", "curbstone")
                );
                leftWay.Tags.Add(
                    new Tag("subtype", "high")
                );
            }
            else if (laneData.mapLane.leftLineBoundry.lineType == MapData.LineType.VIRTUAL)
            {
                leftWay.Tags.Add(
                    new Tag("type", "virtual")
                );
            }
            else
            {
                Debug.LogWarning($"Lane {laneData.go.name} left boundary type is Unknown.");
                leftWay.Tags.Add(
                    new Tag("type", "unknown")
                );
            }

            if (rightWay.Tags.ContainsKey("type")) {}
            else if (laneData.mapLane.rightLineBoundry.lineType == MapData.LineType.DOTTED_WHITE)
            {
                rightWay.Tags.Add(
                    new Tag("type", "line_thin")
                );
                rightWay.Tags.Add(
                    new Tag("subtype", "dashed")
                );
                rightWay.Tags.Add(
                    new Tag("color", "white")
                );
            }
            else if (laneData.mapLane.rightLineBoundry.lineType == MapData.LineType.DOTTED_YELLOW)
            {
                rightWay.Tags.Add(
                    new Tag("type", "line_thin")
                );
                rightWay.Tags.Add(
                    new Tag("subtype", "dashed")
                );
                rightWay.Tags.Add(
                    new Tag("color", "yellow")
                );
            }
            else if (laneData.mapLane.rightLineBoundry.lineType == MapData.LineType.SOLID_WHITE)
            {
                rightWay.Tags.Add(
                    new Tag("type", "line_thin")
                );
                rightWay.Tags.Add(
                    new Tag("subtype", "solid")
                );
                rightWay.Tags.Add(
                    new Tag("color", "white")
                );
            }
            else if (laneData.mapLane.rightLineBoundry.lineType == MapData.LineType.SOLID_YELLOW)
            {
                rightWay.Tags.Add(
                    new Tag("type", "line_thin")
                );
                rightWay.Tags.Add(
                    new Tag("subtype", "solid")
                );
                rightWay.Tags.Add(
                    new Tag("color", "yellow")
                );
            }
            else if (laneData.mapLane.rightLineBoundry.lineType == MapData.LineType.DOUBLE_YELLOW)
            {
                rightWay.Tags.Add(
                    new Tag("type", "line_thin")
                );
                rightWay.Tags.Add(
                    new Tag("subtype", "solid_solid")
                );
                rightWay.Tags.Add(
                    new Tag("color", "yellow")
                );
            }
            else if (laneData.mapLane.rightLineBoundry.lineType == MapData.LineType.CURB)
            {
                rightWay.Tags.Add(
                    new Tag("type", "curbstone")
                );
                rightWay.Tags.Add(
                    new Tag("subtype", "high")
                );
            }
            else if (laneData.mapLane.rightLineBoundry.lineType == MapData.LineType.VIRTUAL)
            {
                rightWay.Tags.Add(
                    new Tag("type", "virtual")
                );
            }
            else
            {
                Debug.LogWarning($"Lane {laneData.go.name} left boundary type is Unknown.");
                rightWay.Tags.Add(
                    new Tag("type", "unknown")
                );
            }
        }


    }
}