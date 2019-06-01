/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Simulator.Editor;
using Simulator.Map;

public class MapExport : EditorWindow
{
    [SerializeField] int Selected = 0;
    [SerializeField] string FileName;

    string[] exportFormats = new string[]
    {
        "Apollo HD Map", "Autoware Vector Map",
    };

    [MenuItem("Simulator/Export HD Map", false, 110)]
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

        if (Selected == 0)
        {
            EditorGUILayout.HelpBox("Output filename", UnityEditor.MessageType.Info);
            GUILayout.BeginHorizontal();
            FileName = EditorGUILayout.TextField(FileName);
            if (GUILayout.Button("...", GUILayout.ExpandWidth(false)))
            {
                var path = EditorUtility.SaveFilePanel("Save apollo hd map as bin file", "", "base_map.bin", "bin");
                if (!string.IsNullOrEmpty(path))
                {
                    FileName = path;
                }
            }
            GUILayout.EndHorizontal();
        }
        else if (Selected == 1)
        {
            EditorGUILayout.HelpBox("Output folder name", UnityEditor.MessageType.Info);
            GUILayout.BeginHorizontal();
            FileName = EditorGUILayout.TextField(FileName);
            if (GUILayout.Button("...", GUILayout.ExpandWidth(false)))
            {
                var path = EditorUtility.SaveFolderPanel("Save autoware vector map in a folder", "", "AutowareVectorMap");
                if (!string.IsNullOrEmpty(path))
                {
                    FileName = path;
                }
            }
            GUILayout.EndHorizontal();
        }

        if (GUILayout.Button(new GUIContent(exportFormats[Selected] + " Export", exportFormats[Selected] + "Export")))
        {
            if (string.IsNullOrEmpty(FileName))
            {
                EditorUtility.DisplayDialog("Error", "Please specify output file/folder name!", "OK");
                return;
            }
            if (exportFormats[Selected] == "Apollo HD Map")
            {
                ApolloMapTool apolloMapTool = new ApolloMapTool();
                apolloMapTool.ExportHDMap(FileName);
            }
            else if (exportFormats[Selected] == "Autoware Vector Map")
            {
                AutowareMapTool autowareMapTool = new AutowareMapTool();
                autowareMapTool.ExportVectorMap(FileName);
            }

        }
    }

}
