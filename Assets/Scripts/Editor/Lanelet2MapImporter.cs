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
using Simulator.Utilities;

namespace Simulator.Editor
{
    public class LaneLet2MapImporter
    {
        MapOrigin MapOrigin;

        float laneLengthThreshold = 1.0f; // Imported lanes shorter than this threshold will be merged into connecting lanes.
        Dictionary<string, OsmGeo> dataSource = new Dictionary<string, OsmGeo>();

        public void ImportLanelet2Map(string filePath)
        {
            if (ImportLanelet2MapCalculate(filePath))
            {
                Debug.Log("Successfully imported Lanelet2 HD Map!\nNote if your map is incorrect, please check if you have set MapOrigin correctly.");
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
            long[] nodeIds = ((Way)dataSource["Way"+lineStringId]).Nodes;
            for (int i = 1; i < nodeIds.Length; i ++)
            {
                var lastNode = (Node)dataSource["Node" + nodeIds[last]];
                var curNode = (Node)dataSource["Node" + nodeIds[i]];
                Vector3 lastPoint = GetVector3FromNode(lastNode); 
                Vector3 curPoint = GetVector3FromNode(curNode); 
                len += Vector3.Distance(lastPoint, curPoint);
                last = i;
            }
            
            return len;
        }

        void SplitLine(long lineStringId, out List<Vector3> splittedLinePoints, float resolution, int partitions, bool reverse=false)
        {
            long[] nodeIds = ((Way)dataSource["Way" + lineStringId]).Nodes; 
            splittedLinePoints = new List<Vector3>();
            splittedLinePoints.Add(GetVector3FromNode((Node)dataSource["Node" + nodeIds[0]])); // Add first point

            float residue = 0; // Residual length from previous segment

            int last = 0;
            // loop through each segment in boundry line
            for (int i = 1; i < nodeIds.Length; i++)
            {
                if (splittedLinePoints.Count >= partitions) break;

                Vector3 lastPoint = GetVector3FromNode((Node)dataSource["Node" + nodeIds[last]]);
                Vector3 curPoint = GetVector3FromNode((Node)dataSource["Node" + nodeIds[i]]);

                // Continue if no points are made within current segment
                float segmentLength = Vector3.Distance(lastPoint, curPoint);
                if (segmentLength + residue < resolution)
                {
                    residue += segmentLength;
                    last = i;
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
                last = i;
            }

            splittedLinePoints.Add(GetVector3FromNode((Node)dataSource["Node" + nodeIds[nodeIds.Length-1]]));

            if (reverse)
            {
                splittedLinePoints.Reverse();
            }
        }

        // Referenced from https://gitlab.com/mitsudome-r/utilities/blob/feature/vector_map_converter/vector_map_converter/src/lanelet2autowaremap_core.cpp#L305
        List<Vector3> ComputerCenterLine(long leftLineStringId, long rightLineStringId)
        {
            // Check the directions of two boundry lines
            //    if they are not same, reverse one and get a temp centerline. Compare centerline with left line, determine direction of the centerlane
            //    if they are same, compute centerline.
            var sameDirection = true;
            var leftNodeIds = ((Way)dataSource["Way" + leftLineStringId]).Nodes;
            var rightNodeIds = ((Way)dataSource["Way" + rightLineStringId]).Nodes;
            var leftFirstPoint = GetVector3FromNode((Node)dataSource["Node" + leftNodeIds[0]]);
            var leftLastPoint = GetVector3FromNode((Node)dataSource["Node" + leftNodeIds[leftNodeIds.Length-1]]);
            var rightFirstPoint = GetVector3FromNode((Node)dataSource["Node" + rightNodeIds[0]]);
            var rightLastPoint = GetVector3FromNode((Node)dataSource["Node" + rightNodeIds[rightNodeIds.Length-1]]);
            var leftDirection = (leftLastPoint - leftFirstPoint).normalized;
            var rightDirection = (rightLastPoint - rightFirstPoint).normalized;

            if (Vector3.Dot(leftDirection, rightDirection) < 0)
            {
                sameDirection = false;
            }

            float resolution = 10; // 10 meters
            List<Vector3> centerLinePoints = new List<Vector3>();
            List<Vector3> leftLinePoints = new List<Vector3>();
            List<Vector3> rightLinePoints = new List<Vector3>();
            
            // Get the length of longer boundary line
            float leftLength = RangedLength(leftLineStringId);
            float rightLength = RangedLength(rightLineStringId);
            float longerDistance = (leftLength > rightLength) ? leftLength : rightLength;
            int partitions = (int)Math.Ceiling(longerDistance / resolution);
            if (partitions < 2)
            {
                // For lineStrings whose length is less than resolution
                partitions = 2; // Make sure every line has at least 2 partitions.
            }
             
            float leftResolution = leftLength / partitions;
            float rightResolution = rightLength / partitions;

            SplitLine(leftLineStringId, out leftLinePoints, leftResolution, partitions);
            // If left and right lines have opposite direction, reverse right line
            if (!sameDirection) SplitLine(rightLineStringId, out rightLinePoints, rightResolution, partitions, true);
            else SplitLine(rightLineStringId, out rightLinePoints, rightResolution, partitions);

            if (leftLinePoints.Count != partitions + 1 || rightLinePoints.Count != partitions + 1)
            {
                Debug.LogError("Something wrong with number of points. (left, right, partitions): (" + leftLinePoints.Count + ", " + rightLinePoints.Count + ", " + partitions);
                return new List<Vector3>();
            }

            for (int i = 0; i < partitions+1; i ++)
            {
                Vector3 centerPoint = (leftLinePoints[i] + rightLinePoints[i]) / 2;
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

        Vector3 GetAverage(List<Vector3> vectors)
        {
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
            var ids = ((Way)dataSource["Way" + id]).Nodes;
            var positions = new List<Vector3>();
            foreach (var nodeId in ids)
            {
                var node = (Node)dataSource["Node" + nodeId];
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

            return mapLineObj;
        }

        public bool ImportLanelet2MapCalculate(string filePath)
        {
            MapOrigin = MapOrigin.Find();
            if (MapOrigin == null)
            {
                return false;
            }

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

                var lineId2GameObject = new Dictionary<long, GameObject>(); // We use id from lineString as lineId

                // Add elements first since some elements appear later after it is referenced.
                foreach (var element in filtered)
                {
                    if (element.Type == OsmGeoType.Node || element.Type == OsmGeoType.Way
                        || element.Type == OsmGeoType.Relation)
                    {
                        dataSource.Add(element.Type.ToString() + element.Id, element);                        
                    }
                }

                var shortLanesId = new List<long>(); // ids of lanes whose length is less than threshold
                var lineId2FirstLastNodeIds = new Dictionary<long, List<long>>(); // Dictionary to store 1st and last node id for every lineId.
                var lineId2LaneIdList = new Dictionary<long, List<long>>(); // Note: MapLine might have opposite direction with the corresponding MapLane.
                var tempLaneSectionsLaneIds = new List<List<long>>(); // List of pairs of MapLane IDs obtained based on shared MapLine
                var mapLaneId2GameObject = new Dictionary<long, GameObject>(); // We use id from Relation as mapLaneId
                var regulatorElementIds = new List<long>();
                var laneId2RegulatoryElementIds = new Dictionary<long, List<long>>(); // We need to connect laneId with corresponding traffic light
                foreach (var element in filtered)
                {
                    // Get StopLine
                    if (element.Type == OsmGeoType.Way && element.Tags != null && element.Tags.Contains("type", "stop_line"))
                    {
                        var mapLineObj = CreateMapLine(element.Id.Value);
                        mapLineObj.GetComponent<MapLine>().lineType = MapData.LineType.STOP;
                        mapLineObj.name = "MapStopLine_" + element.Id;
                        mapLineObj.transform.parent = intersections.transform;
                        lineId2GameObject.Add(element.Id.Value, mapLineObj);
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
                                    var ids = ((Way)dataSource["Way" + member.Id]).Nodes;
                                    lineId2FirstLastNodeIds.Add(member.Id, new List<long>(){ids[0], ids[ids.Length-1]});
                                }
                                                     
                                if (!lineId2GameObject.ContainsKey(member.Id))
                                {
                                    // Create MapLine
                                    var mapLineObj = CreateMapLine(member.Id);
                                    mapLineObj.transform.parent = boundryLines.transform;
                                    lineId2GameObject.Add(member.Id, mapLineObj);
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
                                var regulatorElement = (Relation)dataSource[member.Type.ToString() + member.Id];
                                if (regulatorElement.Tags.GetValue("subtype") == "speed_limit")
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
                                else if (regulatorElement.Tags.GetValue("subtype") == "traffic_light")
                                {
                                    if (laneId2RegulatoryElementIds.ContainsKey(element.Id.Value))
                                    {
                                        laneId2RegulatoryElementIds[element.Id.Value].Add(member.Id);
                                    }
                                    else
                                    {
                                        laneId2RegulatoryElementIds[element.Id.Value] = new List<long>();
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
                        List<Vector3> centerLinePoints = ComputerCenterLine(leftLineStringId, rightLineStringId);

                        // Ignore lanes with three points and shorter than 1 meter
                        if ((centerLinePoints[centerLinePoints.Count-1] - centerLinePoints[0]).magnitude < laneLengthThreshold)
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
                        mapLane.leftLineBoundry = lineId2GameObject[leftLineStringId].GetComponent<MapLine>();
                        mapLane.rightLineBoundry = lineId2GameObject[rightLineStringId].GetComponent<MapLine>();
                        
                        mapLaneId2GameObject[element.Id.Value] = mapLaneObj;

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
                        lanePositions.Add(mapLaneId2GameObject[id].transform.position);
                        mapLaneId2GameObject[id].transform.parent = mapLaneSectionObj.transform;
                    }

                    mapLaneSectionObj.transform.position = GetAverage(lanePositions);
                    mapLaneSectionId2Object[laneSectionId-1] = mapLaneSectionObj;
                    mapLaneSectionId2laneIds[laneSectionId-1] = laneSectionLaneIds;

                    // Update children maplanes to have correct position
                    foreach (var id in laneSectionLaneIds)
                    {
                        MapLane tempLane = mapLaneId2GameObject[id].GetComponent<MapLane>();
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
                        var mapLine = lineId2GameObject[lineStringId].GetComponent<MapLine>();
                        var worldPositions = mapLine.mapWorldPositions;
                        worldPositions[worldPositions.Count-1] = lineId2GameObject[boundaryLineStringId].GetComponent<MapLine>().mapWorldPositions.Last();
                        
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
                    var mapLane = mapLaneId2GameObject[laneId].GetComponent<MapLane>();
                    long leftLineStringId = long.Parse(mapLane.leftLineBoundry.name.Split('_')[1]); // Get lineString id from mapLine name
                    long rightLineStringId = long.Parse(mapLane.rightLineBoundry.name.Split('_')[1]); 

                    var possiblePreLanesSetLeft = ConnectAndReturn(leftLineStringId);
                    var possiblePreLanesSetRight = ConnectAndReturn(rightLineStringId);
                    linesToDestroy.Add(leftLineStringId);
                    linesToDestroy.Add(rightLineStringId);


                    // BruteForce to find preceding lanes and following lanes
                    var worldPositions = mapLane.mapWorldPositions;
                    foreach (var otherLaneId in mapLaneId2GameObject.Keys)
                    {
                        if (laneId == otherLaneId) continue;
                        var otherMapLane = mapLaneId2GameObject[otherLaneId].GetComponent<MapLane>();
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
                    GameObject.DestroyImmediate(lineId2GameObject[lineId]);
                    lineId2GameObject.Remove(lineId);
                }
                foreach (var laneId in lanesToDestroy)
                {
                    GameObject.DestroyImmediate(mapLaneId2GameObject[laneId]);
                    mapLaneId2GameObject.Remove(laneId);
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


                // Import Intersections
                var vistedRegulatoryElementIds = new HashSet<long>();
                var signalId2StopLineId = new Dictionary<long, long>();
                foreach (var regId in regulatorElementIds)
                {
                    var relation = ((Relation)dataSource["Relation"+regId]);
                    var tags = relation.Tags;
                    // Debug.Log(tags.GetValue("type"));
                    // Debug.Log(tags.GetValue("subtype"));
                    // Debug.Log("");
                    
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

                                var mapSignal = mapSignalObj.AddComponent<MapSignal>();
                                
                                // Get position of the signal object
                                var way = (Way)dataSource["Way" + member.Id];
                                var nodes = way.Nodes;
                                Vector3 signalPos;
                                if (nodes.Length == 3)
                                {
                                    // if lanelet2, use middle point
                                    var node = (Node)dataSource["Node" + nodes[1]];
                                    signalPos = GetVector3FromNode(node);
                                }
                                else if (nodes.Length == 2)
                                {
                                    // if lanelet2 Autoware extension, compute based on bottom line and height.
                                    var node1 = (Node)dataSource["Node" + nodes[0]];
                                    var node2 = (Node)dataSource["Node" + nodes[1]];
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
                                    signalPos.y = 4.0f;
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
                                Debug.Log(member.Id);
                                stopLineId = member.Id;
                            }
                        }

                        if (stopLineId == long.MaxValue)
                        {
                            Debug.LogError("Error, traffic light " + relation.Id + " has no related stop line!");
                            return false;
                            // TODO: stop line is optional, if not given, we should hint a stop line by the end of the lanelet.
                        }
                        var stopLine = lineId2GameObject[stopLineId].GetComponent<MapLine>();

                        // Get mapStopLine's intersecting lane direction 
                        float minDistFirst = float.PositiveInfinity;
                        float minDistLast = float.PositiveInfinity;
                        List<long> closestLanesFirst = new List<long>(); // closet lane whose first point is the closet to the stop line
                        List<long> closestLanesLast = new List<long>(); // closet lane whose last point is the closet to the stop line
                        long closestLaneFirst = 0;
                        long closestLaneLast = 0;
                        foreach (var pair in mapLaneId2GameObject)
                        {
                            var worldPositions = pair.Value.GetComponent<MapLane>().mapWorldPositions;
                            var pFirst = worldPositions[0];
                            var pLast = worldPositions[worldPositions.Count-1];

                            float d = Utility.SqrDistanceToSegment(stopLine.mapWorldPositions[0], stopLine.mapWorldPositions.Last(), pFirst);
                            if (d < 0.001) closestLanesFirst.Add(pair.Key);
                            if (d < minDistFirst)
                            {
                                minDistFirst = d;
                                closestLaneFirst = pair.Key;
                            }

                            d = Utility.SqrDistanceToSegment(stopLine.mapWorldPositions[0], stopLine.mapWorldPositions.Last(), pLast);
                            if (d < 0.001) closestLanesLast.Add(pair.Key);
                            if (d < minDistLast)
                            {
                                minDistLast = d;
                                closestLaneLast = pair.Key;
                            }
                        }
                        closestLanesLast.Add(closestLaneLast);
                        closestLanesFirst.Add(closestLaneFirst);

                        Debug.Log("                                      stopline:      " + stopLineId); /////////////////
                        Vector3 signalDirection = Vector3.zero;
                        foreach (var laneId1 in closestLanesLast)
                        {
                            var positions1 = mapLaneId2GameObject[laneId1].GetComponent<MapLane>().mapWorldPositions;
                            var pos1 = positions1.Last();
                            foreach (var laneId2 in closestLanesFirst)
                            {
                                var positions2 = mapLaneId2GameObject[laneId2].GetComponent<MapLane>().mapWorldPositions;
                                var pos2 = positions2.First();
                                if ((pos1 - pos2).magnitude < 0.01)
                                {
                                    signalDirection = positions1[positions1.Count-2] - positions2[1]; // Use nearest two points to compute direction.
                                }
                            }
                        }

                        // TODO Compare signalDirection with the normal direction of the stop line, use the normal of the stop line.
                        var tempDir = stopLine.mapLocalPositions.Last() - stopLine.mapLocalPositions.First();
                        var perp = Vector3.Cross(tempDir, Vector3.up);
                        if (Vector3.Dot(signalDirection, perp) < 0f)
                        {
                            signalDirection = -perp;
                        }
                        else
                        {
                            signalDirection = perp;
                        }

                        if (signalDirection == Vector3.zero)
                        {
                            Debug.LogError("No closest lane found!!!");
                        }

                        // Set all signals to have correct stop line.
                        foreach (var mapSignal in mapSignals)
                        {
                            mapSignal.stopLine = stopLine;
                            signalId2StopLineId[regId] = stopLineId;
                            mapSignal.transform.rotation = Quaternion.LookRotation(signalDirection);
                        }

                        
                    }
                }

                // TO test Set lanes to have correct stop line
                foreach (var pair in laneId2RegulatoryElementIds)
                {
                    Debug.Log(" lane Id is " + pair.Key);
                    var laneId = pair.Key;
                    var regulatoryElementIds = pair.Value;
                    foreach (var signalId in regulatoryElementIds)
                    {
                        mapLaneId2GameObject[laneId].GetComponent<MapLane>().stopLine = lineId2GameObject[signalId2StopLineId[signalId]].GetComponent<MapLine>();
                        Debug.Log("~~~~~~~~~~~~~~~~ lane " + laneId + "   stopLine" + lineId2GameObject[signalId2StopLineId[signalId]].name);
                        break; // Assume the related signals to one lane are having a same stop line.
                    }
                }
            }

            return true;
        }
    }
}