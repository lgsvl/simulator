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
                    if (json["EnableAPI"].AsBool)
                    {
                        var api = Object.Instantiate(Simulator.Loader.Instance.ApiManagerPrefab);
                        api.name = "ApiManager";
                        Simulator.Loader.Instance.LoaderUI.SetLoaderUIState(LoaderUI.LoaderUIStateType.READY);
                    }
                }
            }
            else
            {
                var simObj = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Managers/SimulatorManager.prefab");
                if (simObj == null)
                {
                    Debug.LogError("Missing SimulatorManager.prefab in Resources folder!");
                    return;
                }
                stopWatch.Start();
                var version = "Development";
                var info = Resources.Load<BuildInfo>("BuildInfo");
                if (info != null)
                    version = info.Version;
                SIM.Init(version);
                SIM.LogSimulation(SIM.Simulation.ApplicationStart);
                var sim = Object.Instantiate(simObj).GetComponent<SimulatorManager>();
                sim.name = "SimulatorManager";

                bool useSeed = false;
                int? seed = null;
                bool enableNPCs = false;
                bool enablePEDs = false;

                var data = EditorPrefs.GetString("Simulator/DevelopmentSettings");
                if (data != null)
                {
                    try
                    {
                        var json = JSONNode.Parse(data);

                        useSeed = json["UseSeed"];
                        if (useSeed)
                        {
                            seed = json["Seed"];
                        }

                        enableNPCs = json["EnableNPCs"];
                        enablePEDs = json["EnablePEDs"];
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }

                sim.Init(seed);
                sim.AgentManager.SetupDevAgents();
                sim.NPCManager.NPCActive = enableNPCs;
                sim.PedestrianManager.PedestriansActive = enablePEDs;

            }
        }
        else if (state == PlayModeStateChange.ExitingPlayMode)
        {
            if (stopWatch.IsRunning)
            {
                stopWatch.Stop();
                SIM.LogSimulation(SIM.Simulation.ApplicationExit, value: (long)stopWatch.Elapsed.TotalSeconds);
            }
        }
    }
}
