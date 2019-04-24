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
            GameObject clone = GameObject.Instantiate(Resources.Load("SimulatorManager", typeof(GameObject))) as GameObject;
            clone.name = "SimulatorManager";
        }
    }
}
