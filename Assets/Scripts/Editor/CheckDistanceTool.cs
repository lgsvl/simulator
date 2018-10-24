/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


using UnityEditor;
using UnityEngine;

public class CheckDistanceWindow : EditorWindow {
    static bool invalid = true;
    static float distance = 0;

    [MenuItem("Window/Check Distance Tool")]
    public static void CheckDistanceTool() {
        CheckDistanceWindow window = (CheckDistanceWindow)EditorWindow.GetWindow(typeof(CheckDistanceWindow));
        window.Show();
    }

    static void CheckDistance()
    {
        invalid = false;
        if (Selection.gameObjects.Length != 2)
        {
            invalid = true;
            distance = .0f;
        }
        else
        {
            invalid = false;
            distance = Vector3.Distance(Selection.gameObjects[0].transform.position, Selection.gameObjects[1].transform.position);
        }
    }

    void OnGUI()
    {
        if (GUILayout.Button("Check Distance"))
        {
            CheckDistance();
        }

        GUILayout.Label("Output:", EditorStyles.boldLabel);
        if (invalid)
        {
            GUILayout.Label("You need to select two objects to compute distance", EditorStyles.boldLabel);
        }
        else
        {
            GUILayout.Label(distance.ToString(), EditorStyles.boldLabel);
        }
    }
}
