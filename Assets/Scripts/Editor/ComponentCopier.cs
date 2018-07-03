/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;
using UnityEditor;

public class ComponentCopier : EditorWindow
{
    GameObject source;
    GameObject target;
    string copyPasteBtn = "Copy and Paste";

    [MenuItem("Window/ComponentCopier")]
    static void Init()
    {
        ComponentCopier window = (ComponentCopier)EditorWindow.GetWindow(typeof(ComponentCopier));
        window.Show();
    }

    void OnGUI()
    {
        source = (GameObject)EditorGUILayout.ObjectField("Source Prefab", source, typeof(GameObject), true);
        target = (GameObject)EditorGUILayout.ObjectField("Target Prefab", target, typeof(GameObject), true);
        EditorGUILayout.LabelField("Copy and paste all components");
        if (GUILayout.Button(copyPasteBtn))
        {
            var go = Selection.transforms[0];
            if (go != null)
            {
                Undo.RecordObject(go, "Gameobject");
                var components = go.GetComponents<Component>();
                foreach (var comp in components)
                {
                    UnityEditorInternal.ComponentUtility.CopyComponent(comp);
                    UnityEditorInternal.ComponentUtility.PasteComponentAsNew(target);
                }
            }
        }
    }
}
