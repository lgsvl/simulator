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
using System;

[CustomEditor(typeof(MapLine)), CanEditMultipleObjects]
public class MapLineEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MapLine mapLine = (MapLine)target;

        GUI.enabled = false;
        EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MapLine)target), typeof(MapLine), false);
        GUI.enabled = true;
        SerializedObject serializedObject = new SerializedObject(mapLine);
        serializedObject.Update();
        ShowBool(serializedObject.FindProperty("DisplayHandles"));
        ShowList(serializedObject.FindProperty("mapLocalPositions"));
        serializedObject.ApplyModifiedProperties();
        Repaint();

        mapLine.lineType = (MapData.LineType)EditorGUILayout.EnumPopup("LineType", mapLine.lineType);
        if (mapLine.lineType == MapData.LineType.STOP)
        {
            using (var group = new EditorGUILayout.FadeGroupScope(Convert.ToSingle(mapLine.lineType == MapData.LineType.STOP)))
            {
                if (group.visible == true)
                {
                    EditorGUI.indentLevel++;
                    mapLine.isStopSign = EditorGUILayout.Toggle("Is Stop Sign", mapLine.isStopSign);
                    mapLine.currentState = (MapData.SignalLightStateType)EditorGUILayout.EnumPopup("Current Signal State", mapLine.currentState);
                    EditorGUI.indentLevel--;
                }
            }
        }
    }

    private void ShowBool(SerializedProperty enabled)
    {
        enabled.boolValue = EditorGUILayout.Toggle("Toggle Handles", enabled.boolValue);
    }

    private void ShowList(SerializedProperty list)
    {
        EditorGUILayout.PropertyField(list);
        EditorGUI.indentLevel += 1;
        if (list.isExpanded)
        {
            EditorGUILayout.PropertyField(list.FindPropertyRelative("Array.size"));
            for (int i = 0; i < list.arraySize; i++)
            {
                EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i));
            }
        }
        EditorGUI.indentLevel -= 1;
    }

    protected virtual void OnSceneGUI()
    {
        MapLine vmMapLine = (MapLine)target;
        if (vmMapLine.mapLocalPositions.Count < 1)
            return;

        if (vmMapLine.DisplayHandles)
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
