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
using Simulator;

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

    public AgentManager agentManagerPrefab;
    public MapManager mapManagerPrefab;
    public NPCManager npcManagerPrefab;
    public PedestrianManager pedestrianManagerPrefab;
    public EnvironmentEffectsManager environmentEffectsManagerPrefab;
    public CameraManager cameraManagerPrefab;
    public UIManager uiManagerPrefab;
    public SimulatorControls controls;

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

    private bool headless = false;

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

        RenderLimiter.RenderLimitDisabled();
    }

    private void OnApplicationFocus(bool focus)
    {
        if (headless)
            return;

        if (focus)
            controls.Enable();
        else
            controls.Disable();
    }

    public void Init(int? seed = null)
    {
        controls = new SimulatorControls();
        controls.Enable();

        var config = Loader.Instance?.SimConfig;

        var masterSeed = config?.Seed ?? seed ?? new System.Random().Next();
        System.Random rand = new System.Random(masterSeed);

        AgentManager = Instantiate(agentManagerPrefab, transform);
        CameraManager = Instantiate(cameraManagerPrefab, transform);
        MapManager = Instantiate(mapManagerPrefab, transform);
        MapManager.InitRandomGenerator(rand.Next());
        NPCManager = Instantiate(npcManagerPrefab, transform);
        NPCManager.InitRandomGenerator(rand.Next());
        PedestrianManager = Instantiate(pedestrianManagerPrefab, transform);
        PedestrianManager.InitRandomGenerator(rand.Next());
        EnvironmentEffectsManager = Instantiate(environmentEffectsManagerPrefab, transform);
        EnvironmentEffectsManager.InitRandomGenerator(rand.Next());
        UIManager = Instantiate(uiManagerPrefab, transform);

        if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Linux && Application.isEditor)
        {
            // empty
        }
        else
        {
            controls.Simulator.ToggleNPCS.performed += ctx => NPCManager.NPCActive = !NPCManager.NPCActive;
            controls.Simulator.TogglePedestrians.performed += ctx => PedestrianManager.PedestriansActive = !PedestrianManager.PedestriansActive;
            controls.Simulator.ToggleAgent.performed += ctx =>
            {
                if (int.TryParse(ctx.control.name, out int index))
                {
                    AgentManager.SetCurrentActiveAgent(index - 1);
                }
            };
            controls.Simulator.ToggleReset.performed += ctx => AgentManager.ResetAgent();
            controls.Simulator.ToggleControlsUI.performed += ctx => UIManager.UIActive = !UIManager.UIActive;
        }

        if (config != null)
        {
            NPCManager.NPCActive = config.UseTraffic;
            PedestrianManager.PedestriansActive = config.UsePedestrians;
            if (config.Agents != null)
            {
                AgentManager.SpawnAgents(config.Agents);
            }
            headless = config.Headless;
            if (headless)
                controls.Disable();
            if (config.Interactive)
                Time.timeScale = 0f;
        }

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
        var sharedMaterials = new List<Material>(8);
        var materials = new List<Material>(8);

        var mapping = new Dictionary<Material, Material>();

        foreach (var item in SemanticColors)
        {
            foreach (var obj in GameObject.FindGameObjectsWithTag(item.Tag))
            {
                obj.GetComponentsInChildren(true, renderers);
                renderers.ForEach(renderer =>
                {
                    if (Application.isEditor)
                    {
                        renderer.GetSharedMaterials(sharedMaterials);
                        renderer.GetMaterials(materials);

                        Debug.Assert(sharedMaterials.Count == materials.Count);

                        for (int i = 0; i < materials.Count; i++)
                        {
                            if (mapping.TryGetValue(sharedMaterials[i], out var mat))
                            {
                                DestroyImmediate(materials[i]);
                                materials[i] = mat;
                            }
                            else
                            {
                                mapping.Add(sharedMaterials[i], materials[i]);
                            }
                        }

                        renderer.materials = materials.ToArray();
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

    void Update()
    {
        if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Linux && Application.isEditor)
        {
            // this is a temporary workaround for Unity Editor on Linux
            // see https://issuetracker.unity3d.com/issues/linux-editor-keyboard-when-input-handling-is-set-to-both-keyboard-input-stops-working

            if (Input.GetKeyDown(KeyCode.N)) NPCManager.NPCActive = !NPCManager.NPCActive;
            if (Input.GetKeyDown(KeyCode.P)) PedestrianManager.PedestriansActive = !PedestrianManager.PedestriansActive;
            if (Input.GetKeyDown(KeyCode.F1)) UIManager.UIActive = !UIManager.UIActive;
            if (Input.GetKeyDown(KeyCode.F12)) AgentManager.ResetAgent();
            if (Input.GetKeyDown(KeyCode.Alpha1)) AgentManager.SetCurrentActiveAgent(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) AgentManager.SetCurrentActiveAgent(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) AgentManager.SetCurrentActiveAgent(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) AgentManager.SetCurrentActiveAgent(3);
            if (Input.GetKeyDown(KeyCode.Alpha5)) AgentManager.SetCurrentActiveAgent(4);
            if (Input.GetKeyDown(KeyCode.Alpha6)) AgentManager.SetCurrentActiveAgent(5);
            if (Input.GetKeyDown(KeyCode.Alpha7)) AgentManager.SetCurrentActiveAgent(6);
            if (Input.GetKeyDown(KeyCode.Alpha8)) AgentManager.SetCurrentActiveAgent(7);
            if (Input.GetKeyDown(KeyCode.Alpha9)) AgentManager.SetCurrentActiveAgent(8);
            if (Input.GetKeyDown(KeyCode.Alpha0)) AgentManager.SetCurrentActiveAgent(9);
        }
    }
}
