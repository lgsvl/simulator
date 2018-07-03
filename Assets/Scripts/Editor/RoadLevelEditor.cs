/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RoadLevelManager))]
public class RoadLevelEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        RoadLevelManager roadLevelManager = (RoadLevelManager)target;
        if (GUILayout.Button("Configure Road Materials"))
        {
            roadLevelManager.ConfigureRoadMaterials();
        }
    }
}
