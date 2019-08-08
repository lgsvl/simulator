/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using SimpleJSON;
using Simulator.Utilities;

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
