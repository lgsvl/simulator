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
using System.Xml;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Schemas;
using Unity.Mathematics;
using Utility = Simulator.Utilities.Utility;
using UnityEditor.SceneManagement;

namespace Simulator.Editor
{
    public class OpenDriveMapImporter
    {
        EditorSettings Settings;
        double GeometrySkipLength = 0.01;
        double LaneSectionSkipLength = 0.5; // skip importing such lane sections
        float ConnectLaneThreshold = 5.0f; // distance threshold to connect lanes based on link info
        bool IsCreateStopLines = true;
        bool IsMeshNeeded; // Boolean value for traffic light/sign mesh importing.
        bool IsConnectLanes; // Boolean value for whether to connect lanes based on links in OpenDRIVE.
        static float DownSampleDistanceThreshold; // DownSample distance threshold for points to keep
        static float DownSampleDeltaThreshold; // For down sampling, delta threshold for curve points
        float SignalHeight = 7; // Height for imported signals.
        GameObject TrafficLanes;
        GameObject SingleLaneRoads;
        GameObject Intersections;
        MapOrigin MapOrigin;
        OpenDRIVE OpenDRIVEMap;
        GameObject Map;
        MapHolder MapHolder;
        MapManagerData MapAnnotationData;
        Dictionary<string, MapLine> Id2MapLine = new Dictionary<string, MapLine>();
        Dictionary<string, List<string>> Id2PredecessorIds = new Dictionary<string, List<string>>();
        Dictionary<string, List<string>> Id2SuccessorIds = new Dictionary<string, List<string>>();
        Dictionary<string, List<Dictionary<int, MapLane>>> Roads = new Dictionary<string, List<Dictionary<int, MapLane>>>(); // roadId: laneSectionId: laneId
        Dictionary<MapLane, BeforesAfters> Lane2BeforesAfters = new Dictionary<MapLane, BeforesAfters>();
        Dictionary<MapLane, laneType> Lane2LaneType = new Dictionary<MapLane, laneType>();
        Dictionary<string, MapIntersection> Id2MapIntersection = new Dictionary<string, MapIntersection>();
        Dictionary<string, string> RoadId2IntersectionId = new Dictionary<string, string>();
        Dictionary<string, List<int>> RoadId2laneSections = new Dictionary<string, List<int>>(); // Store laneSections to remove
        Dictionary<string, float> RoadId2Speed = new Dictionary<string, float>();
        Dictionary<GameObject, string> UngroupedObject2RoadId = new Dictionary<GameObject, string>();
        Dictionary<string, HashSet<MapLane>> IncomingRoadId2leftLanes = new Dictionary<string, HashSet<MapLane>>();
        Dictionary<string, HashSet<MapLane>> IncomingRoadId2rightLanes = new Dictionary<string, HashSet<MapLane>>();
        Dictionary<string, List<MapLine>> IntersectionId2StopLines = new Dictionary<string, List<MapLine>>();
        Dictionary<string, List<MapSignal>> IntersectionId2MapSignals = new Dictionary<string, List<MapSignal>>();
        Dictionary<string, List<MapSign>> IntersectionId2MapSigns = new Dictionary<string, List<MapSign>>();
        List<double> SectionRefPointSValue = new List<double>();
        List<double> ReferenceLinePointSValue = new List<double>();

        class BeforesAfters
        {
            public HashSet<MapLane> befores = new HashSet<MapLane>();
            public HashSet<MapLane> afters = new HashSet<MapLane>();
        }

        public OpenDriveMapImporter(float downSampleDistanceThreshold,
            float downSampleDeltaThreshold, bool isMeshNeeded, bool isConnectLanes=true)
        {
            DownSampleDistanceThreshold = downSampleDistanceThreshold;
            DownSampleDeltaThreshold = downSampleDeltaThreshold;
            IsMeshNeeded = isMeshNeeded;
            IsConnectLanes = isConnectLanes;
        }

        public void Import(string filePath)
        {
            Settings = EditorSettings.Load();

            if (Calculate(filePath))
            {
                Debug.Log("Successfully imported OpenDRIVE Map!");
                Debug.Log("Note we only imported lanes of \"driving\" type. If your map is incorrect, please check if you have set MapOrigin correctly.");
                Debug.Log("We generated stop lines for every entering road for an intersection, please make sure they are correct.");
                Debug.LogWarning("!!! You need to adjust the triggerBounds for each MapIntersection.");
                EditorSceneManager.MarkAllScenesDirty();
            }
            else
            {
                Debug.Log("Failed to import OpenDRIVE map.");
            }
        }

        string ReplaceStrs(string filePath)
        {
            string folderPath = Path.GetDirectoryName(filePath);
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            fileName = fileName + "-temp.xodr";
            string tempFilePath = Path.Combine(folderPath, fileName);
            using (StreamReader reader = new StreamReader(filePath))
            {
                using (StreamWriter writer = new StreamWriter(tempFilePath))
                {
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        line = Regex.Replace(line, "\"-?nan\"", "\"0\"", RegexOptions.IgnoreCase);
                        line = Regex.Replace(line, "type=\"bus\"", "type=\"driving\"", RegexOptions.IgnoreCase);
                        line = Regex.Replace(line, "\"-?1.#.*\"", "\"0\"", RegexOptions.IgnoreCase);
                        writer.WriteLine(line);
                    }
                }
            }

            return tempFilePath;
        }

