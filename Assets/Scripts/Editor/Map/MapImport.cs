/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using UnityEditor;
using Simulator.Editor;
using Simulator.Map;
using System.IO;

public class MapImport : EditorWindow
{
    [SerializeField]
    private int Selected = 0;
    [SerializeField]
    private string FileName;
    [SerializeField]
    private float DownSampleDistanceThreshold = 10.0f; // DownSample distance threshold for points to keep
    [SerializeField]
    private float DownSampleDeltaThreshold = 0.35f; // For down sampling, delta threshold for curve points
    [SerializeField]
    private bool IsMeshNeeded = true; // Boolean value for traffic light/sign mesh importing.
    [SerializeField]
    private bool IsConnectLanes = true; // Boolean value for whether to connect lanes based on links in OpenDRIVE.

    string[] importFormats = new string[]
    {
        "Apollo 5 HD Map",
        "Lanelet2 Map",
        "OpenDRIVE Map",
    };

    [MenuItem("Simulator/Import HD Map", false, 110)]
    public static void Open()
    {
        var window = GetWindow(typeof(MapImport), false, "HD Map Import");
        var data = EditorPrefs.GetString("Simulator/MapImport", JsonUtility.ToJson(window, false));
        JsonUtility.FromJsonOverwrite(data, window);
        window.Show();
    }

    void OnDisable()
    {
        var data = JsonUtility.ToJson(this, false);
        EditorPrefs.SetString("Simulator/MapImport", data);
    }

    private void OnGUI()
    {
        // styles
        var titleLabelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14 };
        var subtitleLabelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 10 };

        GUILayout.Space(10);
        EditorGUILayout.LabelField("HD Map Import", titleLabelStyle, GUILayout.ExpandWidth(true));
        GUILayout.Space(5);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Space(10);

        if (!FindObjectOfType<MapHolder>())
        {
            var selectedNew = EditorGUILayout.Popup("Import Format", Selected, importFormats);
            GUILayout.Space(10);

            if (Selected != selectedNew)
            {
                FileName = "";
                Selected = selectedNew;
            }

            IsMeshNeeded = GUILayout.Toggle(IsMeshNeeded, " Create Signal/sign Mesh?");

            if (importFormats[Selected] == "Apollo 5 HD Map")
            {
                DownSampleDistanceThreshold = EditorGUILayout.FloatField(
                    new GUIContent("Distance Threshold", "distance threshold to down sample imported points"),
                    DownSampleDistanceThreshold);
                DownSampleDeltaThreshold = EditorGUILayout.FloatField(
                    new GUIContent("Delta Threshold", "delta threshold to down sample imported turning lines"),
                    DownSampleDeltaThreshold);
                SelectFile(importFormats[Selected], "bin");
            }
            else if (importFormats[Selected] == "Lanelet2 Map")
            {
                SelectFile(importFormats[Selected], "osm");
            }
            else if (importFormats[Selected] == "OpenDRIVE Map")
            {
                IsConnectLanes = GUILayout.Toggle(IsConnectLanes, " Connect Lanes based on Links?");
                DownSampleDistanceThreshold = EditorGUILayout.FloatField(
                    new GUIContent("Distance Threshold", "distance threshold to down sample imported points"),
                    DownSampleDistanceThreshold);
                DownSampleDeltaThreshold = EditorGUILayout.FloatField(
                    new GUIContent("Delta Threshold", "delta threshold to down sample imported turning lines"),
                    DownSampleDeltaThreshold);
                SelectFile(importFormats[Selected], "xodr");
            }

            if (GUILayout.Button(new GUIContent("Import", $"Import {importFormats[Selected]}")))
            {
                if (string.IsNullOrEmpty(FileName))
                {
                    EditorUtility.DisplayDialog("Error", "Please specify input file/folder name!", "OK");
                    return;
                }

                if (importFormats[Selected] == "Apollo 5 HD Map")
                {
                    ApolloMapImporter ApolloMapImporter = new ApolloMapImporter(
                        DownSampleDistanceThreshold, DownSampleDeltaThreshold, IsMeshNeeded);
                    ApolloMapImporter.Import(FileName);
                }
                else if (importFormats[Selected] == "Lanelet2 Map")
                {
                    Lanelet2MapImporter laneLet2MapImporter = new Lanelet2MapImporter(IsMeshNeeded);
                    laneLet2MapImporter.Import(FileName);
                }

                if (importFormats[Selected] == "OpenDRIVE Map")
                {
                    OpenDriveMapImporter openDriveMapImporter = new OpenDriveMapImporter(
                                            DownSampleDistanceThreshold, DownSampleDeltaThreshold,
                                            IsMeshNeeded, IsConnectLanes);
                    openDriveMapImporter.Import(FileName);
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Please delete current map data before importing new data", MessageType.Warning);
            GUILayout.Space(10);
            if (GUILayout.Button(new GUIContent("Delete Map Data", "Delete map annotation holder in scene"), GUILayout.ExpandWidth(true)))
            {
                DeleteMapHolder();
            }
        }
    }

    private void DeleteMapHolder()
    {
        var map = FindObjectOfType<MapHolder>().gameObject;
        if (map != null)
        {
            Undo.DestroyObjectImmediate(map);
            SceneView.RepaintAll();
            Debug.Log("MapHolder object destroyed Ctrl+Z to undo");
        }
    }

    void SelectFile(string mapFormat, string formatExtension)
    {
        EditorGUILayout.HelpBox("Select File...", UnityEditor.MessageType.Info);
        GUILayout.BeginHorizontal();
        FileName = EditorGUILayout.TextField(FileName);
        var directoryName = string.IsNullOrWhiteSpace(FileName) ? "" : Path.GetDirectoryName(FileName);
        if (GUILayout.Button("...", GUILayout.ExpandWidth(false)))
        {
            var path = EditorUtility.OpenFilePanel("Open " + mapFormat + " Map", directoryName, formatExtension);
            if (!string.IsNullOrEmpty(path))
            {
                FileName = path;
            }
        }
        GUILayout.EndHorizontal();
    }
}
