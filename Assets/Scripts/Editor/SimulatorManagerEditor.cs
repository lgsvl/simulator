/**
 * Copyright (c) 2019-2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Simulator.Utilities;
using Simulator.Map;
using Simulator;

[InitializeOnLoad]
public static class SimulatorManagerEditor
{
    private static System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();

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
                var loader = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Managers/Loader.prefab");
                if (loader == null)
                {
                    Debug.LogError("Missing Loader.prefab in Resources folder!");
                    return;
                }

                var obj = Object.Instantiate(loader).GetComponent<Loader>();
                obj.EditorLoader = true;
                obj.gameObject.SetActive(true);
            }
        }
        else if (state == PlayModeStateChange.ExitingPlayMode)
        {
            //if (Simulator.Loader.Instance != null)
            //    Simulator.Loader.Instance.OnApplicationQuit();
        }
    }
}
