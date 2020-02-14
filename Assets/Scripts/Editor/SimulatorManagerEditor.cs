/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using SimpleJSON;
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
        EditorSceneManager.newSceneCreated += NewSceneCreated;
    }

    private static void NewSceneCreated(Scene scene, NewSceneSetup newSceneSetup, NewSceneMode newSceneMode)
    {
        if (Simulator.Editor.Build.Running)
        {
            return;
        }

        var objects = scene.GetRootGameObjects();
        for (int index = objects.Length - 1; index >= 0; --index)
        {
            Object.DestroyImmediate(objects[index]);
        }
        Debug.Log($"Removed {objects.Length} Unity Editor default new scene objects that will conflict with LGSVL Simulator");

        var tempGO = new GameObject("MapHolder");
        tempGO.transform.position = Vector3.zero;
        tempGO.transform.rotation = Quaternion.identity;
        var MapHolder = tempGO.AddComponent<MapHolder>();
        var trafficLanes = new GameObject("TrafficLanes").transform;
        trafficLanes.SetParent(tempGO.transform);
        MapHolder.trafficLanesHolder = trafficLanes;
        var intersections = new GameObject("Intersections").transform;
        intersections.SetParent(tempGO.transform);
        MapHolder.intersectionsHolder = intersections;
        var origin = new GameObject("MapOrigin").AddComponent<MapOrigin>();
        var spawn = new GameObject("SpawnInfo").AddComponent<SpawnInfo>();

        Debug.Log($"Added required MapHolder, MapOrigin and SpawnInfo objects for LGSVL Simulation.  Please set MapOrigin and spawn location");
    }

    private static void LogPlayModeState(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.name == "LoaderScene")
            {
                var data = EditorPrefs.GetString("Simulator/DevelopmentSettings");
                if (data != null)
                {
                    var json = JSONNode.Parse(data);
                    if (json["EnableAPI"] && json["EnableAPI"].AsBool)
                    {
                        var api = Object.Instantiate(Loader.Instance.ApiManagerPrefab);
                        api.name = "ApiManager";
                        Loader.Instance.LoaderUI.SetLoaderUIState(LoaderUI.LoaderUIStateType.READY);
                    }
                }
            }
            else
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
            //if (stopWatch.IsRunning)
            //{
            //    stopWatch.Stop();
            //    SIM.LogSimulation(SIM.Simulation.ApplicationExit, value: (long)stopWatch.Elapsed.TotalSeconds);
            //}
        }
    }
}
