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
    private string HelpText = "Help is displayed here";

    private MapOrigin Origin;

    private void OnEnable()
    {
        TimeZones = TimeZoneInfo.GetSystemTimeZones().OrderBy(tz => tz.BaseUtcOffset).ToArray();
        NPCSizes = Enum.GetNames(typeof(NPCSizeType));
    }

    private void OnSceneGUI()
    {
        Origin = (MapOrigin)target;
        if (Origin == null)
            return;

        if (Origin.transform.rotation != Quaternion.Euler(new Vector3(0f, -90f, 0f)))
        {
            Origin.transform.rotation = Quaternion.Euler(new Vector3(0f, -90f, 0f));
            HelpText = "MapOrigin transform must be -90 in the Y axis";
        }

        var style = new GUIStyle();
        Handles.BeginGUI();
        if (GUILayout.Button("Orient Scene", GUILayout.Width(125)))
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            sceneView.orthographic = true;
            sceneView.LookAt(new Vector3(0f, 0f, 0f), Quaternion.Euler(90, -90, 0));
        }
        Handles.EndGUI();
        var size = HandleUtility.GetHandleSize(Origin.transform.position);
        style.fontSize = (int) (65 / size);
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.red;
        Handles.Label(Origin.transform.position + Origin.transform.forward + new Vector3(0, 1, 0), "N", style);
        style.normal.textColor = Color.white;
        Handles.Label(Origin.transform.position + Origin.transform.right + new Vector3(0, 1, 0), "E", style);
        Handles.Label(Origin.transform.position - Origin.transform.forward + new Vector3(0, 1, 0), "S", style);
        Handles.Label(Origin.transform.position - Origin.transform.right + new Vector3(0, 1, 0), "W", style);
    }

    public override void OnInspectorGUI()
    {
        // styles
        var subtitleLabelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.LowerCenter, fontSize = 12 };

        if (!string.IsNullOrEmpty(HelpText))
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.HelpBox(HelpText, MessageType.Info);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }

        Origin = (MapOrigin)target;
        if (Origin == null)
            return;

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Map Origin", subtitleLabelStyle, GUILayout.ExpandWidth(true));
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        if (GUILayout.Button("Import from Latitude and Longitude"))
        {
            ImportCoordinates w = EditorWindow.GetWindow<ImportCoordinates>(false, nameof(ImportCoordinates), true);
            w.Init(Origin);
            w.Show();
        }

        Origin.OriginEasting = EditorGUILayout.DoubleField("Origin Easting", Origin.OriginEasting);
        Origin.OriginNorthing = EditorGUILayout.DoubleField("Origin Northing", Origin.OriginNorthing);
        Origin.UTMZoneId = EditorGUILayout.IntSlider("UTM Zone ID", Origin.UTMZoneId, 1, 60);
        Origin.AltitudeOffset = EditorGUILayout.FloatField("Altitude Offset", Origin.AltitudeOffset);

        int currentlySelected = -1;
        currentlySelected = Array.FindIndex(TimeZones, tz => tz.DisplayName == Origin.TimeZoneString);
        if (currentlySelected == -1)
        {
            var timeZone = Origin.TimeZone;
            currentlySelected = Array.FindIndex(TimeZones, tz => tz.BaseUtcOffset == timeZone.BaseUtcOffset);
        }

        var values = TimeZones.Select(tz => tz.DisplayName.Replace("&", "&&")).ToArray();
        currentlySelected = EditorGUILayout.Popup("TimeZone", currentlySelected, values);
        if (currentlySelected != -1)
        {
            if (!Origin.TimeZone.Equals(TimeZones[currentlySelected]))
            {
                Origin.TimeZoneSerialized = TimeZones[currentlySelected].ToSerializedString();
                Origin.TimeZoneString = TimeZones[currentlySelected].DisplayName;

                EditorUtility.SetDirty(Origin);
                Repaint();
            }
        }

        if (GUILayout.Button("Add Reference Point"))
        {
            AddReferencePoint(Origin);
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
                var minimizeError = new MapOriginPositionErrorOptimalizer(Origin, points);
                minimizeError.Optimize();
            }
        }
        GUILayout.Space(20);
        EditorGUILayout.LabelField("Map Settings", subtitleLabelStyle, GUILayout.ExpandWidth(true));
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        Origin.IgnoreNPCVisible = EditorGUILayout.Toggle("Ignore NPC Visible", Origin.IgnoreNPCVisible, GUILayout.ExpandWidth(true));
        Origin.IgnoreNPCSpawnable = EditorGUILayout.Toggle("Ignore NPC Spawnable", Origin.IgnoreNPCSpawnable, GUILayout.ExpandWidth(true));
        Origin.IgnoreNPCBounds = EditorGUILayout.Toggle("Ignore NPC Bounds", Origin.IgnoreNPCBounds, GUILayout.ExpandWidth(true));
        Origin.NPCSizeMask = EditorGUILayout.MaskField("NPC Categories", Origin.NPCSizeMask, NPCSizes);
        Origin.NPCMaxCount = EditorGUILayout.IntSlider("NPC Max Count", Origin.NPCMaxCount, 1, 30);
        Origin.NPCSpawnBoundSize = EditorGUILayout.IntSlider("NPC Spawn Bounds Size", Origin.NPCSpawnBoundSize, 25, 300);

        Origin.IgnorePedBounds = EditorGUILayout.Toggle("Ignore Ped Bounds", Origin.IgnorePedBounds, GUILayout.ExpandWidth(true));
        Origin.IgnorePedVisible = EditorGUILayout.Toggle("Ignore Ped Visible", Origin.IgnorePedVisible, GUILayout.ExpandWidth(true));
        Origin.PedMaxCount = EditorGUILayout.IntSlider("Ped Max Count", Origin.PedMaxCount, 1, 30);
        Origin.PedSpawnBoundSize = EditorGUILayout.IntSlider("Ped Spawn Bounds Size", Origin.PedSpawnBoundSize, 25, 300);

        GUILayout.Space(20);
        EditorGUILayout.LabelField("Map Meta Data", subtitleLabelStyle, GUILayout.ExpandWidth(true));
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Map Description");
        Origin.Description = EditorGUILayout.TextArea(Origin.Description);
        EditorGUILayout.EndHorizontal();

        if (GUI.changed)
            EditorUtility.SetDirty(Origin);
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
        var gps = origin.PositionToGpsLocation(p.transform.position);
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
            var gps = origin.PositionToGpsLocation(origin.transform.position);
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
                origin.UTMZoneId = MapOrigin.LatLonToUTMZone(latitude, longitude);
                origin.LatLongToNorthingEasting(latitude, longitude, out var northing, out var easting);
                origin.OriginNorthing = Math.Round(northing, 2);
                origin.OriginEasting = Math.Round(easting, 2);
                this.Close();
            }
        }
    }
}