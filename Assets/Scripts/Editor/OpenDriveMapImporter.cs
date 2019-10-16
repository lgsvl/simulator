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
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Schemas;
using OdrSpiral;
using Unity.Mathematics;


namespace Simulator.Editor
{
    public class OpenDriveMapImporter
    {
        EditorSettings Settings;
        bool IsMeshNeeded; // Boolean value for traffic light/sign mesh importing.
        static float DownSampleDistanceThreshold; // DownSample distance threshold for points to keep 
        static float DownSampleDeltaThreshold; // For down sampling, delta threshold for curve points 
        bool ShowDebugIntersectionArea = true; // Show debug area for intersection area to find left_turn lanes
        GameObject TrafficLanes;
        GameObject SingleLaneRoads;
        GameObject Intersections;
        MapOrigin MapOrigin;
        OpenDRIVE OpenDRIVEMap;
        GameObject Map;
        Dictionary<string, MapLine> Id2MapLine = new Dictionary<string, MapLine>();

        public OpenDriveMapImporter(float downSampleDistanceThreshold, float downSampleDeltaThreshold, bool isMeshNeeded)
        {
            DownSampleDistanceThreshold = downSampleDistanceThreshold;
            DownSampleDeltaThreshold = downSampleDeltaThreshold;
            IsMeshNeeded = isMeshNeeded;
        }
        public void ImportOpenDriveMap(string filePath)
        {
            Settings = EditorSettings.Load();
            
            if (Calculate(filePath))
            {
                Debug.Log("Successfully imported OpenDRIVE Map!");
                Debug.Log("Note if your map is incorrect, please check if you have set MapOrigin correctly.");
                Debug.LogWarning("!!! You need to adjust the triggerBounds for each MapIntersection.");
            }
            else
            {
                Debug.Log("Failed to import OpenDRIVE map.");
            }
        }

        public bool Calculate(string filePath)
        {
            var serializer = new XmlSerializer(typeof(OpenDRIVE));
            using (XmlTextReader reader = new XmlTextReader(filePath))
            {
                reader.Namespaces = false;

                try
                {   
                    OpenDRIVEMap = (OpenDRIVE)serializer.Deserialize(reader);    
                }
                catch
                {
                    Debug.LogError("Sorry, currently we only support version 1.4, please check your map version.");
                    return false;
                }
            }

            // Create Map object
            var fileName = Path.GetFileName(filePath).Split('.')[0];
            string mapName = "Map" + char.ToUpper(fileName[0]) + fileName.Substring(1);
            if (!CreateMapHolder(filePath, mapName)) return false;

            MapOrigin = MapOrigin.Find(); // get or create a map origin
            if (!CreateOrUpdateMapOrigin(OpenDRIVEMap, MapOrigin))
            {
                Debug.LogWarning("Could not find valid latitude or/and longitude in map header Or not supported projection, mapOrigin is not updated, you need manually update it.");
            }

            CreateReferenceLines();
            
            if (SingleLaneRoads.transform.childCount == 0) UnityEngine.Object.DestroyImmediate(SingleLaneRoads);
            
            return true;
        }

        bool CreateMapHolder(string filePath, string mapName)
        {
            // Create Map object
            var fileName = Path.GetFileName(filePath).Split('.')[0];
            
            // Check existence of same name map
            if (GameObject.Find(mapName)) 
            {
                Debug.LogError("A map with same name exists, cancelling map importing.");
                return false;
            }

            Map = new GameObject(mapName);
            var mapHolder = Map.AddComponent<MapHolder>();

            // Create TrafficLanes and Intersections under Map
            TrafficLanes = new GameObject("TrafficLanes");
            Intersections = new GameObject("Intersections");
            SingleLaneRoads = new GameObject("SingleLaneRoads");
            TrafficLanes.transform.parent = Map.transform;
            Intersections.transform.parent = Map.transform;
            SingleLaneRoads.transform.parent = TrafficLanes.transform;

            mapHolder.trafficLanesHolder = TrafficLanes.transform;
            mapHolder.intersectionsHolder = Intersections.transform;

            return true;
        }

