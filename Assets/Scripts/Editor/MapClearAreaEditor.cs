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

[CustomEditor(typeof(MapClearArea)), CanEditMultipleObjects]
public class MapClearAreaEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        MapClearArea mapParkingSpace = (MapClearArea)target;
    }

    protected virtual void OnSceneGUI()
    {
        MapClearArea vmMapClearArea = (MapClearArea)target;
        if (vmMapClearArea.mapLocalPositions.Count < 1)
            return;

        if (vmMapClearArea.displayHandles)
        {
            Undo.RecordObject(vmMapClearArea, "Clear Area points change");
            for (int i = 0; i < vmMapClearArea.mapLocalPositions.Count - 1; i++)
            {
                Vector3 newTargetPosition = Handles.PositionHandle(vmMapClearArea.transform.TransformPoint(vmMapClearArea.mapLocalPositions[i]), Quaternion.identity);
                vmMapClearArea.mapLocalPositions[i] = vmMapClearArea.transform.InverseTransformPoint(newTargetPosition);
            }
            Vector3 lastPoint = Handles.PositionHandle(vmMapClearArea.transform.TransformPoint(vmMapClearArea.mapLocalPositions[vmMapClearArea.mapLocalPositions.Count - 1]), Quaternion.identity);
            vmMapClearArea.mapLocalPositions[vmMapClearArea.mapLocalPositions.Count - 1] = vmMapClearArea.transform.InverseTransformPoint(lastPoint);
        }
    }
}
