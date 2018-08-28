/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;
using UnityEditor;

public class BatchReplaceEditorWindow : EditorWindow
{
    GameObject source;
    string replacebutton = "Replace";

    [MenuItem("Window/Batch Replace Tool")]
    static void Init()
    {
        BatchReplaceEditorWindow window = (BatchReplaceEditorWindow)EditorWindow.GetWindow(typeof(BatchReplaceEditorWindow));
        window.Show();
    }

    void OnGUI()
    {
        source = (GameObject)EditorGUILayout.ObjectField("Source", source, typeof(GameObject), false);
        EditorGUILayout.LabelField("Select scene objects to Replace and then press button");
        if (GUILayout.Button(replacebutton))
        {
            Undo.RecordObjects(Selection.transforms, "Group Gameobjects");
            if (source != null)
            {
                foreach (var t in Selection.transforms)
                {
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(source);
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
