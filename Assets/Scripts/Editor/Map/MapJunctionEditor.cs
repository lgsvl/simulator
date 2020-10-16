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

[CustomEditor(typeof(MapJunction)), CanEditMultipleObjects]
public class MapJunctionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        MapJunction mapJunction = (MapJunction)target;
    }

    protected virtual void OnSceneGUI()
    {
        MapJunction vmMapJunction = (MapJunction)target;
        if (vmMapJunction.mapLocalPositions.Count < 1)
            return;

        if (vmMapJunction.DisplayHandles)
        {
            Undo.RecordObject(vmMapJunction, "Junction points change");
            for (int i = 0; i < vmMapJunction.mapLocalPositions.Count - 1; i++)
            {
                Vector3 newTargetPosition = Handles.PositionHandle(vmMapJunction.transform.TransformPoint(vmMapJunction.mapLocalPositions[i]), Quaternion.identity);
                vmMapJunction.mapLocalPositions[i] = vmMapJunction.transform.InverseTransformPoint(newTargetPosition);
            }
            Vector3 lastPoint = Handles.PositionHandle(vmMapJunction.transform.TransformPoint(vmMapJunction.mapLocalPositions[vmMapJunction.mapLocalPositions.Count - 1]), Quaternion.identity);
            vmMapJunction.mapLocalPositions[vmMapJunction.mapLocalPositions.Count - 1] = vmMapJunction.transform.InverseTransformPoint(lastPoint);
        }
    }
}
