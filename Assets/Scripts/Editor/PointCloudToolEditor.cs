/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PointCloudTool))]
public class PointCloudToolEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        PointCloudTool pointcloudTool = (PointCloudTool)target;        
        if (GUILayout.Button("Generate"))
        {
            pointcloudTool.ClearPointCloud();
            pointcloudTool.GeneratePointCloud(false);
            pointcloudTool.BuildVisualizationMeshes();
        }

        if (GUILayout.Button("Clear"))
        {
            pointcloudTool.ClearPointCloud();
        }

        if (GUILayout.Button("Export"))
        {
            pointcloudTool.ExportPointCloud();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Realtime Export", EditorStyles.boldLabel);
        pointcloudTool.batchSize = EditorGUILayout.IntField("Batch Size", pointcloudTool.batchSize);

        if (GUILayout.Button("Realtime Generate and Export"))
        {
            pointcloudTool.RealtimeGenerateExport();
        }
    }
}
