/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using UnityEditor;
using Simulator.Editor;
using System.IO;

public class MapExport : EditorWindow
{
    [SerializeField] int Selected = 0;
    [SerializeField] string FileName;

    string[] exportFormats = new string[]
    {
        "Apollo 5 HD Map", "Autoware Vector Map", "Lanelet2 Map", "OpenDRIVE Map"
    };

    [MenuItem("Simulator/Export HD Map", false, 120)]
    public static void Open()
    {
        var window = GetWindow(typeof(MapExport), false, "HD Map Export");
        var data = EditorPrefs.GetString("Simulator/MapExport", JsonUtility.ToJson(window, false));
        JsonUtility.FromJsonOverwrite(data, window);
        window.Show();
    }

    void OnDisable()
    {
        var data = JsonUtility.ToJson(this, false);
        EditorPrefs.SetString("Simulator/MapExport", data);
    }

    private void OnGUI()
    {
        // styles
        var titleLabelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14 };
        var subtitleLabelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 10 };

        GUILayout.Space(10);
        EditorGUILayout.LabelField("HD Map Export", titleLabelStyle, GUILayout.ExpandWidth(true));
        GUILayout.Space(5);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox("Settings", UnityEditor.MessageType.Info);
        var selectedNew = EditorGUILayout.Popup("Export Format", Selected, exportFormats);
        GUILayout.Space(10);


        if (Selected != selectedNew)
        {
            FileName = "";
            Selected = selectedNew;
        }

        if (exportFormats[Selected] == "Apollo 5 HD Map")
        {
            EditorGUILayout.HelpBox("Save File As...", UnityEditor.MessageType.Info);
            GUILayout.BeginHorizontal();
            FileName = EditorGUILayout.TextField(FileName);
            var directoryName = string.IsNullOrWhiteSpace(FileName) ? "" : Path.GetDirectoryName(FileName);
            if (GUILayout.Button("...", GUILayout.ExpandWidth(false)))
            {
                var path = EditorUtility.SaveFilePanel("Save Apollo HD Map as BIN File", directoryName, "base_map.bin", "bin");
                if (!string.IsNullOrEmpty(path))
                {
                    FileName = path;
                }
            }
            GUILayout.EndHorizontal();
        }
        else if (exportFormats[Selected] == "Autoware Vector Map")
        {
            EditorGUILayout.HelpBox("Select Folder to Save...", UnityEditor.MessageType.Info);
            GUILayout.BeginHorizontal();
            FileName = EditorGUILayout.TextField(FileName);
            if (GUILayout.Button("...", GUILayout.ExpandWidth(false)))
            {
                var path = EditorUtility.SaveFolderPanel("Select Folder to Save Autoware Vector Map", FileName, "AutowareVectorMap");
                if (!string.IsNullOrEmpty(path))
                {
                    FileName = path;
                }
            }
            GUILayout.EndHorizontal();
        }
        else if (exportFormats[Selected] == "Lanelet2 Map")
        {
            EditorGUILayout.HelpBox("Save File As...", UnityEditor.MessageType.Info);
            GUILayout.BeginHorizontal();
            FileName = EditorGUILayout.TextField(FileName);
            var directoryName = string.IsNullOrWhiteSpace(FileName) ? "" : Path.GetDirectoryName(FileName);
            if (GUILayout.Button("...", GUILayout.ExpandWidth(false)))
            {
                var path = EditorUtility.SaveFilePanel("Save Lanelet2 Map as XML File", directoryName, "lanelet2.osm", "osm");
                if (!string.IsNullOrEmpty(path))
                {
                    FileName = path;
                }
            }
            GUILayout.EndHorizontal();
        }
        else if (exportFormats[Selected] == "OpenDRIVE Map")
        {
            EditorGUILayout.HelpBox("Save File As...", UnityEditor.MessageType.Info);
            GUILayout.BeginHorizontal();
            FileName = EditorGUILayout.TextField(FileName);
            var directoryName = string.IsNullOrWhiteSpace(FileName) ? "" : Path.GetDirectoryName(FileName);
            if (GUILayout.Button("...", GUILayout.ExpandWidth(false)))
            {
                var path = EditorUtility.SaveFilePanel("Save OpenDRIVE Map as xodr File", directoryName, "OpenDRIVE.xodr", "xodr");
                if (!string.IsNullOrEmpty(path))
                {
                    FileName = path;
                }
            }
            GUILayout.EndHorizontal();
        }

        if (GUILayout.Button(new GUIContent("Export", $"Export {exportFormats[Selected]}")))
        {
            if (string.IsNullOrEmpty(FileName))
            {
                EditorUtility.DisplayDialog("Error", "Please specify output file/folder name!", "OK");
                return;
            }
            if (exportFormats[Selected] == "Apollo 5 HD Map")
            {
                ApolloMapTool apolloMapTool = new ApolloMapTool();
                apolloMapTool.Export(FileName);
            }
            else if (exportFormats[Selected] == "Autoware Vector Map")
            {
                AutowareMapTool autowareMapTool = new AutowareMapTool();
                autowareMapTool.Export(FileName);
            }
            else if (exportFormats[Selected] == "Lanelet2 Map")
            {
                Lanelet2MapExporter lanelet2MapExporter = new Lanelet2MapExporter();
                lanelet2MapExporter.Export(FileName);
            }
            else if (exportFormats[Selected] == "OpenDRIVE Map")
            {
                OpenDriveMapExporter openDriveMapExporter = new OpenDriveMapExporter();
                openDriveMapExporter.Export(FileName);
            }
        }
    }

}
