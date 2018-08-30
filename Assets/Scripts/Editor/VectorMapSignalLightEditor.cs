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

        var tForm = signalLight.transform;

        for (int i = 0; i < lightCount; i++)
        {
            var start = tForm.TransformPoint(lightLocalPositions[i]);
            Map.Draw.DrawArrowForDebug(start, start + tForm.forward * 3f, VectorMapSignalLight.GetTypeColor(signalLight.signalDatas[i]), Map.Autoware.VectorMapTool.ARROWSIZE * 1f/* * 1.5f*/);
        }

        //Draw bounds
        Handles.matrix = tForm.parent == null ? Matrix4x4.identity : tForm.parent.localToWorldMatrix * Matrix4x4.TRS(tForm.localPosition + signalLight.offsets, tForm.localRotation, Vector3.Scale(tForm.localScale, signalLight.boundScale));
        Handles.color = Color.red;
        Handles.DrawWireCube(Vector3.zero, Vector3.one);

        ////function test
        //var bounds = signalLight.Get2DBounds();
        //foreach (var p in new Vector3[] { bounds.Item1, bounds.Item2, bounds.Item3, bounds.Item4 })
        //{
        //    Handles.matrix = Matrix4x4.identity;
        //    Handles.DrawWireCube(p, Vector3.one * 0.05f);
        //}
    }
}
