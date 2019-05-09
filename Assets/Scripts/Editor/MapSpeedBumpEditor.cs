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

[CustomEditor(typeof(MapSpeedBump)), CanEditMultipleObjects]
public class MapSpeedBumpEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        MapSpeedBump mapSpeedBump = (MapSpeedBump)target;

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        if (GUILayout.Button("Append Point"))
        {
            //Undo.RecordObject(mapData, "change builder");
            //mapData.AppendPoint();
        }

        if (GUILayout.Button("Prepend Point"))
        {
            //Undo.RecordObject(mapSegment, "change builder");
            //mapSegment.PrependPoint();
        }

        if (GUILayout.Button("Remove First"))
        {
            //Undo.RecordObject(mapSegment, "change builder");
            //mapSegment.RemoveFirstPoint();
        }

        if (GUILayout.Button("Remove Last"))
        {
            //Undo.RecordObject(mapSegment, "change builder");
            //mapSegment.RemoveLastPoint();
        }

        if (GUILayout.Button("Reverse Points"))
        {
            //Undo.RecordObject(mapSegment, "change builder");
            //mapSegment.ReversePoints();
        }

        if (GUILayout.Button("Reset Points"))
        {
            //Undo.RecordObject(mapSegment, "change builder");
            //mapSegment.ResetPoints();
        }

        if (GUILayout.Button("Reset Transform"))
        {
            //Undo.RecordObject(mapSegment, "change builder");
            //mapSegment.ResetPoints();
        }

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        if (GUILayout.Button("Double Waypoint Resolution"))
        {
            //Undo.RecordObject(mapSegment, "change builder");
            //if (!mapSegment.DoubleSubsegments())
            //{
            //    Debug.Log($"{nameof(mapSegment.DoubleSubsegments)} fail");
            //}
        }

        if (GUILayout.Button("Half Waypoint Resolution"))
        {
            //Undo.RecordObject(mapSegment, "change builder");
            //if (!mapSegment.HalfSubsegments())
            //{
            //    Debug.Log($"{nameof(mapSegment.HalfSubsegments)} fail");
            //}
        }
    }

    protected virtual void OnSceneGUI()
    {
        MapSpeedBump vmMapSpeedBump = (MapSpeedBump)target;
        if (vmMapSpeedBump.mapLocalPositions.Count < 1)
            return;

        if (vmMapSpeedBump.displayHandles)
        {
            Undo.RecordObject(vmMapSpeedBump, "Speed Bump points change");
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
