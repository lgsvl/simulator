/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
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

public class MenuScript : MonoBehaviour
{
    #region Singleton
    private static MenuScript _instance = null;
    public static MenuScript Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<MenuScript>();
                if (_instance == null)
                    Debug.LogError("<color=red>MenuScript Not Found!</color>");
            }
            return _instance;
        }
    }
    #endregion

    public GameObject MainPanel;
    public GameObject FreeRoamingPanel;
    public Image MapImage;
    private Sprite defaultMapSprite;

    public AssetBundleManager assetBundleManager;
    private List<string> loadableSceneNames = new List<string>();
    private List<AssetBundle> allLoadedBundles = new List<AssetBundle>();
    private string selectedSceneName = "";

    public Dropdown MapDropdown;
    private List<string> robotOptions = new List<string>();
    private List<Sprite> MapSprites = new List<Sprite>();

    public MenuAddRobot MenuAddRobot;

    public RosRobots Robots;
    public Text leftShiftText;

    public GameObject DuckiebotRobot;
    public Canvas UserInterface;
    public Canvas UserInterfaceRobotList;

    public Button RunButton;
    public Text runButtonText;

    private Image runButtonImage;
    private Color origRunButtonColor;
    private Color errorColor = Color.red;

    public GameObject aboutPanel;
    public Text buildVersionText;

    GameObject CurrentPanel;

    static internal bool IsTrainingMode = false;

    public StaticConfig staticConfig = new StaticConfig();

    static private bool isFirstStart = true;

    void Awake()
    {
        if (_instance == null)
            _instance = this;

        if (_instance != this)
            DestroyImmediate(gameObject);
    }

    public void Start()
    {
        leftShiftText.text = "Hold Left-Shift and Click Run for Standalone Mode";
        runButtonImage = RunButton.GetComponent<Image>();
        origRunButtonColor = runButtonImage.color;

        Ros.Bridge.canConnect = false;
        if (defaultMapSprite == null)
        {
            defaultMapSprite = MapImage.sprite;
        }

        CurrentPanel = MainPanel;
        UpdateRobotDropdownList();

        ReadStaticConfigFile();

        foreach (var robot in Robots.Robots)
        {
            MenuAddRobot.Add(robot);
        }

        if (Robots.Robots.Count == 0)
        {
            MenuAddRobot.Add(Robots.Add());
        }

        UpdateMapsAndMenu();
        InitGlobalShadowSettings();

        if (staticConfig.initialized && isFirstStart)
        {
            isFirstStart = false;
            ShowFreeRoaming();
            OnRunClick();
        }
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.LeftShift))
            leftShiftText.text = "Standalone Mode Ready";
        else
            leftShiftText.text = "Hold Left-Shift and Click Run for Standalone Mode";
    }

    public static void InitGlobalShadowSettings()
    {
        QualitySettings.shadowDistance = 20f;
        QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
    }

    public static void RealSizeGlobalShadowSettings()
    {
        QualitySettings.shadowDistance = 500f;
        QualitySettings.shadowResolution = ShadowResolution.High;
    }

    public void ShowFreeRoaming()
    {
        AnalyticsManager.Instance?.MenuButtonEvent("FreeRoaming");
        Activate(FreeRoamingPanel);
        IsTrainingMode = false;
        Ros.Bridge.canConnect = true;
    }

    public void ShowEditor()
    {
        AnalyticsManager.Instance?.MenuButtonEvent("Editor");
    }

    public void ShowTraining()
    {
        AnalyticsManager.Instance?.MenuButtonEvent("Training");
        //Activate(FreeRoamingPanel);
        //IsTrainingMode = true;
    }

    public void ShowAbout()
    {
        AnalyticsManager.Instance?.MenuButtonEvent("About");
        Activate(aboutPanel);
        MainPanel.SetActive(true);
        buildVersionText.text = BuildInfo.buildVersion;
        IsTrainingMode = false;
    }

    public void ShowMainmenu()
    {
        Activate(MainPanel);
        Ros.Bridge.canConnect = false;
    }

    public void UpdateMapsAndMenu()
    {
        if (!Application.isEditor)
        {
            allLoadedBundles.ForEach(b => b.Unload(false));
            allLoadedBundles.Clear();
            AssetBundle.UnloadAllAssetBundles(true);
        }

        MapDropdown.ClearOptions();
        loadableSceneNames.Clear();
        MapSprites.Clear();

        int selectedMapIndex = 0;
        var selectedMapName = PlayerPrefs.GetString("SELECTED_MAP", null);

        if (staticConfig.initialized)
        {
            selectedMapName = staticConfig.initial_configuration.map;
        }

#if UNITY_EDITOR
        if (assetBundleManager != null)
        {
            foreach (var map in assetBundleManager.assetBundleSettings.maps)
            {
                var scn = map.sceneAsset as UnityEditor.SceneAsset;
                if (scn != null)
                {
                    var sceneName = scn.name;
                    if (Application.CanStreamedLevelBeLoaded(sceneName) && !loadableSceneNames.Contains(sceneName))
                    {
                        if (sceneName == selectedMapName)
                        {
                            selectedMapIndex = loadableSceneNames.Count;
                        }
                        loadableSceneNames.Add(sceneName);
                        MapSprites.Add(map.spriteImg);
                    }
                }
            }
            MapDropdown.AddOptions(loadableSceneNames);
        }
#endif

        if (!Application.isEditor)
        {
            var bundleRoot = Path.Combine(Application.dataPath, "..", "AssetBundles");
            var files = Directory.GetFiles(bundleRoot);
            foreach (var f in files)
            {
                if (Path.HasExtension(f))
                {
                    continue;
                }
                var filename = Path.GetFileName(f);
                if (filename.StartsWith("map_"))
                {
                    var mapName = filename.Substring("map_".Length);
                    var bundle = AssetBundle.LoadFromFile(f); //will take long with many scenes so change to async later
                    if (bundle != null)
                    {
                        allLoadedBundles.Add(bundle);
                    }
                    string[] scenes = bundle.GetAllScenePaths(); //assume each bundle has at most one scene
                    if (scenes.Length > 0)
                    {
                        string sceneName = Path.GetFileNameWithoutExtension(scenes[0]);
                        if (sceneName == selectedMapName)
                        {
                            selectedMapIndex = loadableSceneNames.Count;
                        }
                        loadableSceneNames.Add(sceneName);
                        Sprite spriteImg = null;
                        var spriteBundleFile = f.Replace($"map_{mapName}", $"mapimage_{mapName}");
                        if (File.Exists(spriteBundleFile))
                        {
                            var spriteBundle = AssetBundle.LoadFromFile(spriteBundleFile);
                            if (spriteBundle != null)
                            {
                                allLoadedBundles.Add(spriteBundle);
                                spriteImg = spriteBundle.LoadAsset<Sprite>($"mapimage_{mapName}");
                            }
                        } 
                        MapSprites.Add(spriteImg);
                    }
                }
            }
            MapDropdown.AddOptions(loadableSceneNames);
        }

        MapDropdown.value = selectedMapIndex;
        ChangeMapImage();
    }

    private void UpdateRobotDropdownList()
    {
        robotOptions.Clear();
        foreach (var robot in Robots.robotCandidates)
        {
            robotOptions.Add(robot.name);
        }
    }

    public List<string> GetRobotOptions()
    {
        return robotOptions;
    }

    public void OnRunClick()
    {
        Ros.Bridge.canConnect = true;

        bool allConnected = true;
        foreach (var robot in Robots.Robots)
        {
            if (robot.Bridge.Status != Ros.Status.Connected)
            {
                allConnected = false;
                break;
            }
        }

        if (!allConnected && !staticConfig.initialized)
        {
            if (Input.GetKey(KeyCode.LeftShift) == false)
            {
                StartCoroutine(HideErrorAfter(1.0f));
                return;
            }
        }

        PlayerPrefs.SetString("SELECTED_MAP", loadableSceneNames[MapDropdown.value]);
        Robots.Save();

        selectedSceneName = loadableSceneNames[MapDropdown.value];

        AnalyticsManager.Instance?.MapStartEvent(selectedSceneName);

        // TODO: add nice loading progress to both async operations (bundle and scene loading)
        var loader = SceneManager.LoadSceneAsync(selectedSceneName);
        loader.completed += SceneLoadFinished;

        RunButton.interactable = false;
    }

    IEnumerator HideErrorAfter(float seconds)
    {
        RunButton.interactable = false;
        runButtonText.text = "ERROR: Failed connecting to ROS bridge!";
        runButtonImage.color = errorColor;

        yield return new WaitForSeconds(seconds);

        float elapsedTime = 0f;
        while (elapsedTime < 3.0f)
        {
            runButtonImage.color = Color.Lerp(errorColor, origRunButtonColor, (elapsedTime / 1f));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        runButtonImage.color = origRunButtonColor;
        runButtonText.text = "RUN";
        RunButton.interactable = true;
    }

    void SceneLoadFinished(AsyncOperation op)
    {
        if (!Application.isEditor)
        {
            allLoadedBundles.ForEach(b => b.Unload(false));
            allLoadedBundles.Clear();
        }

        var robotListCanvas = Instantiate(UserInterfaceRobotList);
        var robotList = robotListCanvas.transform.FindDeepChild("Content");

        float height = 0;
        if (Robots.Robots.Count > 1)
        {
            height = robotListCanvas.transform.FindDeepChild("RobotList").GetComponent<RectTransform>().rect.height;
        }
        else
        {
            robotListCanvas.enabled = false;
        }

        // TODO: update spawn position from static config
        Vector3 defaultSpawnPosition = new Vector3(1.0f, 0.018f, 0.7f);
        Quaternion defaultSpawnRotation = Quaternion.identity;

        var spawnInfos = FindObjectsOfType<SpawnInfo>();
        var spawnInfoList = spawnInfos.ToList();
        spawnInfoList.Reverse();

        //avoid first frame collision
        //var sceneRobots = FindObjectsOfType<RobotSetup>();
        //foreach (var robot in sceneRobots)
        //{
        //    var cols = robot.GetComponentsInChildren<Collider>();
        //    foreach (var col in cols)
        //    {
        //        col.enabled = false;
        //    }
        //}

        for (int i = 0; i < Robots.Robots.Count; i++)
        {
            var robotImage = Instantiate(DuckiebotRobot, robotList);
            robotImage.transform.FindDeepChild("Address").GetComponent<Text>().text = Robots.Robots[i].PrettyAddress;
            var ilocal = i;
            var button = robotImage.GetComponent<Button>();
            button.onClick.AddListener(() =>
            {
                UserInterfaceSetup.ChangeFocusUI(Robots.Robots[ilocal], Robots);
                SteeringWheelInputController.ChangeFocusSteerWheel(Robots.Robots[ilocal].Robot.GetComponentInChildren<SteeringWheelInputController>());
            });

            var robotSetup = Robots.Robots[i].robotType;
            var spawnPos = defaultSpawnPosition;
            var spawnRot = defaultSpawnRotation;
            if (spawnInfoList.Count > 0)
            {
                spawnPos = spawnInfoList[spawnInfoList.Count - 1].transform.position;
                spawnRot = spawnInfoList[spawnInfoList.Count - 1].transform.rotation;
                spawnInfoList.RemoveAt(spawnInfoList.Count - 1);
            }

            if (staticConfig.initialized)
            {
                var gps = robotSetup.gameObject.transform.GetComponentInChildren<GpsDevice>();

                var pos = staticConfig.vehicles[i].position;
                if (pos.e != 0.0 || pos.n != 0.0)
                {
                    spawnPos = gps.GetPosition(pos.e, pos.n);
                    spawnPos.y = pos.h;

                    var rot = staticConfig.vehicles[i].orientation;
                    spawnRot = Quaternion.Euler(rot.r, rot.y, rot.p);
                }
            }

            var bot = Instantiate(robotSetup == null ? Robots.robotCandidates[0].gameObject : robotSetup.gameObject, spawnPos - new Vector3(0.25f * i, 0, 0), spawnRot);

            AnalyticsManager.Instance?.EgoStartEvent(robotSetup == null ? Robots.robotCandidates[0].gameObject.name : robotSetup.gameObject.name);

            var bridgeConnector = Robots.Robots[i];

            var uiObject = Instantiate(UserInterface);
            uiObject.GetComponent<RfbClient>().Address = Robots.Robots[i].Address;
            var ui = uiObject.transform;

            if (bot.name.Contains("duckiebot")) {
                HelpScreenUpdate helpScreen = uiObject.GetComponent<HelpScreenUpdate>();
                helpScreen.Help = helpScreen.DuckieHelp;
                helpScreen.RobotsText = helpScreen.DuckieRobotsText;
            }

            // offset for multiple vehicle UI
            RectTransform rect = uiObject.GetComponent<UserInterfaceSetup>().MainPanel;
            if (rect != null)
            {
                rect.offsetMax = new Vector2(0, rect.offsetMax.y - height);
            }
            bridgeConnector.UiObject = uiObject;
            bridgeConnector.UiButton = robotImage;
            bridgeConnector.BridgeStatus = uiObject.GetComponent<UserInterfaceSetup>().BridgeStatus;
            ui.GetComponent<HelpScreenUpdate>().Robots = Robots;

            bot.GetComponent<RobotSetup>().Setup(ui.GetComponent<UserInterfaceSetup>(), bridgeConnector, staticConfig.initialized ? staticConfig.vehicles[i] : null);
            
            bot.GetComponent<RobotSetup>().FollowCamera.gameObject.SetActive(i == 0);
            button.image.sprite = bot.GetComponent<RobotSetup>().robotUISprite;

            uiObject.enabled = i == 0;
            var colors = button.colors;
            colors.normalColor = i == 0 ? new Color(1, 1, 1) : new Color(0.8f, 0.8f, 0.8f);
            button.colors = colors;

            var name = new GameObject($"robot_{i}_name");
            name.transform.parent = robotListCanvas.transform.FindDeepChild("Panel").transform;
            bridgeConnector.UiName = name.AddComponent<Text>();
            bridgeConnector.UiName.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            bridgeConnector.UiName.text = Robots.Robots[i].PrettyAddress;
            bridgeConnector.UiName.fontSize = 16;
            bridgeConnector.UiName.fontStyle = FontStyle.Bold;
            bridgeConnector.UiName.horizontalOverflow = HorizontalWrapMode.Overflow;
            bridgeConnector.UiName.verticalOverflow = VerticalWrapMode.Overflow;

            bridgeConnector.Robot = bot;

            SimulatorManager.Instance?.AddActiveRobot(bot);
        }

        // hack for dev
        SimulatorManager.Instance.SpawnDashUI();

        UserInterfaceSetup.ChangeFocusUI(Robots.Robots[0], Robots);
        SteeringWheelInputController.ChangeFocusSteerWheel(Robots.Robots[0].Robot.GetComponentInChildren<SteeringWheelInputController>());
        SimulatorManager.Instance.SetCurrentActiveRobot(Robots.Robots[0].Robot);

        //destroy spawn information after use
        foreach (var spawnInfo in spawnInfos)
        {
            Destroy(spawnInfo.gameObject);
        }

        //Configure shadow settings due to huge difference between different cars
        bool useRealSizeSetting = false;
        for (int i = 0; i < Robots.Robots.Count; i++)
        {
            var robotSetup = Robots.Robots[i].robotType;

            if (robotSetup.GetComponentInChildren<SimpleCarController>() == null)
            {
                useRealSizeSetting = true;
                break;
            }
        }
        if (useRealSizeSetting)
        {
            RealSizeGlobalShadowSettings();
        }

        UserInterfaceSetup.FocusUI.Invoke("CheckStaticConfigTraffic", 0.5f);
    }

    public static void AssignBridge(GameObject robot, Ros.Bridge bridge)
    {
        var components = robot.GetComponentsInChildren(typeof(Component));
        foreach (var component in components)
        {
            var ros = component as Ros.IRosClient;
            if (ros != null)
            {
                ros.OnRosBridgeAvailable(bridge);
            }
        }
    }

    public void ChangeMapImage()
    {
        if (MapDropdown.value >= MapSprites.Count)
        {
            return;
        }

        var s = MapSprites[MapDropdown.value];
        if (s == null)
        {
            s = defaultMapSprite;
        }
        MapImage.sprite = s;
    }

    void Activate(GameObject target)
    {
        target.SetActive(true);
        CurrentPanel.SetActive(false);
        CurrentPanel = target;
    }

    public void RunButtonInteractiveCheck()
    {
        RunButton.interactable = Robots.Robots.Count > 0;
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
            // uncomment to test static config in Editor
            //configFile = "static_config_sample.yaml";
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
                Debug.Log("Static config map: " + staticConfig.initial_configuration.map + " vehicle: " + staticConfig.vehicles[0].type);
                staticConfig.initialized = true;

                Robots.Robots.Clear();
                var candidate = Robots.robotCandidates[0];

                foreach (var staticVehicle in staticConfig.vehicles)
                {
                    foreach (var rob in Robots.robotCandidates)
                    {
                        if(rob.name == staticVehicle.type)
                        {
                            candidate = rob;
                            break;
                        }
                    }

                    Robots.Robots.Add(new RosBridgeConnector(staticVehicle.address, staticVehicle.port, candidate));
                }
            }

        }
        UserInterfaceSetup.staticConfig = staticConfig;
    }
}
