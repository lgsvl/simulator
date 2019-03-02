/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using YamlDotNet.Serialization;
using YamlDotNet.RepresentationModel;

public class StaticConfig
{
    public bool initialized = false;
    public InitialConfiguration initial_configuration { get; set; }
    public List<VehicleConfig> vehicles { get; set; }
};

public class InitialConfiguration
{
    public string map { get; set; }
    public float time_of_day { get; set; }
    public bool freeze_time_of_day { get; set; }
    public float fog_intensity { get; set; }
    public float rain_intensity { get; set; }
    public float road_wetness { get; set; }
    public bool enable_traffic { get; set; }
    public bool enable_pedestrian { get; set; }
    public int traffic_density { get; set; }
}

public class VehicleConfig
{
    public string type { get; set; } //: XE_Rigged-autoware
    public string address { get; set; }
    public int port { get; set; }
    public string command_type { get; set; } //: twist
    public bool enable_lidar { get; set; }
    public bool enable_gps { get; set; }
    public bool enable_imu { get; set; }
    public bool enable_main_camera { get; set; }
    public bool enable_telephoto_camera { get; set; }
    public bool enable_sensor_effects { get; set; }
    public bool enable_high_quality_rendering { get; set; }
    public PositionVector position { get; set; }
    public OrientationVector orientation { get; set; }
}

public class PositionVector
{
    public float n { get; set; }
    public float e { get; set; }
    public float h { get; set; }
}

public class OrientationVector
{
    public float r { get; set; }
    public float p { get; set; }
    public float y { get; set; }
}

