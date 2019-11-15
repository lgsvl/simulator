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


        if (mapLane.leftLineBoundry != null)
        {
            mapLane.leftLineBoundry.lineType = (MapData.LineType)EditorGUILayout.EnumPopup("Left Boundary Type: ", mapLane.leftLineBoundry?.lineType);
        }

        if (mapLane.rightLineBoundry != null)
        {
            mapLane.rightLineBoundry.lineType = (MapData.LineType)EditorGUILayout.EnumPopup("Right Boundary Type: ", mapLane.rightLineBoundry?.lineType);
        }

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        if (GUILayout.Button("Reverse Lane"))
        {
            Undo.RecordObject(mapLane, "change builder");
            mapLane.ReversePoints();
        }
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
        
        if (vmMapLane.displayLane)
        {
            var left = new Vector3[vmMapLane.mapLocalPositions.Count];
            var right = new Vector3[vmMapLane.mapLocalPositions.Count];
            var halfLaneWidth = vmMapLane.displayLaneWidth / 2;
            var laneTransform = vmMapLane.transform;

            var a = laneTransform.TransformPoint(vmMapLane.mapLocalPositions[0]);
            for (int i = 1; i < vmMapLane.mapLocalPositions.Count; i++)
            {
                var b = laneTransform.TransformPoint(vmMapLane.mapLocalPositions[i]);
                left[i-1] = (GetPerpPoint(a, b, halfLaneWidth));
                right[i-1] = (GetPerpPoint(a, b, -halfLaneWidth));

                if (i == vmMapLane.mapLocalPositions.Count - 1)
                {
                    left[i] = (GetPerpPoint(b, a, -halfLaneWidth));
                    right[i] = (GetPerpPoint(b, a, halfLaneWidth));
                }

                a = b;
            }

            Handles.DrawAAPolyLine(left);
            Handles.DrawAAPolyLine(right);
        }
    }

    private static Vector3 GetPerpPoint(Vector3 A, Vector3 B, float distance)
    {
        var dir = Vector3.Normalize(B - A);
        var normal = new Vector3(-dir.z, dir.y, dir.x);
        
        return A + normal * distance;
    }
    
}
