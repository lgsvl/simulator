/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


using Autoware;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapStopLineSegmentBuilder), true), CanEditMultipleObjects]
public class MapStopLineSegmentBuilderEditor : MapSegmentBuilderEditor
{
    private Color rayColor = Color.magenta;
    private Color segmentPointColor = Color.magenta;
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    }

    protected override void OnSceneGUI()
    {
        base.OnSceneGUI();

        MapSegmentBuilder vmSegBuilder = (MapSegmentBuilder)target;
        Undo.RecordObject(vmSegBuilder, "Segment points change");

        var localPositions = vmSegBuilder.segment.targetLocalPositions;

        var pointCount = localPositions.Count;

        if (pointCount < 2)
        {
            return;
        }

        Transform mainTrans = vmSegBuilder.transform;

        for (int i = 0; i < pointCount - 1; i++)
        {
            Handles.color = segmentPointColor;
            Handles.DrawWireDisc(mainTrans.TransformPoint(localPositions[i]), Vector3.up, Map.Autoware.VectorMapTool.PROXIMITY * 0.5f);
            Handles.color = rayColor;
            Handles.DrawLine(mainTrans.TransformPoint(localPositions[i]), mainTrans.TransformPoint(localPositions[i + 1]));
        }

        Handles.color = segmentPointColor;
        Handles.DrawWireDisc(mainTrans.TransformPoint(localPositions[pointCount - 1]), Vector3.up, Map.Autoware.VectorMapTool.PROXIMITY * 0.5f);
    }
}