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

public class AgentConfig
{
    public string Name;
    public GameObject Prefab;
    public IBridgeFactory Bridge;
    public string Connection;
    public string Sensors;
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Velocity;
    public Vector3 Angular;
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

    public AgentManager agentManagerPrefab;
    public MapManager mapManagerPrefab;
    public NPCManager npcManagerPrefab;
    public PedestrianManager pedestrianManagerPrefab;
    public EnvironmentEffectsManager environmentEffectsManagerPrefab;
    public CameraManager cameraManagerPrefab;
    public UIManager uiManagerPrefab;
    public SimulatorControls controls;

    public AgentManager agentManager { get; private set; }
    public MapManager mapManager { get; private set; }
    public NPCManager npcManager { get; private set; }
    public PedestrianManager pedestrianManager { get; private set; }
    public CameraManager cameraManager { get; private set; }
    public EnvironmentEffectsManager environmentEffectsManager { get; private set; }
    public UIManager uiManager { get; private set; }

    public WireframeBoxes WireframeBoxes { get; private set; }

    public bool isDevMode { get; set; } = false;

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

    public void Init(SimulationConfig config)
    {
        Config = config;
        controls = new SimulatorControls();
        controls.Enable();
        agentManager = Instantiate(agentManagerPrefab, transform);
        cameraManager = Instantiate(cameraManagerPrefab, transform);
        mapManager = Instantiate(mapManagerPrefab, transform);
        npcManager = Instantiate(npcManagerPrefab, transform);
        pedestrianManager = Instantiate(pedestrianManagerPrefab, transform);
        environmentEffectsManager = Instantiate(environmentEffectsManagerPrefab, transform);
        uiManager = Instantiate(uiManagerPrefab, transform);

        controls.Simulator.ToggleNPCS.performed += ctx => npcManager.NPCActive = !npcManager.NPCActive;
        controls.Simulator.TogglePedestrians.performed += ctx => pedestrianManager.PedestriansActive = !pedestrianManager.PedestriansActive;
        controls.Simulator.ToggleAgent.performed += ctx => agentManager.ToggleAgent(ctx);
        controls.Simulator.ToggleReset.performed += ctx => agentManager.ResetAgent();
        controls.Simulator.ToggleControlsUI.performed += ctx => uiManager.UIActive = !uiManager.UIActive;

        agentManager.SpawnAgents();

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
    
    //Vector3 GetPosition(ROSTargetEnvironment targetEnv, double easting, double northing)
    //{
    //    MapOrigin mapOrigin = GameObject.Find("/MapOrigin").GetComponent<MapOrigin>();

    //    if (targetEnv == ROSTargetEnvironment.APOLLO || targetEnv == ROSTargetEnvironment.APOLLO35)
    //    {
    //        easting += 500000;
    //    }
    //    easting -= mapOrigin.OriginEasting;
    //    northing -= mapOrigin.OriginNorthing;

    //    float x = (float)easting;
    //    float z = (float)northing;

    //    if (targetEnv == ROSTargetEnvironment.AUTOWARE)
    //        return new Vector3(x, 0, z);
    //    return Quaternion.Euler(0f, -mapOrigin.Angle, 0f) * new Vector3(x, 0, z);
    //}

}
