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

[CustomEditor(typeof(MapLine)), CanEditMultipleObjects]
public class MapLineEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        MapLine mapLine = (MapLine)target;
    }

    protected virtual void OnSceneGUI()
    {
        MapLine vmMapLine = (MapLine)target;
        if (vmMapLine.mapLocalPositions.Count < 1)
            return;

        if (vmMapLine.displayHandles)
        {
            Undo.RecordObject(vmMapLine, "Line points change");
            for (int i = 0; i < vmMapLine.mapLocalPositions.Count - 1; i++)
            {
                Vector3 newTargetPosition = Handles.PositionHandle(vmMapLine.transform.TransformPoint(vmMapLine.mapLocalPositions[i]), Quaternion.identity);
                vmMapLine.mapLocalPositions[i] = vmMapLine.transform.InverseTransformPoint(newTargetPosition);
            }
            Vector3 lastPoint = Handles.PositionHandle(vmMapLine.transform.TransformPoint(vmMapLine.mapLocalPositions[vmMapLine.mapLocalPositions.Count - 1]), Quaternion.identity);
            vmMapLine.mapLocalPositions[vmMapLine.mapLocalPositions.Count - 1] = vmMapLine.transform.InverseTransformPoint(lastPoint);
        }
    }
}
