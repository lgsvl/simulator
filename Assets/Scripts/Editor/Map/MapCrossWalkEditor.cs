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

[CustomEditor(typeof(MapCrossWalk)), CanEditMultipleObjects]
public class MapCrossWalkEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        MapCrossWalk mapParkingSpace = (MapCrossWalk)target;
    }

    protected virtual void OnSceneGUI()
    {
        MapCrossWalk vmMapCrossWalk = (MapCrossWalk)target;
        if (vmMapCrossWalk.mapLocalPositions.Count < 1)
            return;

        if (vmMapCrossWalk.displayHandles)
        {
            Undo.RecordObject(vmMapCrossWalk, "Cross Walk points change");
            for (int i = 0; i < vmMapCrossWalk.mapLocalPositions.Count - 1; i++)
            {
                Vector3 newTargetPosition = Handles.PositionHandle(vmMapCrossWalk.transform.TransformPoint(vmMapCrossWalk.mapLocalPositions[i]), Quaternion.identity);
                vmMapCrossWalk.mapLocalPositions[i] = vmMapCrossWalk.transform.InverseTransformPoint(newTargetPosition);
            }
            Vector3 lastPoint = Handles.PositionHandle(vmMapCrossWalk.transform.TransformPoint(vmMapCrossWalk.mapLocalPositions[vmMapCrossWalk.mapLocalPositions.Count - 1]), Quaternion.identity);
            vmMapCrossWalk.mapLocalPositions[vmMapCrossWalk.mapLocalPositions.Count - 1] = vmMapCrossWalk.transform.InverseTransformPoint(lastPoint);
        }
    }
}
