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
using Simulator.Map;

[CustomEditor(typeof(MapLane)), CanEditMultipleObjects]
public class MapLaneEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        MapLane mapLane = (MapLane)target;
    }

    protected virtual void OnSceneGUI()
    {
        MapLane vmMapLane = (MapLane)target;
        if (vmMapLane.mapLocalPositions.Count < 1)
            return;

        if (vmMapLane.displayHandles)
        {
            for (int i = 0; i < vmMapLane.mapLocalPositions.Count; i++)
            {
                EditorGUI.BeginChangeCheck();
                Vector3 newTargetPosition = Handles.PositionHandle(vmMapLane.transform.TransformPoint(vmMapLane.mapLocalPositions[i]), vmMapLane.transform.rotation);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(vmMapLane, "Line points change");
                    vmMapLane.mapLocalPositions[i] = vmMapLane.transform.InverseTransformPoint(newTargetPosition);
                }
            }
        }
    }
}
