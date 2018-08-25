/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;
using UnityEditor;
using Map.Apollo;

[CustomEditor(typeof(HDMapTool))]
public class HDMapToolEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        HDMapTool hdMapTool = (HDMapTool)target;

        if (GUILayout.Button("Export HD Map"))
        {
            hdMapTool.ExportHDMap();
        }
    }
}