        // read map origin, update MapOrigin
        bool CreateOrUpdateMapOrigin(OpenDRIVE openDRIVEMap, MapOrigin mapOrigin)
        {
            var header = openDRIVEMap.header;
            var geoReference = header.geoReference;
            if (geoReference == null) return false;
            var items = geoReference.Split('+')
                .Select(s => s.Split('='))
                .Where(s => s.Length > 1)
                .ToDictionary(element => element[0].Trim(), element => element[1].Trim());
            
            if (!items.ContainsKey("proj") || items["proj"] != "tmerc" || 
                    items.ContainsKey("lon_0") || items.ContainsKey("lat_0")) return false;

            double latitude, longitude;
            longitude = float.Parse(items["lon_0"]);
            latitude = float.Parse(items["lat_0"]);

            mapOrigin.UTMZoneId = MapOrigin.GetZoneNumberFromLatLon(latitude, longitude);
            double northing, easting;
            mapOrigin.FromLatitudeLongitude(latitude, longitude, out northing, out easting);
            mapOrigin.OriginNorthing = (float)northing;
            mapOrigin.OriginEasting = (float)easting + 500000;

            return true;
        }

        // Calculate elevation at the specific length along the reference path
        public double GetElevation(double l, OpenDRIVERoadElevationProfile elevationProfile)
        {
            double elevation = 0;
            if (elevationProfile.elevation == null)
            {
                return 0;
            }
            else if(elevationProfile.elevation.Length == 1)
            {
                double a = elevationProfile.elevation[0].a;
                double b = elevationProfile.elevation[0].b;
                double c = elevationProfile.elevation[0].c;
                double d = elevationProfile.elevation[0].d;
                double ds = l;
                elevation = a + b * ds + c * ds * ds + d * ds * ds * ds;
                return elevation;
            }
            else
            {
                // decide which elevation profile to be used
                for (int i = 0; i < elevationProfile.elevation.Length; i++)
                {
                    if (i != elevationProfile.elevation.Length - 1)
                    {
                        double s = elevationProfile.elevation[i].s;
                        double sNext = elevationProfile.elevation[i + 1].s;
                        if (l >= s && l < sNext)
                        {
                            double a = elevationProfile.elevation[i].a;
                            double b = elevationProfile.elevation[i].b;
                            double c = elevationProfile.elevation[i].c;
                            double d = elevationProfile.elevation[i].d;
                            double ds = l - s;
                            elevation = a + b * ds + c * ds * ds + d * ds * ds * ds;

                            return elevation;
                        }
                    }
                    else
                    {
                        double s = elevationProfile.elevation[i].s;
                        double a = elevationProfile.elevation[i].a;
                        double b = elevationProfile.elevation[i].b;
                        double c = elevationProfile.elevation[i].c;
                        double d = elevationProfile.elevation[i].d;
                        double ds = l - s;
                        elevation = a + b * ds + c * ds * ds + d * ds * ds * ds;

                        return elevation;
                    }
                }
            }

            return 0;
        }

        public List<Vector3> CalculateLinePoints(OpenDRIVERoadGeometry geometry, OpenDRIVERoadElevationProfile elevationProfile)
        {
            OpenDRIVERoadGeometryLine line = geometry.Items[0] as OpenDRIVERoadGeometryLine;

            Vector3 origin = new Vector3((float)geometry.x, 0f, (float)geometry.y);
            List<Vector3> points = new List<Vector3>();

            for (int i = 0; i < geometry.length; i++)
            {
                double y = GetElevation(geometry.s + i, elevationProfile);
                Vector3 pos = new Vector3(i, (float)y, 0f);
                // rotate
                pos = Quaternion.Euler(0f, -(float)(geometry.hdg * 180f / Math.PI), 0f) * pos;
                points.Add(origin + pos);
            }

            return points;
        }

