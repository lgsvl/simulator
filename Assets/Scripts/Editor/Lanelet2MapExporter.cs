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
using Simulator.Map;
using OsmSharp;
using OsmSharp.Streams;
using OsmSharp.Tags;
using Utility = Simulator.Utilities.Utility;

namespace Simulator.Editor
{
    public class Lanelet2MapExporter
    {
        private MapManagerData MapAnnotationData;
        MapOrigin MapOrigin;
        List<OsmGeo> map = new List<OsmGeo>();
        Dictionary<int, long> LineId2StartNodeId = new Dictionary<int, long>();
        Dictionary<int, long> LineId2EndNodeId = new Dictionary<int, long>();
        Dictionary<long, Node> Id2Node = new Dictionary<long, Node>();

        public Lanelet2MapExporter()
        {
        }

        bool Calculate()
        {
            MapAnnotationData = new MapManagerData();
            MapAnnotationData.GetIntersections();
            MapAnnotationData.GetTrafficLanes();

            MapOrigin = MapOrigin.Find();

            // Initial collection
            var laneSegments = new HashSet<MapLane>(MapAnnotationData.GetData<MapLane>());
            var lineSegments = new HashSet<MapLine>(MapAnnotationData.GetData<MapLine>());
            var signalLights = new List<MapSignal>(MapAnnotationData.GetData<MapSignal>());
            var crossWalkList = new List<MapCrossWalk>(MapAnnotationData.GetData<MapCrossWalk>());
            var mapSignList = new List<MapSign>(MapAnnotationData.GetData<MapSign>());
            var parkingSpaceList = new List<MapParkingSpace>(MapAnnotationData.GetData<MapParkingSpace>());

            foreach (var mapSign in mapSignList)
            {
                if (mapSign.signType == MapData.SignType.STOP && mapSign.stopLine != null)
                {
                    mapSign.stopLine.stopSign = mapSign;
                }
            }

            var stopLineLanes = new Dictionary<MapLine, List<MapLane>>();

            foreach (var laneSegment in laneSegments)
            {
                if (laneSegment.stopLine != null)
                {
                    stopLineLanes.GetOrCreate(laneSegment.stopLine).Add(laneSegment);
                }
            }

            // Link before and after segment for each lane segment
            if (!AlignPointsInLanes(laneSegments))
            {
                return false;
            }

            // Link points in each crosswalk
            AlignPointsInCrossWalk(crossWalkList);

            // Link Points in each parking area
            AlignPointsInParkingSpace(parkingSpaceList);

            // process lanes - create lanelet from lane and left/right boundary
            if (ExistsLaneWithBoundaries(laneSegments))
            {
                // Link before and after segment for each line segment based on lane's predecessor/successor
                AlignPointsInLines(laneSegments);
                CreateLaneletsFromLanes(laneSegments);
            }
            else // If there are no lanes with left/right boundaries
            {
                Debug.LogWarning("There are no boundaries. Creating fake boundaries.");

                // Create fake boundary lines
                var fakeBoundaryLineList = CreateFakeBoundariesFromLanes(laneSegments);

                var fakeBoundaryLineSegments = new HashSet<MapLine>(fakeBoundaryLineList);

                AlignPointsInLines(laneSegments);
                CreateLaneletsFromLanes(laneSegments);
            }

            // process stop lines - create stop lines
            foreach (var lineSegment in lineSegments)
            {
                if (lineSegment.lineType == MapData.LineType.STOP)
                {
                    Way wayStopLine = CreateWayStopLineFromLine(lineSegment);

                    List<Way> wayTrafficLightList = new List<Way>();
                    List<Way> wayLightBulbsList = new List<Way>();

                    if (lineSegment.signals.Count > 0)
                    {
                        // create way for traffic light and light bulbs
                        foreach (var signal in lineSegment.signals)
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
                        foreach (var lane in stopLineLanes[lineSegment])
                        {
                            RelationMember member = new RelationMember(relationRegulatoryElement.Id.Value, "regulatory_element", OsmGeoType.Relation);
                            AddMemberToLanelet(lane, member);
                        }
                    }

                    if (lineSegment.isStopSign)
                    {
                        if (!stopLineLanes.ContainsKey(lineSegment))
                        {
                            Debug.LogError($"Stop line for {lineSegment.stopSign.gameObject.name} sign is not associated with any lane (lane probably does not intersect stop line in last segment)", lineSegment.stopSign.gameObject);
                            throw new Exception("Export failed");
                        }

                        // create way for stop sign
                        Way wayStopSign = CreateWayFromStopSign(lineSegment.stopSign);
                        Relation relationRegulatoryElement = CreateRegulatoryElementFromStopLineStopSign(wayStopLine, wayStopSign);
                        map.Add(relationRegulatoryElement);

                        // asscoate with lanelet
                        foreach (var lane in stopLineLanes[lineSegment])
                        {
                            RelationMember member = new RelationMember(relationRegulatoryElement.Id.Value, "regulatory_element", OsmGeoType.Relation);
                            AddMemberToLanelet(lane, member);
                        }
                    }
                }
            }

            // process crosswalk
            foreach (var crossWalk in crossWalkList)
            {
                map.Add(CreateLaneletFromCrossWalk(crossWalk));
            }

            // process parking space
            foreach (var parkingSpace in parkingSpaceList)
            {
                map.Add(CreateMultiPolygonFromParkingSpace(parkingSpace));
            }

            return true;
        }

