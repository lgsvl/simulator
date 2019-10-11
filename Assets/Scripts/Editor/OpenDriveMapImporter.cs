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

            createReferenceLines();

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
            
            if (!items.ContainsKey("proj") || items["proj"] != "tmerc") return false;

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
        public double getElevation(double l, OpenDRIVERoadElevationProfile elevationProfile)
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

        public List<Vector3> calculateLinePoints(OpenDRIVERoadGeometry geometry, OpenDRIVERoadElevationProfile elevationProfile)
        {
            OpenDRIVERoadGeometryLine line = geometry.Items[0] as OpenDRIVERoadGeometryLine;

            Vector3 origin = new Vector3((float)geometry.x, 0f, (float)geometry.y);
            List<Vector3> points = new List<Vector3>();

            for (int i = 0; i < geometry.length; i++)
            {
                double y = getElevation(geometry.s + i, elevationProfile);
                Vector3 pos = new Vector3(i, (float)y, 0f);
                // rotate
                pos = Quaternion.Euler(0f, -(float)(geometry.hdg * 180f / Math.PI), 0f) * pos;
                points.Add(origin + pos);
            }

            return points;
        }

        public List<Vector3> calculateSpiralPoints(Vector3 origin, double hdg, double length, double curvStart, double curvEnd)
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

        public List<Vector3> calculateArcPoints(OpenDRIVERoadGeometry geometry, OpenDRIVERoadElevationProfile elevationProfile)
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
                double y = getElevation(geometry.s + i, elevationProfile);

                Vector3 pos = new Vector3((float)x, (float)y, (float)z);
                pos = Quaternion.Euler(0f, -(float)(geometry.hdg * 180f / Math.PI), 0f) * pos;
                points.Add(origin + pos);
            }
            return points;
        }

        public List<Vector3> calculatePoly3Points(Vector3 origin, double hdg, double length, OpenDRIVERoadGeometryPoly3 poly3)
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

        public List<Vector3> calculateParamPoly3Points(OpenDRIVERoadGeometry geometry, OpenDRIVERoadElevationProfile elevationProfile)
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
                double y = getElevation(geometry.s + p * geometry.length, elevationProfile);
                double z = aV + bV * p + cV * p * p + dV * p * p * p;

                Vector3 pos = new Vector3((float)x, (float)y, (float)z);
                pos = Quaternion.Euler(0f, -(float)(geometry.hdg * 180f / Math.PI), 0f) * pos;
                points.Add(origin + pos);
                p += step;
            }
            return points;
        }

        OpenDRIVERoad getRoadById(OpenDRIVE map, int id)
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

        void createReferenceLines()
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
                        List<Vector3> points = calculateLinePoints(geometry, elevationProfile);
                        referenceLinePoints.AddRange(points);
                    }

                    // Spiral
                    if (geometry.Items[0].GetType() == typeof(OpenDRIVERoadGeometrySpiral))
                    {
                        OpenDRIVERoadGeometrySpiral spi = geometry.Items[0] as OpenDRIVERoadGeometrySpiral;

                        if (spi.curvStart == 0)
                        {
                            Vector3 geo = new Vector3((float)geometry.x, 0f, (float)geometry.y);
                            List<Vector3> points = calculateSpiralPoints(geo, geometry.hdg, geometry.length, spi.curvStart, spi.curvEnd);
                            referenceLinePoints.AddRange(points);
                        }
                        else
                        {
                            if (i != road.planView.Count() - 1)
                            {
                                var geometryNext = road.planView[i + 1];
                                Vector3 geo = new Vector3((float)geometryNext.x, 0f, (float)geometryNext.y);
                                List<Vector3> points = calculateSpiralPoints(geo, geometryNext.hdg, geometry.length, spi.curvStart, spi.curvEnd);
                                points.Reverse();
                                referenceLinePoints.AddRange(points);
                            }
                            else
                            {
                                var tmp = getRoadById(OpenDRIVEMap, Int32.Parse(link.successor.elementId));
                                var geometryNext = tmp.planView[tmp.planView.Length - 1];
                                Vector3 geo = new Vector3((float)geometryNext.x, 0f, (float)geometryNext.y);
                                List<Vector3> points = calculateSpiralPoints(geo, -geometryNext.hdg, geometry.length, spi.curvStart, spi.curvEnd);
                                points.Reverse();
                                referenceLinePoints.AddRange(points);
                            }
                        }
                    }

                    // Arc
                    if (geometry.Items[0].GetType() == typeof(OpenDRIVERoadGeometryArc))
                    {
                        List<Vector3> points = calculateArcPoints(geometry, elevationProfile);
                        referenceLinePoints.AddRange(points);
                    }

                    // Poly3
                    if (geometry.Items[0].GetType() == typeof(OpenDRIVERoadGeometryPoly3))
                    {
                        OpenDRIVERoadGeometryPoly3 poly3 = geometry.Items[0] as OpenDRIVERoadGeometryPoly3;

                        Vector3 geo = new Vector3((float)geometry.x, 0f, (float)geometry.y);
                        List<Vector3> points = calculatePoly3Points(geo, geometry.hdg, geometry.length, poly3);

                        referenceLinePoints.AddRange(points);
                    }

                    // ParamPoly3
                    if (geometry.Items[0].GetType() == typeof(OpenDRIVERoadGeometryParamPoly3))
                    {
                        List<Vector3> points = calculateParamPoly3Points(geometry, elevationProfile);
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
                    CreateMapLaneSection(i, laneSectionRefPoints);

                }
            }
        }
        
        void CreateMapLaneSection(int laneSectionIdx, List<Vector3> laneSectionRefPoints)
        {
            var downSampledRefPointsDouble3 = ApolloMapImporter.DownSample(
                laneSectionRefPoints.Select(x => (double3)(float3)x).ToList(), DownSampleDeltaThreshold, DownSampleDistanceThreshold);
            var downSampledRefPoints = downSampledRefPointsDouble3.Select(x => (Vector3)(float3)x).ToList();
            var refMapLineObj = new GameObject($"refMapLine_{laneSectionIdx}_0");
            var refMapLine = refMapLineObj.AddComponent<MapLine>();
            refMapLine.mapWorldPositions = downSampledRefPoints;
            refMapLine.transform.position = Lanelet2MapImporter.GetAverage(downSampledRefPoints);
            ApolloMapImporter.UpdateLocalPositions(refMapLine);
            
            // Downsample to a MapLine
            // From left to right, compute other MapLines
            // Compute center lane from boundary lines
            // Make sure new lanes are connected to its predecessor and successor lanes.
        }
    }
}