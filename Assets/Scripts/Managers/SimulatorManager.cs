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
using Simulator.Analysis;
using Simulator.Network.Core.Messaging.Data;

using UnityEngine.SceneManagement;

public class SimulatorManager : MonoBehaviour
{
    #region Singleton
    private static SimulatorManager _instance = null;
    public static SimulatorManager Instance
    {
        get
        {
            if (!InstanceAvailable)
                Debug.LogError("<color=red>SimulatorManager Not Found!</color>");
            return _instance;
        }
    }

    public static bool InstanceAvailable
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<SimulatorManager>();
            }
            return _instance != null; 
        }
    }
    #endregion

    public AnalysisManager analysisManagerPrefab;
    public AgentManager agentManagerPrefab;
    public MapManager mapManagerPrefab;
    public NPCManager npcManagerPrefab;
    public PedestrianManager pedestrianManagerPrefab;
    public ControllableManager controllableManagerPrefab;
    public EnvironmentEffectsManager environmentEffectsManagerPrefab;
    public CameraManager cameraManagerPrefab;
    public UIManager uiManagerPrefab;
    public SimulatorControls controls;

    public AnalysisManager AnalysisManager { get; private set; }
    public AgentManager AgentManager { get; private set; }
    public MapManager MapManager { get; private set; }
    public NPCManager NPCManager { get; private set; }
    public PedestrianManager PedestrianManager { get; private set; }
    public ControllableManager ControllableManager { get; private set; }
    public CameraManager CameraManager { get; private set; }
    public EnvironmentEffectsManager EnvironmentEffectsManager { get; private set; }
    public UIManager UIManager { get; private set; }
    public SimulatorTimeManager TimeManager { get;  } = new SimulatorTimeManager();
    public SensorsManager Sensors { get; } = new SensorsManager();

    private GameObject ManagerHolder;

    public WireframeBoxes WireframeBoxes { get; private set; }

    public Color SkySegmentationColor;
    public List<SegmentationColor> SegmentationColors;

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
    public bool IsAPI = false;
    
    [HideInInspector]
    public MonoBehaviour FixedUpdateManager;
    
    private bool IsInitialized { get; set; }
    public uint GTIDs { get; set; }
    public uint SignalIDs { get; set; }
    private System.Random RandomGenerator;
    private HashSet<Color> InstanceColorSet = new HashSet<Color>();

    private void Awake()
    {
        if (_instance == null)
            _instance = this;

        if (_instance != this)
        {
            DestroyImmediate(gameObject);
        }

        SIM.StartSession();

        var unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
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

        SimulationConfig config = null;
        if (Loader.Instance != null)
        {
            config = Loader.Instance.SimConfig;
        }

        var masterSeed = seed ?? config?.Seed ?? new System.Random().Next();
        RandomGenerator = new System.Random(masterSeed);

        //Calculate map bounds and limit position compression
        if (Loader.Instance != null && Loader.Instance.Network.IsClusterSimulation)
        {
            var mapBounds = CalculateMapBounds();
            //Add margin to the bounds
            mapBounds.size += Vector3.one * 10;
            ByteCompression.SetPositionBounds(mapBounds);
        }

        ManagerHolder = new GameObject("ManagerHolder");
        ManagerHolder.transform.SetParent(transform);
        AnalysisManager = Instantiate(analysisManagerPrefab, ManagerHolder.transform);
        AgentManager = Instantiate(agentManagerPrefab, ManagerHolder.transform);
        CameraManager = Instantiate(cameraManagerPrefab, ManagerHolder.transform);
        ControllableManager = Instantiate(controllableManagerPrefab, ManagerHolder.transform);
        MapManager = Instantiate(mapManagerPrefab, ManagerHolder.transform);
        NPCManager = Instantiate(npcManagerPrefab, ManagerHolder.transform);
        NPCManager.InitRandomGenerator(RandomGenerator.Next());
        PedestrianManager = Instantiate(pedestrianManagerPrefab, ManagerHolder.transform);
        PedestrianManager.InitRandomGenerator(RandomGenerator.Next());
        EnvironmentEffectsManager = Instantiate(environmentEffectsManagerPrefab, ManagerHolder.transform);
        EnvironmentEffectsManager.InitRandomGenerator(RandomGenerator.Next());
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
        InitSegmenationColors();
        WireframeBoxes = gameObject.AddComponent<WireframeBoxes>();
        if (Loader.Instance != null) TimeManager.Initialize(Loader.Instance.Network.MessagesManager);
        Sensors.Initialize();
        IsInitialized = true;
    }

    private Bounds CalculateMapBounds()
    {
        var gameObjectsOnScene = SceneManager.GetActiveScene().GetRootGameObjects();
        var b = new Bounds(Vector3.zero, Vector3.zero);
        for (var i = 0; i < gameObjectsOnScene.Length; i++)
        {
            var gameObjectOnScene = gameObjectsOnScene[i];
            foreach (Renderer r in gameObjectOnScene.GetComponentsInChildren<Renderer>())
            {
                b.Encapsulate(r.bounds);
            }
        }

        return b;
    }

    public long GetSessionElapsedTime()
    {
        return (long)(CurrentTime - SessionStartTime);
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
        if (IsInitialized)
        {
            controls.Disable();
            var elapsedTime = GetElapsedTime(SessionStartTime);
            SIM.LogSimulation(SIM.Simulation.HeadlessModeStop, value: elapsedTime, state: headless);
            SIM.LogSimulation(SIM.Simulation.InteractiveModeStop, value: elapsedTime, state: interactive);
            SIM.LogSimulation(SIM.Simulation.UsePredefinedSeedStop, state: useSeed);
            SIM.LogSimulation(SIM.Simulation.NPCStop, value: elapsedTime, state: npc);
            SIM.LogSimulation(SIM.Simulation.RandomPedestrianStop, value: elapsedTime, state: pedestrian);
            SIM.LogSimulation(SIM.Simulation.TimeOfDayStop,
                timeOfDay == ""
                    ? string.Format("{0:hh}:{0:mm}", TimeSpan.FromHours(EnvironmentEffectsManager.currentTimeOfDay))
                    : timeOfDay, value: elapsedTime);
            SIM.LogSimulation(SIM.Simulation.RainStop,
                rain == 0f ? EnvironmentEffectsManager.rain.ToString() : rain.ToString(), elapsedTime);
            SIM.LogSimulation(SIM.Simulation.WetnessStop,
                wet == 0f ? EnvironmentEffectsManager.wet.ToString() : wet.ToString(), elapsedTime);
            SIM.LogSimulation(SIM.Simulation.FogStop,
                fog == 0f ? EnvironmentEffectsManager.fog.ToString() : fog.ToString(), elapsedTime);
            SIM.LogSimulation(SIM.Simulation.CloudinessStop,
                cloud == 0f ? EnvironmentEffectsManager.cloud.ToString() : cloud.ToString(), elapsedTime);
            SIM.LogSimulation(SIM.Simulation.MapStop,
                string.IsNullOrEmpty(mapName)
                    ? UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
                    : mapName, elapsedTime);
            SIM.LogSimulation(SIM.Simulation.ClusterNameStop, clusterName, elapsedTime);
            SIM.LogSimulation(SIM.Simulation.SimulationStop, simulationName, elapsedTime);
            SIM.StopSession();
        }

        TimeManager.Deinitialize();
        Sensors.Deinitialize();

        DestroyImmediate(ManagerHolder);
    }

    private Color GenerateSimilarColor(Color color)
    {
        Color newColor;
        float h, s, v;
        Color.RGBToHSV(color, out h, out s, out v);
        do
        {
            // vary s and v a little bit but keep h.
            float new_s = Mathf.Clamp01(s + RandomGenerator.NextFloat(-0.25f, 0.25f));
            float new_v = Mathf.Clamp01(v + RandomGenerator.NextFloat(-0.25f, 0.25f));
            newColor = Color.HSVToRGB(h, new_s, new_v);
        } while (!InstanceColorSet.Add(newColor));
        // TODO: There is a possibility that all possible colors have been generated,
        // which will cause infinite loop here. But since we have at least 128 * 128 = 16384 
        // possible differen colors for each hue, it is very unlikely that we generate 
        // all possible colors.
        // We may improve the logic later to fully avoid infinite loop.

        return newColor;
    }

    public void SetInstanceColor(string tag)
    {
        foreach (var item in SegmentationColors)
        {
            if (item.Tag == tag)
            {
                item.IsInstanceSegmenation = true;
                break;
            }
        }
    }

    void InitSegmenationColors()
    {
        var renderers = new List<Renderer>(1024);
        var materials = new List<Material>(8);

        foreach (var item in SegmentationColors)
        {
            foreach (var obj in GameObject.FindGameObjectsWithTag(item.Tag))
            {
                Color segmentationColor = item.IsInstanceSegmenation ? GenerateSimilarColor(item.Color) : item.Color;

                obj.GetComponentsInChildren(true, renderers);
                renderers.ForEach(renderer =>
                {
                    renderer.GetMaterials(materials);
                    materials.ForEach(material =>
                    {
                        if (material != null)
                        {
                            material.SetColor("_SegmentationColor", segmentationColor);
                        }
                    });
                });
            }
        }
    }

    // TODO: Remove this function after we are able to set SegmentationColors via WebUI.
    // Return true if "IsInstanceSegmentation" is set for any tag, false otherwise.
    public bool CheckInstanceSegmentationSetting()
    {
        foreach (var item in SegmentationColors)
        {
            if (item.IsInstanceSegmenation)
            {
                return true;
            }
        }

        return false;
    }

    // TODO: Remove this function after we are able to set SegmentationColors via WebUI.
    public void ResetSegmentationColors()
    {
        var renderers = new List<Renderer>(1024);
        var materials = new List<Material>(8);

        foreach (var item in SegmentationColors)
        {
            // "Car" and "Pedestrian" may be inactive, and thus cannot be found by tag.
            // So we deal with these two tags separately by loop over the corresponding pools.
            if (item.Tag == "Car")
            {
                foreach (NPCController npcController in NPCManager.CurrentPooledNPCs)
                {
                    UpdateSegmentationColors(npcController.gameObject);
                }
            }
            else if (item.Tag == "Pedestrian")
            {
                foreach (PedestrianController pedestrianController in PedestrianManager.CurrentPooledPeds)
                {
                    UpdateSegmentationColors(pedestrianController.gameObject);
                }
            }
            else
            {
                foreach (var obj in GameObject.FindGameObjectsWithTag(item.Tag))
                {
                    Color segmentationColor = item.IsInstanceSegmenation ? GenerateSimilarColor(item.Color) : item.Color;

                    obj.GetComponentsInChildren(true, renderers);
                    renderers.ForEach(renderer =>
                    {
                        if (item.IsInstanceSegmenation)
                        {
                            renderer.GetMaterials(materials);
                        }
                        else
                        {
                            renderer.GetSharedMaterials(materials);
                        }

                        materials.ForEach(material =>
                        {
                            if (material != null)
                            {
                                material.SetColor("_SegmentationColor", segmentationColor);
                            }
                        });
                    });
                }
            }
        }
    }

    public void UpdateSegmentationColors(GameObject obj)
    {
        var renderers = new List<Renderer>(1024);
        var materials = new List<Material>(8);

        foreach (var item in SegmentationColors)
        {
            if (item.Tag == obj.tag)
            {
                Color segmentationColor = item.IsInstanceSegmenation ? GenerateSimilarColor(item.Color) : item.Color;

                obj.GetComponentsInChildren(true, renderers);
                renderers.ForEach(renderer =>
                {
                    if (Application.isEditor || item.IsInstanceSegmenation)
                    {
                        renderer.GetMaterials(materials);
                    }
                    else
                    {
                        renderer.GetSharedMaterials(materials);
                    }

                    materials.ForEach(material =>
                    {
                        if (material != null)
                        {
                            material.SetColor("_SegmentationColor", segmentationColor);
                        }
                    });
                });
            }
        }
    }

    void FixedUpdate()
    {
        CurrentTime += Time.fixedDeltaTime;
        CurrentFrame += 1;

        if (!IsAPI)
        {
            PhysicsUpdate();
        }
    }

    public static void SetTimeScale(float scale)
    {
        //Null value is expected when simulation is stopped
        if (_instance != null)
            Instance.TimeManager.TimeScale = scale;
        else
            SimulatorTimeManager.SetUnityTimeScale(scale);
    }

    float oldTimeScale = 0f;
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

        if (Input.GetKeyDown(KeyCode.Space))
        {
            float temp = Time.timeScale;
            Time.timeScale = oldTimeScale;
            oldTimeScale = temp;
        }
    }

    public void PhysicsUpdate()
    {
        NPCManager.PhysicsUpdate();
        PedestrianManager.PhysicsUpdate();
    }
}
