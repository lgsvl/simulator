/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using UnityEngine;
using UnityEditor;
using Simulator.Map;
using System.Linq;

[ExecuteInEditMode]
[CustomEditor(typeof(MapOrigin))]
public class MapOriginEditor : Editor
{
    private TimeZoneInfo[] TimeZones;
    private string[] NPCSizes;

    private void OnEnable()
    {
        TimeZones = TimeZoneInfo.GetSystemTimeZones().OrderBy(tz => tz.BaseUtcOffset).ToArray();
        NPCSizes = Enum.GetNames(typeof(NPCSizeType));
    }

    public override void OnInspectorGUI()
    {
        // styles
        var subtitleLabelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.LowerCenter, fontSize = 12 };

        MapOrigin origin = (MapOrigin)target;

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Map Origin", subtitleLabelStyle, GUILayout.ExpandWidth(true));
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        if (GUILayout.Button("Import from Latitude and Longitude"))
        {
            ImportCoordinates w = EditorWindow.GetWindow<ImportCoordinates>(false, nameof(ImportCoordinates), true);
            w.Init(origin);
            w.Show();
        }

        origin.OriginEasting = EditorGUILayout.DoubleField("Origin Easting", origin.OriginEasting);
        origin.OriginNorthing = EditorGUILayout.DoubleField("Origin Northing", origin.OriginNorthing);
        origin.UTMZoneId = EditorGUILayout.IntSlider("UTM Zone ID", origin.UTMZoneId, 1, 60);
        origin.AltitudeOffset = EditorGUILayout.FloatField("Altitude Offset", origin.AltitudeOffset);

        int currentlySelected = -1;
        currentlySelected = Array.FindIndex(TimeZones, tz => tz.DisplayName == origin.TimeZoneString);
        if (currentlySelected == -1)
        {
            var timeZone = origin.TimeZone;
            currentlySelected = Array.FindIndex(TimeZones, tz => tz.BaseUtcOffset == timeZone.BaseUtcOffset);
        }

        var values = TimeZones.Select(tz => tz.DisplayName.Replace("&", "&&")).ToArray();
        currentlySelected = EditorGUILayout.Popup("TimeZone", currentlySelected, values);
        if (currentlySelected != -1)
        {
            if (!origin.TimeZone.Equals(TimeZones[currentlySelected]))
            {
                origin.TimeZoneSerialized = TimeZones[currentlySelected].ToSerializedString();
                origin.TimeZoneString = TimeZones[currentlySelected].DisplayName;

                EditorUtility.SetDirty(origin);
                Repaint();
            }
        }

        if (GUILayout.Button("Add Reference Point"))
        {
            AddReferencePoint(origin);
        }
        if (GUILayout.Button("Update Map Origin using Reference Points"))
        {
            var points = FindObjectsOfType<MapOriginReferencePoint>();
            if (points.Length < 2)
            {
                Debug.LogError("We need at least 2 reference points");
            }
            else
            {
                var minimizeError = new MapOriginPositionErrorOptimalizer(origin, points);
                minimizeError.Optimize();
            }
        }
        GUILayout.Space(20);
        EditorGUILayout.LabelField("Map Settings", subtitleLabelStyle, GUILayout.ExpandWidth(true));
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        origin.IgnoreNPCVisible = EditorGUILayout.Toggle("Ignore NPC Visible", origin.IgnoreNPCVisible, GUILayout.ExpandWidth(true));
        origin.IgnoreNPCSpawnable = EditorGUILayout.Toggle("Ignore NPC Spawnable", origin.IgnoreNPCSpawnable, GUILayout.ExpandWidth(true));
        origin.IgnoreNPCBounds = EditorGUILayout.Toggle("Ignore NPC Bounds", origin.IgnoreNPCBounds, GUILayout.ExpandWidth(true));
        origin.NPCSizeMask = EditorGUILayout.MaskField("NPC Categories", origin.NPCSizeMask, NPCSizes);
        origin.NPCMaxCount = EditorGUILayout.IntSlider("NPC Max Count", origin.NPCMaxCount, 1, 30);
        origin.NPCSpawnBoundSize = EditorGUILayout.IntSlider("NPC Spawn Bounds Size", origin.NPCSpawnBoundSize, 25, 300);

        origin.IgnorePedBounds = EditorGUILayout.Toggle("Ignore Ped Bounds", origin.IgnorePedBounds, GUILayout.ExpandWidth(true));
        origin.IgnorePedVisible = EditorGUILayout.Toggle("Ignore Ped Visible", origin.IgnorePedVisible, GUILayout.ExpandWidth(true));
        origin.PedMaxCount = EditorGUILayout.IntSlider("Ped Max Count", origin.PedMaxCount, 1, 30);
        origin.PedSpawnBoundSize = EditorGUILayout.IntSlider("Ped Spawn Bounds Size", origin.PedSpawnBoundSize, 25, 300);

        GUILayout.Space(20);
        EditorGUILayout.LabelField("Map Meta Data", subtitleLabelStyle, GUILayout.ExpandWidth(true));
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Map Description");
        origin.Description = EditorGUILayout.TextArea(origin.Description);
        EditorGUILayout.EndHorizontal();

        if (GUI.changed)
            EditorUtility.SetDirty(origin);
    }


    private void AddReferencePoint(MapOrigin origin)
    {
        var index = FindObjectsOfType<MapOriginReferencePoint>(true).Length + 1;
        var p = new GameObject("ReferencePoint" + index).AddComponent<MapOriginReferencePoint>();
        if (SceneView.lastActiveSceneView != null)
        {
            var camera = SceneView.lastActiveSceneView.camera.transform;
            if (Physics.Raycast(new Ray(camera.position, camera.forward), out var hit))
            {
                p.transform.position = hit.point;
            }
        }
        var gps = origin.GetGpsLocation(p.transform.position);
        p.latitue = gps.Latitude;
        p.longitude = gps.Longitude;
        var mapHolder = FindObjectOfType<MapHolder>().transform;
        var holder = mapHolder.Find("ReferencePoints");
        if (holder == null)
        {
            holder = new GameObject("ReferencePoints").transform;
            holder.parent = mapHolder;
        }
        p.transform.parent = holder;
        Selection.activeGameObject = p.gameObject;
    }

    public class ImportCoordinates : EditorWindow
    {
        private double latitude;
        private double longitude;
        public MapOrigin origin;

        public void Init(MapOrigin origin)
        {
            this.origin = origin;
            var gps = origin.GetGpsLocation(origin.transform.position);
            latitude = Math.Round(gps.Latitude, 6);
            longitude = Math.Round(gps.Longitude, 6);
            minSize = new Vector2(250, 120);
            maxSize = new Vector2(300, 120);
        }
        void OnGUI()
        {
            GUILayout.Space(10);
            if (GUILayout.Button("Open Google Maps \u2316"))
            {
                Application.OpenURL($"https://www.google.com/maps/@{latitude},{longitude},15z");
            }
            GUILayout.Space(10);
            latitude = EditorGUILayout.DoubleField("Latitude", latitude);
            longitude = EditorGUILayout.DoubleField("Longitude", longitude);
            GUILayout.Space(10);

            if (GUILayout.Button("Import Coordinates"))
            {
                origin.UTMZoneId = MapOrigin.GetZoneNumberFromLatLon(latitude, longitude);
                origin.FromLatitudeLongitude(latitude, longitude, out var northing, out var easting);
                origin.OriginNorthing = Math.Round(northing, 2);
                origin.OriginEasting = Math.Round(easting, 2);
                this.Close();
            }
        }
    }
}