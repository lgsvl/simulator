/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Simulator.Map;
using OsmSharp;
using OsmSharp.Streams;
using OsmSharp.Tags;

namespace Simulator.Editor
{
    public class Lanelet2MapExporter
    {
        private MapManagerData MapAnnotationData;
        MapOrigin MapOrigin;
        List<OsmGeo> map = new List<OsmGeo>();

        public Lanelet2MapExporter()
        {
        }

        public void ExportLanelet2Map(string filePath)
        { 
            if (Calculate())
            {
                Export(filePath);
                Debug.Log("Successfully generated and exported Lanelet2 Map!");
            }
            else
            {
                Debug.LogError("Failed to export Lanelet2 Map!");
            }
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

            // Link before and after segment for each lane segment
            foreach (var laneSegment in laneSegments)
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

                foreach (var laneSegmentCmp in laneSegments)
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

            // Link before and after segment for each line segment
            foreach (var lineSegment in lineSegments)
            {
                // Each segment must have at least 2 waypoints for calculation, otherwise exit
                while (lineSegment.mapLocalPositions.Count < 2)
                {
                    Debug.LogError("Some segment has less than 2 waypoints. Cancelling map generation.");
                    return false;
                }

                // Link lanes
                var firstPt = lineSegment.transform.TransformPoint(lineSegment.mapLocalPositions[0]);
                var lastPt = lineSegment.transform.TransformPoint(lineSegment.mapLocalPositions[lineSegment.mapLocalPositions.Count - 1]);

                foreach (var lineSegmentCmp in lineSegments)
                {
                    if (lineSegment == lineSegmentCmp)
                    {
                        continue;
                    }

                    var firstPt_cmp = lineSegmentCmp.transform.TransformPoint(lineSegmentCmp.mapLocalPositions[0]);
                    var lastPt_cmp = lineSegmentCmp.transform.TransformPoint(lineSegmentCmp.mapLocalPositions[lineSegmentCmp.mapLocalPositions.Count - 1]);

                    if ((firstPt - lastPt_cmp).magnitude < MapAnnotationTool.PROXIMITY / MapAnnotationTool.EXPORT_SCALE_FACTOR)
                    {
                        lineSegmentCmp.mapLocalPositions[lineSegmentCmp.mapLocalPositions.Count - 1] = lineSegmentCmp.transform.InverseTransformPoint(firstPt);
                        lineSegmentCmp.mapWorldPositions[lineSegmentCmp.mapWorldPositions.Count - 1] = firstPt;
                        lineSegment.befores.Add(lineSegmentCmp);
                    }

                    if ((lastPt - firstPt_cmp).magnitude < MapAnnotationTool.PROXIMITY / MapAnnotationTool.EXPORT_SCALE_FACTOR)
                    {
                        lineSegmentCmp.mapLocalPositions[0] = lineSegmentCmp.transform.InverseTransformPoint(lastPt);
                        lineSegmentCmp.mapWorldPositions[0] = lastPt;
                        lineSegment.afters.Add(lineSegmentCmp);
                    }
                }
            }

            // process lanes - create lanelet from lane and left/right boundary
            foreach (var laneSegment in laneSegments)
            {
                map.Add(CreateLaneletFromLane(laneSegment));
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
                            Way wayLightBulbs = CreateWayLightBulbsFromSignal(signal, (long) wayTrafficLight.Id);
                            wayLightBulbsList.Add(wayLightBulbs);
                        }

                        // create relation of regulatory element
                        Relation relationReguratoryElement = CreateRegulatoryElementFromStopLineSignals(wayStopLine, wayTrafficLightList, wayLightBulbsList);
                        map.Add(relationReguratoryElement);
                    }
                }
            }

            // process crosswalk
            foreach (var crossWalk in crossWalkList)
            {
                map.Add(CreateLaneletFromCrossWalk(crossWalk));
            }

            return true;
        }

        public long GetNewId()
        {
            return map.Count + 1;
        }

        public Node GetNodeById(long id)
        {
            foreach (OsmGeo element in map)
            {
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

        public Way CreateWayFromLine(MapLine line, TagsCollection tags)
        {
            List<Node> nodes = new List<Node>();
            // create nodes
            for (int p = 0; p < line.mapWorldPositions.Count; p++)
            {
                Vector3 pos = line.mapWorldPositions[p];
                var location = MapOrigin.GetGpsLocation(pos);

                Node node = CreateNodeFromPoint(pos);
                nodes.Add(node);
            }

            return CreateWayFromNodes(nodes, tags);
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

                switch(signalLight.signalData[p].signalColor)
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

        public void Export(string filePath)
        {
            using (var file = File.Create(filePath))
            using (var target = new XmlOsmStreamTarget(file))
            {
                target.Generator = "LGSVL Simulator";
                target.Initialize();

                foreach (OsmGeo element in map)
                {
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
            }
        }
    }      
}

