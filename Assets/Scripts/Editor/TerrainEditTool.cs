/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class TerrainEditTool : EditorWindow
{
    Terrain terrain = new Terrain();

    List<GameObject> prefabs = new List<GameObject>();
    string replacebutton = "Replace";

    [MenuItem("Window/TerrainEditTool")]
    static void Init()
    {
        TerrainEditTool window = (TerrainEditTool)EditorWindow.GetWindow(typeof(TerrainEditTool));
        window.Show();
    }

    void OnGUI()
    {
        terrain = (Terrain)EditorGUILayout.ObjectField("", terrain, typeof(Terrain), true);

        int number = EditorGUILayout.IntField("Prefab Amount", prefabs.Count);
        if (number < 0)
        {
            number = 0;
        }
        if (number > 100)
        {
            number = 100;
        }

        EditorGUILayout.LabelField("Prefabs");
        var prefabCount = prefabs.Count;
        GameObject last = null;
        if (prefabCount > 0)
        {
            last = prefabs[prefabCount - 1];
        }
        if (prefabCount < number)
        {
            for (int i = 0; i < number - prefabCount; i++)
            {
                prefabs.Add(last);
            }
        }
        else if (prefabCount > number)
        {
            for (int j = 0; j < prefabCount - number; j++)
            {
                prefabs.RemoveAt(prefabs.Count - 1);
            }
        }

        for (int i = 0; i < prefabs.Count; i++)
        {
            prefabs[i] = (GameObject)EditorGUILayout.ObjectField("", prefabs[i], typeof(GameObject), true);
        }

        if (GUILayout.Button(replacebutton))
        {
            Undo.RecordObject(terrain.gameObject, "Terrain Changes");

            TerrainData data = terrain.terrainData;
            float width = data.size.x;
            float height = data.size.z;
            float y = data.size.y;
            foreach (var tree in data.treeInstances)
            {
                Vector3 position = new Vector3(tree.position.x * width, tree.position.y * y, tree.position.z * height);
                position = terrain.transform.TransformPoint(position);
                var newTree = Instantiate(prefabs[0], position, Quaternion.identity);
                newTree.transform.eulerAngles = new Vector3(0.0f, Random.Range(-180.0f, 180.0f), 0.0f);

                newTree.transform.Translate(Vector3.up * 50.0f);

                RaycastHit hit;
                Physics.Raycast(newTree.transform.position, -Vector3.up, out hit, 100.0f, ~(1 << LayerMask.NameToLayer("Terrain")));

                newTree.transform.position = new Vector3(newTree.transform.position.x, hit.point.y, newTree.transform.position.z);
            }

            data.treeInstances = new TreeInstance[0];
        }
    }
}
