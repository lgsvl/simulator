/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using UnityEditor;
using Simulator.Map;

[CustomEditor(typeof(MapPedestrian)), CanEditMultipleObjects]
public class MapPedestrianEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        MapPedestrian mapLane = (MapPedestrian)target;
    }

    protected virtual void OnSceneGUI()
    {
        MapPedestrian vmMapPed = (MapPedestrian)target;
        if (vmMapPed.mapLocalPositions.Count < 1)
            return;

        if (vmMapPed.displayHandles)
        {
            for (int i = 0; i < vmMapPed.mapLocalPositions.Count; i++)
            {
                EditorGUI.BeginChangeCheck();
                Vector3 newTargetPosition = Handles.PositionHandle(vmMapPed.transform.TransformPoint(vmMapPed.mapLocalPositions[i]), vmMapPed.transform.rotation);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(vmMapPed, "Pedestrian points change");
                    vmMapPed.mapLocalPositions[i] = vmMapPed.transform.InverseTransformPoint(newTargetPosition);
                }
            }
        }
    }
}