        public List<Vector3> CalculateSpiralPoints(Vector3 origin, double hdg, double length, double curvStart, double curvEnd)
        {
            List<Vector3> points = new List<Vector3>();
            OdrSpiral.Spiral spiral = new OdrSpiral.Spiral();

            double x_ = new double();
            double y_ = new double();
            double t_ = new double();

            spiral.odrSpiral(length, (curvEnd - curvStart) / length, ref x_, ref y_, ref t_);

            for (int i = 0; i < length; i++)
            {
                double x = new double();
                double y = new double();
                double t = new double();

                if (curvStart == 0)
                {
                    spiral.odrSpiral(i, (curvEnd - curvStart) / length, ref x, ref y, ref t);
                    Vector3 pos = new Vector3((float)x, 0f, (float)y);
                    // rotate
                    pos = Quaternion.Euler(0f, -(float)(hdg * 180f / Math.PI), 0f) * pos;
                    points.Add(origin + pos);
                }

                else
                {
                    spiral.odrSpiral(i, (curvEnd - curvStart) / length, ref x, ref y, ref t);
                    Vector3 pos = new Vector3(-(float)x, 0f, -(float)y);
                    // rotate
                    pos = Quaternion.Euler(0f, -(float)(hdg * 180f / Math.PI), 0f) * pos;
                    points.Add(origin + pos);
                }
            }

            return points;
        }

        public List<Vector3> CalculateArcPoints(OpenDRIVERoadGeometry geometry, OpenDRIVERoadElevationProfile elevationProfile)
        {
            OpenDRIVERoadGeometryArc arc = geometry.Items[0] as OpenDRIVERoadGeometryArc;

            Vector3 origin = new Vector3((float)geometry.x, 0f, (float)geometry.y);
            List<Vector3> points = new List<Vector3>();
            OdrSpiral.Spiral a = new OdrSpiral.Spiral();

            for (int i = 0; i < geometry.length; i++)
            {
                double x = new double();
                double z = new double();
                a.odrArc(i, arc.curvature, ref x, ref z);
                double y = GetElevation(geometry.s + i, elevationProfile);

                Vector3 pos = new Vector3((float)x, (float)y, (float)z);
                pos = Quaternion.Euler(0f, -(float)(geometry.hdg * 180f / Math.PI), 0f) * pos;
                points.Add(origin + pos);
            }
            return points;
        }

        public List<Vector3> CalculatePoly3Points(Vector3 origin, double hdg, double length, OpenDRIVERoadGeometryPoly3 poly3)
        {
            List<Vector3> points = new List<Vector3>();
            double a = poly3.a;
            double b = poly3.b;
            double c = poly3.c;
            double d = poly3.d;

            for (int i = 0; i < length; i++)
            {
                double x = i;
                double y = a + b * i + c * i * i + d * i * i * i;

                Vector3 pos = new Vector3((float)x, 0f, (float)y);
                pos = Quaternion.Euler(0f, -(float)(hdg * 180f / Math.PI), 0f) * pos;
                points.Add(origin + pos);
            }
            return points;
        }

        public List<Vector3> CalculateParamPoly3Points(OpenDRIVERoadGeometry geometry, OpenDRIVERoadElevationProfile elevationProfile)
        {
            OpenDRIVERoadGeometryParamPoly3 pPoly3 = geometry.Items[0] as OpenDRIVERoadGeometryParamPoly3;
            Vector3 origin = new Vector3((float)geometry.x, 0f, (float)geometry.y);

            List<Vector3> points = new List<Vector3>();
            double aU = pPoly3.aU;
            double bU = pPoly3.bU;
            double cU = pPoly3.cU;
            double dU = pPoly3.dU;
            double aV = pPoly3.aV;
            double bV = pPoly3.bV;
            double cV = pPoly3.cV;
            double dV = pPoly3.dV;

            double step = 1 / geometry.length;
            double p = 0;
            while (p <= 1)
            {
                double x = aU + bU * p + cU * p * p + dU * p * p * p;
                double y = GetElevation(geometry.s + p * geometry.length, elevationProfile);
                double z = aV + bV * p + cV * p * p + dV * p * p * p;

                Vector3 pos = new Vector3((float)x, (float)y, (float)z);
                pos = Quaternion.Euler(0f, -(float)(geometry.hdg * 180f / Math.PI), 0f) * pos;
                points.Add(origin + pos);
                p += step;
            }
            return points;
        }

        OpenDRIVERoad GetRoadById(OpenDRIVE map, int id)
        {
            foreach (var road in map.road)
            {
                if (Int32.Parse(road.id) == id)
                {
                    return road;
                }
            }
            return null;
        }

