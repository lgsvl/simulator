/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ReplacePrefabTool : ScriptableWizard
{
    [Tooltip("Prefab that replaces selected objects")]
    public GameObject replacePrefab;
    [Tooltip("Parent object - optional")]
    public GameObject customParent;

    [Tooltip("Apply new rotation")]
    public bool isCustomRot = false;
    public Vector3 customRot = Vector3.zero;

    [Tooltip("Apply new scale")]
    public bool isCustomScale = false;
    public Vector3 customScale = Vector3.zero;

    [MenuItem("SimulatorUtil/Replace Prefab Tool")]
    static void CreateWizard()
    {
        DisplayWizard("Replace Prefab Tool", typeof(ReplacePrefabTool), "Replace");
    }

    void OnWizardCreate()
    {
        foreach (Transform t in Selection.transforms)
        {
            GameObject newObject = PrefabUtility.InstantiatePrefab(replacePrefab) as GameObject;
            Undo.RegisterCreatedObjectUndo(newObject, "created prefab");
            Transform newT = newObject.transform;
            newObject.name = replacePrefab.name;

            // parent
            if (customParent != null)
                newT.SetParent(customParent.transform);
            else
                newT.SetParent(t.parent);

            newT.position = t.position;
            newT.localRotation = isCustomRot ? Quaternion.Euler(customRot) : t.localRotation;
            newT.localScale = isCustomScale ? customScale : t.localScale;
        }

        foreach (GameObject go in Selection.gameObjects)
        {
            Undo.DestroyObjectImmediate(go);
        }
    }
}
