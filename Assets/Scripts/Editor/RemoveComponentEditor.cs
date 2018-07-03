/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RemoveComponent))]
public class RemoveComponentEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        RemoveComponent rmc = (RemoveComponent)target;
        if (GUILayout.Button("Remove Component"))
        {
            rmc.RemoveComponents();
        }
    }
}