        void CreateReferenceLines()
        {
            GameObject referenceLines = new GameObject("ReferenceLines");
            referenceLines.transform.parent = Map.transform;

            foreach (var road in OpenDRIVEMap.road)
            {
                var roadLength = road.length;
                var roadId = road.id;
                var link = road.link;
                var elevationProfile = road.elevationProfile;
                var lanes = road.lanes;

                List<Vector3> referenceLinePoints = new List<Vector3>();

                for (int i = 0; i < road.planView.Count(); i++)
                {
                    var geometry = road.planView[i];

                    // Line
                    if (geometry.Items[0].GetType() == typeof(OpenDRIVERoadGeometryLine))
                    {
                        List<Vector3> points = CalculateLinePoints(geometry, elevationProfile);
                        referenceLinePoints.AddRange(points);
                    }

                    // Spiral
                    if (geometry.Items[0].GetType() == typeof(OpenDRIVERoadGeometrySpiral))
                    {
                        OpenDRIVERoadGeometrySpiral spi = geometry.Items[0] as OpenDRIVERoadGeometrySpiral;

                        if (spi.curvStart == 0)
                        {
                            Vector3 geo = new Vector3((float)geometry.x, 0f, (float)geometry.y);
                            List<Vector3> points = CalculateSpiralPoints(geo, geometry.hdg, geometry.length, spi.curvStart, spi.curvEnd);
                            referenceLinePoints.AddRange(points);
                        }
                        else
                        {
                            if (i != road.planView.Count() - 1)
                            {
                                var geometryNext = road.planView[i + 1];
                                Vector3 geo = new Vector3((float)geometryNext.x, 0f, (float)geometryNext.y);
                                List<Vector3> points = CalculateSpiralPoints(geo, geometryNext.hdg, geometry.length, spi.curvStart, spi.curvEnd);
                                points.Reverse();
                                referenceLinePoints.AddRange(points);
                            }
                            else
                            {
                                var tmp = GetRoadById(OpenDRIVEMap, Int32.Parse(link.successor.elementId));
                                var geometryNext = tmp.planView[tmp.planView.Length - 1];
                                Vector3 geo = new Vector3((float)geometryNext.x, 0f, (float)geometryNext.y);
                                List<Vector3> points = CalculateSpiralPoints(geo, -geometryNext.hdg, geometry.length, spi.curvStart, spi.curvEnd);
                                points.Reverse();
                                referenceLinePoints.AddRange(points);
                            }
                        }
                    }

                    // Arc
                    if (geometry.Items[0].GetType() == typeof(OpenDRIVERoadGeometryArc))
                    {
                        List<Vector3> points = CalculateArcPoints(geometry, elevationProfile);
                        referenceLinePoints.AddRange(points);
                    }

                    // Poly3
                    if (geometry.Items[0].GetType() == typeof(OpenDRIVERoadGeometryPoly3))
                    {
                        OpenDRIVERoadGeometryPoly3 poly3 = geometry.Items[0] as OpenDRIVERoadGeometryPoly3;

                        Vector3 geo = new Vector3((float)geometry.x, 0f, (float)geometry.y);
                        List<Vector3> points = CalculatePoly3Points(geo, geometry.hdg, geometry.length, poly3);

                        referenceLinePoints.AddRange(points);
                    }

                    // ParamPoly3
                    if (geometry.Items[0].GetType() == typeof(OpenDRIVERoadGeometryParamPoly3))
                    {
                        List<Vector3> points = CalculateParamPoly3Points(geometry, elevationProfile);
                        referenceLinePoints.AddRange(points);
                    }
                }

                GameObject mapLaneObj = new GameObject("road_" + roadId);
                MapLane mapLane = mapLaneObj.AddComponent<MapLane>();
                mapLane.mapWorldPositions = referenceLinePoints;

                if (referenceLinePoints.Count != 0)
                {
                    mapLaneObj.transform.position = Lanelet2MapImporter.GetAverage(referenceLinePoints);

                    for (int k = 0; k < referenceLinePoints.Count; k++)
                    {
                        mapLane.mapLocalPositions.Add(mapLaneObj.transform.InverseTransformPoint(referenceLinePoints[k]));
                    }
                    mapLaneObj.transform.parent = referenceLines.transform;
                }

                // We get reference points with 1 meter resolution and the last point is lost.
                for (int i = 0; i < lanes.laneSection.Count(); i++)
                {
                    var startIdx = (int)lanes.laneSection[i].s;
                    int endIdx;
                    if (i == lanes.laneSection.Count() - 1) endIdx = referenceLinePoints.Count() - 1;
                    else  endIdx = (int)lanes.laneSection[i + 1].s;

                    var laneSectionRefPoints = new List<Vector3>(referenceLinePoints.GetRange(startIdx, endIdx - startIdx + 1));
                    var roadIdLaneSectionId = $"road{roadId}_section{i}";
                    CreateMapLanes(roadIdLaneSectionId, lanes.laneSection[i], laneSectionRefPoints);

                }
            }
        }
        
