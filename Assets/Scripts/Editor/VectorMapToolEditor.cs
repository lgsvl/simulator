/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(VectorMapTool))]
public class VectorMapToolEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        VectorMapTool vectorMapTool = (VectorMapTool)target;

        if (GUILayout.Button("Export Vector Map"))
        {
            vectorMapTool.ExportVectorMap();
        }
    }
}