/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;

[InitializeOnLoadAttribute]
public static class SimulatorManagerEditor
{
    // register an event handler when the class is initialized
    static SimulatorManagerEditor()
    {
        EditorApplication.playModeStateChanged += LogPlayModeState;
    }

    private static void LogPlayModeState(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.name != "Menu")
            {
                GameObject clone = GameObject.Instantiate(Resources.Load("Managers/SimulatorManager", typeof(GameObject))) as GameObject;
                //GameObject clone = PrefabUtility.InstantiatePrefab(Resources.Load("Managers/SimulatorManager", typeof(GameObject))) as GameObject;
            }
            
        }
    }
}