        public bool Calculate(string filePath)
        {
            var fileName = Path.GetFileName(filePath).Split('.')[0];
            string mapName = "Map" + char.ToUpper(fileName[0]) + fileName.Substring(1);

            filePath = ReplaceStrs(filePath);
            try
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
                        var msg = "Map parsing failed! Please check whether your map has ";
                        msg += "invalid values, and also check your map version, currently we only support version 1.4.";
                        Debug.LogError(msg);
                        DeleteTempFile(filePath);
                        return false;
                    }
                }

                // Create Map object
                if (!CreateMapHolder(filePath, mapName)) return false;

                MapOrigin = MapOrigin.Find(); // get or create a map origin
                if (!CreateOrUpdateMapOrigin(OpenDRIVEMap, MapOrigin))
                {
                    Debug.LogWarning("Could not find valid latitude or/and longitude in map header Or not supported projection, mapOrigin is not updated, you need manually update it.");
                }

                ImportRoads();
                ImportJunctions();
                UpdateAllLanesBeforesAfters();

                if (IsConnectLanes) ConnectLanes();
                CleanUp();
                LinkSignalsWithStopLines();

                UpdateMapIntersectionPositions();
            }
            finally
            {
                DeleteTempFile(filePath);
            }

            return true;
        }

        void DeleteTempFile(string filePath)
        {
            if (File.Exists(filePath)) File.Delete(filePath);
        }

        void UpdateMapIntersectionPositions()
        {
            foreach (var entry in IntersectionId2StopLines)
            {
                var mapIntersection = Id2MapIntersection[entry.Key];
                var stopLineCenterPositions = new List<Vector3>();
                foreach (var stopLine in entry.Value)
                {
                    var centerPos = (stopLine.mapWorldPositions.First() + stopLine.mapWorldPositions.Last()) / 2;
                    stopLineCenterPositions.Add(centerPos);
                }
                mapIntersection.transform.position = Lanelet2MapImporter.GetAverage(stopLineCenterPositions);

                // Update children positions
                foreach (Transform child in mapIntersection.transform)
                {
                    child.transform.position -= mapIntersection.transform.position;
                    if (child.GetComponent<MapDataPoints>() != null)
                        ApolloMapImporter.UpdateLocalPositions(child.GetComponent<MapDataPoints>());
                }
            }
        }

        bool CreateMapHolder(string filePath, string mapName)
        {
            // Create Map object
            var fileName = Path.GetFileName(filePath).Split('.')[0];

            // Check existence of same name map
            if (GameObject.Find(mapName))
            {
                Debug.LogError("A map with same name exists, cancelling map importing.");
                File.Delete(filePath);
                return false;
            }

            Map = new GameObject(mapName);
            MapHolder = Map.AddComponent<MapHolder>();

            // Create TrafficLanes and Intersections under Map
            TrafficLanes = new GameObject("TrafficLanes");
            Intersections = new GameObject("Intersections");
            SingleLaneRoads = new GameObject("SingleLaneRoads");
            TrafficLanes.transform.parent = Map.transform;
            Intersections.transform.parent = Map.transform;
            SingleLaneRoads.transform.parent = TrafficLanes.transform;

            MapHolder.trafficLanesHolder = TrafficLanes.transform;
            MapHolder.intersectionsHolder = Intersections.transform;

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
            mapOrigin.OriginNorthing = northing;
            mapOrigin.OriginEasting = easting;

            return true;
        }

        // Calculate elevation at the specific length along the reference path
        public double GetElevation(double l, OpenDRIVERoadElevationProfile elevationProfile)
        {
            if (l < 0) l = 0;
            double elevation = 0;
            if (elevationProfile == null || elevationProfile.elevation == null)
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

        Vector3 GetPos(OpenDRIVERoadElevationProfile elevationProfile,
            double s, double x, double z, double hdg)
        {
            double y = GetElevation(s, elevationProfile);
            Vector3 pos = new Vector3((float)x, (float)y, (float)z);
            // rotate
            pos = Quaternion.Euler(0f, -(float)(hdg * 180f / Math.PI), 0f) * pos;

            return pos;
        }

        public List<Tuple<Vector3, double>> CalculateLinePoints(OpenDRIVERoadGeometry geometry,
            OpenDRIVERoadElevationProfile elevationProfile, List<double> dists)
        {
            OpenDRIVERoadGeometryLine line = geometry.Items[0] as OpenDRIVERoadGeometryLine;

            Vector3 origin = new Vector3((float)geometry.x, 0f, (float)geometry.y);
            List<Tuple<Vector3, double>> pointsAndS = new List<Tuple<Vector3, double>>();

            double hdg = geometry.hdg;
            double s = geometry.s;
            for (int i = 0; i < dists.Count; i++)
            {
                var dist = dists[i];
                var x = dist;
                var curS = s + dist;
                var pos = GetPos(elevationProfile, curS, x, 0, hdg);
                pointsAndS.Add(new Tuple<Vector3, double>(origin + pos, curS));
            }

            return pointsAndS;
        }

        public List<Tuple<Vector3, double>> CalculateSpiralPoints(OpenDRIVERoadGeometry geometry,
            OpenDRIVERoadElevationProfile elevationProfile, Vector3 origin, double hdg,
            List<double> dists)
        {
            OpenDRIVERoadGeometrySpiral spi = geometry.Items[0] as OpenDRIVERoadGeometrySpiral;

            List<Tuple<Vector3, double>> pointsAndS = new List<Tuple<Vector3, double>>();
            OdrSpiral.Spiral spiral = new OdrSpiral.Spiral();

            double x_ = new double();
            double z_ = new double();
            double t_ = new double();

            var curvStart = spi.curvStart;
            var curvEnd = spi.curvEnd;
            var length = geometry.length;
            spiral.odrSpiral(length, (curvEnd - curvStart) / length, ref x_, ref z_, ref t_);

            var s = geometry.s;
            for (int i = 0; i < dists.Count; i++)
            {
                var dist = dists[i];
                double x = new double();
                double z = new double();
                double t = new double();
                spiral.odrSpiral(dist, (curvEnd - curvStart) / length, ref x, ref z, ref t);

                var curS = s + dist;
                double y = GetElevation(curS, elevationProfile);
                Vector3 pos;
                if (curvStart == 0)
                {
                    pos = new Vector3((float)x, (float)y, (float)z);
                }
                else
                {
                    pos = new Vector3(-(float)x, (float)y, -(float)z);
                }
                // rotate
                pos = Quaternion.Euler(0f, -(float)(hdg * 180f / Math.PI), 0f) * pos;
                pointsAndS.Add(new Tuple<Vector3, double>(origin + pos, curS));
            }

            return pointsAndS;
        }

        public List<Tuple<Vector3, double>> CalculateArcPoints(OpenDRIVERoadGeometry geometry,
            OpenDRIVERoadElevationProfile elevationProfile, List<double> dists)
        {
            OpenDRIVERoadGeometryArc arc = geometry.Items[0] as OpenDRIVERoadGeometryArc;

            Vector3 origin = new Vector3((float)geometry.x, 0f, (float)geometry.y);
            List<Tuple<Vector3, double>> pointsAndS = new List<Tuple<Vector3, double>>();
            OdrSpiral.Spiral a = new OdrSpiral.Spiral();

            double s = geometry.s;
            var hdg = geometry.hdg;
            for (int i = 0; i < dists.Count; i++)
            {
                var dist = dists[i];
                double x = new double();
                double z = new double();
                a.odrArc(dist, arc.curvature, ref x, ref z);

                var curS = s + dist;
                var pos = GetPos(elevationProfile, curS, x, z, hdg);
                pointsAndS.Add(new Tuple<Vector3, double>(origin + pos, curS));
            }

            return pointsAndS;
        }

        public List<Tuple<Vector3, double>> CalculatePoly3Points(OpenDRIVERoadGeometry geometry,
            OpenDRIVERoadElevationProfile elevationProfile, List<double> dists)
        {
            OpenDRIVERoadGeometryPoly3 poly3 = geometry.Items[0] as OpenDRIVERoadGeometryPoly3;

            Vector3 origin = new Vector3((float)geometry.x, 0f, (float)geometry.y);
            var hdg = geometry.hdg;

            List<Tuple<Vector3, double>> pointsAndS = new List<Tuple<Vector3, double>>();
            double a = poly3.a;
            double b = poly3.b;
            double c = poly3.c;
            double d = poly3.d;

            double s = geometry.s;
            for (int i = 0; i < dists.Count; i++)
            {
                var dist = dists[i];
                double x = dist;
                double z = a + b * x + c * x * x + d * x * x * x;

                var curS = s + dist;
                var pos = GetPos(elevationProfile, curS, x, z, hdg);
                pointsAndS.Add(new Tuple<Vector3, double>(origin + pos, curS));
            }

            return pointsAndS;
        }

        public List<Tuple<Vector3, double>> CalculateParamPoly3Points(OpenDRIVERoadGeometry geometry,
            OpenDRIVERoadElevationProfile elevationProfile, List<double> dists)
        {
            OpenDRIVERoadGeometryParamPoly3 pPoly3 = geometry.Items[0] as OpenDRIVERoadGeometryParamPoly3;
            Vector3 origin = new Vector3((float)geometry.x, 0f, (float)geometry.y);

            List<Tuple<Vector3, double>> pointsAndS = new List<Tuple<Vector3, double>>();
            double aU = pPoly3.aU;
            double bU = pPoly3.bU;
            double cU = pPoly3.cU;
            double dU = pPoly3.dU;
            double aV = pPoly3.aV;
            double bV = pPoly3.bV;
            double cV = pPoly3.cV;
            double dV = pPoly3.dV;

            bool useArcLength = false;
            double pMax = 1;
            var pPoly3Length = geometry.length;
            if (pPoly3.pRangeSpecified == true && pPoly3.pRange == pRange.arcLength)
            {
                useArcLength = true;
                pMax = pPoly3Length;
            }

            double s = geometry.s;
            var hdg = geometry.hdg;
            for (int i = 0; i < dists.Count; i++)
            {
                var dist = dists[i];
                var p = useArcLength ? dist : dist / pPoly3Length;

                double x = aU + bU * p + cU * p * p + dU * p * p * p;
                double z = aV + bV * p + cV * p * p + dV * p * p * p;

                var curS = s + dist;
                var pos = GetPos(elevationProfile, curS, x, z, hdg);
                pointsAndS.Add(new Tuple<Vector3, double>(origin + pos, curS));
            }

            return pointsAndS;
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

        void ImportRoads()
        {
            foreach (var road in OpenDRIVEMap.road)
            {
                var roadLength = road.length;
                var roadId = road.id;
                var link = road.link;
                var elevationProfile = road.elevationProfile;
                var lanes = road.lanes;
                ImportRoadSpeed(road);
                var laneSections = lanes.laneSection;
                var junctionId = road.junction;
                if (junctionId != "-1") RoadId2IntersectionId[roadId] = junctionId;

                List<Vector3> referenceLinePoints = new List<Vector3>();
                ReferenceLinePointSValue.Clear();
                var sectionsS = laneSections.Select(laneSection => laneSection.s).ToList();
                var sectionsPointsIdx = new List<int>();
                var sectionIdx = 0;

                for (int i = 0; i < road.planView.Length; i++)
                {
                    var geometry = road.planView[i];
                    if (geometry.length < GeometrySkipLength) continue; // skip if it is too short

                    var affectedIdxStart = sectionsPointsIdx.Count;
                    var dists = GetDists(ref sectionsS, ref sectionIdx, geometry, sectionsPointsIdx, referenceLinePoints);
                    var affectedIdxEnd = sectionsPointsIdx.Count;
                    // Line
                    if (geometry.Items[0] is OpenDRIVERoadGeometryLine)
                    {
                        List<Tuple<Vector3, double>> pointsAndS = CalculateLinePoints(geometry, elevationProfile, dists);
                        UpdateReferenceLinePoint2s(referenceLinePoints.Count, pointsAndS, ReferenceLinePointSValue);
                        referenceLinePoints.AddRange(pointsAndS.Select(entry => entry.Item1).ToList());
                    }
                    else
                    // Spiral
                    if (geometry.Items[0] is OpenDRIVERoadGeometrySpiral)
                    {
                        OpenDRIVERoadGeometrySpiral spi = geometry.Items[0] as OpenDRIVERoadGeometrySpiral;
                        if (spi.curvStart == 0)
                        {
                            Vector3 geo = new Vector3((float)geometry.x, 0f, (float)geometry.y);
                            List<Tuple<Vector3, double>> pointsAndS = CalculateSpiralPoints(geometry,
                                elevationProfile, geo, geometry.hdg, dists);
                            UpdateReferenceLinePoint2s(referenceLinePoints.Count, pointsAndS, ReferenceLinePointSValue);
                            referenceLinePoints.AddRange(pointsAndS.Select(entry => entry.Item1).ToList());
                        }
                        else
                        {
                            // Update index if points get reversed
                            UpdateSectionsPointsIdx(sectionsPointsIdx, affectedIdxStart,
                                affectedIdxEnd, referenceLinePoints.Count, dists.Count);
                            List<Tuple<Vector3, double>> pointsAndS;
                            if (i != road.planView.Length - 1)
                            {
                                var geometryNext = road.planView[i + 1];
                                Vector3 geo = new Vector3((float)geometryNext.x, 0f, (float)geometryNext.y);
                                pointsAndS = CalculateSpiralPoints(geometry, elevationProfile, geo, geometryNext.hdg, dists);
                            }
                            else
                            {
                                var tmp = GetRoadById(OpenDRIVEMap, Int32.Parse(link.successor.elementId));
                                var geometryNext = tmp.planView[tmp.planView.Length - 1];
                                Vector3 geo = new Vector3((float)geometryNext.x, 0f, (float)geometryNext.y);
                                pointsAndS = CalculateSpiralPoints(geometry, elevationProfile, geo, -geometryNext.hdg, dists);
                            }
                            UpdateReferenceLinePoint2s(referenceLinePoints.Count, pointsAndS, ReferenceLinePointSValue); // sequence of s doesn't need to be reversed
                            pointsAndS.Reverse();
                            referenceLinePoints.AddRange(pointsAndS.Select(entry => entry.Item1).ToList());
                        }
                    }
                    else
                    // Arc
                    if (geometry.Items[0] is OpenDRIVERoadGeometryArc)
                    {
                        List<Tuple<Vector3, double>> pointsAndS = CalculateArcPoints(geometry, elevationProfile, dists);
                        UpdateReferenceLinePoint2s(referenceLinePoints.Count, pointsAndS, ReferenceLinePointSValue);
                        referenceLinePoints.AddRange(pointsAndS.Select(entry => entry.Item1).ToList());
                    }
                    else
                    // Poly3
                    if (geometry.Items[0] is OpenDRIVERoadGeometryPoly3)
                    {
                        List<Tuple<Vector3, double>> pointsAndS = CalculatePoly3Points(geometry, elevationProfile, dists);
                        UpdateReferenceLinePoint2s(referenceLinePoints.Count, pointsAndS, ReferenceLinePointSValue);
                        referenceLinePoints.AddRange(pointsAndS.Select(entry => entry.Item1).ToList());
                    }
                    else
                    // ParamPoly3
                    if (geometry.Items[0] is OpenDRIVERoadGeometryParamPoly3)
                    {
                        List<Tuple<Vector3, double>> pointsAndS = CalculateParamPoly3Points(geometry, elevationProfile, dists);
                        UpdateReferenceLinePoint2s(referenceLinePoints.Count, pointsAndS, ReferenceLinePointSValue);
                        referenceLinePoints.AddRange(pointsAndS.Select(entry => entry.Item1).ToList());
                    }
                }

                if (referenceLinePoints.Count == 0)
                {
                    var geometry = road.planView[0];
                    Vector3 origin = new Vector3((float)geometry.x, 0f, (float)geometry.y);
                    referenceLinePoints.Add(origin);
                    ReferenceLinePointSValue.Add(geometry.s);

                    double y = GetElevation(geometry.s + roadLength, elevationProfile);
                    Vector3 pos = new Vector3((float)roadLength, (float)y, 0f);
                    // rotate
                    pos = Quaternion.Euler(0f, -(float)(geometry.hdg * 180f / Math.PI), 0f) * pos;
                    referenceLinePoints.Add(origin + pos);
                    ReferenceLinePointSValue.Add(geometry.s + roadLength);
                }
                // if the length of the reference line is less than 1 meter
                else if (referenceLinePoints.Count == 1)
                {
                    var geometry = road.planView[0];
                    Vector3 origin = new Vector3((float)geometry.x, 0f, (float)geometry.y);

                    double y = GetElevation(geometry.s + geometry.length, elevationProfile);
                    Vector3 pos = new Vector3((float)geometry.length, (float)y, 0f);
                    // rotate
                    pos = Quaternion.Euler(0f, -(float)(geometry.hdg * 180f / Math.PI), 0f) * pos;
                    referenceLinePoints.Add(origin + pos);
                    ReferenceLinePointSValue.Add(geometry.s + geometry.length);
                }

                Roads[roadId] = new List<Dictionary<int, MapLane>>();

                var referenceLinePointsOffset = new List<Vector3>(referenceLinePoints);
                if (lanes.laneOffset != null)
                {
                    referenceLinePointsOffset = UpdateReferencePoints(lanes,
                       referenceLinePoints, ReferenceLinePointSValue);
                }

                var laneSectionsLength = ComputeSectionLength(laneSections, roadLength);
                for (int i = 0; i < laneSections.Length; i++)
                {
                    Roads[roadId].Add(new Dictionary<int, MapLane>());

                    if (laneSectionsLength[i] < LaneSectionSkipLength)
                    {
                        Debug.LogWarning($"Road {roadId} laneSection {i} is too short, skipped importing");
                        if (RoadId2laneSections.ContainsKey(roadId)) RoadId2laneSections[roadId].Add(i);
                        else RoadId2laneSections[roadId] = new List<int> {i};
                        continue;
                    }

                    var startIdx = sectionsPointsIdx[i];
                    int endIdx;
                    if (i == laneSections.Length - 1) endIdx = referenceLinePointsOffset.Count;
                    else  endIdx = sectionsPointsIdx[i+1];

                    if (endIdx == startIdx) continue;

                    var sectionRefPointsOffset = new List<Vector3>(referenceLinePointsOffset.GetRange(startIdx, endIdx - startIdx));
                    var sectionRefPoints = new List<Vector3>(referenceLinePoints.GetRange(startIdx, endIdx - startIdx));
                    SectionRefPointSValue = new List<double>(ReferenceLinePointSValue.GetRange(startIdx, endIdx - startIdx));
                    for (int j = endIdx - startIdx - 1; j >= 0; --j)
                    {
                        SectionRefPointSValue[j] -= SectionRefPointSValue[0];
                    }

                    Debug.Assert(referenceLinePoints.Count > 0,
                        $"referenceLinePointsOffset has no elements, roadId {roadId} laneSectionId {i} s = {laneSections[i].s}");
                    CreateMapLanes(roadId, i, laneSections[i], sectionRefPointsOffset, sectionRefPoints);
                }

                if (road.signals != null) ImportSignals(road, road.signals, referenceLinePointsOffset);
            }
        }

        void ImportRoadSpeed(OpenDRIVERoad road)
        {
            if (road.type != null && road.type[0].speed != null)
            {
                var speed = road.type[0].speed; // only use 1st type's speed if road has multiple types
                if (speed.max == "no limit" || speed.max == "undefined") return;
                if (!float.TryParse(speed.max, out float speedLimit) || speedLimit < 0) return;
                var speedUnit = speed.unit;
                if (!speed.unitSpecified || !ConvertSpeed(speedUnit, ref speedLimit)) return;
                RoadId2Speed[road.id] = speedLimit;
            }
        }

        bool ConvertSpeed(unit speedUnit, ref float speedLimit)
        {
            if (speedUnit == unit.ms) return true;
            if (speedUnit == unit.kmh)
            {
                speedLimit *= 0.277778f;
                return true;
            }
            if (speedUnit == unit.mph)
            {
                speedLimit *= 0.44704f;
                return true;
            }

            return false;
        }

        void UpdateReferenceLinePoint2s(int refPointsCount, List<Tuple<Vector3, double>> pointsAndS,
            List<double> referenceLinePointSValue)
        {
            for (int i = 0; i < pointsAndS.Count; i++)
            {
                var s = pointsAndS[i].Item2;
                var index = refPointsCount + i;
                referenceLinePointSValue.Add(s);
            }
        }

        void UpdateSectionsPointsIdx(List<int> sectionsPointsIdx, int affectedIdxStart,
            int affectedIdxEnd, int refPointsCount, int distsCount)
        {
            for (int idx = affectedIdxStart; idx < affectedIdxEnd; idx++)
            {
                var offset = sectionsPointsIdx[idx] - refPointsCount;
                offset = distsCount - offset;
                sectionsPointsIdx[idx] = refPointsCount + offset;
            }
        }

        List<double> GetDists(ref List<double> sectionsS, ref int sectionIdx,
            OpenDRIVERoadGeometry geometry, List<int> sectionPointsIdx, List<Vector3> refPoints)
        {
            // Add two closest dists for nonzero laneSections, s - 0.1 and s to get correct laneOffsets
            // previous lane section points until the point of s - 0.1, next lane section start the point of s
            if (sectionIdx < sectionsS.Count && sectionsS[sectionIdx] < 0)
            {
                sectionPointsIdx.Add(refPoints.Count);
                sectionIdx++;
            }

            var dists = new List<double>(); // distances from geometry.s
            var resolution = 1;
            for (var i = 0; i < geometry.length - 0.1; i += resolution)
            {
                dists.Add(i);
            }
            if (geometry.length - 0.1 >= 0) dists.Add(geometry.length - 0.1);

            // separate dists at least 1 meter except for the two points around lanesection idx
            if (dists.Count > 1 && (dists.Last() - dists[dists.Count - 2]) < 0.1)
            {
                dists.RemoveAt(dists.Count - 2);
            }

            if (sectionIdx < sectionsS.Count && Math.Abs(geometry.s - sectionsS[sectionIdx]) < 0.001)
            {
                sectionPointsIdx.Add(refPoints.Count);
                sectionIdx++;
            }

            int idx = 1;
            while (sectionIdx < sectionsS.Count && sectionsS[sectionIdx] < geometry.s + geometry.length - 0.1)
            {
                var sectionS = sectionsS[sectionIdx];
                while (idx < dists.Count && geometry.s + dists[idx] < sectionS) idx++;

                double diff = geometry.s + dists[idx] - sectionS;
                if (diff < 0.001)
                {
                    dists.Insert(idx, dists[idx++] - 0.1);
                    sectionPointsIdx.Add(refPoints.Count + idx);
                    sectionIdx++;
                }
                else
                {
                    var sectionDist = sectionS - geometry.s;
                    // Remove closest element in dists
                    if (sectionDist - dists[idx - 1] < dists[idx] - sectionDist)
                    {
                        dists.RemoveAt(idx - 1);
                        idx--;
                    }
                    else
                    {
                        dists.RemoveAt(idx);
                    }
                    dists.Insert(idx++, sectionDist - 0.1);
                    dists.Insert(idx, sectionDist);
                    sectionPointsIdx.Add(refPoints.Count + idx);
                    sectionIdx++;
                }
            }

            Debug.Assert(IsIncreasingMontonically<double>(dists));
            return dists;
        }

        List<double> ComputeSectionLength(OpenDRIVERoadLanesLaneSection[] laneSections, double roadLength)
        {
            var sectionLengths = new List<double>();
            for (int i = 0; i < laneSections.Length; i++)
            {
                if (i < laneSections.Length - 1)
                {
                    sectionLengths.Add(laneSections[i+1].s - laneSections[i].s);
                }
                else
                {
                    sectionLengths.Add(roadLength - laneSections[i].s);
                }
            }

            return sectionLengths;
        }

        List<Vector3> UpdateReferencePoints(OpenDRIVERoadLanes lanes,
            List<Vector3> referencePoints, List<double> referenceLinePointSValue)
        {
            var updatedReferencePoints = new List<Vector3>();
            var laneOffsets = lanes.laneOffset;
            int curLaneOffsetIdx = 0;
            Debug.Assert(laneOffsets.Length > 0);

            var laneOffset = laneOffsets[0];
            var laneOffsetS = laneOffset.s;
            for (int idx = 0; idx < referencePoints.Count; idx++)
            {
                Vector3 point = referencePoints[idx];
                var curS = referenceLinePointSValue[idx];
                if (curS >= laneOffsetS)
                {
                    while (curLaneOffsetIdx < laneOffsets.Length - 1 && curS >= (laneOffsets[curLaneOffsetIdx + 1]).s)
                    {
                        laneOffset = laneOffsets[++curLaneOffsetIdx];
                        laneOffsetS = laneOffset.s;
                    }
                }

                var ds = curS - laneOffsetS;
                var offsetValue = laneOffset.a + laneOffset.b * ds + laneOffset.c * ds * ds + laneOffset.d * ds * ds * ds;
                // Debug.Log($"idx {idx} s: {curS} offset: {offsetValue} curLaneOffsetIdx {curLaneOffsetIdx}");
                var normalDir = GetNormalDir(referencePoints, idx, true);
                updatedReferencePoints.Add(point + (float)offsetValue * normalDir);
            }

            return updatedReferencePoints;
        }

        void ImportSignals(OpenDRIVERoad road, OpenDRIVERoadSignals roadSignals, List<Vector3> referenceLinePoints)
        {
            if (roadSignals.signal != null)
            {
                foreach (var roadSignal in roadSignals.signal)
                {
                    if (roadSignal.country != "OpenDRIVE")
                    {
                        Debug.LogWarning($"Currently we only support signals with country: OpenDRIVE");
                        continue;
                    }
                    GameObject mapIntersectionObj = GetRelatedIntersectionObject(road, roadSignal.s, referenceLinePoints);

                    if (roadSignal.dynamic == dynamic.yes) ImportTrafficLight(road.id, roadSignal, referenceLinePoints, mapIntersectionObj);
                    else ImportStopSign(road.id, roadSignal, referenceLinePoints, mapIntersectionObj);
                }
            }
        }

        // Get related intersection object when the signal/sign is not on a road inside an intersection
        GameObject GetRelatedIntersectionObject(OpenDRIVERoad road, double s, List<Vector3> referenceLinePoints)
        {
            var roadId = road.id;
            string intersectionId = null;

            var link = road.link;
            if (link != null)
            {
                var predecessor = link.predecessor;
                var successor = link.successor;
                if ((predecessor != null && predecessor.elementType == elementType.junction)
                    || (successor != null && successor.elementType == elementType.junction))
                {
                    if (predecessor != null && successor != null)
                    {
                        if (predecessor.elementType == elementType.junction && successor.elementType == elementType.junction)
                        {
                            intersectionId = s < referenceLinePoints.Count / 2 ? predecessor.elementId : successor.elementId;
                        }
                        else
                        {
                            intersectionId = predecessor.elementType == elementType.junction ? predecessor.elementId : successor.elementId;
                        }
                    }
                    else
                    {
                        intersectionId = predecessor != null ? predecessor.elementId : successor.elementId;
                    }
                }
            }

            if (intersectionId != null) return CreateMapIntersection(intersectionId).gameObject;
            return null;
        }

        void ImportTrafficLight(string roadId, OpenDRIVERoadSignalsSignal roadSignal, List<Vector3> referenceLinePoints, GameObject mapIntersectionObj)
        {
            if (roadSignal.type != "1000001")
            {
                Debug.LogWarning($"Currently we are not supporting importing traffic signal of type: {roadSignal.type}");
                return;
            }

            var id = roadSignal.id;
            var mapSignalObj = new GameObject("MapSignal_" + id);
            var signalPosition = GetSignalPositionRotation(roadSignal, referenceLinePoints, out Quaternion rotation);
            signalPosition.y += SignalHeight;

            mapSignalObj.transform.position = signalPosition;
            mapSignalObj.transform.rotation = rotation;

            SetParent(mapSignalObj, mapIntersectionObj, roadId);

            var mapSignal = mapSignalObj.AddComponent<MapSignal>();
            mapSignal.signalData = new List<MapData.SignalData> {
                new MapData.SignalData() { localPosition = Vector3.up * 0.4f, signalColor = MapData.SignalColorType.Red },
                new MapData.SignalData() { localPosition = Vector3.zero, signalColor = MapData.SignalColorType.Yellow },
                new MapData.SignalData() { localPosition = Vector3.up * -0.4f, signalColor = MapData.SignalColorType.Green },
            };

            mapSignal.boundScale = new Vector3(0.65f, 1.5f, 0.0f);
            mapSignal.signalType = MapData.SignalType.MIX_3_VERTICAL;

            mapSignal.signalLightMesh = GetSignalMesh(roadId, id, mapIntersectionObj, mapSignal);

            if (mapIntersectionObj != null)
            {
                var intersectionId = mapIntersectionObj.name.Split('_')[1];
                if (IntersectionId2MapSignals.ContainsKey(intersectionId)) IntersectionId2MapSignals[intersectionId].Add(mapSignal);
                else IntersectionId2MapSignals[intersectionId] = new List<MapSignal>(){mapSignal};
            }
        }

        void ImportStopSign(string roadId, OpenDRIVERoadSignalsSignal roadSignal, List<Vector3> referenceLinePoints, GameObject mapIntersectionObj)
        {
            if (roadSignal.type != "206")
            {
                Debug.LogWarning($"Currently we are not supporting importing sign of type: {roadSignal.type}");
                return;
            }

            if (mapIntersectionObj == null) Debug.LogWarning($"Cannot find associated intersection for the stop sign {roadSignal.id}.");

            var stopSignLocation = GetSignalPositionRotation(roadSignal, referenceLinePoints, out Quaternion rotation);

            var id = roadSignal.id;
            var mapSignObj = new GameObject("MapStopSign_" + id);
            mapSignObj.transform.position = stopSignLocation;
            mapSignObj.transform.rotation = rotation;
            SetParent(mapSignObj, mapIntersectionObj, roadId);

            var mapSign = mapSignObj.AddComponent<MapSign>();
            mapSign.signType = MapData.SignType.STOP;
            mapSign.boundOffsets = new Vector3(0f, 2.55f, 0f);
            mapSign.boundScale = new Vector3(0.95f, 0.95f, 0f);

            // Create stop sign mesh
            if (IsMeshNeeded) CreateStopSignMesh(roadId, id, mapIntersectionObj, mapSign);

            if (mapIntersectionObj != null)
            {
                var intersectionId = mapIntersectionObj.name.Split('_')[1];
                if (IntersectionId2MapSigns.ContainsKey(intersectionId)) IntersectionId2MapSigns[intersectionId].Add(mapSign);
                else IntersectionId2MapSigns[intersectionId] = new List<MapSign>(){mapSign};
            }
        }

        void CreateStopSignMesh(string roadId, string id, GameObject mapIntersectionObj, MapSign mapSign)
        {
            GameObject stopSignPrefab = Settings.MapStopSignPrefab;
            var stopSignObj = UnityEngine.Object.Instantiate(stopSignPrefab, mapSign.transform.position + mapSign.boundOffsets, mapSign.transform.rotation);
            if (mapIntersectionObj != null) stopSignObj.transform.parent = mapIntersectionObj.transform;
            SetParent(stopSignObj, mapIntersectionObj, roadId);
            stopSignObj.name = "MapStopSignMesh_" + id;
        }

        int GetClosestPointIdx(double s)
        {
            for (int i = 0; i < ReferenceLinePointSValue.Count; i++)
            {
                if ((s - ReferenceLinePointSValue[i]) < 0.5) return i;
            }
            return ReferenceLinePointSValue.Count - 1;
        }

        Vector3 GetSignalPositionRotation(OpenDRIVERoadSignalsSignal roadSignal, List<Vector3> referenceLinePoints, out Quaternion rotation)
        {
            var sIdx = GetClosestPointIdx(roadSignal.s);
            var signalPosition = referenceLinePoints[sIdx];
            Vector3 p1, p2;
            if (sIdx == 0)
            {
                p1 = referenceLinePoints[0];
                p2 = referenceLinePoints[1];
            }
            else
            {
                p1 = referenceLinePoints[sIdx - 1];
                p2 = referenceLinePoints[sIdx];
            }
            var tDirection = Vector3.Cross((p2 - p1), Vector3.up).normalized;
            signalPosition += tDirection * (float)roadSignal.t;
            var signalDirection = p2 - p1;
            if (roadSignal.orientation == orientation.Item1) signalDirection = -signalDirection;
            if (roadSignal.hOffsetSpecified == true)
            {
                signalDirection = Quaternion.AngleAxis((float)roadSignal.hOffset * Mathf.Rad2Deg, Vector3.up) * signalDirection;
            }
            rotation = Quaternion.LookRotation(signalDirection);

            return signalPosition;
        }

        Renderer GetSignalMesh(string roadId, string id, GameObject mapIntersectionObj, MapSignal mapSignal)
        {
            var mapSignalMesh = UnityEngine.Object.Instantiate(Settings.MapTrafficSignalPrefab, mapSignal.transform.position, mapSignal.transform.rotation);
            SetParent(mapSignalMesh, mapIntersectionObj, roadId);
            mapSignalMesh.name = "MapSignalMeshVertical_" + id;
            var mapSignalMeshRenderer = mapSignalMesh.AddComponent<SignalLight>().GetComponent<Renderer>();

            return mapSignalMeshRenderer;
        }

        void SetParent(GameObject obj, GameObject mapIntersectionObj, string roadId)
        {
            if (mapIntersectionObj != null) obj.transform.parent = mapIntersectionObj.transform;
            else UngroupedObject2RoadId[obj] = roadId;
        }

        void CreateMapLanes(string roadId, int laneSectionId,
            OpenDRIVERoadLanesLaneSection laneSection, List<Vector3> sectionRefPointsOffset,
            List<Vector3> sectionRefPoints)
        {
            var roadIdLaneSectionId = $"road{roadId}_section{laneSectionId}";
            MapLine refMapLine = GetRefMapLine(roadIdLaneSectionId, laneSection, sectionRefPointsOffset);

            // From left to right, compute other MapLines
            // Get number of lanes and move lane into a new MapLaneSection or SingleLanes
            GameObject parentObj = GetParentObj(roadIdLaneSectionId, laneSection);
            if (laneSection.left != null) CreateLinesLanes(roadId, laneSectionId, sectionRefPoints, refMapLine, laneSection.left.lane, parentObj, true);
            if (laneSection.right != null) CreateLinesLanes(roadId, laneSectionId, sectionRefPoints, refMapLine, laneSection.right.lane, parentObj, false);

            DownSampleLaneLines(roadId, laneSectionId);

            refMapLine.transform.parent = parentObj.transform;
            ApolloMapImporter.UpdateLocalPositions(refMapLine);
            // Update parent object position if it is a MapLaneSection
            if (parentObj != SingleLaneRoads) UpdateMapLaneSectionPosition(parentObj);
        }

        void DownSampleLaneLines(string roadId, int laneSectionId)
        {
            var Id2MapLane = Roads[roadId][laneSectionId];
            var visitedLines = new HashSet<int>();
            foreach (var entry in Id2MapLane)
            {
                var mapLane = entry.Value;
                var leftLine = mapLane.leftLineBoundry;
                var rightLine = mapLane.rightLineBoundry;
                DownSample(mapLane);
                if (!visitedLines.Contains(leftLine.GetInstanceID()))
                {
                    DownSample(leftLine);
                    visitedLines.Add(leftLine.GetInstanceID());
                }
                if (!visitedLines.Contains(rightLine.GetInstanceID()))
                {
                    DownSample(rightLine);
                    visitedLines.Add(rightLine.GetInstanceID());
                }
            }
        }

        void DownSample(MapDataPoints mapDataPoints)
        {
            var worldPositions = mapDataPoints.mapWorldPositions;
            var downSampledPositionsDouble3 = ApolloMapImporter.DownSample(
                worldPositions.Select(x => (double3)(float3)x).ToList(), DownSampleDeltaThreshold, DownSampleDistanceThreshold);
            var downSampledPositions = downSampledPositionsDouble3.Select(x => (Vector3)(float3)x).ToList();
            mapDataPoints.mapWorldPositions = downSampledPositions;
            ApolloMapImporter.UpdateLocalPositions(mapDataPoints);
        }

        void UpdateMapLaneSectionPosition(GameObject parentObj)
        {
            var parentPos = Vector3.zero;
            foreach (Transform laneLineObj in parentObj.transform)
            {
                parentPos += laneLineObj.transform.position;
            }
            parentObj.transform.position = parentPos / parentObj.transform.childCount;
            foreach (Transform childObj in parentObj.transform)
            {
                var mapDataPoints = childObj.GetComponent<MapDataPoints>();
                mapDataPoints.transform.position -= parentObj.transform.position;
                ApolloMapImporter.UpdateLocalPositions(mapDataPoints);
            }
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

        private MapLine GetRefMapLine(string roadIdLaneSectionId, OpenDRIVERoadLanesLaneSection laneSection, List<Vector3> sectionRefPointsOffset)
        {
            var refMapLineId = $"MapLine_{roadIdLaneSectionId}_0";
            var refMapLineObj = new GameObject(refMapLineId);
            var refMapLine = refMapLineObj.AddComponent<MapLine>();
            Id2MapLine[refMapLineId] = refMapLine;

            var centerLane = laneSection.center.lane;
            refMapLine.mapWorldPositions = sectionRefPointsOffset;
            refMapLine.transform.position = Lanelet2MapImporter.GetAverage(sectionRefPointsOffset);

            if (centerLane.roadMark != null)
            {
                // Note: centerLane might have more than one road mark, currently we only use the first one
                var centerLaneRoadMark = centerLane.roadMark[0];
                refMapLine.lineType = GetLineType(centerLaneRoadMark.type1, centerLaneRoadMark.color);
            }
            else
            {
                refMapLine.lineType = MapData.LineType.DOTTED_WHITE;
            }

            return refMapLine;
        }

        void CreateLinesLanes(string roadId, int laneSectionId, List<Vector3> sectionRefPoints, MapLine refMapLine, lane[] lanes, GameObject parentObj, bool isLeft)
        {
            var centerLinePoints = new List<Vector3>(refMapLine.mapWorldPositions);
            List<Vector3> curLeftBoundaryPoints = centerLinePoints;

            IEnumerable<lane> it;
            it = isLeft ? lanes.Reverse() : lanes;

            foreach (var curLane in it)
            {
                var id = curLane.id.ToString();
                List<Vector3> curRightBoundaryPoints = CalculateRightBoundaryPoints(sectionRefPoints, curLeftBoundaryPoints, curLane, isLeft);

                var mapLineId = $"MapLine_road{roadId}_section{laneSectionId}_{id}";
                var mapLineObj = new GameObject(mapLineId);
                var mapLine = mapLineObj.AddComponent<MapLine>();
                Id2MapLine[mapLineId] = mapLine;

                var lanePoints = GetLanePoints(curLeftBoundaryPoints, curRightBoundaryPoints);
                if (lanePoints.Count == 0)
                {
                    Debug.LogError($"Not able to get correct lane points, skip importing lane {id} (roadId: {roadId})");
                }
                if (isLeft) lanePoints.Reverse();

                CreateLane(roadId, laneSectionId, curLane, lanePoints, parentObj);

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

        private List<Vector3> CalculateRightBoundaryPoints(List<Vector3> sectionRefPoints, List<Vector3> curLeftBoundaryPoints, lane curLane, bool isLeft)
        {
            var curRightBoundaryPoints = new List<Vector3>();
            int curWidthIdx = 0;
            Debug.Assert(curLane.Items.Length > 0);

            var width = curLane.Items[0] as laneWidth;
            var sOffset = width.sOffset;
            for (int idx = 0; idx < curLeftBoundaryPoints.Count; idx++)
            {
                Vector3 point = curLeftBoundaryPoints[idx];
                var s = SectionRefPointSValue[idx];
                if (s >= sOffset)
                {
                    while (curWidthIdx < curLane.Items.Length - 1 && s >= ((laneWidth)curLane.Items[curWidthIdx + 1]).sOffset)
                    {
                        width = curLane.Items[++curWidthIdx] as laneWidth;
                        sOffset = width.sOffset;
                    }
                }

                var ds = s - sOffset;
                var widthValue = width.a + width.b * ds + width.c * ds * ds + width.d * ds * ds * ds;
                Vector3 normalDir;
                normalDir = GetNormalDir(sectionRefPoints, idx, isLeft);

                var newPoint = point + (float)widthValue * normalDir;
                if (idx > 0)
                {
                    var p1 = curLeftBoundaryPoints[idx-1];
                    var p2 = curRightBoundaryPoints[idx-1];
                    var p3 = curLeftBoundaryPoints[idx];
                    var isIntersect = Utility.LineSegementsIntersect(ToVector2(p1), ToVector2(p2), ToVector2(p3), ToVector2(newPoint), out var intersect);
                    if (isIntersect) newPoint = p2;
                }
                curRightBoundaryPoints.Add(newPoint);
            }

            return curRightBoundaryPoints;
        }

        public static Vector3 GetNormalDir(List<Vector3> points, int index, bool isLeft)
        {
            Vector3 normalDir = Vector3.zero;

            for (int i = index + 1; i < points.Count; i++)
            {
                if (Vector3.Distance(points[index], points[i]) > 0.01)
                {
                    normalDir += Vector3.Cross(Vector3.up, points[i] - points[index]);
                    break;
                }
            }
            for (int i = index - 1; i >= 0; i--)
            {
                if (Vector3.Distance(points[index], points[i]) > 0.01)
                {
                    normalDir += Vector3.Cross(Vector3.up, points[index] - points[i]);
                    break;
                }
            }
            return normalDir.normalized * (isLeft ? -1 : +1);
        }

        static Vector3 GetRightNormalDir(Vector3 p1, Vector3 p2)
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

            if (leftBoundaryPoints.Count != rightBoundaryPoints.Count ||
                (longerDistance > 1 && leftBoundaryPoints.Count != partitions + 1)) // neglect too short lines
            {
                Debug.LogError("Something wrong with number of points. (left, right, partitions): (" +
                    leftBoundaryPoints.Count + ", " + rightBoundaryPoints.Count + ", " + partitions + ")");
                return new List<Vector3>();
            }

            List<Vector3> lanePoints = new List<Vector3>();
            for (int i = 0; i < leftBoundaryPoints.Count; i++)
            {
                Vector3 centerPoint = (leftBoundaryPoints[i] + rightBoundaryPoints[i]) / 2;
                lanePoints.Add(centerPoint);
            }

            return lanePoints;
        }

        void CreateLane(string roadId, int laneSectionId, lane curLane, List<Vector3> lanePoints, GameObject parentObj)
        {
            var roadIdLaneSectionId = $"road{roadId}_section{laneSectionId}";
            int laneId = curLane.id;

            var mapLaneObj = new GameObject($"MapLane_{roadIdLaneSectionId}_{laneId}");
            var mapLane = mapLaneObj.AddComponent<MapLane>();
            mapLane.mapWorldPositions = lanePoints;
            mapLane.transform.position = Lanelet2MapImporter.GetAverage(lanePoints);
            mapLane.transform.parent = parentObj.transform;
            ApolloMapImporter.UpdateLocalPositions(mapLane);

            float? laneSpeed = null;
            if (curLane.speed != null && ParseLaneSpeed(curLane, out float speedLimit))
            {
                laneSpeed = speedLimit;
            }
            else if (RoadId2Speed.ContainsKey(roadId))
            {
                laneSpeed = RoadId2Speed[roadId];
            }

            if (laneSpeed != null) mapLane.speedLimit = laneSpeed.Value;

            var mapLine = Id2MapLine[$"MapLine_{roadIdLaneSectionId}_{laneId}"];
            mapLane.rightLineBoundry = mapLine;
            // Left lanes
            if (laneId > 0)
            {
                mapLine = Id2MapLine[$"MapLine_{roadIdLaneSectionId}_{laneId - 1}"];
            }
            // Right lanes
            else
            {
                mapLine = Id2MapLine[$"MapLine_{roadIdLaneSectionId}_{laneId + 1}"];
            }
            mapLane.leftLineBoundry = mapLine;
            var Id2MapLane = Roads[roadId][laneSectionId];
            Id2MapLane[laneId] = mapLane;

            Lane2LaneType[mapLane] = curLane.type;
        }

        bool ParseLaneSpeed(lane curLane, out float speedLimit)
        {
            var speed = curLane.speed[0]; // currently we don't support multiple speed limit for same lane
            speedLimit = (float)speed.max;
            if (speedLimit < 0) return false;
            if (ConvertSpeed(speed.unit, ref speedLimit)) return true;
            return false;
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

        void ImportJunctions()
        {
            if (OpenDRIVEMap.junction == null) return;

            foreach (var junction in OpenDRIVEMap.junction)
            {
                var mapIntersection = CreateMapIntersection(junction.id);
                var incomingRoadIds = new HashSet<string>();
                foreach (var connection in junction.connection)
                {
                    var incomingRoadId = connection.incomingRoad;
                    incomingRoadIds.Add(incomingRoadId);
                    var connectingRoadId = connection.connectingRoad;
                    var contactPoint = connection.contactPoint; // contact point on the connecting road
                    var incomingLaneSections = Roads[incomingRoadId];
                    var connectingLaneSections = Roads[connectingRoadId];
                    Dictionary<int, MapLane> incomingId2MapLane, connectingId2MapLane;

                    // First assume roads are with same directions
                    if (contactPoint == contactPoint.start)
                    {
                        incomingId2MapLane = incomingLaneSections.Last();
                        connectingId2MapLane = connectingLaneSections.First();
                    }
                    else
                    {
                        incomingId2MapLane = incomingLaneSections.First();
                        connectingId2MapLane = connectingLaneSections.Last();
                    }

                    if (!IncomingRoadId2leftLanes.ContainsKey(incomingRoadId)) IncomingRoadId2leftLanes[incomingRoadId] = new HashSet<MapLane>();
                    if (!IncomingRoadId2rightLanes.ContainsKey(incomingRoadId)) IncomingRoadId2rightLanes[incomingRoadId] = new HashSet<MapLane>();
                    MapLane incomingLane, connectingLane;
                    if (connection.laneLink.Length == 0)
                    {
                        // All incoming lanes are linked to lanes with identical IDs on the connecting road
                        // Get all lanes in last laneSection of incomingRoad connect with corresponding lanes in connecting road
                        foreach (var laneId in incomingId2MapLane.Keys)
                        {
                            incomingLane = incomingId2MapLane[laneId];
                            connectingLane = connectingId2MapLane[laneId];
                            UpdateBeforesAfters(contactPoint, connectingLane, incomingLane);
                            if (laneId > 0 && Lane2LaneType[incomingLane] == laneType.driving) IncomingRoadId2leftLanes[incomingRoadId].Add(incomingLane);
                            if (laneId < 0 && Lane2LaneType[incomingLane] == laneType.driving) IncomingRoadId2rightLanes[incomingRoadId].Add(incomingLane);
                        }
                    }
                    else
                    {
                        // Update incomingId2MapLane if two roads directions are opposite
                        if (connection.laneLink[0].from * connection.laneLink[0].to < 0) incomingId2MapLane =
                            contactPoint == contactPoint.start ? incomingLaneSections.First() : incomingLaneSections.Last();
                        foreach (var laneLink in connection.laneLink)
                        {
                            var incomingLaneId = laneLink.from;
                            if (!incomingId2MapLane.ContainsKey(incomingLaneId))
                            {
                                Debug.LogWarning($"incoming lane not found: junction {junction.id} incoming {incomingRoadId} connecting {connectingRoadId} laneId {incomingLaneId}");
                                continue;
                            }
                            incomingLane = incomingId2MapLane[incomingLaneId];

                            if (!connectingId2MapLane.ContainsKey(laneLink.to))
                            {
                                Debug.LogWarning($"connecting lane not found: junction {junction.id} incoming {incomingRoadId} connecting {connectingRoadId} laneId {laneLink.to}");
                                continue;
                            }
                            connectingLane = connectingId2MapLane[laneLink.to];
                            UpdateBeforesAfters(contactPoint, incomingLane, connectingLane);
                            if (incomingLaneId > 0 && Lane2LaneType[incomingLane] == laneType.driving) IncomingRoadId2leftLanes[incomingRoadId].Add(incomingLane);
                            if (incomingLaneId < 0 && Lane2LaneType[incomingLane] == laneType.driving) IncomingRoadId2rightLanes[incomingRoadId].Add(incomingLane);
                        }
                    }

                    RoadId2IntersectionId[connectingRoadId] = junction.id;
                    MoveConnectingLanesUnderIntersection(mapIntersection, connectingId2MapLane.Values.ToList()); // intersection position is 0, 0, 0 here
                }
                if (IsCreateStopLines) CreateStopLines(incomingRoadIds, mapIntersection);
            }
        }

        // Create a stop line for every incoming road
        void CreateStopLines(HashSet<string> incomingRoadIds, MapIntersection mapIntersection)
        {
            foreach (var roadId in incomingRoadIds)
            {
                var isLeftLanes = isLeftLanesConnected(roadId, mapIntersection);
                if (isLeftLanes)
                {
                    var mapLanesLeft = IncomingRoadId2leftLanes[roadId];
                    CreateStopLine(mapIntersection, roadId, mapLanesLeft);
                }
                else
                {
                    var mapLanesRight = IncomingRoadId2rightLanes[roadId];
                    CreateStopLine(mapIntersection, roadId, mapLanesRight);
                }
            }
        }

        // Check is leftLanes of this road is connected with mapIntersection
        private bool isLeftLanesConnected(string roadId, MapIntersection mapIntersection)
        {
            var firstLaneSection = Roads[roadId][0];
            MapLane firstLane;
            bool isLeft = true;
            if (firstLaneSection.ContainsKey(-1))
            {
                firstLane = firstLaneSection[-1];
                isLeft = false;
            }
            else
            {
                firstLane = firstLaneSection[1];
            }

            var firstLanePositions = firstLane.mapWorldPositions.ToList();
            if (isLeft) firstLanePositions.Reverse();

            MapDataPoints anyLaneLine = null;
            foreach (Transform child in mapIntersection.transform)
            {
                var mapDataPoints = child.gameObject.GetComponent<MapDataPoints>();
                if (mapDataPoints != null)
                {
                    anyLaneLine = mapDataPoints;
                }
            }
            if (anyLaneLine == null)
            {
                var msg = $"No lane/line imported under mapIntersection, we cannot find mapIntersection location.";
                msg += $"Please check roadId {roadId}, mapIntersection {mapIntersection.name}";
                Debug.LogWarning(msg);
            }
            var intersectionPosition = anyLaneLine.transform.position;
            // check is left lanes or right lanes connected with the intersection
            var roadStart2Intersection = (firstLanePositions.First() - intersectionPosition).magnitude;
            var roadEnd2Intersection = (firstLanePositions.Last() - intersectionPosition).magnitude;
            if (roadStart2Intersection < roadEnd2Intersection) return true;
            return false;
        }

        string GetSection(string laneName)
        {
            return laneName.Split('_')[2];
        }

        bool HaveDiffLanesSections(HashSet<MapLane> lanesSet)
        {
            var lanes = lanesSet.ToList();
            var section = GetSection(lanes[0].name);
            for (int i = 1; i < lanes.Count; i++)
            {
                if (GetSection(lanes[i].name) != section) return true;
            }

            return false;
        }

        private void CreateStopLine(MapIntersection mapIntersection, string roadId, HashSet<MapLane> mapLanes)
        {
            if (mapLanes.Count == 0 || HaveDiffLanesSections(mapLanes)) return;

            var firstMapLanePositions = GetOnlyItemFromSet(mapLanes).mapWorldPositions;
            var laneDir = (firstMapLanePositions.Last() - firstMapLanePositions[firstMapLanePositions.Count - 2]).normalized;
            var stopLinePositions = new List<Vector3>();
            var halfLaneWidth = 2;

            if (mapLanes.Count == 1)
            {
                GetStopLinePositions(firstMapLanePositions, laneDir, stopLinePositions, halfLaneWidth);
            }
            else
            {
                foreach (var mapLane in mapLanes)
                {
                    stopLinePositions.Add(mapLane.mapWorldPositions.Last() - laneDir * 1); // move back 1 m
                }
            }

            // For merging cases
            var lastIdx = 0;
            var needUpdate = true;
            for (int i = 1; i < stopLinePositions.Count; i++)
            {
                if ((stopLinePositions[i] - stopLinePositions[lastIdx]).magnitude > halfLaneWidth)
                {
                    needUpdate = false;
                    break;
                }
            }
            if (needUpdate)
            {
                stopLinePositions.Clear();
                GetStopLinePositions(firstMapLanePositions, laneDir, stopLinePositions, halfLaneWidth);
            }

            GameObject stopLineObj = new GameObject($"stopLine_road{roadId}");
            var stopLine = stopLineObj.AddComponent<MapLine>();
            stopLine.transform.position = Lanelet2MapImporter.GetAverage(stopLinePositions);
            stopLine.transform.rotation = Quaternion.LookRotation(laneDir);
            stopLine.lineType = MapData.LineType.STOP;
            stopLine.mapWorldPositions = stopLinePositions;
            stopLine.transform.parent = mapIntersection.transform;
            ApolloMapImporter.UpdateLocalPositions(stopLine);

            var intersectionId = mapIntersection.name.Split('_')[1];
            if (IntersectionId2StopLines.ContainsKey(intersectionId)) IntersectionId2StopLines[intersectionId].Add(stopLine);
            else IntersectionId2StopLines[intersectionId] = new List<MapLine>() { stopLine };
        }

        private static void GetStopLinePositions(List<Vector3> firstMapLanePositions, Vector3 laneDir, List<Vector3> stopLinePositions, int halfLaneWidth)
        {
            var endPoint = firstMapLanePositions.Last();
            var normalDir = Vector3.Cross(laneDir, Vector3.up).normalized;
            stopLinePositions.Add(endPoint + normalDir * halfLaneWidth - laneDir * 1); // half width to the left and 1 m back
            stopLinePositions.Add(endPoint - normalDir * halfLaneWidth - laneDir * 1);
        }

        static MapLane GetOnlyItemFromSet(HashSet<MapLane> mapLanes)
        {
            MapLane mapLane = null;
            foreach (var lane in mapLanes)
            {
                return lane;
            }
            return mapLane;
        }

        void MoveConnectingLanesUnderIntersection(MapIntersection mapIntersection, List<MapLane> connectingMapLanes)
        {
            foreach (var lane in connectingMapLanes)
            {
                MoveGameObjectUnderIntersection(mapIntersection, lane);

                // Move MapLine with same name
                StringBuilder lineName = new StringBuilder(lane.name);
                lineName[4] = 'i';
                var mapLine = Id2MapLine[lineName.ToString()];
                mapLine.lineType = MapData.LineType.VIRTUAL;
                MoveGameObjectUnderIntersection(mapIntersection, mapLine);

                // Move reference MapLine
                var splittedName = lineName.ToString().Split('_');
                splittedName[splittedName.Length - 1] = "0";
                string refMapLineName = string.Join("_", splittedName);
                MapLine refMapLine = Id2MapLine[refMapLineName];
                refMapLine.lineType = MapData.LineType.VIRTUAL;
                MoveGameObjectUnderIntersection(mapIntersection, refMapLine);
            }
        }

        private static void MoveGameObjectUnderIntersection(MapIntersection mapIntersection, MapDataPoints mapDataPoints)
        {
            mapDataPoints.transform.parent = mapIntersection.transform;
            ApolloMapImporter.UpdateLocalPositions(mapDataPoints);
        }

        void UpdateBeforesAfters(contactPoint contactPoint, MapLane curLane, MapLane linkedLane)
        {
            var curLaneId = GetLaneId(curLane.name);
            var linkedLaneId = GetLaneId(linkedLane.name);
            var isOppositeRoads = curLaneId * linkedLaneId < 0; // If the roads of the two lanes are opposite or not

            HashSet<MapLane> curLaneSetToAddTo, linkedLaneSetToAddTo;
            CheckExistence(curLane);
            CheckExistence(linkedLane);

            // contactPoint: contact point of link on the linked element
            if (contactPoint == contactPoint.start)
            {
                linkedLaneSetToAddTo = linkedLaneId < 0 ? Lane2BeforesAfters[linkedLane].befores : Lane2BeforesAfters[linkedLane].afters;
                if (curLaneId < 0) // same direction with reference line
                {
                    curLaneSetToAddTo = isOppositeRoads ? Lane2BeforesAfters[curLane].befores : Lane2BeforesAfters[curLane].afters;
                }
                else
                {
                    curLaneSetToAddTo = isOppositeRoads ? Lane2BeforesAfters[curLane].afters : Lane2BeforesAfters[curLane].befores;
                }
            }
            else
            {
                linkedLaneSetToAddTo = linkedLaneId < 0 ? Lane2BeforesAfters[linkedLane].afters : Lane2BeforesAfters[linkedLane].befores;
                if (curLaneId < 0)
                {
                    curLaneSetToAddTo = isOppositeRoads ? Lane2BeforesAfters[curLane].afters : Lane2BeforesAfters[curLane].befores;
                }
                else
                {
                    curLaneSetToAddTo = isOppositeRoads ? Lane2BeforesAfters[curLane].befores : Lane2BeforesAfters[curLane].afters;
                }
            }

            curLaneSetToAddTo.Add(linkedLane);
            linkedLaneSetToAddTo.Add(curLane);
        }

        int GetLaneId(string name)
        {
            return int.Parse(name.Split('_').Last());
        }

        void CheckExistence(MapLane lane)
        {
            if (!Lane2BeforesAfters.ContainsKey(lane)) Lane2BeforesAfters[lane] = new BeforesAfters();
        }
        MapIntersection CreateMapIntersection(string id)
        {
            if (Id2MapIntersection.ContainsKey(id)) return Id2MapIntersection[id];
            var mapIntersectionObj = new GameObject("MapIntersection_" + id);
            var mapIntersection = mapIntersectionObj.AddComponent<MapIntersection>();
            mapIntersection.transform.parent = Intersections.transform;
            mapIntersection.triggerBounds = new Vector3(0, 10, 0);
            Id2MapIntersection[id] = mapIntersection;

            return mapIntersection;
        }

        void UpdateAllLanesBeforesAfters()
        {
            foreach (var road in OpenDRIVEMap.road)
            {
                var roadId = road.id;
                var lanes = road.lanes;
                var roadLink = road.link;
                for (int i = 0; i < lanes.laneSection.Length; i++)
                {
                    var Id2MapLane = Roads[road.id][i];
                    if (Id2MapLane.Count == 0) continue; // Skip empty laneSection

                    // Default values if not on the first or last laneSection
                    var preLaneSectionId = i - 1;
                    var sucLaneSectionId = i + 1;
                    var preRoadId = roadId;
                    var sucRoadId = roadId;
                    contactPoint? preContactPoint = null;
                    contactPoint? sucContactPoint = null;

                    if (i == 0)
                    {
                        preRoadId = null;
                        if (roadLink != null && roadLink.predecessor != null
                            && roadLink.predecessor.elementType == elementType.road)
                        {
                            preRoadId = roadLink.predecessor.elementId;
                            preContactPoint = roadLink.predecessor.contactPoint;
                            preLaneSectionId = GetLaneSectionId(preRoadId, preContactPoint);
                        }
                    }
                    if (i == lanes.laneSection.Length - 1)
                    {
                        sucRoadId = null;
                        if (roadLink != null && roadLink.successor != null
                            && roadLink.successor.elementType == elementType.road)
                        {
                            sucRoadId = roadLink.successor.elementId;
                            sucContactPoint = roadLink.successor.contactPoint;
                            sucLaneSectionId = GetLaneSectionId(sucRoadId, sucContactPoint);
                        }
                    }

                    var laneSection = lanes.laneSection[i];
                    if (preContactPoint == null) preContactPoint = contactPoint.end;
                    if (sucContactPoint == null) sucContactPoint = contactPoint.start;

                    if (laneSection.left != null)
                    {
                        UpdateLanesBeforesAfters(laneSection.left.lane, Id2MapLane, preRoadId, preLaneSectionId,
                                            sucRoadId, sucLaneSectionId, preContactPoint.Value, sucContactPoint.Value);
                    }

                    if (laneSection.right != null)
                    {
                        UpdateLanesBeforesAfters(laneSection.right.lane, Id2MapLane, preRoadId, preLaneSectionId,
                                            sucRoadId, sucLaneSectionId, preContactPoint.Value, sucContactPoint.Value);
                    }

                }
            }
        }

        int GetLaneSectionId(string linkedRoadId, contactPoint? contactPoint)
        {
            int laneSectionId;
            if (contactPoint == Schemas.contactPoint.start) laneSectionId = 0;
            else laneSectionId = Roads[linkedRoadId].Count - 1;
            return laneSectionId;
        }

        void UpdateLanesBeforesAfters(lane[] lanes, Dictionary<int, MapLane> curId2MapLane,
            string preRoadId, int preLaneSectionId, string sucRoadId, int sucLaneSectionId,
            contactPoint preContactPoint, contactPoint sucContactPoint)
        {
            foreach (var lane in lanes)
            {
                var link = lane.link;
                if (link == null) continue;

                var curLane = curId2MapLane[lane.id];

                var lanePredecessor = link.predecessor;
                if (lanePredecessor != null)
                {
                    if (preRoadId == null)
                    {
                        Debug.LogWarning($"Lane has predecessor but the corresponding road has no predecessor road id. Skip updating beforeAfters for lane {curLane.name}.");
                        continue;
                    }
                    var preLaneId = lanePredecessor.id;
                    var preLane = GetLaneFromRoads(preRoadId, preLaneSectionId, preLaneId);
                    if (preLane != null) UpdateBeforesAfters(preContactPoint, curLane, preLane);
                }

                var laneSuccessor = link.successor;
                if (laneSuccessor != null)
                {
                    if (sucRoadId == null)
                    {
                        Debug.LogWarning($"Lane has successor but the corresponding road has no successor road id. Skip updating beforeAfters for lane {curLane.name}.");
                        continue;
                    }
                    var sucLaneId = laneSuccessor.id;
                    var sucLane = GetLaneFromRoads(sucRoadId, sucLaneSectionId, sucLaneId);
                    if (sucLane != null) UpdateBeforesAfters(sucContactPoint, curLane, sucLane);
                }
            }
        }

        MapLane GetLaneFromRoads(string roadId, int laneSectionId, int laneId)
        {
            var laneSections = Roads[roadId];
            Dictionary<int, MapLane> Id2MapLane = laneSections[laneSectionId];
            if (Id2MapLane.ContainsKey(laneId)) return Id2MapLane[laneId];
            else return null; // in case too short lanes are not created
        }

        // Connect lanes by adjusting starting/ending points
        void ConnectLanes()
        {
            var visitedLaneIdsEnd = new HashSet<string>(); // lanes whose end point has been visited
            var visitedLaneIdsStart = new HashSet<string>(); // lanes whose start point has been visited
            foreach (var mapLane in Lane2BeforesAfters.Keys)
            {
                var beforesAfters = Lane2BeforesAfters[mapLane];
                var laneId = mapLane.name;
                var befores = beforesAfters.befores;
                var afters = beforesAfters.afters;
                var positions = mapLane.mapWorldPositions;
                if (befores.Count > 0)
                {
                    foreach (var beforeLane in befores)
                    {
                        var belowThreshold = (positions[0] - beforeLane.mapWorldPositions.Last()).magnitude < ConnectLaneThreshold;
                        if (belowThreshold) mapLane.befores.Add(beforeLane);
                        else Debug.LogWarning($"{mapLane.name} is far away from its before lane {beforeLane.name}, skipping connecting them.");
                        if (!visitedLaneIdsEnd.Contains(beforeLane.name) && belowThreshold) AdjustStartOrEndPoint(positions, beforeLane, true);
                        visitedLaneIdsEnd.Add(beforeLane.name);
                    }
                }

                if (afters.Count > 0)
                {
                    foreach (var afterLane in afters)
                    {
                        var belowThreshold = (positions.Last() - afterLane.mapWorldPositions.First()).magnitude < ConnectLaneThreshold;
                        if (belowThreshold) mapLane.afters.Add(afterLane);
                        else Debug.LogWarning($"{mapLane.name} is far away from its after lane {afterLane.name}, skipping connecting them.");
                        if (!visitedLaneIdsStart.Contains(afterLane.name) && belowThreshold) AdjustStartOrEndPoint(positions, afterLane, false);
                        visitedLaneIdsStart.Add(afterLane.name);
                    }
                }
            }
        }

        // Make predecessor/successor lane's end/start point same as current lane's start/end point
        void AdjustStartOrEndPoint(List<Vector3> positions, MapLane connectLane, bool adjustEndPoint)
        {
            var connectLaneWorldPositions = connectLane.mapWorldPositions;
            var connectLaneLocalPositions = connectLane.mapLocalPositions;
            if (adjustEndPoint)
            {
                connectLaneWorldPositions[connectLaneWorldPositions.Count - 1] = positions.First();
                connectLaneLocalPositions[connectLaneLocalPositions.Count - 1] = connectLane.transform.InverseTransformPoint(positions.First());
            }
            else
            {
                connectLaneWorldPositions[0] = positions.Last();
                connectLaneLocalPositions[0] = connectLane.transform.InverseTransformPoint(positions.Last());
            }
        }

        void LinkSignalsWithStopLines()
        {
            foreach (var entry in IntersectionId2StopLines)
            {
                var intersectionId = entry.Key;
                var stopLines = entry.Value;

                if (IntersectionId2MapSigns.ContainsKey(intersectionId))
                {
                    var mapSigns = IntersectionId2MapSigns[intersectionId];
                    foreach (var mapSign in mapSigns)
                    {
                        var position = mapSign.transform.position;
                        var stopLine = GetNearestStopLine(stopLines, position);
                        if (stopLine == null)
                        {
                            Debug.LogError($"No nearest stop line found for mapSign {mapSign.name}");
                            throw new Exception();
                        }
                        stopLine.isStopSign = true;
                        mapSign.stopLine = stopLine;
                    }
                }

                if (IntersectionId2MapSignals.ContainsKey(intersectionId))
                {
                    var mapSignals = IntersectionId2MapSignals[intersectionId];
                    foreach (var mapSignal in mapSignals)
                    {
                        var signalDirection = mapSignal.transform.forward;
                        var stopLine = GetSignalPointedStopLine(stopLines, signalDirection);
                        if (stopLine == null)
                        {
                            Debug.LogError($"No nearest stop line found for mapSignal {mapSignal.name}");
                            throw new Exception();
                        }
                        mapSignal.stopLine = stopLine;
                    }
                }
            }
        }

        MapLine GetSignalPointedStopLine(List<MapLine> stopLines, Vector3 signalDirection)
        {
            var minProdut = float.MaxValue;
            MapLine pointedStopLine = null;
            foreach (var stopLine in stopLines)
            {
                var product = Vector3.Dot(signalDirection, stopLine.transform.forward);
                if (product < minProdut)
                {
                    minProdut = product;
                    pointedStopLine = stopLine;
                }
            }

            return pointedStopLine;
        }

        MapLine GetNearestStopLine(List<MapLine> stopLines, Vector3 position)
        {
            var minDist = float.MaxValue;
            MapLine nearestStopLine = null;
            foreach (var stopLine in stopLines)
            {
                var dist = (position - stopLine.transform.position).magnitude;
                if (dist < minDist)
                {
                    minDist = dist;
                    nearestStopLine = stopLine;
                }
            }

            return nearestStopLine;
        }

        void CleanUp()
        {
            MapAnnotationData = new MapManagerData();

            // Destroy nondrivable lanes
            var linesToRemove = new HashSet<MapLine>();
            var lanesToKeep = new List<MapLane>();
            foreach (var entry in Lane2LaneType)
            {
                if (entry.Value != laneType.driving)
                {
                    linesToRemove.Add(entry.Key.leftLineBoundry);
                    linesToRemove.Add(entry.Key.rightLineBoundry);
                    UpdateLaneBeforeAfterLanes(entry.Key);
                    UnityEngine.Object.DestroyImmediate(entry.Key.gameObject);
                    Lane2BeforesAfters.Remove(entry.Key);
                }
                else
                {
                    lanesToKeep.Add(entry.Key);
                }
            }

            foreach (var entry in RoadId2laneSections)
            {
                var roadId = entry.Key;
                var laneSections = entry.Value;
                foreach (var laneSectionId in laneSections)
                {
                    foreach (var laneEntry in Roads[roadId][laneSectionId])
                    {
                        var lane = laneEntry.Value;
                        if (Lane2LaneType[lane] != laneType.driving) continue; // already removed.

                        linesToRemove.Add(lane.leftLineBoundry);
                        linesToRemove.Add(lane.rightLineBoundry);
                        UpdateLaneBeforeAfterLanes(lane, true);
                        UnityEngine.Object.DestroyImmediate(lane.gameObject);
                        Lane2BeforesAfters.Remove(lane);
                        lanesToKeep.Remove(lane);
                    }
                }
            }

            if (IsConnectLanes)
            {
                var lanesDataToKeep = GetLanesData(lanesToKeep);
                Lanelet2MapExporter.AlignPointsInLines(lanesDataToKeep);
                CopyLinesData(lanesDataToKeep);
            }

            foreach (var lane in lanesToKeep)
            {
                RemoveLineFromSet(linesToRemove, lane.leftLineBoundry);
                RemoveLineFromSet(linesToRemove, lane.rightLineBoundry);
            }

            foreach (var line in linesToRemove)
            {
                UnityEngine.Object.DestroyImmediate(line.gameObject);
            }

            MoveSingleLaneSections();
            RemoveEmptyLaneSections();
            if (SingleLaneRoads.transform.childCount == 0) UnityEngine.Object.DestroyImmediate(SingleLaneRoads);

            // Move ungrouped objects to correct intersection
            foreach (var entry in UngroupedObject2RoadId)
            {
                var obj = entry.Key;
                var roadId = entry.Value;
                if (RoadId2IntersectionId.ContainsKey(roadId))
                {
                    var intersectionId = RoadId2IntersectionId[roadId];
                    obj.transform.parent = Id2MapIntersection[intersectionId].transform;

                    // if signal
                    if (obj.GetComponent<MapSignal>() != null)
                    {
                        var mapSignal = obj.GetComponent<MapSignal>();
                        if (IntersectionId2MapSignals.ContainsKey(intersectionId)) IntersectionId2MapSignals[intersectionId].Add(mapSignal);
                        else IntersectionId2MapSignals[intersectionId] = new List<MapSignal>(){mapSignal};
                    }
                    // if sign
                    if (obj.GetComponent<MapSign>() != null)
                    {
                        var mapSign = obj.GetComponent<MapSign>();
                        if (IntersectionId2MapSigns.ContainsKey(intersectionId)) IntersectionId2MapSigns[intersectionId].Add(mapSign);
                        else IntersectionId2MapSigns[intersectionId] = new List<MapSign>(){mapSign};
                    }
                }
                else
                {
                    Debug.LogWarning($"Cannot find associated intersection for {obj.name}.");
                }
            }

            var mapAnnotations = ScriptableObject.CreateInstance<MapAnnotations>();
            mapAnnotations.RemoveExtraLines(false, MapHolder);
        }

        private void MoveSingleLaneSections()
        {
            var mapLaneSections = new List<MapLaneSection>(MapAnnotationData.GetData<MapLaneSection>());
            foreach (var mapLaneSection in mapLaneSections)
            {
                var lanes = new List<MapLane>(mapLaneSection.GetComponentsInChildren<MapLane>());
                if (lanes.Count == 1)
                {
                    lanes[0].transform.parent = SingleLaneRoads.transform;
                    var lines = new List<MapLine>(mapLaneSection.GetComponentsInChildren<MapLine>());
                    foreach (var line in lines) line.transform.parent = SingleLaneRoads.transform;
                }
            }
        }

        private void RemoveEmptyLaneSections()
        {
            var mapLaneSections = new List<MapLaneSection>(MapAnnotationData.GetData<MapLaneSection>());
            foreach (var mapLaneSection in mapLaneSections)
            {
                if (mapLaneSection.transform.childCount == 0) UnityEngine.Object.DestroyImmediate(mapLaneSection.gameObject);
            }
        }

        void UpdateLaneBeforeAfterLanes(MapLane lane, bool updateBeforeAfter=false)
        {
            if (lane.befores.Count > 0)
            {
                var afters = lane.afters;
                foreach (var before in lane.befores)
                {
                    if (before.afters.Contains(lane)) before.afters.Remove(lane);
                    if (updateBeforeAfter && afters.Count > 0) before.afters.AddRange(afters);
                }
            }

            if (lane.afters.Count > 0)
            {
                var befores = lane.befores;
                foreach (var after in lane.afters)
                {
                    if (after.befores.Contains(lane)) after.befores.Remove(lane);
                    if (updateBeforeAfter && befores.Count > 0) after.befores.AddRange(befores);
                }
            }
        }

        void CopyLinesData(HashSet<LaneData> lanesData)
        {
            foreach (var laneData in lanesData)
            {
                var leftLine = laneData.mapLane.leftLineBoundry;
                CopyLineData(leftLine);
                var rightLine = laneData.mapLane.rightLineBoundry;
                CopyLineData(rightLine);
            }
        }

        void CopyLineData(MapLine line)
        {
            var lineData = LineData.Line2LineData[line];
            line.mapWorldPositions = lineData.mapWorldPositions;
            line.mapLocalPositions = lineData.mapLocalPositions;
        }

        HashSet<LaneData> GetLanesData(List<MapLane> laneSegments)
        {
            LaneData.Lane2LaneData.Clear();
            LineData.Line2LineData.Clear();
            var lanesData = new HashSet<LaneData>();
            foreach (var lane in laneSegments)
            {
                var laneData = new LaneData(lane);
                lanesData.Add(laneData);
                LaneData.Lane2LaneData[lane] = laneData;
                var leftLineData = new LineData(lane.leftLineBoundry);
                LineData.Line2LineData[lane.leftLineBoundry] = leftLineData;
                var rightLineData = new LineData(lane.rightLineBoundry);
                LineData.Line2LineData[lane.rightLineBoundry] = rightLineData;
            }

            CopyLaneBeforeAfters(lanesData);

            return lanesData;
        }

        void CopyLaneBeforeAfters(HashSet<LaneData> lanesData)
        {
            foreach (var laneData in lanesData)
            {
                var befores = laneData.mapLane.befores;
                foreach (var before in befores)
                {
                    if (LaneData.Lane2LaneData.ContainsKey(before))
                    {
                        var beforeLaneData = LaneData.Lane2LaneData[before];
                        laneData.befores.Add(beforeLaneData);
                    }
                }

                var afters = laneData.mapLane.afters;
                foreach (var after in afters)
                {
                    if (LaneData.Lane2LaneData.ContainsKey(after))
                    {
                        var afterLaneData = LaneData.Lane2LaneData[after];
                        laneData.afters.Add(afterLaneData);
                    }
                }
            }
        }

        private void RemoveLineFromSet(HashSet<MapLine> linesToRemove, MapLine line)
        {
            if (linesToRemove.Contains(line))
            {
                linesToRemove.Remove(line);
            }
        }

        static Vector2 ToVector2(Vector3 pt)
        {
            return new Vector2(pt.x, pt.z);
        }

        static Vector3 ToVector3(Vector2 p)
        {
            return new Vector3(p.x, 0f, p.y);
        }

        public static bool IsIncreasingMontonically<T>(List<T> list) where T : IComparable
        {
            return list.Zip(list.Skip(1), (a, b) => a.CompareTo(b) <= 0)
                .All(b => b);
        }
    }


}