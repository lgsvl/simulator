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


namespace Simulator.Editor
{
    public class OpenDriveMapImporter
    {
        EditorSettings Settings;
        bool IsMeshNeeded; // Boolean value for traffic light/sign mesh importing.
        bool ShowDebugIntersectionArea = true; // Show debug area for intersection area to find left_turn lanes
        GameObject TrafficLanes;
        GameObject SingleLaneRoads;
        GameObject Intersections;
        MapOrigin MapOrigin;
        OpenDRIVE OpenDRIVEMap;
        GameObject map;


        public void ImportOpenDriveMap(string filePath)
        {
            Settings = EditorSettings.Load();

            
            if (Calculate(filePath))
            {
                Debug.Log("Successfully imported OpenDRIVE Map!");
                Debug.Log("\nNote if your map is incorrect, please check if you have set MapOrigin correctly.");
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
                Debug.LogWarning("Could not find latitude or/and longitude in map header Or not supported projection, mapOrigin is not updated.");
            }

            // Check existence of same name map
            if (GameObject.Find(mapName))
            {
                Debug.LogError("A map with same name exists, cancelling map importing.");
                return false;
            }
            map = new GameObject(mapName);
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

            // create reference lines
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

            GameObject map = new GameObject(mapName);
            var mapHolder = map.AddComponent<MapHolder>();

            // Create TrafficLanes and Intersections under Map
            TrafficLanes = new GameObject("TrafficLanes");
            Intersections = new GameObject("Intersections");
            SingleLaneRoads = new GameObject("SingleLaneRoads");
            TrafficLanes.transform.parent = map.transform;
            Intersections.transform.parent = map.transform;
            SingleLaneRoads.transform.parent = TrafficLanes.transform;

            mapHolder.trafficLanesHolder = TrafficLanes.transform;
            mapHolder.intersectionsHolder = Intersections.transform;

            return true;
        }

