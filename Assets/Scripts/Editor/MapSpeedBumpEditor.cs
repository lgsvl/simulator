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

[CustomEditor(typeof(MapSpeedBump)), CanEditMultipleObjects]
public class MapSpeedBumpEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        MapSpeedBump mapSpeedBump = (MapSpeedBump)target;
    }

    protected virtual void OnSceneGUI()
    {
        MapSpeedBump vmMapSpeedBump = (MapSpeedBump)target;
        if (vmMapSpeedBump.mapLocalPositions.Count < 1)
            return;

        if (vmMapSpeedBump.displayHandles)
        {
            Undo.RecordObject(vmMapSpeedBump, "Parking Space points change");
            for (int i = 0; i < vmMapSpeedBump.mapLocalPositions.Count - 1; i++)
            {
                Vector3 newTargetPosition = Handles.PositionHandle(vmMapSpeedBump.transform.TransformPoint(vmMapSpeedBump.mapLocalPositions[i]), Quaternion.identity);
                vmMapSpeedBump.mapLocalPositions[i] = vmMapSpeedBump.transform.InverseTransformPoint(newTargetPosition);
            }
            Vector3 lastPoint = Handles.PositionHandle(vmMapSpeedBump.transform.TransformPoint(vmMapSpeedBump.mapLocalPositions[vmMapSpeedBump.mapLocalPositions.Count - 1]), Quaternion.identity);
            vmMapSpeedBump.mapLocalPositions[vmMapSpeedBump.mapLocalPositions.Count - 1] = vmMapSpeedBump.transform.InverseTransformPoint(lastPoint);
        }
    }
}
