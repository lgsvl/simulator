/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using Simulator.Utilities;
using Simulator.Bridge;
using Api;

public class AgentConfig
{
    public string Name;
    public GameObject Prefab;
    public IBridgeFactory Bridge;
    public string Connection;
    public string Sensors;
}

public class SimulationConfig
{
    public string Name;
    public string Cluster;
    public bool ApiOnly;
    public bool Interactive;
    public bool OffScreen;
    public DateTime TimeOfDay;
    public float Rain;
    public float Fog;
    public float Wetness;
    public float Cloudiness;
    public AgentConfig[] Agents;
    public bool UseTraffic;
    public bool UsePedestrians;
}

public class SimulatorManager : MonoBehaviour
{
    #region Singleton
    private static SimulatorManager _instance = null;
    public static SimulatorManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<SimulatorManager>();
                if (_instance == null)
                    Debug.LogError("<color=red>SimulatorManager Not Found!</color>");
            }
            return _instance;
        }
    }
    #endregion

    public SimulationConfig Config;

    public ApiManager apiManagerPrefab;
    public AgentManager agentManagerPrefab;
    public MapManager mapManagerPrefab;
    public NPCManager npcManagerPrefab;
    public PedestrianManager pedestrianManagerPrefab;
    public EnvironmentEffectsManager environmentEffectsManagerPrefab;
    public CameraManager cameraManagerPrefab;
    public UIManager uiManagerPrefab;
    public SimulatorControls controls;

    public ApiManager ApiManager { get; private set; }
    public AgentManager AgentManager { get; private set; }
    public MapManager MapManager { get; private set; }
    public NPCManager NPCManager { get; private set; }
    public PedestrianManager PedestrianManager { get; private set; }
    public CameraManager CameraManager { get; private set; }
    public EnvironmentEffectsManager EnvironmentEffectsManager { get; private set; }
    public UIManager UIManager { get; private set; }

    public WireframeBoxes WireframeBoxes { get; private set; }

    public Color SemanticSkyColor;
    public List<SemanticColor> SemanticColors;

    // time in seconds since Unix Epoch (January 1st, 1970, UTC)
    public double CurrentTime { get; set; }

    private void Awake()
    {
        if (_instance == null)
            _instance = this;

        if (_instance != this)
        {
            DestroyImmediate(gameObject);
        }

        // TODO
        //if (FindObjectOfType<AnalyticsManager>() == null)
        //    new GameObject("GA").AddComponent<AnalyticsManager>();

        var unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
        CurrentTime = (DateTime.UtcNow - unixEpoch).TotalSeconds;
    }

    public void Init()
    {
        controls = new SimulatorControls();
        controls.Enable();
        if (Config != null && Config.ApiOnly)
        {
            ApiManager = Instantiate(apiManagerPrefab, transform);
        }
        AgentManager = Instantiate(agentManagerPrefab, transform);
        CameraManager = Instantiate(cameraManagerPrefab, transform);
        MapManager = Instantiate(mapManagerPrefab, transform);
        NPCManager = Instantiate(npcManagerPrefab, transform);
        PedestrianManager = Instantiate(pedestrianManagerPrefab, transform);
        EnvironmentEffectsManager = Instantiate(environmentEffectsManagerPrefab, transform);
        UIManager = Instantiate(uiManagerPrefab, transform);

        controls.Simulator.ToggleNPCS.performed += ctx => NPCManager.NPCActive = !NPCManager.NPCActive;
        controls.Simulator.TogglePedestrians.performed += ctx => PedestrianManager.PedestriansActive = !PedestrianManager.PedestriansActive;
        controls.Simulator.ToggleAgent.performed += ctx => AgentManager.ToggleAgent(ctx);
        controls.Simulator.ToggleReset.performed += ctx => AgentManager.ResetAgent();
        controls.Simulator.ToggleControlsUI.performed += ctx => UIManager.UIActive = !UIManager.UIActive;

        AgentManager.SpawnAgents();

        InitSemanticTags();

        WireframeBoxes = gameObject.AddComponent<WireframeBoxes>();
    }

    public void QuitSimulator()
    {
        Debug.Log("Quit Simulator");
        controls.Disable();
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void OnDestroy()
    {
        controls.Disable();
    }

    void InitSemanticTags()
    {
        var renderers = new List<Renderer>(1024);
        var materials = new List<Material>(8);

        foreach (var item in SemanticColors)
        {
            foreach (var obj in GameObject.FindGameObjectsWithTag(item.Tag))
            {
                obj.GetComponentsInChildren(true, renderers);
                renderers.ForEach(renderer =>
                {
                    if (Application.isEditor)
                    {
                        renderer.GetMaterials(materials);
                    }
                    else
                    {
                        renderer.GetSharedMaterials(materials);
                    }
                    materials.ForEach(material => material?.SetColor("_SemanticColor", item.Color));
                });
            }
        }
    }

    public void UpdateSemanticTags(GameObject obj)
    {
        var renderers = new List<Renderer>(1024);
        var materials = new List<Material>(8);

        foreach (var item in SemanticColors)
        {
            obj.GetComponentsInChildren(true, renderers);
            renderers.ForEach(renderer =>
            {
                if (Application.isEditor)
                {
                    renderer.GetMaterials(materials);
                }
                else
                {
                    renderer.GetSharedMaterials(materials);
                }
                materials.ForEach(material => material?.SetColor("_SemanticColor", item.Color));
            });
        }
    }

    void FixedUpdate()
    {
        CurrentTime += Time.fixedDeltaTime;
    }
}
