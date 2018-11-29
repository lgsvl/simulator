/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VectorMapPoleBuilder)), CanEditMultipleObjects]
public class VectorMapPoleBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        VectorMapPoleBuilder polebuilder = (VectorMapPoleBuilder)target;

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        if (GUILayout.Button("Link To Contained SignalLights"))
        {
            Undo.RecordObject(polebuilder, "change builder");
            polebuilder.LinkContainedSignalLights();
        }
    }

    protected virtual void OnSceneGUI()
    {
        //placeholder
    }
}
