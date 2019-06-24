/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RadarMesh)), CanEditMultipleObjects]
public class RadarMeshEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        RadarMesh radarMesh = (RadarMesh)target;

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        if (GUILayout.Button("Edit Radar Mesh : " + radarMesh.DisplayHandles))
        {
            Undo.RecordObject(radarMesh, "Edit Mesh");
            radarMesh.DisplayHandles = !radarMesh.DisplayHandles;
        }
    }

    protected virtual void OnSceneGUI()
    {
        RadarMesh vmRadarMesh = (RadarMesh)target;
        if (vmRadarMesh.LocalPositions.Count < 3)
        {
            vmRadarMesh.ClearMesh();
            return;
        }

        if (vmRadarMesh.DisplayHandles)
        {
            for (int i = 0; i < vmRadarMesh.LocalPositions.Count; i++)
            {
                EditorGUI.BeginChangeCheck();
                Vector3 newTargetPosition = Handles.PositionHandle(vmRadarMesh.transform.TransformPoint(vmRadarMesh.LocalPositions[i]), vmRadarMesh.transform.rotation);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(vmRadarMesh, "mesh change");
                    vmRadarMesh.LocalPositions[i] = vmRadarMesh.transform.InverseTransformPoint(newTargetPosition);
                }
            }
            vmRadarMesh.RefreshMesh();
        }
    }
}