        void CreateMapLanes(string roadIdLaneSectionId, OpenDRIVERoadLanesLaneSection laneSection, List<Vector3> laneSectionRefPoints)
        {
            MapLine refMapLine = GetRefMapLine(roadIdLaneSectionId, laneSection, laneSectionRefPoints);

            // From left to right, compute other MapLines
            // Get number of lanes and move lane into a new MapLaneSection or SingleLanes
            GameObject parentObj = GetParentObj(roadIdLaneSectionId, laneSection);
            if (laneSection.left != null) CreateLinesLanes(roadIdLaneSectionId, refMapLine, laneSection.left.lane, parentObj, true);
            if (laneSection.right != null) CreateLinesLanes(roadIdLaneSectionId, refMapLine, laneSection.right.lane, parentObj, false);

            refMapLine.transform.parent = parentObj.transform;
            ApolloMapImporter.UpdateLocalPositions(refMapLine);
            // Update parent object position if it is a MapLaneSection
            // Compute center lane from boundary lines
            // Make sure new lanes are connected to its predecessor and successor lanes.
        }

        private GameObject GetParentObj(string roadIdLaneSectionId, OpenDRIVERoadLanesLaneSection laneSection)
        {
            var leftLanes = laneSection.left?.lane;
            var rightLanes = laneSection.right?.lane;
            var numLanes = (leftLanes?.Length ?? 0) + (rightLanes?.Length ?? 0);

            GameObject parentObj;
            if (numLanes > 1)
            {
                parentObj = new GameObject($"MapLaneSection_{roadIdLaneSectionId}");
                parentObj.transform.parent = TrafficLanes.transform;
                parentObj.AddComponent<MapLaneSection>();
            }
            else parentObj = SingleLaneRoads;

            return parentObj;
        }

        private MapLine GetRefMapLine(string roadIdLaneSectionId, OpenDRIVERoadLanesLaneSection laneSection, List<Vector3> laneSectionRefPoints)
        {
            var downSampledRefPointsDouble3 = ApolloMapImporter.DownSample(
                laneSectionRefPoints.Select(x => (double3)(float3)x).ToList(), DownSampleDeltaThreshold, DownSampleDistanceThreshold);
            var downSampledRefPoints = downSampledRefPointsDouble3.Select(x => (Vector3)(float3)x).ToList();
            var refMapLineId = $"MapLine_{roadIdLaneSectionId}_0";
            var refMapLineObj = new GameObject(refMapLineId);
            var refMapLine = refMapLineObj.AddComponent<MapLine>();
            Id2MapLine[refMapLineId] = refMapLine;

            var centerLane = laneSection.center.lane;
            refMapLine.mapWorldPositions = downSampledRefPoints;
            refMapLine.transform.position = Lanelet2MapImporter.GetAverage(downSampledRefPoints);

            // Note: centerLane might have more than one road mark, currently we only use the first one
            var centerLaneRoadMark = centerLane.roadMark[0];
            refMapLine.lineType = GetLineType(centerLaneRoadMark.type1, centerLaneRoadMark.color);
            return refMapLine;
        }