        private void CreateLaneletsFromLanes(HashSet<MapLane> laneSegments)
        {
            foreach (var laneSegment in laneSegments)
            {
                Relation lanelet = CreateLaneletFromLane(laneSegment);
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

        public Way CreateWayFromLine(MapLine line, TagsCollection tags, bool isBasedOnLane = false)
        {
            var startIdx = 0;
            var endIdx = line.mapWorldPositions.Count;
            List<Node> nodes = new List<Node>();
            Node firstNode, lastNode;
            var lineId = line.GetInstanceID();

            if (isBasedOnLane)
            {
                startIdx = 1;
                endIdx = line.mapWorldPositions.Count - 1;
                if (LineId2StartNodeId.ContainsKey(lineId))
                {
                    var nodeId = LineId2StartNodeId[lineId];
                    firstNode = Id2Node[nodeId];
                }
                else firstNode = CreateNodeByIndex(line, 0);
                nodes.Add(firstNode);
            }

            // create nodes
            for (int p = startIdx; p < endIdx; p++)
            {
                Node node = CreateNodeByIndex(line, p);
                nodes.Add(node);
            }

            if (isBasedOnLane)
            {
                if (LineId2EndNodeId.ContainsKey(lineId))
                {
                    var nodeId = LineId2EndNodeId[lineId];
                    lastNode = Id2Node[nodeId];
                }
                else lastNode = CreateNodeByIndex(line, line.mapWorldPositions.Count - 1);
                nodes.Add(lastNode);
            } 

            return CreateWayFromNodes(nodes, tags);
        }

        private Node CreateNodeByIndex(MapLine line, int p)
        {
            Vector3 pos = line.mapWorldPositions[p];
            var location = MapOrigin.GetGpsLocation(pos);
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
            var location = MapOrigin.GetGpsLocation(point);
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

        public Relation CreateLaneletFromLane(MapLane lane)
        {
            var leftLine = lane.leftLineBoundry;
            var rightLine = lane.rightLineBoundry;
            var isSameDirectionLeft = isSameDirection(lane, leftLine);
            var isSameDirectionRight = isSameDirection(lane, rightLine);

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
                Way leftWay = CreateWayFromLine(leftLine, left_way_tags, true);
                Way rightWay = CreateWayFromLine(rightLine, right_way_tags, true);
                
                UpdateLineStartEnd2NodeId(leftLine, leftWay);
                UpdateLineStartEnd2NodeId(rightLine, rightWay);

                var leftWayStartNodeId = leftWay.Nodes.First();
                var leftWayEndNodeId = leftWay.Nodes.Last();

                var rightWayStartNodeId = rightWay.Nodes.First();
                var rightWayEndNodeId = rightWay.Nodes.Last();

                var beforeLanes = lane.befores;
                var leftStartNodeBasedOnLane = isSameDirectionLeft ? leftWayStartNodeId : leftWayEndNodeId;
                var rightStartNodeBasedOnLane = isSameDirectionRight ? rightWayStartNodeId : rightWayEndNodeId;

                foreach (var beforeLane in beforeLanes)
                {
                    var beforeLaneLeftLine = beforeLane.leftLineBoundry;
                    UpdateBeforeLane(leftStartNodeBasedOnLane, beforeLane, beforeLaneLeftLine);

                    var beforeLaneRightLine = beforeLane.rightLineBoundry;
                    UpdateBeforeLane(rightStartNodeBasedOnLane, beforeLane, beforeLaneRightLine);
                }

                var afterLanes = lane.afters;
                var leftEndNodeBasedOnLane = isSameDirectionLeft ? leftWayEndNodeId : leftWayStartNodeId;
                var rightEndNodeBasedOnLane = isSameDirectionRight ? rightWayEndNodeId : rightWayStartNodeId;
                foreach (var afterLane in afterLanes)
                {
                    var afterLaneLeftLine = afterLane.leftLineBoundry;
                    UpdateAfterLane(leftEndNodeBasedOnLane, afterLane, afterLaneLeftLine);

                    var afterLaneRightLine = afterLane.rightLineBoundry;
                    UpdateAfterLane(rightEndNodeBasedOnLane, afterLane, afterLaneRightLine);
                }


                AddBoundaryTagToWay(lane, leftWay, rightWay);
                // create lanelet from left/right way
                if (lane.laneTurnType == MapLane.LaneTurnType.NO_TURN)
                {
                    lanelet_tags.Add(
                        new Tag("turn_direction", "straight")
                    );
                }
                if (lane.laneTurnType == MapLane.LaneTurnType.RIGHT_TURN)
                {
                    lanelet_tags.Add(
                        new Tag("turn_direction", "right")
                    );
                }
                if (lane.laneTurnType == MapLane.LaneTurnType.LEFT_TURN)
                {
                    lanelet_tags.Add(
                        new Tag("turn_direction", "left")
                    );
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

        private void UpdateBeforeLane(long startNodeBasedOnLane, MapLane beforeLane, MapLine beforeLaneLine)
        {
            if (isSameDirection(beforeLane, beforeLaneLine))
            {
                LineId2EndNodeId[beforeLaneLine.GetInstanceID()] = startNodeBasedOnLane;
            }
            else
            {
                LineId2StartNodeId[beforeLaneLine.GetInstanceID()] = startNodeBasedOnLane;
            }
        }

        private void UpdateAfterLane(long EndNodeBasedOnLane, MapLane afterLane, MapLine afterLaneLine)
        {
            if (isSameDirection(afterLane, afterLaneLine))
            {
                LineId2StartNodeId[afterLaneLine.GetInstanceID()] = EndNodeBasedOnLane;
            }
            else
            {
                LineId2EndNodeId[afterLaneLine.GetInstanceID()] = EndNodeBasedOnLane;
            }
        }
        void UpdateLineStartEnd2NodeId(MapLine line, Way way)
        {
            var lineId = line.GetInstanceID();
            LineId2StartNodeId[lineId] = way.Nodes.First();
            LineId2EndNodeId[lineId] = way.Nodes.Last();
        }

        public Way CreateWayStopLineFromLine(MapLine line)
        {
            if (line.lineType == MapData.LineType.STOP)
            {
                var tagStopLine = new TagsCollection(
                    new Tag("type", "stop_line")
                );

                // create way for stop line
                Way wayStopLine = CreateWayFromLine(line, tagStopLine);

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

        public Relation CreateLaneletFromCrossWalk(MapCrossWalk crossWalk)
        {
            Vector3 p0 = crossWalk.transform.TransformPoint(crossWalk.mapLocalPositions[0]);
            Vector3 p1 = crossWalk.transform.TransformPoint(crossWalk.mapLocalPositions[1]);
            Vector3 p2 = crossWalk.transform.TransformPoint(crossWalk.mapLocalPositions[2]);
            Vector3 p3 = crossWalk.transform.TransformPoint(crossWalk.mapLocalPositions[3]);

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

        public Relation CreateMultiPolygonFromParkingSpace(MapParkingSpace parkingSpace)
        {
            Vector3 p0 = parkingSpace.transform.TransformPoint(parkingSpace.mapLocalPositions[0]);
            Vector3 p1 = parkingSpace.transform.TransformPoint(parkingSpace.mapLocalPositions[1]);
            Vector3 p2 = parkingSpace.transform.TransformPoint(parkingSpace.mapLocalPositions[2]);
            Vector3 p3 = parkingSpace.transform.TransformPoint(parkingSpace.mapLocalPositions[3]);

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

        public void AddMemberToLanelet(MapLane lane, RelationMember member)
        {
            // check if a lane has both left and right boundary
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
                Way leftWay = CreateWayFromLine(lane.leftLineBoundry, way_tags);
                Way rightWay = CreateWayFromLine(lane.rightLineBoundry, way_tags);

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

        List<Vector3> ComputeBoundary(List<Vector3> leftLanePoints, List<Vector3> rightLanePoints)
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


        List<Vector3> ComputeBoundary(List<Vector3> lanePoints, string side, double width, double pitch)
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

        bool AlignPointsInLanes(HashSet<MapLane> lanes)
        {
            foreach (var lane in lanes)
            {
                lane.befores.Clear(); // should clear since befores/afters are serialized
                lane.afters.Clear();

                // Each segment must have at least 2 waypoints for calculation, otherwise exit
                while (lane.mapLocalPositions.Count < 2)
                {
                    Debug.Log("Some segment has less than 2 waypoints. Cancelling map generation.");
                    UnityEditor.Selection.activeGameObject = lane.gameObject;
                    return false;
                }

                // Link lanes
                var firstPt = lane.transform.TransformPoint(lane.mapLocalPositions[0]);
                var lastPt = lane.transform.TransformPoint(lane.mapLocalPositions[lane.mapLocalPositions.Count - 1]);

                foreach (var laneCmp in lanes)
                {
                    if (lane == laneCmp)
                    {
                        continue;
                    }

                    var firstPt_cmp = laneCmp.transform.TransformPoint(laneCmp.mapLocalPositions[0]);
                    var lastPt_cmp = laneCmp.transform.TransformPoint(laneCmp.mapLocalPositions[laneCmp.mapLocalPositions.Count - 1]);

                    if ((firstPt - lastPt_cmp).magnitude < MapAnnotationTool.PROXIMITY / MapAnnotationTool.EXPORT_SCALE_FACTOR)
                    {
                        laneCmp.mapLocalPositions[laneCmp.mapLocalPositions.Count - 1] = laneCmp.transform.InverseTransformPoint(firstPt);
                        laneCmp.mapWorldPositions[laneCmp.mapWorldPositions.Count - 1] = firstPt;
                        lane.befores.Add(laneCmp);
                    }

                    if ((lastPt - firstPt_cmp).magnitude < MapAnnotationTool.PROXIMITY / MapAnnotationTool.EXPORT_SCALE_FACTOR)
                    {
                        laneCmp.mapLocalPositions[0] = laneCmp.transform.InverseTransformPoint(lastPt);
                        laneCmp.mapWorldPositions[0] = lastPt;
                        lane.afters.Add(laneCmp);
                    }
                }
            }

            // Check validity of lane segment builder relationship but it won't warn you if have A's right lane to be null or B's left lane to be null
            foreach (var lane in lanes)
            {
                if (lane.leftLaneForward != null && lane != lane.leftLaneForward.rightLaneForward ||
                    lane.rightLaneForward != null && lane != lane.rightLaneForward.leftLaneForward)
                {
                    Debug.Log("Some lane segments neighbor relationships are wrong. Cancelling map generation.");
                    UnityEditor.Selection.activeGameObject = lane.gameObject;
                    return false;
                }
            }

            return true;
        }

        // Connect all lines by adjusting starting/ending points
        public void AlignPointsInLines(HashSet<MapLane> allLanes)
        {
            var visitedLaneIdsEnd = new HashSet<int>(); // lanes whose end point has been visited
            var visitedLaneIdsStart = new HashSet<int>(); // lanes whose start point has been visited
            foreach (var mapLane in allLanes)
            {
                // Handle lane start connecting point
                if (!visitedLaneIdsStart.Contains(mapLane.GetInstanceID()))
                    AlignLines(visitedLaneIdsEnd, visitedLaneIdsStart, mapLane, true);

                // Handle lane end connecting point
                if (!visitedLaneIdsEnd.Contains(mapLane.GetInstanceID()))
                    AlignLines(visitedLaneIdsEnd, visitedLaneIdsStart, mapLane, false);
            }
        }

        void AlignLines(HashSet<int> visitedLaneIdsEnd, HashSet<int> visitedLaneIdsStart, 
            MapLane mapLane, bool isStart)
        {
            var allConnectedLanes2InOut = new Dictionary<MapLane, InOut>();
            if (isStart)
            {
                allConnectedLanes2InOut[mapLane] = InOut.Out;
                AddInOutToDictionary(mapLane.befores, allConnectedLanes2InOut, InOut.In);
                foreach (var beforeLane in mapLane.befores) AddInOutToDictionary(beforeLane.afters, allConnectedLanes2InOut, InOut.Out);
            }
            else
            {
                allConnectedLanes2InOut[mapLane] = InOut.In;
                AddInOutToDictionary(mapLane.afters, allConnectedLanes2InOut, InOut.Out);
                foreach (var afterLane in mapLane.afters) AddInOutToDictionary(afterLane.befores, allConnectedLanes2InOut, InOut.In);
            }

            if (allConnectedLanes2InOut.Count > 1)
            {
                var boundaryLine2InOut = GetBoundaryLine2InOut(allConnectedLanes2InOut);
                List<Vector3> leftMergingPoints, rightMergingPoints;
                UpdateMergingPoints(allConnectedLanes2InOut, boundaryLine2InOut, out leftMergingPoints, out rightMergingPoints);

                var leftEndPoint = Lanelet2MapImporter.GetAverage(leftMergingPoints);
                var rightEndPoint = Lanelet2MapImporter.GetAverage(rightMergingPoints);
                SetEndPoints(allConnectedLanes2InOut, boundaryLine2InOut, leftEndPoint, rightEndPoint);
            }

            UpdateVisitedSets(allConnectedLanes2InOut, visitedLaneIdsStart, visitedLaneIdsEnd);
        }

        private void SetEndPoints(Dictionary<MapLane, InOut> allConnectedLanes2InOut, Dictionary<MapLine, InOut> boundaryLine2InOut, Vector3 leftEndPoint, Vector3 rightEndPoint)
        {
            foreach (var entry in allConnectedLanes2InOut)
            {
                var lane = entry.Key;
                var leftLine = lane.leftLineBoundry;
                var rightLine = lane.rightLineBoundry;
                SetEndPoint(leftLine, leftEndPoint, boundaryLine2InOut[leftLine]);
                SetEndPoint(rightLine, rightEndPoint, boundaryLine2InOut[rightLine]);
            }
        }

        private void UpdateMergingPoints(Dictionary<MapLane, InOut> allConnectedLanes2InOut, Dictionary<MapLine, InOut> boundaryLine2InOut, out List<Vector3> leftMergingPoints, out List<Vector3> rightMergingPoints)
        {
            leftMergingPoints = new List<Vector3>();
            rightMergingPoints = new List<Vector3>();
            foreach (var entry in allConnectedLanes2InOut)
            {
                var lane = entry.Key;
                var leftLine = lane.leftLineBoundry;
                var rightLine = lane.rightLineBoundry;
                var leftPoint = GetEndPoint(boundaryLine2InOut[leftLine], leftLine.mapWorldPositions);
                var rightPoint = GetEndPoint(boundaryLine2InOut[rightLine], rightLine.mapWorldPositions);
                leftMergingPoints.Add(leftPoint);
                rightMergingPoints.Add(rightPoint);
            }
        }

        void SetEndPoint(MapLine line, Vector3 endPoint, InOut inOut)
        {
            RemoveOverlappingEndPoints(line, inOut);
            var index = GetEndPointIndex(inOut, line.mapWorldPositions);
            line.mapWorldPositions[index] = endPoint;
            line.mapLocalPositions[index] = line.transform.InverseTransformPoint(endPoint);
        }

        void RemoveOverlappingEndPoints(MapLine line, InOut inOut)
        {
            var positions = line.mapWorldPositions;
            Vector3 p1 = positions.First(), p2 = positions[1];
            var changed = false;
            var distThreshold = 0.5;
            if ((p2 - p1).magnitude < distThreshold)
            {
                Debug.LogWarning($"Line {line.name} {line.GetInstanceID()} first two points are overlapping, remove second one.");
                CheckLinePositionsSize(line);
                if (positions.Count > 2 && !isSameDirection3Points(p1, p2, positions[2]))
                {
                    positions.RemoveAt(1);
                    changed = true;
                }
            }
            Vector3 p3 = positions[positions.Count - 2], p4 = positions.Last();
            if ((p3 - p4).magnitude < distThreshold)
            {
                Debug.LogWarning($"Line {line.name} {line.GetInstanceID()} last two points are overlapping, remove the one before last one.");
                CheckLinePositionsSize(line);
                if (positions.Count > 2 && !isSameDirection3Points(positions[positions.Count - 3], p3, p4))
                {
                    positions.RemoveAt(positions.Count - 2);
                    changed = true;
                }
            }

            if (changed)
            {
                ApolloMapImporter.UpdateLocalPositions(line);
            }
        }

        bool isSameDirection3Points(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            var vec12 = p2 - p1;
            var vec23 = p3 - p2;
            return Vector3.Dot(vec12, vec23) >= 0;
        }

        void CheckLinePositionsSize(MapLine line)
        {
            if (line.mapWorldPositions.Count == 2)
            {
                var msg = $"Line {line.name} {line.GetInstanceID()} only has two points and overlapping with each other, please check.";
                UnityEditor.Selection.activeGameObject = line.gameObject;
                throw new Exception(msg);
            }
        }

        Vector3 GetEndPoint(InOut inOut, List<Vector3> positions)
        {
            return inOut == InOut.In ? positions.Last() : positions.First();
        }

        int GetEndPointIndex(InOut inOut, List<Vector3> positions)
        {
            return inOut == InOut.In ? positions.Count - 1 : 0;
        }

        void UpdateVisitedSets(Dictionary<MapLane, InOut> allConnectedLanes2InOut, 
            HashSet<int> visitedLaneInstanceIdsStart, HashSet<int> visitedLaneInstanceIdsEnd)
        {
            foreach (var entry in allConnectedLanes2InOut)
            {
                var lane = entry.Key;
                var inOut = entry.Value;
                if (inOut == InOut.In) visitedLaneInstanceIdsEnd.Add(lane.GetInstanceID());
                else visitedLaneInstanceIdsStart.Add(lane.GetInstanceID());
            }
        }

        Dictionary<MapLine, InOut> GetBoundaryLine2InOut(Dictionary<MapLane, InOut> lane2InOut)
        {
            var boundaryLine2InOut = new Dictionary<MapLine, InOut>();
            foreach (var entry in lane2InOut)
            {
                var lane = entry.Key;
                var laneInOut = entry.Value;
                var leftBoundaryLine = lane.leftLineBoundry;
                var rightBoundaryLine = lane.rightLineBoundry;
                AddToLine2InOut(boundaryLine2InOut, leftBoundaryLine, lane, laneInOut);
                AddToLine2InOut(boundaryLine2InOut, rightBoundaryLine, lane, laneInOut);
            }

            return boundaryLine2InOut;
        }

        void AddToLine2InOut(Dictionary<MapLine, InOut> boundaryLine2InOut, MapLine mapLine, MapLane mapLane, InOut laneInOut)
        {
            if (isSameDirection(mapLane, mapLine)) boundaryLine2InOut[mapLine] = laneInOut;
            else boundaryLine2InOut[mapLine] = ReverseInOut(laneInOut);
        }

        InOut ReverseInOut(InOut inOut)
        {
            if (inOut == InOut.In) return InOut.Out;
            else return InOut.In;
        }

        Vector3 GetPoint(MapDataPoints mapDataPoints, int index, InOut inOut)
        {
            var positions = mapDataPoints.mapWorldPositions;
            var point = positions[index];
            if (inOut == InOut.In) point = positions[positions.Count - 1 - index];
            return point;
        }

        enum InOut {In, Out};

        void AddInOutToDictionary(List<MapLane> lanes, Dictionary<MapLane, InOut> allConnectedLanes2InOut, InOut inOut)
        {
            foreach (var lane in lanes)
            {
                if (allConnectedLanes2InOut.ContainsKey(lane))
                {
                    if (allConnectedLanes2InOut[lane] != inOut)
                        {
                            var message = $"Lane {lane.name} {lane.GetInstanceID()} is added ";
                            message += $"already and with {allConnectedLanes2InOut[lane]} not {inOut}";
                            throw new Exception(message);
                        }
                    continue;
                }
                allConnectedLanes2InOut[lane] = inOut;
            }
        }

        bool isSameDirection(MapLane mapLane, MapLine mapLine)
        {
            var lanePositions = mapLane.mapWorldPositions;
            var linePositions = mapLine.mapWorldPositions;;
            var dir = lanePositions.Last() - lanePositions[0];
            var lineDir = linePositions.Last() - linePositions[0];
            return Vector3.Dot(dir, lineDir) > 0;
        }

        void AlignPointsInCrossWalk(List<MapCrossWalk> crossWalkList)
        {
            foreach (var crossWalk in crossWalkList)
            {
                for (int i = 0; i < crossWalk.mapLocalPositions.Count; i++)
                {
                    var pt = crossWalk.transform.TransformPoint(crossWalk.mapLocalPositions[i]);

                    foreach (var crossWalkCmp in crossWalkList)
                    {
                        if (crossWalk == crossWalkCmp)
                        {
                            continue;
                        }

                        for (int j = 0; j < crossWalkCmp.mapLocalPositions.Count; j++)
                        {
                            var ptCmp = crossWalkCmp.transform.TransformPoint(crossWalkCmp.mapLocalPositions[j]);

                            if ((pt - ptCmp).magnitude < MapAnnotationTool.PROXIMITY / MapAnnotationTool.EXPORT_SCALE_FACTOR)
                            {
                                crossWalkCmp.mapLocalPositions[j] = crossWalkCmp.transform.InverseTransformPoint(pt);
                                crossWalkCmp.mapWorldPositions.Add(pt);
                            }
                        }
                    }
                }
            }
        }

        void AlignPointsInParkingSpace(List<MapParkingSpace> parkingSpaceList)
        {
            foreach (var parkingSpace in parkingSpaceList)
            {
                for (int i = 0; i < parkingSpace.mapLocalPositions.Count; i++)
                {
                    var pt = parkingSpace.transform.TransformPoint(parkingSpace.mapLocalPositions[i]);

                    foreach (var parkingSpaceCmp in parkingSpaceList)
                    {
                        if (parkingSpace == parkingSpaceCmp)
                        {
                            continue;
                        }

                        for (int j = 0; j < parkingSpaceCmp.mapLocalPositions.Count; j++)
                        {
                            var ptCmp = parkingSpaceCmp.transform.TransformPoint(parkingSpaceCmp.mapLocalPositions[j]);

                            if ((pt - ptCmp).magnitude < MapAnnotationTool.PROXIMITY / MapAnnotationTool.EXPORT_SCALE_FACTOR)
                            {
                                parkingSpaceCmp.mapLocalPositions[j] = parkingSpaceCmp.transform.InverseTransformPoint(pt);
                                parkingSpaceCmp.mapWorldPositions.Add(pt);
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
                for (float length = resolution - residue; length < segmentLength; length += resolution)
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

        public static bool ExistsLaneWithBoundaries(HashSet<MapLane> lanes)
        {
            foreach (var lane in lanes)
            {
                if (lane.leftLineBoundry != null && lane.rightLineBoundry != null)
                {
                    return true;
                }
            }
            return false;
        }

        public List<MapLine> CreateFakeBoundariesFromLanes(HashSet<MapLane> laneSegments)
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
            return fakeBoundaryLineList;
        }

        static bool HasAdjacentLane(MapLane lane, string side)
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

        static MapLane GetAdjacentLane(MapLane lane, string side)
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

        static bool HasAdjacentLaneBoundaryInBetween(MapLane lane1, MapLane lane2)
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

        static MapLine GetAdjacentLaneBoundaryInBetween(MapLane lane1, MapLane lane2)
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

        public void Export(string filePath)
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
                Debug.Log("Successfully generated and exported Lanelet2 Map!");
            }
            else
            {
                Debug.LogError("Failed to export Lanelet2 Map!");
            }
        }

        public void AddBoundaryTagToWay(MapLane lane, Way leftWay, Way rightWay)
        {
            // set boundary type
            if (leftWay.Tags.ContainsKey("type")) {}
            else if (lane.leftLineBoundry.lineType == MapData.LineType.DOTTED_WHITE)
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
            else if (lane.leftLineBoundry.lineType == MapData.LineType.DOTTED_YELLOW)
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
            else if (lane.leftLineBoundry.lineType == MapData.LineType.SOLID_WHITE)
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
            else if (lane.leftLineBoundry.lineType == MapData.LineType.SOLID_YELLOW)
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
            else if (lane.leftLineBoundry.lineType == MapData.LineType.DOUBLE_YELLOW)
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
            else if (lane.leftLineBoundry.lineType == MapData.LineType.CURB)
            {
                leftWay.Tags.Add(
                    new Tag("type", "curbstone")
                );
                leftWay.Tags.Add(
                    new Tag("subtype", "high")
                );
            }
            else if (lane.leftLineBoundry.lineType == MapData.LineType.VIRTUAL)
            {
                leftWay.Tags.Add(
                    new Tag("type", "virtual")
                );
            }
            else
            {
                Debug.LogWarning($"Lane {lane.name} left boundary type is Unknown.");
                leftWay.Tags.Add(
                    new Tag("type", "unknown")
                );
            }

            if (rightWay.Tags.ContainsKey("type")) {}
            else if (lane.rightLineBoundry.lineType == MapData.LineType.DOTTED_WHITE)
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
            else if (lane.rightLineBoundry.lineType == MapData.LineType.DOTTED_YELLOW)
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
            else if (lane.rightLineBoundry.lineType == MapData.LineType.SOLID_WHITE)
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
            else if (lane.rightLineBoundry.lineType == MapData.LineType.SOLID_YELLOW)
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
            else if (lane.rightLineBoundry.lineType == MapData.LineType.DOUBLE_YELLOW)
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
            else if (lane.rightLineBoundry.lineType == MapData.LineType.CURB)
            {
                rightWay.Tags.Add(
                    new Tag("type", "curbstone")
                );
                rightWay.Tags.Add(
                    new Tag("subtype", "high")
                );
            }
            else if (lane.rightLineBoundry.lineType == MapData.LineType.VIRTUAL)
            {
                rightWay.Tags.Add(
                    new Tag("type", "virtual")
                );
            }
            else
            {
                Debug.LogWarning($"Lane {lane.name} left boundary type is Unknown.");
                rightWay.Tags.Add(
                    new Tag("type", "unknown")
                );            
            }
        }


    }
}