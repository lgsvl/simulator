/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VectorMapSignalLight)), CanEditMultipleObjects]
public class VectorMapSignalLightEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
    }

    protected void OnSceneGUI()
    {
        VectorMapSignalLight signalLight = (VectorMapSignalLight)target;

        Undo.RecordObject(signalLight, "Signal light change");

        var lightLocalPositions = signalLight.signalDatas.Select(x => x.localPosition).ToList();

        var lightCount = lightLocalPositions.Count;

        if (lightCount < 1)
        {
            return;
        }

        var mainTrans = signalLight.transform;

        for (int i = 0; i < lightCount; i++)
        {
            var start = mainTrans.TransformPoint(lightLocalPositions[i]);
            VectorMap.Draw.DrawArrowForDebug(start, start + mainTrans.forward * 2.0f, VectorMapSignalLight.GetTypeColor(signalLight.signalDatas[i]), VectorMapTool.ARROWSIZE * 0.5f);
        }
    }
}
