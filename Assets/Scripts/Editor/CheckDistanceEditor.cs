/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CheckDistanceTool))]
public class CheckDistanceEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        CheckDistanceTool checkDistanceTool = (CheckDistanceTool)target;
        if (GUILayout.Button("Check Distance"))
        {
            Debug.Log(checkDistanceTool.GetDistance());
        }
    }
}