        // read map origin, update MapOrigin
        bool CreateOrUpdateMapOrigin(OpenDRIVE apolloMap, MapOrigin mapOrigin)
        {
            var header = apolloMap.header;
            var geoReference = header.projection.proj;
            var items = geoReference.Split('+')
                .Select(s => s.Split('='))
                .Where(s => s.Length > 1)
                .ToDictionary(element => element[0].Trim(), element => element[1].Trim());
            
            if (!items.ContainsKey("proj") || items["proj"] != "utm") return false;

            double latitude, longitude;
            longitude = (header.left + header.right) / 2;
            latitude = (header.top + header.bottom) / 2;

            int zoneNumber;
            if (items.ContainsKey("zone"))
            {
                zoneNumber = int.Parse(items["zone"]);
            }
            else
            {               
                zoneNumber = GetZoneNumberFromLatLon(latitude, longitude);
            }
            
            mapOrigin.UTMZoneId = zoneNumber;
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

        public List<Vector3> calculateLinePoints(OpenDRIVERoadGeometry planView, OpenDRIVERoadElevationProfile elevationProfile)
        {
            OpenDRIVERoadGeometryLine line = planView.Items[0] as OpenDRIVERoadGeometryLine;

            Vector3 origin = new Vector3((float)planView.x, 0f, (float)planView.y);
            List<Vector3> points = new List<Vector3>();

            for (int i = 0; i < planView.length; i++)
            {
                double y = getElevation(planView.s + i, elevationProfile);
                Vector3 pos = new Vector3(i, (float)y, 0f);
                // rotate
                pos = Quaternion.Euler(0f, -(float)(planView.hdg * 180f / Math.PI), 0f) * pos;
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

        public List<Vector3> calculateArcPoints(OpenDRIVERoadGeometry planView, OpenDRIVERoadElevationProfile elevationProfile)
        {
            OpenDRIVERoadGeometryArc arc = planView.Items[0] as OpenDRIVERoadGeometryArc;

            Vector3 origin = new Vector3((float)planView.x, 0f, (float)planView.y);
            List<Vector3> points = new List<Vector3>();
            OdrSpiral.Spiral a = new OdrSpiral.Spiral();

            for (int i = 0; i < planView.length; i++)
            {
                double x = new double();
                double z = new double();
                a.odrArc(i, arc.curvature, ref x, ref z);
                double y = getElevation(planView.s + i, elevationProfile);

                Vector3 pos = new Vector3((float)x, (float)y, (float)z);
                pos = Quaternion.Euler(0f, -(float)(planView.hdg * 180f / Math.PI), 0f) * pos;
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

        public List<Vector3> calculateParamPoly3Points(OpenDRIVERoadGeometry planView, OpenDRIVERoadElevationProfile elevationProfile)
        {
            OpenDRIVERoadGeometryParamPoly3 pPoly3 = planView.Items[0] as OpenDRIVERoadGeometryParamPoly3;
            Vector3 origin = new Vector3((float)planView.x, 0f, (float)planView.y);

            List<Vector3> points = new List<Vector3>();
            double aU = pPoly3.aU;
            double bU = pPoly3.bU;
            double cU = pPoly3.cU;
            double dU = pPoly3.dU;
            double aV = pPoly3.aV;
            double bV = pPoly3.bV;
            double cV = pPoly3.cV;
            double dV = pPoly3.dV;

            double step = 1 / planView.length;
            double p = 0;
            while (p <= 1)
            {
                double x = aU + bU * p + cU * p * p + dU * p * p * p;
                double y = getElevation(planView.s + p * planView.length, elevationProfile);
                double z = aV + bV * p + cV * p * p + dV * p * p * p;

                Vector3 pos = new Vector3((float)x, (float)y, (float)z);
                pos = Quaternion.Euler(0f, -(float)(planView.hdg * 180f / Math.PI), 0f) * pos;
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
            referenceLines.transform.parent = map.transform;

            foreach (var road in OpenDRIVEMap.road)
            {
                var roadLength = road.length;
                var roadId = road.id;
                var link = road.link;
                var elevationProfile = road.elevationProfile;
                var lanes = road.lanes;

                List<Vector3> referenceLinePoints = new List<Vector3>();

                //foreach (var planView in road.planView)
                for (int i = 0; i < road.planView.Count(); i++)
                {
                    var planView = road.planView[i];

                    // Line
                    if (planView.Items[0].GetType() == typeof(OpenDRIVERoadGeometryLine))
                    {
                        List<Vector3> points = calculateLinePoints(planView, elevationProfile);
                        referenceLinePoints.AddRange(points);
                    }

                    // Spiral
                    if (planView.Items[0].GetType() == typeof(OpenDRIVERoadGeometrySpiral))
                    {
                        OpenDRIVERoadGeometrySpiral spi = planView.Items[0] as OpenDRIVERoadGeometrySpiral;

                        if (spi.curvStart == 0)
                        {
                            Vector3 geo = new Vector3((float)planView.x, 0f, (float)planView.y);
                            List<Vector3> points = calculateSpiralPoints(geo, planView.hdg, planView.length, spi.curvStart, spi.curvEnd);
                            referenceLinePoints.AddRange(points);
                        }
                        else
                        {
                            if (i != road.planView.Count() - 1)
                            {
                                var planViewNext = road.planView[i + 1];
                                Vector3 geo = new Vector3((float)planViewNext.x, 0f, (float)planViewNext.y);
                                List<Vector3> points = calculateSpiralPoints(geo, planViewNext.hdg, planView.length, spi.curvStart, spi.curvEnd);
                                points.Reverse();
                                referenceLinePoints.AddRange(points);
                            }
                            else
                            {
                                var tmp = getRoadById(OpenDRIVEMap, Int32.Parse(link.successor.elementId));
                                var planViewNext = tmp.planView[tmp.planView.Length - 1];
                                Vector3 geo = new Vector3((float)planViewNext.x, 0f, (float)planViewNext.y);
                                List<Vector3> points = calculateSpiralPoints(geo, -planViewNext.hdg, planView.length, spi.curvStart, spi.curvEnd);
                                points.Reverse();
                                referenceLinePoints.AddRange(points);
                            }
                        }
                    }

                    // Arc
                    if (planView.Items[0].GetType() == typeof(OpenDRIVERoadGeometryArc))
                    {
                        List<Vector3> points = calculateArcPoints(planView, elevationProfile);
                        referenceLinePoints.AddRange(points);
                    }

                    // Poly3
                    if (planView.Items[0].GetType() == typeof(OpenDRIVERoadGeometryPoly3))
                    {
                        OpenDRIVERoadGeometryPoly3 poly3 = planView.Items[0] as OpenDRIVERoadGeometryPoly3;

                        Vector3 geo = new Vector3((float)planView.x, 0f, (float)planView.y);
                        List<Vector3> points = calculatePoly3Points(geo, planView.hdg, planView.length, poly3);

                        referenceLinePoints.AddRange(points);
                    }

                    // ParamPoly3
                    if (planView.Items[0].GetType() == typeof(OpenDRIVERoadGeometryParamPoly3))
                    {
                        List<Vector3> points = calculateParamPoly3Points(planView, elevationProfile);
                        referenceLinePoints.AddRange(points);
                    }
                }

                GameObject mapLaneObj = new GameObject("road_" + roadId);
                MapLane mapLane = mapLaneObj.AddComponent<MapLane>();
                mapLane.mapWorldPositions = referenceLinePoints;

                if (referenceLinePoints.Count != 0)
                {
                    var laneX = referenceLinePoints[0].x;
                    var laneY = referenceLinePoints[0].y;
                    var laneZ = referenceLinePoints[0].z;

                    mapLaneObj.transform.position = new Vector3(laneX, laneY, laneZ);

                    for (int k = 0; k < referenceLinePoints.Count; k++)
                    {
                        mapLane.mapLocalPositions.Add(mapLaneObj.transform.InverseTransformPoint(referenceLinePoints[k]));
                    }
                    mapLaneObj.transform.parent = referenceLines.transform;
                }

                foreach(var laneSection in lanes.laneSection)
                {

                }
            }
        }
    }
}