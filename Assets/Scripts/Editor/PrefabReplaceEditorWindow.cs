/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;
using UnityEditor;

public class PrefabReplaceEditorWindow : EditorWindow
{
    GameObject prefab;
    string replacebutton = "Replace";

    [MenuItem("Window/PrefabReplaceEditorWindow")]
    static void Init()
    {
        PrefabReplaceEditorWindow window = (PrefabReplaceEditorWindow)EditorWindow.GetWindow(typeof(PrefabReplaceEditorWindow));
        window.Show();
    }

    void OnGUI()
    {
        prefab = (GameObject)EditorGUILayout.ObjectField("Source Prefab", prefab, typeof(GameObject), false);
        EditorGUILayout.LabelField("Select scene objects to Replace and then press button");
        if (GUILayout.Button(replacebutton))
        {
            Undo.RecordObjects(Selection.transforms, "Group Gameobjects");
            if (prefab != null)
            {
                foreach (var t in Selection.transforms)
                {
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    go.transform.SetParent(t.parent);
                    go.transform.position = t.position;
                    go.transform.rotation = t.rotation;
                    go.transform.localScale = t.localScale;
                    Undo.DestroyObjectImmediate(t.gameObject);
                    Undo.RegisterCreatedObjectUndo(go, "Create object");
                }
            }
        }
    }
}
