/**
 * Copyright (c) 2019-2020 LG Electronics, Inc.
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
using Utility = Simulator.Utilities.Utility;
using UnityEditor.SceneManagement;

namespace Simulator.Editor
{
    public partial class Lanelet2MapImporter
    {
        EditorSettings Settings;

        bool IsMeshNeeded = true; // Boolean value for traffic light/sign mesh importing.
        bool ShowDebugIntersectionArea = false; // Show debug area for intersection area to find left_turn lanes
        MapOrigin MapOrigin;
        
        float LaneLengthThreshold = 1.0f; // Imported lanes shorter than this threshold will be merged into connecting lanes.
        Dictionary<string, OsmGeo> DataSource = new Dictionary<string, OsmGeo>();
        Dictionary<long, GameObject> LineId2GameObject = new Dictionary<long, GameObject>(); // We use id from lineString as lineId
        Dictionary<long, GameObject> MapLaneId2GameObject = new Dictionary<long, GameObject>(); // We use id from Relation as mapLaneId
        Dictionary<long, List<long>> StopLineId2laneIds = new Dictionary<long, List<long>>(); // Connect stop line with referenced lanes
        
        public Lanelet2MapImporter(bool isMeshNeeded)
        {
            IsMeshNeeded = isMeshNeeded;
        }
        
        public void Import(string filePath)
        {
            Settings = EditorSettings.Load();

            if (ImportLanelet2MapCalculate(filePath))
            {
                Debug.Log("Successfully imported Lanelet2 HD Map!\nPlease check your imported intersections and adjust if they are wrongly grouped.");
                Debug.Log("Note if your map is incorrect, please check if you have set MapOrigin correctly.");
                Debug.LogWarning("!!! You need to adjust the triggerBounds for each MapIntersection.");
                var warning = "!!! Grouping objects into intersections is an experimental feature, ";
                    warning += "please check ungrouped objects under Intersections, you need to move them manually to corresponding MapIntersections.";
                Debug.LogWarning(warning);
                EditorSceneManager.MarkAllScenesDirty();
            }
            else
            {
                Debug.Log("Failed to import lanelet2 map.");
            }
        }

        Vector3 GetVector3FromNode(Node node)
        {
            double lat = (double)node.Latitude;
            double lon = (double)node.Longitude;        
            double northing, easting;

            MapOrigin.FromLatitudeLongitude(lat, lon, out northing, out easting);
            Vector3 positionVec = MapOrigin.FromNorthingEasting(northing, easting); // note here y=0 in vec

            if (node.Tags?.Count > 0)
            {
                if (node.Tags.ContainsKey("ele"))
                {
                    var y = float.Parse(node.Tags.GetValue("ele"));
                    positionVec.y = y;
                }
            }

            return positionVec;
        }

        float RangedLength(long lineStringId)
        {
            float len = 0;
            int last = 0;
            long[] nodeIds = ((Way)DataSource["Way"+lineStringId]).Nodes;
            for (int i = 1; i < nodeIds.Length; i ++)
            {
                var lastNode = (Node)DataSource["Node" + nodeIds[last]];
                var curNode = (Node)DataSource["Node" + nodeIds[i]];
                Vector3 lastPoint = GetVector3FromNode(lastNode);
                Vector3 curPoint = GetVector3FromNode(curNode);
                len += Vector3.Distance(lastPoint, curPoint);
                last = i;
            }

            return len;
        }

        public static Vector3 GetAverage(List<Vector3> vectors)
        {
            if (vectors.Count == 0)
            {
                Debug.LogError("Given points has no elements. Returning (0, 0, 0) instead.");
                return new Vector3(0, 0, 0);
            }

            float x = 0f, y = 0f, z = 0f;
            foreach(var vector in vectors)
            {
                x += vector.x;
                y += vector.y;
                z += vector.z;
            }
            return new Vector3(x / vectors.Count, y / vectors.Count, z / vectors.Count);
        }

        GameObject CreateMapLine(long id)
        {
            var way = (Way)DataSource["Way" + id];
            var ids = way.Nodes;
            var positions = new List<Vector3>();
            foreach (var nodeId in ids)
            {
                var node = (Node)DataSource["Node" + nodeId];
                Vector3 positionVec = GetVector3FromNode(node);
                positions.Add(positionVec);
            }
            
            GameObject mapLineObj = new GameObject("MapLine_" + id);
            MapLine mapLine = mapLineObj.AddComponent<MapLine>();
            mapLine.mapWorldPositions = positions;

            // Set transform of the line as the middle of first and last positions
            var lineX = (positions[0].x + positions[positions.Count - 1].x) / 2;
            var lineZ = (positions[0].z + positions[positions.Count - 1].z) / 2;
            var lineY = Math.Max(positions[0].y , positions[positions.Count - 1].y); // TODO, maybe height should also be the average value?
            mapLineObj.transform.position = new Vector3(lineX, lineY, lineZ);

            for (int i = 0; i < positions.Count; i++)
            {
                mapLine.mapLocalPositions.Add(mapLineObj.transform.InverseTransformPoint(positions[i]));
            }

            // Update MapLine line type, note line types are different for each country, you may want to customize here
            mapLine.lineType = MapData.LineType.DOTTED_WHITE; // Default type is dotted white
            if (way.Tags?.Count > 0)
            {
                if (way.Tags.Contains("type", "curb_stone")) mapLine.lineType = MapData.LineType.CURB;
                else if(way.Tags.Contains("type", "virtual")) mapLine.lineType = MapData.LineType.VIRTUAL;
                else if(way.Tags.Contains("subtype", "solid") && way.Tags.Contains("color", "white")) mapLine.lineType = MapData.LineType.SOLID_WHITE;
                else if(way.Tags.Contains("subtype", "solid") && way.Tags.Contains("color", "yellow")) mapLine.lineType = MapData.LineType.SOLID_YELLOW;
                else if(way.Tags.Contains("sybtype", "dashed") && way.Tags.Contains("color", "white")) mapLine.lineType = MapData.LineType.DOTTED_WHITE;
                else if(way.Tags.Contains("sybtype", "dashed") && way.Tags.Contains("color", "yellow")) mapLine.lineType = MapData.LineType.DOTTED_YELLOW;
                else if(way.Tags.Contains("subtype", "solid_solid") && way.Tags.Contains("color", "yellow")) mapLine.lineType = MapData.LineType.DOUBLE_YELLOW;
            }

            return mapLineObj;
        }

        void UpdateMapOrigin(IEnumerable<OsmGeo> Lanelet2Map)
        {
            // Get first node as the origin
            Node originNode = null;
            foreach (var element in Lanelet2Map)
            {
                if (element.Type == OsmGeoType.Node)
                {
                    originNode = (Node)element;
                    break;
                }
            }
            
            if (originNode == null)
            {
                Debug.LogError("Could not find any node, cannot update MapOrigin!");
                return;
            }
            double latitude, longitude;
            longitude = originNode.Longitude.Value;
            latitude = originNode.Latitude.Value;

            int zoneNumber = MapOrigin.GetZoneNumberFromLatLon(latitude, longitude);
            
            MapOrigin.UTMZoneId = zoneNumber;
            double northing, easting;
            MapOrigin.FromLatitudeLongitude(latitude, longitude, out northing, out easting);
            MapOrigin.OriginNorthing = northing;
            MapOrigin.OriginEasting = easting;
        }

        public bool ImportLanelet2MapCalculate(string filePath)
        {
            using (var fileStream = new FileInfo(@filePath).OpenRead())
            {
                var source = new OsmSharp.Streams.XmlOsmStreamSource(fileStream);
                
                // filter all ways and keep all nodes.
                var filtered = from osmGeo in source
                where osmGeo.Type == OsmSharp.OsmGeoType.Node || 
                    (osmGeo.Type == OsmSharp.OsmGeoType.Way) || 
                    (osmGeo.Type == OsmSharp.OsmGeoType.Relation && (osmGeo.Tags.Contains("type", "regulatory_element") || osmGeo.Tags.Contains("type", "lanelet")))
                select osmGeo;

                // Create Map object
                var fileName = Path.GetFileName(filePath).Split('.')[0];
                string mapName = "Map" + char.ToUpper(fileName[0]) + fileName.Substring(1);
                // Check existence of same name map
                if (GameObject.Find(mapName)) 
                {
                    Debug.LogError("A map with same name exists, cancelling map importing.");
                    return false;
                }

                GameObject map = new GameObject(mapName);
                var mapHolder = map.AddComponent<MapHolder>();

                // Create TrafficLanes and Intersections under Map
                GameObject trafficLanes = new GameObject("TrafficLanes");
                GameObject intersections = new GameObject("Intersections");
                GameObject boundryLines = new GameObject("BoundryLines");
                trafficLanes.transform.parent = map.transform;
                intersections.transform.parent = map.transform;
                boundryLines.transform.parent = map.transform;

                mapHolder.trafficLanesHolder = trafficLanes.transform;
                mapHolder.intersectionsHolder = intersections.transform;

                MapOrigin = MapOrigin.Find();
                UpdateMapOrigin(filtered);

                // Add elements first since some elements appear later after it is referenced.
                foreach (var element in filtered)
                {
                    if (element.Type == OsmGeoType.Node || element.Type == OsmGeoType.Way
                        || element.Type == OsmGeoType.Relation)
                    {
                        DataSource.Add(element.Type.ToString() + element.Id, element);                        
                    }
                }

                var shortLanesId = new List<long>(); // ids of lanes whose length is less than threshold
                var lineId2FirstLastNodeIds = new Dictionary<long, List<long>>(); // Dictionary to store 1st and last node id for every lineId.
                var lineId2LaneIdList = new Dictionary<long, List<long>>(); // Note: MapLine might have opposite direction with the corresponding MapLane.
                var tempLaneSectionsLaneIds = new List<List<long>>(); // List of pairs of MapLane IDs obtained based on shared MapLine
                var regulatorElementIds = new List<long>();
                var laneId2RegIds = new Dictionary<long, List<long>>(); // We need to connect laneId with corresponding reg: traffic light / stop sign
                foreach (var element in filtered)
                {
                    // Get StopLine
                    if (element.Type == OsmGeoType.Way && element.Tags != null && element.Tags.Contains("type", "stop_line"))
                    {
                        var mapLineObj = CreateMapLine(element.Id.Value);
                        mapLineObj.GetComponent<MapLine>().lineType = MapData.LineType.STOP;
                        mapLineObj.name = "MapStopLine_" + element.Id;
                        mapLineObj.transform.parent = intersections.transform;
                        LineId2GameObject.Add(element.Id.Value, mapLineObj);
                    }
                    else if (element.Type == OsmGeoType.Relation && element.Tags.Contains("type", "regulatory_element"))
                    {
                        regulatorElementIds.Add(element.Id.Value);
                    }
                    else if (element.Type == OsmGeoType.Relation && element.Tags.Contains("type", "lanelet"))// && element.Tags.Contains("participant:vehicle", "yes")) // only get lanelets for vehicles
                    {
                        // Skip types
                        if (element.Tags.Contains("type", "pedestrian_marking") ||  element.Tags.Contains("subtype", "crosswalk") 
                            || element.Tags.Contains("participant:bicycle", "yes") ||  element.Tags.Contains("participant:pedestrian", "yes"))
                        {
                            continue;
                        }

                        // Process lanelet relation
                        var speedLimit = -1f;

                        long leftLineStringId = 0;
                        long rightLineStringId = 0;

                        List<long> tempLaneSectionLaneIds = new List<long>(); // If we get two lanes for a same line, we make a temp laneSection

                        // Get left and right ways, note: way in OSM == lineString in lanelet2
                        var relation = element as OsmSharp.Relation;
                        RelationMember[] members = relation.Members;
                        foreach (var member in members)
                        {
                            // Assumption: one lineString will be referenced at most two times: left and right roles for two lanelets
                            
                            if (member.Role == "left" || member.Role == "right")
                            {           
                                if (!lineId2FirstLastNodeIds.ContainsKey(member.Id))
                                {
                                    var ids = ((Way)DataSource["Way" + member.Id]).Nodes;
                                    lineId2FirstLastNodeIds.Add(member.Id, new List<long>(){ids[0], ids[ids.Length-1]});
                                }
                                                     
                                if (!LineId2GameObject.ContainsKey(member.Id))
                                {
                                    // Create MapLine
                                    var mapLineObj = CreateMapLine(member.Id);
                                    mapLineObj.transform.parent = boundryLines.transform;
                                    LineId2GameObject.Add(member.Id, mapLineObj);
                                }

                                if (member.Role == "left")
                                {
                                    leftLineStringId = member.Id;
                                }
                                else if (member.Role == "right")
                                {
                                    rightLineStringId = member.Id;
                                }

                                // Save dictionary, key: MapLine lineStringId, value:list of MapLanes
                                if (lineId2LaneIdList.ContainsKey(member.Id)) 
                                {
                                    lineId2LaneIdList[member.Id].Add(element.Id.Value);
                                    tempLaneSectionLaneIds = lineId2LaneIdList[member.Id];
                                }
                                else
                                {
                                    lineId2LaneIdList[member.Id] = new List<long>()
                                    {
                                        element.Id.Value
                                    };
                                }

                                if (lineId2LaneIdList[member.Id].Count > 2) Debug.LogError("One lineString should not be used by more than 2 lanelet, LineString id: " + member.Id); 
                            }
                            else if (member.Role == "regulatory_element")
                            {
                                var regulatorElement = (Relation)DataSource[member.Type.ToString() + member.Id];
                                var subType = regulatorElement.Tags.GetValue("subtype");
                                var type = regulatorElement.Tags.GetValue("type");

                                if (subType == "speed_limit")
                                {
                                    speedLimit = float.Parse(regulatorElement.Tags.GetValue("max_speed"));
                                    var speedLimitUnit = regulatorElement.Tags.GetValue("max_speed_unit");
                                    if (speedLimitUnit == "mph")
                                    {
                                        // unit: mile per hour, convert to meter per second
                                        speedLimit *= 0.44704f;
                                    }
                                    else
                                    {
                                        Debug.LogError("Speed limit unit not seen before, please implement conversion for this unit!");
                                        return false;
                                    }
                                }
                                else if (subType == "traffic_light" || subType == "stop_sign")
                                {
                                    if (laneId2RegIds.ContainsKey(element.Id.Value))
                                    {
                                        laneId2RegIds[element.Id.Value].Add(member.Id);
                                    }
                                    else
                                    {
                                        laneId2RegIds[element.Id.Value] = new List<long>()
                                        {
                                            member.Id
                                        };
                                    }
                                }

                                if (speedLimit == -1)
                                {
                                    speedLimit = 20f; // Set default speed limit if not given
                                }
                            }
                        }

                        if ((leftLineStringId == 0 && rightLineStringId == 0) || leftLineStringId == rightLineStringId)
                        {
                            Debug.LogError("Cannot find correct left/right LineStrings for current lanelet, cancelling map importing.");
                            Debug.LogError("leftLineStringId: " + leftLineStringId + ", rightLineStringId: " + rightLineStringId);
                            return false;
                        }

                        // Compute temp center line
                        List<Vector3> centerLinePoints = ComputeCenterLine(leftLineStringId, rightLineStringId);
                        if (centerLinePoints.Count == 0)
                        {
                            Debug.LogError($"Something wrong with this lanelet, skipping it. leftLineStringId: {leftLineStringId}, rightLineStringId: {rightLineStringId}");
                            continue;
                        }

                        // Ignore lanes with three points and shorter than 1 meter
                        if ((centerLinePoints[centerLinePoints.Count-1] - centerLinePoints[0]).magnitude < LaneLengthThreshold)
                        {
                            shortLanesId.Add(element.Id.Value);
                        }


                        // Create MapLane based on center line, MapLine based on two ways
                        GameObject mapLaneObj = new GameObject("MapLane_" + element.Id);
                        MapLane mapLane = mapLaneObj.AddComponent<MapLane>();
                        mapLane.mapWorldPositions = centerLinePoints;

                        // Set transform of the line as the middle of first and last positions
                        var laneX = (centerLinePoints[0].x + centerLinePoints[centerLinePoints.Count - 1].x) / 2;
                        var laneZ = (centerLinePoints[0].z + centerLinePoints[centerLinePoints.Count - 1].z) / 2;
                        var laneY = Math.Max(centerLinePoints[0].y , centerLinePoints[centerLinePoints.Count - 1].y); // TODO height, do we need to average as well?
                        mapLaneObj.transform.position = new Vector3(laneX, laneY, laneZ);

                        for (int i = 0; i < centerLinePoints.Count; i++)
                        {
                            mapLane.mapLocalPositions.Add(mapLaneObj.transform.InverseTransformPoint(centerLinePoints[i]));
                        }
                        mapLaneObj.transform.parent = trafficLanes.transform;         

                        if (speedLimit != -1)
                        {
                            mapLane.speedLimit = speedLimit;
                        }
                        // // Fill left/right boundryLine          
                        mapLane.leftLineBoundry = LineId2GameObject[leftLineStringId].GetComponent<MapLine>();
                        mapLane.rightLineBoundry = LineId2GameObject[rightLineStringId].GetComponent<MapLine>();
                        
                        MapLaneId2GameObject[element.Id.Value] = mapLaneObj;

                        // Make temp laneSection
                        if (tempLaneSectionLaneIds.Count > 0)
                        {
                            tempLaneSectionsLaneIds.Add(tempLaneSectionLaneIds);
                        }
                    }
                    
                }

                var laneId2LaneSectionLaneIds = new Dictionary<long, List<long>>();
                var visitedLanes = new HashSet<long>();
                void updatelaneId2LaneSectionLaneIds(List<long> laneIds) 
                {
                    foreach (var laneId in laneIds)
                    {
                        laneId2LaneSectionLaneIds[laneId] = laneIds;
                    }
                }
                var uniqueLaneSections = new HashSet<List<long>>();
                foreach (var tempLaneSectionLaneIds in tempLaneSectionsLaneIds)
                {
                    var laneId1 = tempLaneSectionLaneIds[0];
                    var laneId2 = tempLaneSectionLaneIds[1];
                    var notSeen = true;

                    if (visitedLanes.Contains(laneId1))
                    {
                        var existingLaneSectionLaneIds = laneId2LaneSectionLaneIds[laneId1];
                        uniqueLaneSections.Remove(existingLaneSectionLaneIds);
                        existingLaneSectionLaneIds.Add(laneId2);
                        uniqueLaneSections.Add(existingLaneSectionLaneIds);
                        updatelaneId2LaneSectionLaneIds(existingLaneSectionLaneIds);
                        notSeen = false;
                    }
                    if (visitedLanes.Contains(laneId2))
                    {
                        var existingLaneSectionLaneIds = laneId2LaneSectionLaneIds[laneId2];
                        uniqueLaneSections.Remove(existingLaneSectionLaneIds);
                        existingLaneSectionLaneIds.Add(laneId1);
                        uniqueLaneSections.Add(existingLaneSectionLaneIds);
                        updatelaneId2LaneSectionLaneIds(existingLaneSectionLaneIds);
                        notSeen = false;
                    }
                    
                    if (notSeen)
                    {
                        laneId2LaneSectionLaneIds[laneId1] = tempLaneSectionLaneIds;
                        laneId2LaneSectionLaneIds[laneId2] = tempLaneSectionLaneIds;
                        uniqueLaneSections.Add(tempLaneSectionLaneIds);
                    }
                    
                    visitedLanes.Add(laneId1);
                    visitedLanes.Add(laneId2);
                }

                // Create MapLaneSection objects
                int laneSectionId = 0;
                var mapLaneSectionId2Object = new Dictionary<long, GameObject>();
                var mapLaneSectionId2laneIds = new Dictionary<long, List<long>>();
                foreach (var laneSectionLaneIds in uniqueLaneSections)
                {
                    var mapLaneSectionObj = new GameObject("MapLaneSection_" + laneSectionId++);
                    mapLaneSectionObj.AddComponent<MapLaneSection>();
                    mapLaneSectionObj.transform.parent = trafficLanes.transform;
                    var lanePositions = new List<Vector3>();
                    foreach (var id in laneSectionLaneIds)
                    {
                        lanePositions.Add(MapLaneId2GameObject[id].transform.position);
                        MapLaneId2GameObject[id].transform.parent = mapLaneSectionObj.transform;
                    }

                    mapLaneSectionObj.transform.position = GetAverage(lanePositions);
                    mapLaneSectionId2Object[laneSectionId-1] = mapLaneSectionObj;
                    mapLaneSectionId2laneIds[laneSectionId-1] = laneSectionLaneIds;

                    // Update children maplanes to have correct position
                    foreach (var id in laneSectionLaneIds)
                    {
                        MapLane tempLane = MapLaneId2GameObject[id].GetComponent<MapLane>();
                        tempLane.transform.position = tempLane.transform.position - mapLaneSectionObj.transform.position;
                        
                        // Update localpositions after lane position update due to mapLaneSection
                        tempLane.mapLocalPositions.Clear();
                        for (int i = 0; i < tempLane.mapWorldPositions.Count; i++)
                        {
                            tempLane.mapLocalPositions.Add(tempLane.transform.InverseTransformPoint(tempLane.mapWorldPositions[i]));
                        }
                    }
                }

                HashSet<long> ConnectAndReturn(long boundaryLineStringId)
                {
                    // Find connecting lines
                    var firstLastNodeIds = lineId2FirstLastNodeIds[boundaryLineStringId];
                    long firstNodeId = firstLastNodeIds[0], lastNodeId = firstLastNodeIds[1];
                    List<long> precedingLineIds = new List<long>();
                    List<long> followingLineIds = new List<long>();
                    foreach (var lineStringId in lineId2FirstLastNodeIds.Keys)
                    {
                        var otherFirstLastNodeIds = lineId2FirstLastNodeIds[lineStringId];
                        long otherFirstNodeId = otherFirstLastNodeIds[0], otherLastNodeId = otherFirstLastNodeIds[1];
                        if (firstNodeId == otherLastNodeId)
                        {
                            precedingLineIds.Add(lineStringId);
                        }

                        if (lastNodeId == otherFirstNodeId)
                        {
                            followingLineIds.Add(lineStringId);
                        }

                        if (firstNodeId == otherLastNodeId && lastNodeId == otherFirstNodeId)
                        {
                            Debug.LogError("This should only happen when we have a circle!");
                        }
                    }

                    var possiblePrecedingLaneIds = new HashSet<long>();
                    // Remove shortline and connect preceding and following lines, update actual MapLine object as well
                    foreach (var lineStringId in precedingLineIds)
                    {
                        // Set preceding line's last point as the same as last point of current line
                        var mapLine = LineId2GameObject[lineStringId].GetComponent<MapLine>();
                        var worldPositions = mapLine.mapWorldPositions;
                        worldPositions[worldPositions.Count-1] = LineId2GameObject[boundaryLineStringId].GetComponent<MapLine>().mapWorldPositions.Last();
                        
                        var localPositions = mapLine.mapLocalPositions;
                        localPositions[localPositions.Count-1] = mapLine.transform.InverseTransformPoint(worldPositions[worldPositions.Count-1]);
                        foreach (var laneId in lineId2LaneIdList[lineStringId])
                        {
                            possiblePrecedingLaneIds.Add(laneId);
                        }
                    }

                    return possiblePrecedingLaneIds;
                }
                
                // Backtracking to add all lanes in the laneSection if we are deleting any lane in that laneSection.
                foreach (var laneIds in mapLaneSectionId2laneIds.Values)
                {
                    var isShort = false;
                    var idx = 0; 

                    while (true)
                    {
                        if (idx == laneIds.Count) break;

                        if (!isShort)
                        {
                            // If current lane is in shortLanesIdSet, all other lanes should be in shortLanesIdSet as well.
                            if (shortLanesId.Contains(laneIds[idx]))
                            {
                                isShort = true;
                                if (idx == 0) idx = 1;
                                else idx = 0;
                            }
                            else
                            { 
                                // Not in shortLanesIdSet
                                idx += 1;
                            }
                        }
                        else
                        {
                            if (!shortLanesId.Contains(laneIds[idx])) shortLanesId.Add(laneIds[idx]);
                            idx += 1;
                        }
                    }
                }

                var lanesToDestroy = new HashSet<long>();
                var linesToDestroy = new HashSet<long>();
                foreach (var laneId in shortLanesId)
                {
                    var mapLane = MapLaneId2GameObject[laneId].GetComponent<MapLane>();
                    long leftLineStringId = long.Parse(mapLane.leftLineBoundry.name.Split('_')[1]); // Get lineString id from mapLine name
                    long rightLineStringId = long.Parse(mapLane.rightLineBoundry.name.Split('_')[1]); 

                    var possiblePreLanesSetLeft = ConnectAndReturn(leftLineStringId);
                    var possiblePreLanesSetRight = ConnectAndReturn(rightLineStringId);
                    linesToDestroy.Add(leftLineStringId);
                    linesToDestroy.Add(rightLineStringId);


                    // BruteForce to find preceding lanes and following lanes
                    var worldPositions = mapLane.mapWorldPositions;
                    foreach (var otherLaneId in MapLaneId2GameObject.Keys)
                    {
                        if (laneId == otherLaneId) continue;
                        var otherMapLane = MapLaneId2GameObject[otherLaneId].GetComponent<MapLane>();
                        var otherWorldPositions = otherMapLane.mapWorldPositions;

                        if ((worldPositions[0] - otherWorldPositions[otherWorldPositions.Count-1]).magnitude < 0.001)
                        {
                            otherWorldPositions[otherWorldPositions.Count-1] = worldPositions.Last();
                            var otherLocalPositions = otherMapLane.mapLocalPositions;
                            otherLocalPositions[otherLocalPositions.Count-1] = otherMapLane.transform.InverseTransformPoint(otherWorldPositions.Last());
                        }
                    }

                    lanesToDestroy.Add(laneId);      
                }

                foreach (var lineId in linesToDestroy)
                {
                    GameObject.DestroyImmediate(LineId2GameObject[lineId]);
                    LineId2GameObject.Remove(lineId);
                }
                foreach (var laneId in lanesToDestroy)
                {
                    GameObject.DestroyImmediate(MapLaneId2GameObject[laneId]);
                    MapLaneId2GameObject.Remove(laneId);
                }

                // Check validity of all laneSections
                foreach (var mapLaneSectionId in mapLaneSectionId2Object.Keys)
                {
                    var laneSectionObj = mapLaneSectionId2Object[mapLaneSectionId];
                    if (laneSectionObj.transform.childCount == 1)
                    {
                        Debug.LogError("You have MapLaneSection with only one lane, it should have at least 2 lanes, please check laneSection id " + mapLaneSectionId);
                        return false; 
                    }
                }
                
                // Destroy empty LaneSection objects
                foreach (var mapLaneSectionId in mapLaneSectionId2Object.Keys)
                {
                    var laneSectionObj = mapLaneSectionId2Object[mapLaneSectionId];
                    if (laneSectionObj.transform.childCount == 0)
                    {
                        GameObject.DestroyImmediate(laneSectionObj);
                    }
                }

                SetCorrectDirectionForStopLines(LineId2GameObject.Keys.ToList());

                ///// Import Intersections
                var vistedRegulatoryElementIds = new HashSet<long>();
                var regId2StopLineId = new Dictionary<long, long>();
                var stopLineId2regIds = new Dictionary<long, HashSet<long>>();
                var relatedLaneGroups = new List<HashSet<long>>();
                var regId2regObj = new Dictionary<long, GameObject>();
                var regId2Mesh = new Dictionary<long, GameObject>();
                foreach (var regId in regulatorElementIds)
                {
                    var relation = ((Relation)DataSource["Relation"+regId]);
                    var tags = relation.Tags;
                    
                    if (vistedRegulatoryElementIds.Contains(regId))
                    {
                        Debug.LogError(regId + " has been visited.");
                        continue;
                    }
                    else
                    {
                        vistedRegulatoryElementIds.Add(regId);
                    }
                    
                    long stopLineId = long.MaxValue;
                    if (tags.GetValue("subtype") == "traffic_light")
                    {
                        var mapSignals = new List<MapSignal>();
                        foreach (var member in relation.Members)
                        {
                            if (member.Role == "refers")
                            {
                                // CreateMapSignal()
                                var mapSignalObj = new GameObject("MapSignal_" + member.Id);
                                mapSignalObj.transform.parent = intersections.transform;
                                regId2regObj[member.Id] = mapSignalObj;
                                var mapSignal = mapSignalObj.AddComponent<MapSignal>();
                                
                                // Get position of the signal object
                                var way = (Way)DataSource["Way" + member.Id];
                                var nodes = way.Nodes;
                                Vector3 signalPos;
                                if (nodes.Length == 3)
                                {
                                    // if lanelet2, use middle point
                                    var node = (Node)DataSource["Node" + nodes[1]];
                                    signalPos = GetVector3FromNode(node);
                                }
                                else if (nodes.Length == 2)
                                {
                                    // if lanelet2 Autoware extension, compute based on bottom line and height.
                                    var node1 = (Node)DataSource["Node" + nodes[0]];
                                    var node2 = (Node)DataSource["Node" + nodes[1]];
                                    var node1Pos = GetVector3FromNode(node1);
                                    var node2Pos = GetVector3FromNode(node2);
                                    signalPos = (node1Pos + node2Pos) / 2;
                                    var height = float.Parse(way.Tags.GetValue("height"));
                                    signalPos.y += height / 2;
                                }
                                else
                                {
                                    Debug.LogError("Error, traffic signal " + member.Id + " doesn't have 2 or 3 points!");
                                    return false;
                                }


                                if (signalPos.y < 0.1f)
                                {
                                    // if height not given.
                                    signalPos.y = 5.0f;
                                }

                                mapSignalObj.transform.position = signalPos;

                                mapSignal.signalData = new List<MapData.SignalData> {
                                    new MapData.SignalData() { localPosition = Vector3.up * 0.4f, signalColor = MapData.SignalColorType.Red },
                                    new MapData.SignalData() { localPosition = Vector3.zero, signalColor = MapData.SignalColorType.Yellow },
                                    new MapData.SignalData() { localPosition = Vector3.up * -0.4f, signalColor = MapData.SignalColorType.Green },
                                };

                                mapSignals.Add(mapSignal);
                            }
                            else if (member.Role == "ref_line")
                            {
                                // Set stop line to traffic signals
                                stopLineId = member.Id;
                            }
                        }

                        if (stopLineId == long.MaxValue)
                        {
                            Debug.LogError("Error, traffic light " + relation.Id + " has no related stop line!");
                            return false;
                            // TODO: stop line is optional, if not given, we should hint a stop line by the end of the lanelet.
                        }
                        
                        var signalDirection = GetDirectionFromStopLine(stopLineId);
                        var stopLine = LineId2GameObject[stopLineId].GetComponent<MapLine>();

                        // Set all signals to have correct stop line and create signal mesh object.
                        foreach (var mapSignal in mapSignals)
                        {
                            mapSignal.stopLine = stopLine;
                            var signalId = long.Parse(mapSignal.name.Split('_')[1]);
                            mapSignal.transform.rotation = Quaternion.LookRotation(signalDirection);
                            
                            if (IsMeshNeeded)
                            {
                                var trafficLightObj = UnityEngine.Object.Instantiate(Settings.MapTrafficSignalPrefab, mapSignal.transform.position, mapSignal.transform.rotation);
                                trafficLightObj.transform.parent = intersections.transform;
                                trafficLightObj.name = "MapSignalTrafficVertical_" + mapSignal.transform.name.Split('_')[1];
                                trafficLightObj.AddComponent<SignalLight>();
                                regId2Mesh[signalId] = trafficLightObj;
                            }   
                            stopLine.signals.Add(mapSignal);
                        }

                        if (mapSignals.Count == 0) continue;
                        if (!stopLineId2regIds.ContainsKey(stopLineId))
                        {
                            stopLineId2regIds[stopLineId] = new HashSet<long>();
                        }
                        foreach (var mapSignal in mapSignals)
                        {
                            stopLineId2regIds[stopLineId].Add(long.Parse(mapSignal.name.Split('_')[1]));
                        }
                    }
                    // // get all related lanes by right of way within one intersection
                    // else if (tags.GetValue("subtype") == "right_of_way")
                    // {
                    //     var relatedLaneGroup = new HashSet<long>();
                    //     foreach (var member in relation.Members)
                    //     {
                    //         if (member.Role == "right_of_way" || member.Role == "yield")
                    //         {
                    //             relatedLaneGroup.Add(member.Id);
                    //         }
                    //     }
                    //     relatedLaneGroups.Add(relatedLaneGroup);
                    // }
                    else if (tags.GetValue("subtype") == "stop_sign")
                    {
                        // Get StopLine and Signs
                        var mapSigns = new List<MapSign>();
                        Vector3 signMeshPos = Vector3.zero;
                        foreach (var member in relation.Members)
                        {
                            if (member.Role == "refers")
                            {
                                // Create MapSign
                                var mapSignObj = new GameObject("MapSign_" + member.Id);
                                mapSignObj.transform.parent = intersections.transform;
                                regId2regObj[member.Id] = mapSignObj;
                                var mapSign = mapSignObj.AddComponent<MapSign>();
                                mapSign.signType = MapData.SignType.STOP;

                                // Get positions of the sign object and sign mesh
                                var way = (Way)DataSource["Way" + member.Id];
                                var nodes = way.Nodes;
                                if (nodes.Length != 2)
                                {
                                    Debug.Log("Not supported stop sign format!!!");
                                    return false;
                                }
                                var node1 = (Node)DataSource["Node" + nodes[0]];
                                var node2 = (Node)DataSource["Node" + nodes[1]];
                                signMeshPos = (GetVector3FromNode(node1) + GetVector3FromNode(node2)) / 2;
                                var height = float.Parse(way.Tags.GetValue("height"));
                                signMeshPos.y += height / 2;

                                // Raycast to compute the height of the stop sign mesh
                                RaycastHit hit = new RaycastHit();
                                int mapLayerMask = LayerMask.GetMask("Default");
                                var boundOffsets = Vector3.zero;
                                if (Physics.Raycast(signMeshPos, Vector3.down, out hit, 1000.0f, mapLayerMask))
                                {
                                    boundOffsets.y = hit.distance;
                                    mapSign.transform.position = hit.point;
                                }
                                else
                                {
                                    // If no ground, set position same as signMeshPos and set y to 0.
                                    boundOffsets.y = 0f;
                                    mapSign.transform.position = new Vector3(signMeshPos.x, 0f, signMeshPos.z);
                                }
                                mapSign.boundOffsets = boundOffsets;
                                mapSign.boundScale = new Vector3(0.95f, 0.95f, 0f);
                                mapSigns.Add(mapSign);
                            }
                            else if (member.Role == "ref_line")
                            {
                                stopLineId = member.Id;
                            }
                        }

                        if (stopLineId == long.MaxValue)
                        {
                            Debug.LogError("Error, stop sign " + relation.Id + " has no related stop line!");
                            return false;
                            // TODO: stop line is optional, if not given, we should hint a stop line by the end of the lanelet.
                        }
                        
                        {
                            var signDirection = GetDirectionFromStopLine(stopLineId);
                            var mapSign = mapSigns[0]; // We only use the 1st stop sign
                            mapSign.transform.rotation = Quaternion.LookRotation(signDirection);

                            var stopLine = LineId2GameObject[stopLineId].GetComponent<MapLine>();
                            stopLine.isStopSign = true;
                            stopLine.stopSign = mapSign;

                            // Set sign to have correct stop line and create stop sign mesh object.
                            mapSign.stopLine = stopLine;
                            var stopSignId = long.Parse(mapSign.name.Split('_')[1]);
                            regId2StopLineId[stopSignId] = stopLineId;

                            if (IsMeshNeeded)
                            {
                                GameObject stopSignPrefab = Settings.MapStopSignPrefab;
                                var stopSignObj = UnityEngine.Object.Instantiate(stopSignPrefab, mapSign.transform.position + mapSign.boundOffsets, mapSign.transform.rotation);
                                stopSignObj.transform.parent = intersections.transform;
                                stopSignObj.transform.position = signMeshPos;
                                stopSignObj.name = "MapStopSign_" + mapSign.transform.name.Split('_')[1];
                                regId2Mesh[stopSignId] = stopSignObj;
                            }

                            stopLineId2regIds[stopLineId] = new HashSet<long>()
                            {
                                long.Parse(mapSign.name.Split('_')[1])
                            };                            
                        }
                    }

                    if (stopLineId != long.MaxValue)
                    {
                        regId2StopLineId[regId] = stopLineId;
                    }
                    
                }
                
                /////////// Group signs / signals based on their corresponding stop lines. //////////
                // Compute nearest stop line pairs, one stopline + nearest stopline with opposite signal direction.
                // Combine pairs if they are close and have perpendicular directions.
                var nearestStopLinePairs = new List<List<int>>();
                var visitedPairs = new HashSet<Tuple<int, int>>();
                List<long> stopLineIds = new List<long>(stopLineId2regIds.Keys);

                Func<int, Vector3> GetStopLineCenterPos = i => 
                {
                    var positions = LineId2GameObject[stopLineIds[i]].GetComponent<MapLine>().mapWorldPositions;
                    return (positions.First() + positions.Last()) / 2;
                };

                Action<GameObject, GameObject> SetParent = (obj, parent) =>
                {
                    obj.transform.parent = parent.transform;
                };
                Action<GameObject, GameObject> SetParentPos = (obj, parent) =>
                {
                    obj.transform.position -= parent.transform.position;
                };
                
                // Find out related lanes to each stop line that has a signal/sign, laneId -> regId -> stopLineId
                foreach (var pair in laneId2RegIds)
                {
                    var laneId = pair.Key;
                    var regIds = pair.Value;

                    var regId = regIds[0]; // Assume one lane only has 1 corresponding stop line, so we only use the 1st signal/sign.
                    
                    var stopLineId = regId2StopLineId[regId];
                    if (StopLineId2laneIds.ContainsKey(stopLineId))
                    {
                        var laneIds = stopLineId2regIds[stopLineId];
                        if (laneIds.Contains(laneId)) Debug.LogError("Not possible!!!!");
                        StopLineId2laneIds[stopLineId].Add(laneId);
                    }
                    else
                    {
                        StopLineId2laneIds[stopLineId] = new List<long>()
                        {
                            laneId
                        };
                    }
                }

                if (StopLineId2laneIds.Count == 0)
                {
                    Debug.Log("No associations between stop lines and lanes found.");
                }

                // Find stop line pairs
                for (int i = 0; i < stopLineIds.Count; i++)
                {
                    var minDist = float.MaxValue;
                    var center1 = GetStopLineCenterPos(i);
                    var minDistIdx = i;
                    var stopLine = LineId2GameObject[stopLineIds[i]].GetComponent<MapLine>();
                    Vector3 direction1 = stopLine.transform.forward;

                    for (int j = 0; j < stopLineIds.Count; j++)
                    {
                        if (i == j) continue;
                        var center2 = GetStopLineCenterPos(j);
                        var center1Tocenter2Vec = center2 - center1;
                        var dist = center1Tocenter2Vec.magnitude;
                        var direction2 = LineId2GameObject[stopLineIds[j]].GetComponent<MapLine>().transform.forward;
                        var dot = Vector3.Dot(direction1, center1Tocenter2Vec.normalized);
                        if (dist < minDist && (Vector3.Dot(direction1, direction2) < -0.7) && dot > 0) // close and opposite
                        {
                            minDist = dist;
                            minDistIdx = j;
                        }
                    }

                    if (minDistIdx != i)
                    {
                        if (visitedPairs.Contains(Tuple.Create(i, minDistIdx)) || visitedPairs.Contains(Tuple.Create(minDistIdx, i)))
                        {
                            nearestStopLinePairs.Add(new List<int>(){i, minDistIdx});
                        }
                        visitedPairs.Add(Tuple.Create(i, minDistIdx));
                    }
                }

                // find nearest perpendicular pairs
                var intersectionGroups = new List<List<int>>(); // int is index in StopLineIds;
                var visitedStopLineIdxs = new HashSet<int>();
                var mapIntersectionId = 0;
                var visitedNearestPairsId = new HashSet<int>();
                for (int i = 0; i < nearestStopLinePairs.Count; i++)
                {
                    if (visitedNearestPairsId.Contains(i)) continue;
                    visitedNearestPairsId.Add(i);
                    var minDist = float.MaxValue;
                    var minDistIdx = i;
                    var pos1 = GetStopLineCenterPos(nearestStopLinePairs[i][0]); // center position of ith pair's first stop line
                    var pos2 = GetStopLineCenterPos(nearestStopLinePairs[i][1]);
                    var center1 = (pos1 + pos2) / 2; 
                    var direction1 = (pos1 - pos2).normalized;
                    for (int j = i+1; j < nearestStopLinePairs.Count; j++)
                    {
                        var pos3 = GetStopLineCenterPos(nearestStopLinePairs[j][0]);
                        var pos4 = GetStopLineCenterPos(nearestStopLinePairs[j][1]);
                        var center2 = (pos3 + pos4) / 2;
                        var direction2 = (pos3 - pos4).normalized;
                        var dist = (center1 - center2).magnitude;
                        var dotProduct = Vector3.Dot(direction1, direction2);
                        var isIntersect = Utility.LineSegementsIntersect(ToVector2(pos1), ToVector2(pos2), ToVector2(pos3), ToVector2(pos4), out Vector2 intersection);
                        if (isIntersect && dist < minDist && dotProduct > -0.7f && dotProduct < 0.7f)
                        {
                            minDist = dist;
                            minDistIdx = j;
                        }
                    }
                    
                    var firstStopLineId = stopLineIds[nearestStopLinePairs[i][0]];
                    visitedNearestPairsId.Add(minDistIdx);
                    // If perpendicular pair found or if stop sign, we also group even only two stop lines
                    if (minDistIdx != i || LineId2GameObject[firstStopLineId].GetComponent<MapLine>().isStopSign)
                    {
                        List<int> group;
                        if (minDistIdx == i) group = nearestStopLinePairs[i];
                        else group = nearestStopLinePairs[i].Concat(nearestStopLinePairs[minDistIdx]).ToList();
                        
                        if (visitedStopLineIdxs.Contains(group[0])) continue;

                        // Create intersection object
                        var mapIntersectionObj = new GameObject("MapIntersection_" + mapIntersectionId++);
                        var mapIntersection = mapIntersectionObj.AddComponent<MapIntersection>();
                        // Set trigger bounds y for mapIntersection, User still need to adjust x and z manually.
                        mapIntersection.triggerBounds.y = 10;

                        mapIntersectionObj.transform.parent = intersections.transform;
                 
                        var stopLineCenterPositions = new List<Vector3>();
                        foreach (var idx in group)
                        {
                            stopLineCenterPositions.Add(GetStopLineCenterPos(idx));
                        }
                        var mapIntersectionPos = GetAverage(stopLineCenterPositions);

                        // Set parent
                        foreach (var idx in group)
                        {
                            var stopLineId = stopLineIds[idx];
                            SetParent(LineId2GameObject[stopLineId], mapIntersectionObj);

                            // Move all signals/signs related to this stopline under intersection and Update children signals/signs to have correct position
                            foreach (var regId in stopLineId2regIds[stopLineId])
                            {
                                SetParent(regId2regObj[regId], mapIntersectionObj);
                                // Update corresponding mesh
                                if (IsMeshNeeded) SetParent(regId2Mesh[regId], mapIntersectionObj);
                            }

                            visitedStopLineIdxs.Add(idx);
                        }
                        // Update parent object position
                        mapIntersectionObj.transform.position = mapIntersectionPos;
                        // Update children objects's positions
                        foreach (var idx in group)
                        {
                            var stopLineId = stopLineIds[idx];
                            SetParentPos(LineId2GameObject[stopLineId], mapIntersectionObj);

                            // Move all signals/signs related to this stopline under intersection and Update children signals/signs to have correct position
                            foreach (var regId in stopLineId2regIds[stopLineId])
                            {
                                SetParentPos(regId2regObj[regId], mapIntersectionObj);
                                // Update corresponding mesh
                                if (IsMeshNeeded) SetParentPos(regId2Mesh[regId], mapIntersectionObj);
                            }
                        }
                        
                        // Set yield lanes for left turn lanes within intersection 
                        if (minDistIdx != i)
                        {
                            SetYieldLanes(mapIntersection, stopLineIds[nearestStopLinePairs[i][0]], stopLineIds[nearestStopLinePairs[i][1]], false);
                            SetYieldLanes(mapIntersection, stopLineIds[nearestStopLinePairs[minDistIdx][0]], stopLineIds[nearestStopLinePairs[minDistIdx][1]], false);
                        }
                        else
                        {
                            SetYieldLanes(mapIntersection, stopLineIds[nearestStopLinePairs[i][0]], stopLineIds[nearestStopLinePairs[i][1]], true);
                        }
                    }
                }
            }

            return true;
        }

        // Set yiled lanes for left turn lanes and move all intersection lanes under intersection object
        void SetYieldLanes(MapIntersection mapIntersection, long stopLineId, long otherStopLineId, bool setMainRoads)
        {
            Vector2 GetStartingPoint(List<Vector3> positions, Vector3 direction, out Vector2 ePoint)
            {
                Vector3 sPoint;
                if (Vector3.Dot(positions.Last() - positions.First(), direction) > 0)
                {
                    // Same direction
                    sPoint = positions.First();
                    ePoint = ToVector2(positions.Last());
                }
                else
                {
                    sPoint = positions.Last();
                    ePoint = ToVector2(positions.First());
                }

                return ToVector2(sPoint);
            }
            // Create virtual stop line, extend one side's stop line by the length of the other side's stop line's length toward the other side's stop line
            // for example:           |     ->     |   |
            //                    |         ->     |   |

            // Get coordinate for four points
            var virtualStopLineDir = Vector3.Cross(Vector3.up, GetDirectionFromStopLine(stopLineId)).normalized;
            var otherVirtualStopLineDir = Vector3.Cross(Vector3.up, GetDirectionFromStopLine(otherStopLineId)).normalized;
            // Find out starting point for the virtual stop line
            var stopLinePositions = LineId2GameObject[stopLineId].GetComponent<MapLine>().mapWorldPositions;
            var otherStopLinePositions = LineId2GameObject[otherStopLineId].GetComponent<MapLine>().mapWorldPositions;

            Vector2 endingPoint, otherEndingPoint;
            Vector2 startingPoint = GetStartingPoint(stopLinePositions, virtualStopLineDir, out endingPoint);
            Vector2 otherStartingPoint = GetStartingPoint(otherStopLinePositions, otherVirtualStopLineDir, out otherEndingPoint);
            // Use projection to get virtual ending points
            Vector2 GetProjectedPoint(Vector2 A, Vector2 B, Vector2 P)
            {
                var AB = B - A;
                var AP = P - A;
                Vector2 projected = A + Vector2.Dot(AP, AB) / Vector2.Dot(AB, AB) * AB;
                return projected;
            }
            Vector2 virtualEndingPoint = GetProjectedPoint(startingPoint, endingPoint, otherStartingPoint);
            Vector2 otherVirtualEndingPoint = GetProjectedPoint(otherStartingPoint, otherEndingPoint, startingPoint);

            var referencedLanes = StopLineId2laneIds[stopLineId];
            var otherReferencedLanes = StopLineId2laneIds[otherStopLineId];

            if (ShowDebugIntersectionArea)
            {
                // Debug area, if lanes are not included in the rectangle, consider to move away start and end points from each other
                Debug.DrawLine(ToVector3(startingPoint), ToVector3(virtualEndingPoint), Color.red, 60f);
                Debug.DrawLine(ToVector3(startingPoint), ToVector3(otherVirtualEndingPoint), Color.red, 60f);
                Debug.DrawLine(ToVector3(otherStartingPoint), ToVector3(otherVirtualEndingPoint), Color.red, 60f);
                Debug.DrawLine(ToVector3(otherStartingPoint), ToVector3(virtualEndingPoint), Color.red, 60f);
            }
            
            var initialQueue = GetInitialQueueFromReferencedLanes(referencedLanes);
            var otherInitialQueue = GetInitialQueueFromReferencedLanes(otherReferencedLanes);
            // Go through all intersection lanes, update yield lanes for left turn lane
            var intersectionLanes = GetIntersectionLanes(initialQueue, startingPoint, virtualEndingPoint, otherStartingPoint, otherVirtualEndingPoint);
            var otherIntersectionLanes = GetIntersectionLanes(otherInitialQueue, otherStartingPoint, otherVirtualEndingPoint, startingPoint, virtualEndingPoint);

            MoveLanesUnderMapIntersection(mapIntersection, intersectionLanes);
            MoveLanesUnderMapIntersection(mapIntersection, otherIntersectionLanes);

            SetYieldLanesForLeftTurn(intersectionLanes, otherIntersectionLanes);
            SetYieldLanesForLeftTurn(otherIntersectionLanes, intersectionLanes);

            // Set yield lanes for main roads when there are no stop lines on main roads
            if (setMainRoads)
            {
                // Get referencedLanes, otherReferencedLanes for virtual stop line on main road
                // the order of starting and endingPoint should obey actual stop line
                initialQueue = GetInitialQueue(otherVirtualEndingPoint, startingPoint);
                otherInitialQueue = GetInitialQueue(virtualEndingPoint, otherStartingPoint);

                // Go through all intersection lanes, update yield lanes for left turn lane
                intersectionLanes = GetIntersectionLanes(initialQueue, otherVirtualEndingPoint, startingPoint, virtualEndingPoint, otherStartingPoint);
                otherIntersectionLanes = GetIntersectionLanes(otherInitialQueue, virtualEndingPoint, otherStartingPoint, otherVirtualEndingPoint, startingPoint);
            
                MoveLanesUnderMapIntersection(mapIntersection, intersectionLanes);
                MoveLanesUnderMapIntersection(mapIntersection, otherIntersectionLanes);
                
                SetYieldLanesForLeftTurn(intersectionLanes, otherIntersectionLanes);
                SetYieldLanesForLeftTurn(otherIntersectionLanes, intersectionLanes);
            }
        }

        void MoveLanesUnderMapIntersection(MapIntersection mapIntersection, List<long> intersectionLanes)
        {
            foreach (var laneId in intersectionLanes)
            {
                var mapLane = MapLaneId2GameObject[laneId].GetComponent<MapLane>();
                mapLane.transform.parent = mapIntersection.transform;
                ApolloMapImporter.UpdateLocalPositions(mapLane);
            }
        }


        // Get intersecting lanes with given stopline's starting and ending points, return offset starting position
        Queue<Tuple<long, Vector2, Vector2>> GetInitialQueue(Vector2 startingPoint, Vector2 endingPoint)
        {
            var initialQueue = new Queue<Tuple<long, Vector2, Vector2>>();
            var normal = ToVector2(Vector3.Cross(Vector3.up, ToVector3(endingPoint - startingPoint))).normalized;

            foreach (var pair in MapLaneId2GameObject)
            {
                var worldPositions = pair.Value.GetComponent<MapLane>().mapWorldPositions;
                var pFirst = ToVector2(worldPositions[0]);
                var pLast = ToVector2(worldPositions[worldPositions.Count-1]);
                
                // if intersect and have correct direction
                Vector2 intersectPos;
                if (Utility.LineSegementsIntersect(startingPoint, endingPoint, pFirst, pLast, out intersectPos))
                {
                    var laneDir = (pLast - pFirst).normalized;
                    if (Vector2.Dot(laneDir, normal) > 0)
                    {
                        var offset = (pLast - intersectPos).magnitude * 0.1f;
                        if (offset > 1) offset = 1;
                        var offsetStartPos = intersectPos + offset * normal;
                        initialQueue.Enqueue(Tuple.Create(pair.Key, offsetStartPos, pLast));
                    }
                }
            }

            return initialQueue;
        }
        
        // Set yield lanes for left_turn lanes
        void SetYieldLanesForLeftTurn(List<long> intersectionLanes, List<long> otherIntersectionLanes)
        {
            foreach (var laneId in intersectionLanes)
            {
                var mapLane = MapLaneId2GameObject[laneId].GetComponent<MapLane>();
                var positions = mapLane.mapWorldPositions;

                // Update MapLine type to be VIRTUAL
                mapLane.leftLineBoundry.lineType = MapData.LineType.VIRTUAL;
                mapLane.rightLineBoundry.lineType = MapData.LineType.VIRTUAL;

                if (mapLane.laneTurnType == MapData.LaneTurnType.LEFT_TURN)
                {
                    // loop through lanes in otherIntersectionLanes
                    foreach (var otherLaneId in otherIntersectionLanes)
                    {
                        var otherMapLane = MapLaneId2GameObject[otherLaneId].GetComponent<MapLane>();
                        otherMapLane.leftLineBoundry.lineType = MapData.LineType.VIRTUAL;
                        otherMapLane.rightLineBoundry.lineType = MapData.LineType.VIRTUAL;
                        var otherPositions = otherMapLane.mapWorldPositions;
                        if (LineSegementsIntersect(ToVector2(positions.First()), ToVector2(positions.Last()), 
                            ToVector2(otherPositions.First()), ToVector2(otherPositions.Last())))
                        {
                            mapLane.yieldToLanes.Add(otherMapLane);
                        }
                    }
                }
            }
        }

        // Return initial intersection lanes queue based on referencedLanes
        Queue<Tuple<long, Vector2, Vector2>> GetInitialQueueFromReferencedLanes(List<long> referencedLanes)
        {
            Queue<Tuple<long, Vector2, Vector2>> queue = new Queue<Tuple<long, Vector2, Vector2>>(); // <laneId, startPos, endPos>

            // Add initial lanes
            foreach (var laneId in referencedLanes)
            {
                foreach (var followingLaneId in GetFollowingLanes(laneId))
                {
                    // move start position by an offset for initial lanes since some initial lanes intersect with stop line
                    // we want to start with lanes that inside intersection, otherwise, this lane will be classified as U-TURN
                    var positions = MapLaneId2GameObject[followingLaneId].GetComponent<MapLane>().mapWorldPositions;
                    var startPos = positions.First();
                    var endPos = positions.Last();
                    var dir = (endPos - startPos).normalized;
                    var length = (endPos - startPos).magnitude;
                    var offset = 1.0f;
                    if (length < offset)
                    {
                        offset = 0.7f; // Use 0.7m and this should not happen since we should have removed all lanes shorter than 1.0m
                        Debug.Log("We should not have lanes shorter than 1.0m.");
                    }

                    var offsetStartPos = ToVector2(startPos + offset * dir);
                    queue.Enqueue(Tuple.Create(followingLaneId, offsetStartPos, ToVector2(endPos)));
                };                               
            }
            return queue;
        }

        // Find out intersection lanes starting from the stop line, and compute the turn type for them. Using 2D coordinates
        // Use BFS to visit all possible intersection lanes
        List<long> GetIntersectionLanes(Queue<Tuple<long, Vector2, Vector2>> initialQueue, Vector2 startPosStopLine, Vector2 endPosStopLine, Vector2 otherStartPosStopLine, Vector2 otherEndPosStopLine)
        {          
            List<long> intersectionLanes = new List<long>();
            Queue<Tuple<long, Vector2, Vector2>> queue = new Queue<Tuple<long, Vector2, Vector2>>(initialQueue); // <laneId, startPos, endPos>

            // Given a found left turn lane, recursively set all previous lanes within intersection as left turn as well
            void FindPrecedingLanesAndSetLeftTurn(long laneId)
            {
                var firstPosLane = MapLaneId2GameObject[laneId].GetComponent<MapLane>().mapWorldPositions[0];
                foreach (var intersectionLaneId in intersectionLanes)
                {
                    if (laneId == intersectionLaneId) continue;

                    var mapLane = MapLaneId2GameObject[intersectionLaneId].GetComponent<MapLane>();
                    var lastPosIntersectionLane = mapLane.mapWorldPositions.Last();

                    if ((firstPosLane - lastPosIntersectionLane).magnitude < 0.001f)
                    {
                        mapLane.laneTurnType = MapData.LaneTurnType.LEFT_TURN;
                        Debug.Log($"Recursively setting lane {intersectionLaneId} as left turn since its following lane {laneId} is left turn");
                        FindPrecedingLanesAndSetLeftTurn(intersectionLaneId);
                    }
                }
            }

            while (queue.Any())
            {
                var (laneId, startPosLane, endPosLane) = queue.Dequeue();
                var mapLane = MapLaneId2GameObject[laneId].GetComponent<MapLane>();
                Vector2 intersectPoint;
                // For example                                  endPosStopLine   otherStartPosStopLine
                //                        |<---        ->                    |   |
                //               --->|                 ->                    |   |
                //                                                startPosLane   otherEndPosStopLine 
                // Check intersection with four boundary virtual stop lines and update turn type
                // paired virtual stop line

                MapData.LaneTurnType? laneTurnType = null;

                if (Utility.LineSegementsIntersect(startPosLane, endPosLane, otherStartPosStopLine, otherEndPosStopLine, out intersectPoint))
                {
                    // straight
                    laneTurnType = MapData.LaneTurnType.NO_TURN;
                }
                // virtual left stop line
                else if (Utility.LineSegementsIntersect(startPosLane, endPosLane, endPosStopLine, otherStartPosStopLine, out intersectPoint))
                {
                    laneTurnType = MapData.LaneTurnType.LEFT_TURN;
                    FindPrecedingLanesAndSetLeftTurn(laneId);
                }
                // virtual right stop line
                else if (Utility.LineSegementsIntersect(startPosLane, endPosLane, startPosStopLine, otherEndPosStopLine, out intersectPoint))
                {
                    laneTurnType = MapData.LaneTurnType.RIGHT_TURN;
                }
                // virtual self stop line
                else if (Utility.LineSegementsIntersect(startPosLane, endPosLane, startPosStopLine, endPosStopLine, out intersectPoint))
                {
                    laneTurnType = MapData.LaneTurnType.U_TURN;
                    Debug.Log("Please double check U turn lane: " + laneId);
                }
                // no intersect, laneId is within the intersection.
                else
                {
                    // Get following lanes
                    foreach (var followingLaneId in GetFollowingLanes(laneId))
                    {
                        var positions = MapLaneId2GameObject[followingLaneId].GetComponent<MapLane>().mapWorldPositions;
                        var startPos = ToVector2(positions.First());
                        var endPos = ToVector2(positions.Last());
                        queue.Enqueue(Tuple.Create(followingLaneId, startPos, endPos));
                    }
                }

                if (laneTurnType.HasValue)
                {
                    mapLane.laneTurnType = laneTurnType.Value;
                    // For intersected lanes, compute if most of the lane is inside the intersection, do not include if most of the lane is not inside
                    if (((startPosLane - intersectPoint).magnitude / (endPosLane - intersectPoint).magnitude) < 0.4f)
                    {
                        continue;
                    }
                }

                intersectionLanes.Add(laneId);
                if (intersectionLanes.Count > 100)
                {
                    Debug.LogWarning("Experimental feature might went wrong, an intersection should not have more than 100 lanes.");
                    break;
                }
            }
            return intersectionLanes;
        }

        void SetCorrectDirectionForStopLines(List<long> lineIds)
        {
            foreach (long stopLineId in lineIds)
            {
                var stopLine = LineId2GameObject[stopLineId].GetComponent<MapLine>();
                if (stopLine.lineType != MapData.LineType.STOP) continue;

                // Get mapStopLine's intersecting lane direction 
                float minDistLast = float.PositiveInfinity;
                List<long> closestLanesLast = new List<long>(); // closet lane whose last point is the closet to the stop line
                long closestLaneLast = 0;
                foreach (var pair in MapLaneId2GameObject)
                {
                    var worldPositions = pair.Value.GetComponent<MapLane>().mapWorldPositions;
                    var pLast = worldPositions.Last();

                    var d = Utility.SqrDistanceToSegment(stopLine.mapWorldPositions[0], stopLine.mapWorldPositions.Last(), pLast);
                    if (d < 0.001) 
                    {
                        closestLanesLast.Add(pair.Key);
                        break;
                    }
                    if (d < minDistLast)
                    {
                        minDistLast = d;
                        closestLaneLast = pair.Key;
                    }
                }
                closestLanesLast.Add(closestLaneLast);

                var laneId = closestLanesLast[0]; // pick first one
                var positions = MapLaneId2GameObject[laneId].GetComponent<MapLane>().mapWorldPositions;
                var direction = (positions.Last() - positions[positions.Count-2]).normalized; // Use last two points to compute direction.

                // Set direction as the rotation of the stop line
                stopLine.transform.rotation = Quaternion.LookRotation(direction);
                ApolloMapImporter.UpdateLocalPositions(stopLine);
            }
        }

        // Return direction for signal/sign based on given stop line Id
        Vector3 GetDirectionFromStopLine(long stopLineId)
        {
            var stopLine = LineId2GameObject[stopLineId].GetComponent<MapLine>();

            return -stopLine.transform.forward;
        }
        
        // Return following lanes for a given laneId
        List<long> GetFollowingLanes(long laneId)
        {
            var lastPoint = MapLaneId2GameObject[laneId].GetComponent<MapLane>().mapWorldPositions.Last();
            List<long> followingLaneIds = new List<long>();
            foreach (var pair in MapLaneId2GameObject)
            {
                var otherLaneId = pair.Key;
                if (otherLaneId == laneId) continue; 

                var otherFirstPoint = MapLaneId2GameObject[otherLaneId].GetComponent<MapLane>().mapWorldPositions.First();
                if ((lastPoint - otherFirstPoint).magnitude < 0.001)
                {
                    followingLaneIds.Add(otherLaneId);
                }
            }

            return followingLaneIds;
        }

        Vector2 ToVector2(Vector3 pt)
        {
            return new Vector2(pt.x, pt.z);
        }

        Vector3 ToVector3(Vector2 p)
        {
            return new Vector3(p.x, 0f, p.y);
        }

        bool LineSegementsIntersect(Vector2 startPosLane, Vector2 endPosLane, Vector2 otherStartPosStopLine, Vector2 otherEndPosStopLine)
        {
            return Utility.LineSegementsIntersect(startPosLane, endPosLane, otherStartPosStopLine, otherEndPosStopLine, out var dummy);
        }
    }
}
