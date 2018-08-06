/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(WetRoads), true)]
public class WetRoadsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.DrawDefaultInspector();
        var t = target as WetRoads;
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Dry"))
        {
            t.SetWetness(0f);
            SceneView.RepaintAll();
        }
        if (GUILayout.Button("Wet"))
        {
            t.SetWetness(1f);
            SceneView.RepaintAll();
        }
        GUILayout.EndHorizontal();
    }
}
