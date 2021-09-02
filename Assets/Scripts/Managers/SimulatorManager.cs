/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
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
using Simulator.Components;
using Simulator.Network.Core.Messaging.Data;
using Simulator.Sensors;
using UnityEngine.SceneManagement;
using static Simulator.Web.Config;

public class SimulatorManager : MonoBehaviour
{
    #region Singleton
    private static SimulatorManager _instance = null;
    public static SimulatorManager Instance
    {
        get
        {
            if (!InstanceAvailable)
                Debug.LogWarning("SimulatorManager not found");
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
    public CustomPassManager customPassManagerPrefab;
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
    public CustomPassManager CustomPassManager { get; private set; }
    public UIManager UIManager { get; private set; }
    public SimulatorTimeManager TimeManager { get;  } = new SimulatorTimeManager();
    public SensorsManager Sensors { get; } = new SensorsManager();

    public BridgeMessageDispatcher BridgeMessageDispatcher { get; private set; }

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
    private float Damage = 0f;
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

    private SegmentationIdMapping segmentationIdMapping;

    public SegmentationIdMapping SegmentationIdMapping => segmentationIdMapping;

    private void Awake()
    {
        if (_instance == null)
            _instance = this;

        if (_instance != this)
        {
            DestroyImmediate(gameObject);
        }

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
        segmentationIdMapping = new SegmentationIdMapping();

        //Calculate map bounds and limit position compression
        if (Loader.Instance != null && Loader.Instance.Network.IsClusterSimulation)
        {
            var mapBounds = CalculateMapBounds();
            //Add margin to the bounds
            mapBounds.size += Vector3.one * 50;
            ByteCompression.SetPositionBounds(mapBounds);
        }

        ManagerHolder = new GameObject("ManagerHolder");
        ManagerHolder.transform.SetParent(transform);
        BridgeMessageDispatcher = new BridgeMessageDispatcher();
        AnalysisManager = InstantiateManager(analysisManagerPrefab, ManagerHolder.transform);
        AgentManager = InstantiateManager(agentManagerPrefab, ManagerHolder.transform);
        CameraManager = InstantiateManager(cameraManagerPrefab, ManagerHolder.transform);
        ControllableManager = InstantiateManager(controllableManagerPrefab, ManagerHolder.transform);
        MapManager = InstantiateManager(mapManagerPrefab, ManagerHolder.transform);
        NPCManager = InstantiateManager(npcManagerPrefab, ManagerHolder.transform);
        NPCManager.InitRandomGenerator(RandomGenerator.Next());
        PedestrianManager = InstantiateManager(pedestrianManagerPrefab, ManagerHolder.transform);
        PedestrianManager.InitRandomGenerator(RandomGenerator.Next());
        EnvironmentEffectsManager = InstantiateManager(environmentEffectsManagerPrefab, ManagerHolder.transform);
        EnvironmentEffectsManager.InitRandomGenerator(RandomGenerator.Next());
        CustomPassManager = InstantiateManager(customPassManagerPrefab, ManagerHolder.transform);
        UIManager = InstantiateManager(uiManagerPrefab, ManagerHolder.transform);

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
            Damage = config.Damage;

            if (headless)
            {
                controls.Disable();
            }
        }
        InitSegmenationColors();
        WireframeBoxes = gameObject.AddComponent<WireframeBoxes>();
        if (Loader.Instance != null) TimeManager.Initialize(Loader.Instance.Network.MessagesManager);
        Sensors.Initialize();
        IsInitialized = true;
    }

    public T InstantiateManager<T>(T prefab, Transform holder) where T : Component
    {
        foreach (var customManager in CustomManagers)
        {
            var isReplaceable = prefab.GetType().IsAssignableFrom(customManager.Key);
            if (isReplaceable)
            {
                return (T)Instantiate(customManager.Value.GetComponent(customManager.Key), ManagerHolder.transform);
            }
        }
        return Instantiate(prefab, ManagerHolder.transform);
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

    public TimeSpan GetSessionElapsedTimeSpan()
    {
        return TimeSpan.FromSeconds(CurrentTime - SessionStartTime);
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
        }

        TimeManager.Deinitialize();
        Sensors.Deinitialize();
        RenderLimiter.RenderLimitEnabled();
        BridgeMessageDispatcher?.Dispose();

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

    private void InitSegmenationColors()
    {
        var renderers = new List<Renderer>(1024);
        var sharedMaterials = new List<Material>(8);
        var materials = new List<Material>(8);
        var mapping = new Dictionary<Material, Material>();

        foreach (var item in SegmentationColors)
        {
            // "Car" and "Pedestrian" may be inactive, and thus cannot be found by tag.
            // So we deal with these two tags separately by loop over the corresponding pools.
            if (item.Tag == "Car")
            {
                foreach (NPCController npcController in NPCManager.CurrentPooledNPCs)
                {
                    UpdateSegmentationColors(npcController.gameObject, npcController.GTID);
                }
            }
            else if (item.Tag == "Pedestrian")
            {
                foreach (PedestrianController pedestrianController in PedestrianManager.CurrentPooledPeds)
                {
                    UpdateSegmentationColors(pedestrianController.gameObject, pedestrianController.GTID);
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
                            renderer.GetMaterials(materials);
                        else
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
                                        Debug.LogWarning($"{renderer.gameObject.name} has null material", renderer.gameObject);
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
        var sharedMaterials = new List<Material>(8);
        var materials = new List<Material>(8);
        var mapping = new Dictionary<Material, Material>();

        foreach (var item in SegmentationColors)
        {
            // "Car" and "Pedestrian" may be inactive, and thus cannot be found by tag.
            // So we deal with these two tags separately by loop over the corresponding pools.
            if (item.Tag == "Car")
            {
                foreach (NPCController npcController in NPCManager.CurrentPooledNPCs)
                {
                    UpdateSegmentationColors(npcController.gameObject, npcController.GTID);
                }
            }
            else if (item.Tag == "Pedestrian")
            {
                foreach (PedestrianController pedestrianController in PedestrianManager.CurrentPooledPeds)
                {
                    UpdateSegmentationColors(pedestrianController.gameObject, pedestrianController.GTID);
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
                            if (Application.isEditor)
                            {
                                renderer.GetSharedMaterials(sharedMaterials);
                                renderer.GetMaterials(materials);

                                Debug.Assert(sharedMaterials.Count == materials.Count);

                                for (int i = 0; i < materials.Count; i++)
                                {
                                    if (sharedMaterials[i] == null)
                                    {
                                        Debug.LogWarning($"{renderer.gameObject.name} has null material", renderer.gameObject);
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

    public void UpdateSegmentationColors(GameObject obj, uint? gtid = null)
    {
        var renderers = new List<Renderer>(1024);
        var sharedMaterials = new List<Material>(8);
        var materials = new List<Material>(8);
        var mapping = new Dictionary<Material, Material>();

        var segId = gtid != null ? SegmentationIdMapping.AddSegmentationId(obj, gtid.Value) : -1;

        foreach (var item in SegmentationColors)
        {
            if (item.Tag == obj.tag)
            {
                Color segmentationColor = item.IsInstanceSegmenation ? GenerateSimilarColor(item.Color) : item.Color;
                if (segId > 0)
                {
                    segmentationColor.a = segId / 255f;
                }

                obj.GetComponentsInChildren(true, renderers);
                renderers.ForEach(renderer =>
                {
                    if (item.IsInstanceSegmenation || segId > 0)
                    {
                        renderer.GetMaterials(materials);
                    }
                    else
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
                                    Debug.LogWarning($"{renderer.gameObject.name} has null material", renderer.gameObject);
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

    private void FixedUpdate()
    {
        if (Time.timeScale == 0) // prevents random frames during init
            return;

        CurrentTime += Time.fixedDeltaTime;
        CurrentFrame += 1;

        if (!IsAPI && !Loader.Instance.Network.IsClient)
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

    public void PhysicsUpdate()
    {
        //Client applications does not perform physics updates
        if (Loader.Instance.Network.IsClient)
            return;
        NPCManager.PhysicsUpdate();
        PedestrianManager.PhysicsUpdate();
    }
}
