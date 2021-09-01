/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Simulator.Map;

[CustomEditor(typeof(MapParkingSpace)), CanEditMultipleObjects]
public class MapParkingSpaceEditor : Editor
{
    private float targetWidth = 3;
    private float targetLength = 6;
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        MapParkingSpace mapParkingSpace = (MapParkingSpace)target;

        GUILayout.Label("Length: " + mapParkingSpace.Length);
        GUILayout.Label("Width: " + mapParkingSpace.Width);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Force right angle by moving right side"))
        {
            MakeEdit(space =>
            {
                var forward = space.mapWorldPositions[2] - space.mapWorldPositions[1];
                var right = Quaternion.AngleAxis(90, Vector3.up) * forward.normalized;
                var fixed0 = space.mapWorldPositions[1] +
                             Vector3.Dot(space.mapWorldPositions[0] - space.mapWorldPositions[1],
                                 right) * right;
                var fixed3 = space.mapWorldPositions[2] +
                             Vector3.Dot(space.mapWorldPositions[3] - space.mapWorldPositions[2],
                                 right) * right;
                space.mapLocalPositions[0] = space.transform.InverseTransformPoint(fixed0);
                space.mapLocalPositions[3] = space.transform.InverseTransformPoint(fixed3);
                SceneView.RepaintAll();
            }, "Force right angle by moving right side");
        }
        if (GUILayout.Button("Force right angle by moving left side"))
        {
            MakeEdit(space =>
            {
                var forward = space.mapWorldPositions[3] - space.mapWorldPositions[0];
                var right = Quaternion.AngleAxis(90, Vector3.up) * forward.normalized;
                var fixed2 = space.mapWorldPositions[3] -
                             Vector3.Dot(space.mapWorldPositions[0] - space.mapWorldPositions[1],
                                 right) * right;
                var fixed1 = space.mapWorldPositions[0] -
                             Vector3.Dot(space.mapWorldPositions[3] - space.mapWorldPositions[2],
                                 right) * right;
                space.mapLocalPositions[2] = space.transform.InverseTransformPoint(fixed2);
                space.mapLocalPositions[1] = space.transform.InverseTransformPoint(fixed1);
                SceneView.RepaintAll();
            }, "Force right angle by moving left side");
        }
        GUILayout.EndHorizontal();
        if (GUILayout.Button("Make right side parallel to the left side"))
        {
            MakeEdit(space =>
            {
                var offset = 0.5f * ((space.mapWorldPositions[0] - space.mapWorldPositions[1]) + (space.mapWorldPositions[3] - space.mapWorldPositions[2]));
                space.mapLocalPositions[0] = space.transform.InverseTransformPoint(space.mapWorldPositions[1] + offset);
                space.mapLocalPositions[3] = space.transform.InverseTransformPoint(space.mapWorldPositions[2] + offset);
            }, "Make right side parallel to the left side");
        }
        GUILayout.BeginHorizontal();
        targetWidth = EditorGUILayout.FloatField("Set width to (only with right angle)", targetWidth);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("By moving left side"))
        {
            MakeEdit(space =>
            {
                space.mapLocalPositions[2] = space.transform.InverseTransformPoint(space.mapWorldPositions[3] + (space.mapWorldPositions[2] - space.mapWorldPositions[3]).normalized * targetWidth);
                space.mapLocalPositions[1] = space.transform.InverseTransformPoint(space.mapWorldPositions[0] + (space.mapWorldPositions[1] - space.mapWorldPositions[0]).normalized * targetWidth);
            }, "Set width left");
        }
        if (GUILayout.Button("By moving right side"))
        {
            MakeEdit(space =>
            {
                space.mapLocalPositions[3] = space.transform.InverseTransformPoint(space.mapWorldPositions[2] + (space.mapWorldPositions[3] - space.mapWorldPositions[2]).normalized * targetWidth);
                space.mapLocalPositions[0] = space.transform.InverseTransformPoint(space.mapWorldPositions[1] + (space.mapWorldPositions[0] - space.mapWorldPositions[1]).normalized * targetWidth);
            }, "Set width right");
        }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        targetLength = EditorGUILayout.FloatField("Set length to", targetLength);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("By moving front"))
        {
            MakeEdit(space =>
            {
                var offset = (space.MiddleExit - space.MiddleEnter).normalized * targetLength;
                space.mapLocalPositions[2] = space.transform.InverseTransformPoint(space.mapWorldPositions[1] + offset);
                space.mapLocalPositions[3] = space.transform.InverseTransformPoint(space.mapWorldPositions[0] + offset);
            }, "By moving front");
        }
        if (GUILayout.Button("By moving back"))
        {
            MakeEdit(space =>
            {
                var offset = (space.MiddleExit - space.MiddleEnter).normalized * targetLength;
                space.mapLocalPositions[0] =
                    space.transform.InverseTransformPoint(space.mapWorldPositions[3] - offset);
                space.mapLocalPositions[1] =
                    space.transform.InverseTransformPoint(space.mapWorldPositions[2] - offset);
            }, "Set length back");
        }
        GUILayout.EndHorizontal();
        if (GUILayout.Button("Fix Y"))
        {
            MakeEdit(space =>
            {
                for (int i = 0; i < space.mapWorldPositions.Count; i++)
                {
                    Ray ray = new Ray(space.mapWorldPositions[i] + Vector3.up * 2, Vector3.down);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit, 4))
                    {
                        space.mapLocalPositions[i] = space.transform.InverseTransformPoint(hit.point);
                    }
                }
            }, "Fix Y");
        }
    }

    private void MakeEdit(Action<MapParkingSpace> action, string actionName)
    {
        Undo.RecordObjects(targets, actionName);
        foreach (MapParkingSpace space in targets)
        {
            space.RefreshWorldPositions();
            action(space);
        }
        SceneView.RepaintAll();
    }

    protected virtual void OnSceneGUI()
    {
        MapParkingSpace vmMapParkingSpace = (MapParkingSpace)target;
        if (vmMapParkingSpace.mapLocalPositions.Count < 1)
            return;

        if (vmMapParkingSpace.DisplayHandles)
        {
            for (int i = 0; i < vmMapParkingSpace.mapLocalPositions.Count; i++)
            {
                Vector3 oldWorld = vmMapParkingSpace.transform.TransformPoint(vmMapParkingSpace.mapLocalPositions[i]);

                Vector3 newTargetPosition = Handles.PositionHandle(oldWorld, Quaternion.identity);

                if (oldWorld != newTargetPosition)
                {
                    Undo.RecordObject(vmMapParkingSpace, "Speed Bump points change");
                    vmMapParkingSpace.mapLocalPositions[i] = vmMapParkingSpace.transform.InverseTransformPoint(newTargetPosition);
                }
            }

        }

        if (vmMapParkingSpace.DisplayHandles)
        {
            for (int i = 0; i < vmMapParkingSpace.mapLocalPositions.Count; i++)
            {
                int iPlus1 = (i + 1) % vmMapParkingSpace.mapLocalPositions.Count;
                var iWorld = vmMapParkingSpace.transform.TransformPoint(vmMapParkingSpace.mapLocalPositions[i]);
                var iPlus1World = vmMapParkingSpace.transform.TransformPoint(vmMapParkingSpace.mapLocalPositions[iPlus1]);
                var oldMiddleWorld = 0.5f * (iWorld + iPlus1World);
                Vector3 newTargetPosition = Handles.PositionHandle(oldMiddleWorld, Quaternion.identity);
                if (oldMiddleWorld != newTargetPosition)
                {
                    var diff = newTargetPosition - oldMiddleWorld;
                    Undo.RecordObject(vmMapParkingSpace, "Speed Bump points change");
                    vmMapParkingSpace.mapLocalPositions[i] = vmMapParkingSpace.transform.InverseTransformPoint(iWorld + diff);
                    vmMapParkingSpace.mapLocalPositions[iPlus1] = vmMapParkingSpace.transform.InverseTransformPoint(iPlus1World + diff);
                }
            }
        }
    }
}
