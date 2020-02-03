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
using Simulator.Api;
using Simulator.Controllable;

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

    private GameObject ManagerHolder;

    public WireframeBoxes WireframeBoxes { get; private set; }

    public Color SemanticSkyColor;
    public List<SemanticColor> SemanticColors;

    // time in seconds since Unix Epoch (January 1st, 1970, UTC)
    public double CurrentTime { get; private set; }
    public double SessionStartTime { get; private set; }

    [NonSerialized]
    public int CurrentFrame = 0;

    private bool apiMode = false;
    private bool headless = false;
    private bool interactive = false;
    private bool useSeed = false;
    private bool npc = false;
    private bool pedestrian = false;
    private string timeOfDay = "";
    private float rain = 0f;
    private float wet = 0f;
    private float fog = 0f;
    private float cloud = 0f;
    private string simulationName = "Development";
    private string mapName;
    private string clusterName = "Development";

    private DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);

    public bool IsAPI = false;
    [HideInInspector]
    public List<IControllable> Controllables = new List<IControllable>();
    [HideInInspector]
    public MonoBehaviour FixedUpdateManager;
    public uint GTIDs { get; set; }
    public uint SignalIDs { get; set; }

    private void Awake()
    {
        if (_instance == null)
            _instance = this;

        if (_instance != this)
        {
            DestroyImmediate(gameObject);
        }

        SIM.StartSession();

        CurrentTime = (DateTime.UtcNow - unixEpoch).TotalSeconds;
        SessionStartTime = CurrentTime;
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
        if (ApiManager.Instance != null)
        {
            FixedUpdateManager = ApiManager.Instance;
            IsAPI = true;
        }
        else
        {
            FixedUpdateManager = Instance;
            IsAPI = false;
        }

        controls = new SimulatorControls();
        controls.Enable();

        var config = Loader.Instance?.SimConfig;

        var masterSeed = seed ?? config?.Seed ?? new System.Random().Next();
        System.Random rand = new System.Random(masterSeed);

        ManagerHolder = new GameObject("ManagerHolder");
        ManagerHolder.transform.SetParent(transform);
        AgentManager = Instantiate(agentManagerPrefab, ManagerHolder.transform);
        CameraManager = Instantiate(cameraManagerPrefab, ManagerHolder.transform);
        MapManager = Instantiate(mapManagerPrefab, ManagerHolder.transform);
        NPCManager = Instantiate(npcManagerPrefab, ManagerHolder.transform);
        NPCManager.InitRandomGenerator(rand.Next());
        PedestrianManager = Instantiate(pedestrianManagerPrefab, ManagerHolder.transform);
        PedestrianManager.InitRandomGenerator(rand.Next());
        EnvironmentEffectsManager = Instantiate(environmentEffectsManagerPrefab, ManagerHolder.transform);
        EnvironmentEffectsManager.InitRandomGenerator(rand.Next());
        UIManager = Instantiate(uiManagerPrefab, ManagerHolder.transform);

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
            simulationName = config.Name;
            clusterName = config.ClusterName;
            mapName = config.MapName;
            NPCManager.NPCActive = config.UseTraffic;
            PedestrianManager.PedestriansActive = config.UsePedestrians;
            if (config.Agents != null)
            {
                AgentManager.SpawnAgents(config.Agents);
            }
            apiMode = config.ApiOnly;
            headless = config.Headless;
            interactive = config.Interactive;
            useSeed = config.Seed.HasValue;
            npc = config.UseTraffic;
            pedestrian = config.UsePedestrians;
            timeOfDay = config.TimeOfDay.ToString("HH:mm");
            rain = config.Rain;
            wet = config.Wetness;
            fog = config.Fog;
            cloud = config.Cloudiness;

            if (headless)
            {
                controls.Disable();
            }

            if (config.Interactive)
            {
                SetTimeScale(0.0f);
            }
        }
        SIM.APIOnly = apiMode;
        SIM.LogSimulation(SIM.Simulation.SimulationStart, simulationName);
        SIM.LogSimulation(SIM.Simulation.ClusterNameStart, clusterName);
        SIM.LogSimulation(SIM.Simulation.MapStart, string.IsNullOrEmpty(mapName) ? UnityEngine.SceneManagement.SceneManager.GetActiveScene().name : mapName);
        SIM.LogSimulation(SIM.Simulation.HeadlessModeStart, state: headless);
        SIM.LogSimulation(SIM.Simulation.InteractiveModeStart, state: interactive);
        SIM.LogSimulation(SIM.Simulation.UsePredefinedSeedStart, state: useSeed);
        SIM.LogSimulation(SIM.Simulation.NPCStart, state: npc);
        SIM.LogSimulation(SIM.Simulation.RandomPedestrianStart, state: pedestrian);
        SIM.LogSimulation(SIM.Simulation.TimeOfDayStart, timeOfDay == "" ? string.Format("{0:hh}:{0:mm}", TimeSpan.FromHours(EnvironmentEffectsManager.currentTimeOfDay)) : timeOfDay);
        SIM.LogSimulation(SIM.Simulation.RainStart, rain == 0f ? EnvironmentEffectsManager.rain.ToString() : rain.ToString());
        SIM.LogSimulation(SIM.Simulation.WetnessStart, wet == 0f ? EnvironmentEffectsManager.wet.ToString() : wet.ToString());
        SIM.LogSimulation(SIM.Simulation.FogStart, fog == 0f ? EnvironmentEffectsManager.fog.ToString() : fog.ToString());
        SIM.LogSimulation(SIM.Simulation.CloudinessStart, cloud == 0f ? EnvironmentEffectsManager.cloud.ToString() : cloud.ToString());
        InitSemanticTags();
        WireframeBoxes = gameObject.AddComponent<WireframeBoxes>();
    }

    public long GetElapsedTime(double startTime)
    {
        return (long)(CurrentTime - startTime);
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
        var elapsedTime = GetElapsedTime(SessionStartTime);
        SIM.LogSimulation(SIM.Simulation.HeadlessModeStop, value: elapsedTime, state: headless);
        SIM.LogSimulation(SIM.Simulation.InteractiveModeStop, value: elapsedTime, state: interactive);
        SIM.LogSimulation(SIM.Simulation.UsePredefinedSeedStop, state: useSeed);
        SIM.LogSimulation(SIM.Simulation.NPCStop, value: elapsedTime, state: npc);
        SIM.LogSimulation(SIM.Simulation.RandomPedestrianStop, value: elapsedTime, state: pedestrian);
        SIM.LogSimulation(SIM.Simulation.TimeOfDayStop, timeOfDay == "" ? string.Format("{0:hh}:{0:mm}", TimeSpan.FromHours(EnvironmentEffectsManager.currentTimeOfDay)) : timeOfDay, value: elapsedTime);
        SIM.LogSimulation(SIM.Simulation.RainStop, rain == 0f ? EnvironmentEffectsManager.rain.ToString() : rain.ToString(), elapsedTime);
        SIM.LogSimulation(SIM.Simulation.WetnessStop, wet == 0f ? EnvironmentEffectsManager.wet.ToString() : wet.ToString(), elapsedTime);
        SIM.LogSimulation(SIM.Simulation.FogStop, fog == 0f ? EnvironmentEffectsManager.fog.ToString() : fog.ToString(), elapsedTime);
        SIM.LogSimulation(SIM.Simulation.CloudinessStop, cloud == 0f ? EnvironmentEffectsManager.cloud.ToString() : cloud.ToString(), elapsedTime);
        SIM.LogSimulation(SIM.Simulation.MapStop, string.IsNullOrEmpty(mapName) ? UnityEngine.SceneManagement.SceneManager.GetActiveScene().name : mapName, elapsedTime);
        SIM.LogSimulation(SIM.Simulation.ClusterNameStop, clusterName, elapsedTime);
        SIM.LogSimulation(SIM.Simulation.SimulationStop, simulationName, elapsedTime);
        SIM.StopSession();

        DestroyImmediate(ManagerHolder);
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
                            if (sharedMaterials[i] == null)
                            {
                                Debug.LogError($"{renderer.gameObject.name} has null material", renderer.gameObject);
                            }
                            else
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
            if (item.Tag == obj.tag)
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

    void FixedUpdate()
    {
        // CurrentTime += Time.fixedDeltaTime;
        CurrentTime = (DateTime.UtcNow - unixEpoch).TotalSeconds;
        CurrentFrame += 1;

        if (!IsAPI)
        {
            PhysicsUpdate();
        }
    }

    public static void SetTimeScale(float scale)
    {
        Time.timeScale = scale;

        // we want FixedUpdate to be called with 100Hz normally
        if (scale == 0)
        {
            Physics.autoSimulation = false;
            Time.fixedDeltaTime = 0.01f;
        }
        else
        {
            Physics.autoSimulation = true;
            Time.fixedDeltaTime = 0.01f / scale;
        }
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

    public void PhysicsUpdate()
    {
        NPCManager.PhysicsUpdate();
        PedestrianManager.PhysicsUpdate();
    }
}

namespace Simulator.Controllable
{
    public struct ControlAction
    {
        public string Action;
        public string Value;
    }

    public interface IControllable
    {
        Transform transform { get; }

        string ControlType { get; set; }  // Control type of a controllable object (i.e., signal)
        string CurrentState { get; set; }  // Current state of a controllable object (i.e., green)
        string[] ValidStates { get; }  // Valid states (i.e., green, yellow, red)
        string[] ValidActions { get; }  // Valid actions (i.e., trigger, wait)

        // Control policy defines rules for control actions
        string DefaultControlPolicy { get; set; }  // Default control policy
        string CurrentControlPolicy { get; set; }  // Control policy that's currently active

        /// <summary>Control a controllable object with a new control policy</summary>
        /// <param name="controlPolicy">A new control policy to control this object</param>
        /// <param name="errorMsg">Error message for invalid control policy</param>
        void Control(List<ControlAction> controlActions);
    }
}
