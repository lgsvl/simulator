/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(BuildingLights), true)]
public class BuildingLightsEditor : Editor
{

    public override void OnInspectorGUI()
    {
        base.DrawDefaultInspector();
        var t = target as BuildingLights;
        GUILayout.BeginHorizontal();
        if(GUILayout.Button("Lights off"))
        {
            t.LightsOff();
            SceneView.RepaintAll();
        }
        if(GUILayout.Button("Lights on"))
        {
            t.LightsOn();
            SceneView.RepaintAll();
        }
        GUILayout.EndHorizontal();

       

    }
}
