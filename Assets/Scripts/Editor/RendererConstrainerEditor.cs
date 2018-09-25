/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RendererConstrainer)), CanEditMultipleObjects]
public class RendererConstrainerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        RendererConstrainer rendConstrainer = (RendererConstrainer)target;
        //Undo.RecordObjects(rendConstrainer.activeRenderers.ToArray(), "changes");

        if (GUILayout.Button("Reload All Active Renderers"))
        {
            rendConstrainer.ReloadAllActiveRenderers();
        }

        if (GUILayout.Button("Hide Outside Active Renderers"))
        {
            Undo.RecordObjects(rendConstrainer.hiddenRenderers.ToArray(), "Changes");
            rendConstrainer.HideOutsideActiveRenderers();
        }

        if (GUILayout.Button("Show Hidden Renderers"))
        {
            Undo.RecordObjects(rendConstrainer.hiddenRenderers.ToArray(), "Changes");
            rendConstrainer.ShowHiddenRenderers();
        }

        if (GUILayout.Button("Clear All"))
        {
            rendConstrainer.ClearAll();
        }
    }
}

