/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;
using UnityEditor;

public class ReplaceObject : EditorWindow
{
    [MenuItem("Window/Replace Children Only")]
    static void Replace()
    {
        GameObject src = null;
        GameObject dest = null;

        if (Selection.gameObjects.Length != 2)
        {
            Debug.LogError("You need to select exactly two object to do this operation");
            Selection.objects = new Object[0];
            return;
        }

        if (IsPrefabAsset(Selection.gameObjects[0]) && IsPrefabAsset(Selection.gameObjects[1]))
        {
            Debug.LogError("At least one object need to be non-prefab gameobject");
            Selection.objects = new Object[0];
            return;
        }
        else if (!IsPrefabAsset(Selection.gameObjects[0]) && !IsPrefabAsset(Selection.gameObjects[1]))
        {
            src = Selection.gameObjects[0];
            dest = Selection.gameObjects[1];
        }
        else
        {
            if (IsPrefabAsset(Selection.gameObjects[0]))
            {
                src = Selection.gameObjects[0];
                dest = Selection.gameObjects[1];
            }
            else
            {
                src = Selection.gameObjects[1];
                dest = Selection.gameObjects[0];
            }
        }

        if (IsPrefabAsset(src))
        {
            src = (GameObject)PrefabUtility.InstantiatePrefab(src);
            Undo.RegisterCreatedObjectUndo(src, "Create object");
        }

        Undo.RecordObjects(Selection.gameObjects, "Gameobjects");

        src.transform.position = dest.transform.position;
        src.transform.rotation = dest.transform.rotation;
        src.transform.localScale = dest.transform.localScale;
        foreach (Transform child in src.transform)
        {
            child.SetParent(dest.transform);
        }
        foreach (Transform child in dest.transform)
        {
            Undo.DestroyObjectImmediate(child);
        }
        Undo.DestroyObjectImmediate(src);

        Selection.objects = new Object[0];
    }

    static bool IsPrefabAsset(GameObject go)
    {
        return PrefabUtility.GetCorrespondingObjectFromSource(go) == null && PrefabUtility.GetPrefabObject(go) != null;
    }
}