public class StaticConfigManager : MonoBehaviour
{
    #region Singleton
    private static StaticConfigManager _instance = null;
    public static StaticConfigManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<StaticConfigManager>();
                if (_instance == null)
                    Debug.LogError("<color=red>StaticConfigManager" + " Not Found!</color>");
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null)
            _instance = this;

        if (_instance != this)
            DestroyImmediate(gameObject);
        else
            DontDestroyOnLoad(gameObject);

        if (FindObjectOfType<AnalyticsManager>() == null)
            new GameObject("GA").AddComponent<AnalyticsManager>();
    }
    #endregion
    
    public StaticConfig staticConfig = new StaticConfig();
    public AssetBundleSettings assetBundleSettings;
    private AssetBundle currentBundle;
    public Text loadingText;

    private bool isLoadDevConfig = false; // for testing

    private void Start()
    {
        ReadStaticConfigFile();

        if (staticConfig.initialized)
        {
            SpawnAgentManager();
            SetConfigAgents();
            LoadStaticConfigScene();
        } 
        else
        {
            SceneManager.LoadScene("Menu");
            Destroy(gameObject);
        }
    }

    void ReadStaticConfigFile()
    {
        var configFile = "";
        if (!Application.isEditor)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--config" && args.Length > i + 1)
                {
                    configFile = args[i + 1];
                }
            }
        }
        else
        {
            if (isLoadDevConfig)
                configFile = "static_config_sample.yaml";
        }

        if (!String.IsNullOrEmpty(configFile))
        {
            StreamReader reader = new StreamReader(configFile);
            var deserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .Build();

            staticConfig = deserializer.Deserialize<StaticConfig>(reader);

            // need map and at least one vehicle specified in the static config
            if (!String.IsNullOrEmpty(staticConfig.initial_configuration.map) && staticConfig.vehicles.Count > 0)
            {
                //Debug.Log("Static config map: " + staticConfig.initial_configuration.map + " vehicle: " + staticConfig.vehicles[0].type);
                staticConfig.initialized = true;
            }
        }
    }

    private void SpawnAgentManager()
    {
        if (FindObjectOfType<ROSAgentManager>() == null)
        {
            GameObject clone = GameObject.Instantiate(Resources.Load("Managers/ROSAgentManager", typeof(GameObject))) as GameObject;
            clone.GetComponent<ROSAgentManager>().currentMode = StartModeTypes.StaticConfig;
            clone.name = "ROSAgentManager";
        }
    }

    private void SetConfigAgents()
    {
        var candidate = ROSAgentManager.Instance.agentPrefabs[0];

        foreach (var staticVehicle in staticConfig.vehicles)
        {
            foreach (var agentPrefabs in ROSAgentManager.Instance.agentPrefabs)
            {
                if (agentPrefabs.name == staticVehicle.type)
                {
                    candidate = agentPrefabs;
                    break;
                }
            }
            ROSAgentManager.Instance.Add(new RosBridgeConnector(staticVehicle.address, staticVehicle.port, candidate));
        }
        ROSAgentManager.Instance.SaveAgents();
        RosBridgeConnector.canConnect = true;
    }

    private void LoadStaticConfigScene()
    {
#if UNITY_EDITOR
        if (Application.isEditor)
        {
            if (assetBundleSettings != null)
            {
                foreach (var map in assetBundleSettings.maps)
                {
                    var scn = map.sceneAsset as UnityEditor.SceneAsset;
                    if (scn != null)
                    {
                        var sceneName = scn.name;
                        if (sceneName == staticConfig.initial_configuration.map && Application.CanStreamedLevelBeLoaded(sceneName))
                        {
                            StartCoroutine(StartSceneLoad(sceneName));
                            PlayerPrefs.SetString("SELECTED_MAP", sceneName);
                            AnalyticsManager.Instance?.MapStartEvent(sceneName);
                        }
                    }
                }
            }
        }
        else
#endif
        {
            var bundleRoot = Path.Combine(Application.dataPath, "..", "AssetBundles");
            var files = Directory.GetFiles(bundleRoot);
            foreach (var file in files)
            {
                if (Path.HasExtension(file))
                    continue;
                
                var filename = Path.GetFileName(file);
                if (filename.StartsWith("map_"))
                {
                    var mapName = filename.Substring("map_".Length);
                    if (mapName == staticConfig.initial_configuration.map.ToLower())
                    {
                        currentBundle = AssetBundle.LoadFromFile(file); //will take long with many scenes so change to async later
                        if (currentBundle != null)
                        {
                            string[] scenes = currentBundle.GetAllScenePaths(); //assume each bundle has at most one scene TODO unload scene async
                            if (scenes.Length > 0)
                            {
                                string sceneName = Path.GetFileNameWithoutExtension(scenes[0]);
                                StartCoroutine(StartSceneLoad(sceneName));
                                PlayerPrefs.SetString("SELECTED_MAP", sceneName);
                                AnalyticsManager.Instance?.MapStartEvent(sceneName);
                            }
                        }
                    }
                }
            }
        }
    }

    private IEnumerator StartSceneLoad(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;
        float elapsedTime = 0f;
        float value = 0f;
        while (elapsedTime < 3f)
        {
            if (loadingText != null)
                loadingText.text = "Loading " + (value * 100).ToString("00") + "%";
            value = Mathf.Lerp(0f, 1f, (elapsedTime / 3f));
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }
        value = 1f;
        if (loadingText != null)
            loadingText.text = "Loading " + (value * 100).ToString("00") + "%";
        asyncLoad.allowSceneActivation = true;

        while (!asyncLoad.isDone)
            yield return null;
        
        AssetBundle.UnloadAllAssetBundles(false); // editor check?

        if (FindObjectOfType<SimulatorManager>() == null)
        {
            GameObject go = Instantiate(Resources.Load("Managers/SimulatorManager", typeof(GameObject))) as GameObject;
        }

        ROSAgentManager.Instance.RemoveDevModeAgents(); // remove ui and go's of agents left in scene
    }

    public void UnloadStaticConfigBundles()
    {
        if (!Application.isEditor)
        {
            currentBundle.Unload(false);
            AssetBundle.UnloadAllAssetBundles(true);
        }
    }
}
