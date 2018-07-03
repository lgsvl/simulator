/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CreateComponent))]
public class CreateComponentEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        CreateComponent cc = (CreateComponent)target;
        if (GUILayout.Button("Create Component"))
        {
            cc.CreateComponents();
        }
    }
}