        void CreateLinesLanes(string roadIdLaneSectionId, MapLine refMapLine, lane[] lanes, GameObject parentObj, bool isLeft)
        {
            var centerLinePoints = new List<Vector3>(refMapLine.mapWorldPositions);
            List<Vector3> curLeftBoundaryPoints = centerLinePoints;

            int cur = 0, end = lanes.Length - 1;
            if (isLeft)
            {
                cur = lanes.Length - 1;
                end = 0;
            }

            IEnumerable<lane> it;
            it = isLeft ? lanes.Reverse() : lanes;

            foreach (var curLane in it)
            {
                var id = curLane.id.ToString();

                List<Vector3> curRightBoundaryPoints = CalculateRightBoundaryPoints(curLeftBoundaryPoints, curLane, isLeft);

                var mapLineId = $"MapLine_{roadIdLaneSectionId}_{id}";
                var mapLineObj = new GameObject(mapLineId);
                var mapLine = mapLineObj.AddComponent<MapLine>();
                Id2MapLine[mapLineId] = mapLine;

                var lanePoints = GetLanePoints(curLeftBoundaryPoints, curRightBoundaryPoints);
                if (isLeft) lanePoints.Reverse();
                CreateLane(roadIdLaneSectionId, curLane, lanePoints, parentObj);

                curLeftBoundaryPoints = new List<Vector3>(curRightBoundaryPoints);

                if (isLeft) curRightBoundaryPoints.Reverse();
                mapLine.mapWorldPositions = curRightBoundaryPoints;
                mapLine.transform.position = Lanelet2MapImporter.GetAverage(curRightBoundaryPoints);
                
                mapLine.transform.parent = parentObj.transform;
                ApolloMapImporter.UpdateLocalPositions(mapLine);

                if (curLane.roadMark == null)
                {
                    mapLine.lineType = MapData.LineType.DOTTED_WHITE;
                    continue;
                }
                // Note: lane might have more than one road mark, currently we only use the first one
                var roadMark = curLane.roadMark[0];
                mapLine.lineType = GetLineType(roadMark.type1, roadMark.color);
            }
        }

        private List<Vector3> CalculateRightBoundaryPoints(List<Vector3> curLeftBoundaryPoints, lane curLane, bool isLeft)
        {
            var curRightBoundaryPoints = new List<Vector3>();
            int curWidthIdx = 0;
            float s = 0;
            Debug.Assert(curLane.Items.Length > 0);

            var width = curLane.Items[0] as laneWidth;
            var sOffset = width.sOffset;
            for (int idx = 0; idx < curLeftBoundaryPoints.Count; idx++)
            {
                Vector3 point = curLeftBoundaryPoints[idx];
                if (idx == 0) s = 0;
                else s += (point - curLeftBoundaryPoints[idx - 1]).magnitude;
                if (s > sOffset)
                {
                    while (curWidthIdx < curLane.Items.Length - 1 && s > ((laneWidth)curLane.Items[curWidthIdx + 1]).sOffset)
                    {
                        width = curLane.Items[++curWidthIdx] as laneWidth;
                        sOffset = width.sOffset;
                    }
                }

                var ds = s - sOffset;
                var widthValue = width.a + width.b * ds + width.c * ds * ds + width.d * ds * ds * ds;
                var normalDir = GetNormalDir(curLeftBoundaryPoints, idx, isLeft);

                curRightBoundaryPoints.Add(point + (float)widthValue * normalDir);
            }

            return curRightBoundaryPoints;
        }

        Vector3 GetNormalDir(List<Vector3> points, int index, bool isLeft)
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

            if (isLeft) return -normalDir;
            return normalDir;
        }

        Vector3 GetRightNormalDir(Vector3 p1, Vector3 p2)
        {
            var dir = p2 - p1;
            return Vector3.Cross(Vector3.up, dir).normalized;
        }

        List<Vector3> GetLanePoints(List<Vector3> leftBoundaryPoints, List<Vector3> rightBoundaryPoints)
        {
            float resolution = 5; // 5 meters

            // Get the length of longer boundary line
            float leftLength = Lanelet2MapExporter.RangedLength(leftBoundaryPoints);
            float rightLength = Lanelet2MapExporter.RangedLength(rightBoundaryPoints);
            float longerDistance = (leftLength > rightLength) ? leftLength : rightLength;
            int partitions = (int)Math.Ceiling(longerDistance / resolution);
            if (partitions < 2)
            {
                // For boundary line whose length is less than resolution
                partitions = 2; // Make sure every line has at least 2 partitions.
            }

            float leftResolution = leftLength / partitions;
            float rightResolution = rightLength / partitions;

            leftBoundaryPoints = Lanelet2MapExporter.SplitLine(leftBoundaryPoints, leftResolution, partitions);
            rightBoundaryPoints = Lanelet2MapExporter.SplitLine(rightBoundaryPoints, rightResolution, partitions);
            
            if (leftBoundaryPoints.Count != partitions + 1 || rightBoundaryPoints.Count != partitions + 1)
            {
                Debug.LogError("Something wrong with number of points. (left, right, partitions): (" + 
                    leftBoundaryPoints.Count + ", " + rightBoundaryPoints.Count + ", " + partitions);
                return new List<Vector3>();
            }

            List<Vector3> lanePoints = new List<Vector3>();
            for (int i = 0; i < partitions + 1; i++)
            {
                Vector3 centerPoint = (leftBoundaryPoints[i] + rightBoundaryPoints[i]) / 2;
                lanePoints.Add(centerPoint);
            }

            return lanePoints;
        }

