/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEditor;
using UnityEngine;
using static VectorMap.VectorMapUtility;

[CustomEditor(typeof(VectorMapSegmentBuilder)), CanEditMultipleObjects]
public class VectorMapSegmentBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        VectorMapSegmentBuilder vectorMapSegment = (VectorMapSegmentBuilder)target;
        Undo.RecordObject(vectorMapSegment, "change points");

        if (GUILayout.Button("Add Point"))
        {
            vectorMapSegment.AddPoint();
        }

        if (GUILayout.Button("Remove Point"))
        {
            vectorMapSegment.RemovePoint();
        }

        if (GUILayout.Button("Reverse Points"))
        {
            vectorMapSegment.ReversePoints();
        }

        if (GUILayout.Button("Reset Points"))
        {
            vectorMapSegment.ResetPoints();
        }      
    }

    protected virtual void OnSceneGUI()
    {
        VectorMapSegmentBuilder vmSegBuilder = (VectorMapSegmentBuilder)target;
        Undo.RecordObject(vmSegBuilder, "Segment points change");

        var localPositions = vmSegBuilder.segment.targetLocalPositions;

        var pointCount = localPositions.Count;

        if (pointCount < 1)
        {
            return;
        }

        Transform mainTrans = vmSegBuilder.transform;

        if (vmSegBuilder.showHandles)
        {
            for (int i = 0; i < pointCount - 1; i++)
            {
                Vector3 newTargetPosition = Handles.PositionHandle(mainTrans.TransformPoint(localPositions[i]), Quaternion.identity);
                localPositions[i] = mainTrans.InverseTransformPoint(newTargetPosition);
            }
            Vector3 lastPoint = Handles.PositionHandle(mainTrans.TransformPoint(localPositions[pointCount - 1]), Quaternion.identity);
            localPositions[pointCount - 1] = mainTrans.InverseTransformPoint(lastPoint);
        }
    }
}