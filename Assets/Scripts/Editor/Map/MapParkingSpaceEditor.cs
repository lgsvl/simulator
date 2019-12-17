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

[CustomEditor(typeof(MapParkingSpace)), CanEditMultipleObjects]
public class MapParkingSpaceEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        MapParkingSpace mapParkingSpace = (MapParkingSpace)target;
    }

    protected virtual void OnSceneGUI()
    {
        MapParkingSpace vmMapParkingSpace = (MapParkingSpace)target;
        if (vmMapParkingSpace.mapLocalPositions.Count < 1)
            return;

        if (vmMapParkingSpace.displayHandles)
        {
            Undo.RecordObject(vmMapParkingSpace, "Speed Bump points change");
            for (int i = 0; i < vmMapParkingSpace.mapLocalPositions.Count - 1; i++)
            {
                Vector3 newTargetPosition = Handles.PositionHandle(vmMapParkingSpace.transform.TransformPoint(vmMapParkingSpace.mapLocalPositions[i]), Quaternion.identity);
                vmMapParkingSpace.mapLocalPositions[i] = vmMapParkingSpace.transform.InverseTransformPoint(newTargetPosition);
            }
            Vector3 lastPoint = Handles.PositionHandle(vmMapParkingSpace.transform.TransformPoint(vmMapParkingSpace.mapLocalPositions[vmMapParkingSpace.mapLocalPositions.Count - 1]), Quaternion.identity);
            vmMapParkingSpace.mapLocalPositions[vmMapParkingSpace.mapLocalPositions.Count - 1] = vmMapParkingSpace.transform.InverseTransformPoint(lastPoint);
        }
    }
}
