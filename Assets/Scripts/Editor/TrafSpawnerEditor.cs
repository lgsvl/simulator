/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using UnityEditor;
using System.Collections;

using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(TrafSpawner), true)]
public class TrafSpawnerEditor : Editor
{

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (GUILayout.Button("Spawn"))
        {
            (target as TrafSpawner).SpawnHeaps();
        }
    }
}
