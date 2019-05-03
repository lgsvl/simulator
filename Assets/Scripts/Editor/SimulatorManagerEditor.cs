using UnityEditor;
/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoadAttribute]
public static class SimulatorManagerEditor
{
    static SimulatorManagerEditor()
    {
        EditorApplication.playModeStateChanged += LogPlayModeState;
    }

    private static void LogPlayModeState(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.name != "LoaderScene")
            {
                GameObject simObj = Resources.Load<GameObject>("SimulatorManager");
                if (simObj == null)
                {
                    Debug.LogError("Missing SimulatorManager.prefab in Resources folder!");
                    return;
                }
                GameObject clone = GameObject.Instantiate(simObj);
                clone.name = "SimulatorManager";
            }
        }
    }
}
