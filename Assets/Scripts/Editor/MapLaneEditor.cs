using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapLane)), CanEditMultipleObjects]
public class MapLaneEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        MapLane mapLane = (MapLane)target;

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
        MapLane vmMapLane = (MapLane)target;
        if (vmMapLane.mapLocalPositions.Count < 1)
            return;

        if (vmMapLane.displayHandles)
        {
            Undo.RecordObject(vmMapLane, "Line points change");
            for (int i = 0; i < vmMapLane.mapLocalPositions.Count - 1; i++)
            {
                Vector3 newTargetPosition = Handles.PositionHandle(vmMapLane.transform.TransformPoint(vmMapLane.mapLocalPositions[i]), Quaternion.identity);
                vmMapLane.mapLocalPositions[i] = vmMapLane.transform.InverseTransformPoint(newTargetPosition);
            }
            Vector3 lastPoint = Handles.PositionHandle(vmMapLane.transform.TransformPoint(vmMapLane.mapLocalPositions[vmMapLane.mapLocalPositions.Count - 1]), Quaternion.identity);
            vmMapLane.mapLocalPositions[vmMapLane.mapLocalPositions.Count - 1] = vmMapLane.transform.InverseTransformPoint(lastPoint);
        }
    }
}
