/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapSignalLight)), CanEditMultipleObjects]
public class MapSignalLightEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
    }

    protected virtual void OnSceneGUI()
    {
        MapSignalLight signalLight = (MapSignalLight)target;

        Undo.RecordObject(signalLight, "Signal light change");

        var lightLocalPositions = signalLight.signalDatas.Select(x => x.localPosition).ToList();

        var lightCount = lightLocalPositions.Count;

        if (lightCount < 1)
        {
            return;
        }

        var tForm = signalLight.transform;

        for (int i = 0; i < lightCount; i++)
        {
            var start = tForm.TransformPoint(lightLocalPositions[i]);
            Map.Draw.DrawArrowForDebug(start, start + tForm.forward * 3f, VectorMapSignalLight.GetTypeColor(signalLight.signalDatas[i]), Map.Autoware.VectorMapTool.ARROWSIZE * 1f/* * 1.5f*/);
        }
    }
}
