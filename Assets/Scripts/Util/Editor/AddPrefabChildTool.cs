/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class AddPrefabChildTool : ScriptableWizard
{
    [Tooltip("Prefab child of selected objects")]
    public GameObject childPrefab;

    [MenuItem("SimulatorUtil/Add Prefab Child Tool")]
    static void CreateWizard()
    {
        DisplayWizard("Add Prefab Child Tool", typeof(AddPrefabChildTool), "Add");
    }

    void OnWizardCreate()
    {
        foreach (Transform t in Selection.transforms)
        {
            GameObject newObject = PrefabUtility.InstantiatePrefab(childPrefab) as GameObject;
            Undo.RegisterCreatedObjectUndo(newObject, "created prefab");
            newObject.name = childPrefab.name;
            newObject.transform.position = t.position;
            newObject.transform.SetParent(t, true);
        }
    }
}
