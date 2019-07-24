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

public class MapImport : EditorWindow
{
    [SerializeField] int Selected = 0;
    [SerializeField] string FileName;

    string[] importFormats = new string[]
    {
        "Apollo HD Map", "Lanelet2 Map", "OpenDRIVE Map"
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

        EditorGUILayout.HelpBox("Settings", UnityEditor.MessageType.Info);
        var selectedNew = EditorGUILayout.Popup("Import Format", Selected, importFormats);
        GUILayout.Space(10);


        if (Selected != selectedNew)
        {
            FileName = "";
            Selected = selectedNew;
        }

        if (importFormats[Selected] == "Lanelet2 Map")
        {
            EditorGUILayout.HelpBox("Select File...", UnityEditor.MessageType.Info);
            GUILayout.BeginHorizontal();
            FileName = EditorGUILayout.TextField(FileName);
            if (GUILayout.Button("...", GUILayout.ExpandWidth(false)))
            {
                var path = EditorUtility.OpenFilePanel("Open Lanelet2 HD Map", "", "osm");
                if (!string.IsNullOrEmpty(path))
                {
                    FileName = path;
                }
            }
            GUILayout.EndHorizontal();
        }

        if (importFormats[Selected] == "OpenDRIVE Map")
        {
            EditorGUILayout.HelpBox("Select File...", UnityEditor.MessageType.Info);
            GUILayout.BeginHorizontal();
            FileName = EditorGUILayout.TextField(FileName);
            if (GUILayout.Button("...", GUILayout.ExpandWidth(false)))
            {
                var path = EditorUtility.OpenFilePanel("Open OpenDRIVE Map", "", "xodr");
                if (!string.IsNullOrEmpty(path))
                {
                    FileName = path;
                }
            }
            GUILayout.EndHorizontal();
        }

        if (GUILayout.Button(new GUIContent("Import", $"Import {importFormats[Selected]}")))
        {
            if (string.IsNullOrEmpty(FileName))
            {
                EditorUtility.DisplayDialog("Error", "Please specify input file/folder name!", "OK");
                return;
            }
           
            if (importFormats[Selected] == "Lanelet2 Map")
            {
                LaneLet2MapImporter laneLet2MapImporter = new LaneLet2MapImporter();
                laneLet2MapImporter.ImportLanelet2Map(FileName);
            }

            if (importFormats[Selected] == "OpenDRIVE Map")
            {
                OpenDriveMapImporter openDriveMapImporter = new OpenDriveMapImporter();
                openDriveMapImporter.ImportOpenDriveMap(FileName);
            }
        }
    }

}