        void CreateLane(string roadIdLaneSectionId, lane curLane, List<Vector3> lanePoints, GameObject parentObj)
        {
            var mapLaneObj = new GameObject($"MapLane_{roadIdLaneSectionId}_{curLane.id}");
            var mapLane = mapLaneObj.AddComponent<MapLane>();
            mapLane.mapWorldPositions = lanePoints;
            mapLane.transform.parent = parentObj.transform;
            ApolloMapImporter.UpdateLocalPositions(mapLane);
            
            var mapLine = Id2MapLine[$"MapLine_{roadIdLaneSectionId}_{curLane.id}"];
            mapLane.rightLineBoundry = mapLine;
            mapLane.rightBoundType = GetBoundTypeFromMapLineType(mapLine.lineType);
            // Left lanes
            if (curLane.id > 0)
            {
                mapLine = Id2MapLine[$"MapLine_{roadIdLaneSectionId}_{curLane.id - 1}"];

            }
            // Right lanes
            else
            {                
                mapLine = Id2MapLine[$"MapLine_{roadIdLaneSectionId}_{curLane.id + 1}"];
            }
            mapLane.leftLineBoundry = mapLine;
            mapLane.leftBoundType = GetBoundTypeFromMapLineType(mapLine.lineType);
        }

        static MapData.LaneBoundaryType GetBoundTypeFromMapLineType(MapData.LineType type)
        {
            MapData.LaneBoundaryType boundType = MapData.LaneBoundaryType.UNKNOWN;
            if (type == MapData.LineType.UNKNOWN) boundType = MapData.LaneBoundaryType.UNKNOWN;
            else if (type == MapData.LineType.DOTTED_YELLOW) boundType = MapData.LaneBoundaryType.DOTTED_YELLOW;
            else if (type == MapData.LineType.DOTTED_WHITE) boundType = MapData.LaneBoundaryType.DOTTED_WHITE;
            else if (type == MapData.LineType.SOLID_YELLOW) boundType = MapData.LaneBoundaryType.SOLID_YELLOW;
            else if (type == MapData.LineType.SOLID_WHITE) boundType = MapData.LaneBoundaryType.SOLID_WHITE;
            else if (type == MapData.LineType.DOUBLE_YELLOW) boundType = MapData.LaneBoundaryType.DOUBLE_YELLOW;
            else if (type == MapData.LineType.CURB) boundType = MapData.LaneBoundaryType.CURB;

            return boundType;
        }

        MapData.LineType GetLineType(roadmarkType roadMarkType, color roadMarkColor)
        {
            if (roadMarkType == roadmarkType.solid)
            {
                if (roadMarkColor == color.white || roadMarkColor == color.standard) return MapData.LineType.SOLID_WHITE;
                else if (roadMarkColor == color.yellow) return MapData.LineType.SOLID_YELLOW;
            } 
            else if (roadMarkType == roadmarkType.broken)
            {
                if (roadMarkColor == color.white || roadMarkColor == color.standard) return MapData.LineType.DOTTED_WHITE;
                else if (roadMarkColor == color.yellow) return MapData.LineType.DOTTED_YELLOW;
            }
            else if (roadMarkType == roadmarkType.solidsolid)
            {
                if (roadMarkColor == color.white || roadMarkColor == color.standard) return MapData.LineType.DOUBLE_WHITE;
                else if (roadMarkColor == color.yellow) return MapData.LineType.DOUBLE_YELLOW;
            }
            else if (roadMarkType == roadmarkType.curb) return MapData.LineType.CURB;
            else if (roadMarkType == roadmarkType.none) return MapData.LineType.VIRTUAL;

            Debug.LogWarning("Not supported road mark and color, using default dotted white line type.");
            return MapData.LineType.DOTTED_WHITE;
        }
    }
}