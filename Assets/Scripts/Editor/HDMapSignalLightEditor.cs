/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HDMapSignalLight)), CanEditMultipleObjects]
public class HDMapSignalLightEditor : MapSignalLightEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    }

    protected override void OnSceneGUI()
    {
        base.OnSceneGUI();

        HDMapSignalLight hdMapSignalLight = (HDMapSignalLight)target;

        Undo.RecordObject(hdMapSignalLight, "HD Signal light change");

        var tForm = hdMapSignalLight.transform;

        //Draw bounds
        Handles.matrix = tForm.parent == null ? Matrix4x4.identity : tForm.parent.localToWorldMatrix * Matrix4x4.TRS(tForm.localPosition + hdMapSignalLight.boundOffsets, tForm.localRotation, Vector3.Scale(tForm.localScale, hdMapSignalLight.boundScale));
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
