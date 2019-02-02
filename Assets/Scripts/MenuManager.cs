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

public class MenuManager : MonoBehaviour
{
    #region Singleton
    private static MenuManager _instance = null;
    public static MenuManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<MenuManager>();
                if (_instance == null)
                    Debug.LogError("<color=red>MenuManager" + " Not Found!</color>");
            }
            return _instance;
        }
    }
    #endregion

    public GameObject MainPanel;
    public GameObject FreeRoamingPanel;
    public Image MapImage;
    private Sprite defaultMapSprite;

    public AssetBundleSettings assetBundleSettings;
    private List<string> loadableSceneNames = new List<string>();
    private List<AssetBundle> allLoadedBundles = new List<AssetBundle>();
    private string selectedSceneName = "";

    public Dropdown MapDropdown;
    private List<string> agentOptions = new List<string>();
    private List<Sprite> MapSprites = new List<Sprite>();
    
    public Text leftShiftText;

    public GameObject AgentUI;
    public Canvas UserInterfaceAgent;
    public Canvas UserInterfaceAgentList;

    public Button RunButton;
    public Text runButtonText;

    private Image runButtonImage;
    private Color origRunButtonColor;
    private Color errorColor = Color.red;

    public GameObject aboutPanel;
    public Text buildVersionText;

    GameObject CurrentPanel;

    public GameObject ScrollArea;
    public BridgeConnectionUI connectTemplateUI;
    
    private void Awake()
    {
        if (_instance == null)
            _instance = this;

        if (_instance != this)
            DestroyImmediate(gameObject);

        if (FindObjectOfType<AnalyticsManager>() == null)
            new GameObject("GA").AddComponent<AnalyticsManager>();
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
        UpdateAgentDropdownList();

        foreach (var agent in ROSAgentManager.Instance.activeAgents)
        {
            AddAgent(agent);
        }

        if (ROSAgentManager.Instance.activeAgents.Count == 0)
            AddAgent();

        UpdateMapsAndMenu();
        InitGlobalShadowSettings();

        if (StaticConfigManager.Instance.staticConfig.initialized && StaticConfigManager.Instance.isFirstStart)
        {
            ShowFreeRoaming();
            OnRunClick();
            //isFirstStart = false; // UserInterfaceSetup.cs sets this
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
    }

    public void ShowAbout()
    {
        AnalyticsManager.Instance?.MenuButtonEvent("About");
        Activate(aboutPanel);
        MainPanel.SetActive(true);
        buildVersionText.text = BuildInfo.buildVersion;
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

        if (StaticConfigManager.Instance.staticConfig.initialized)
        {
            selectedMapName = StaticConfigManager.Instance.staticConfig.initial_configuration.map;
        }

#if UNITY_EDITOR
        if (assetBundleSettings != null)
        {
            foreach (var map in assetBundleSettings.maps)
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

    private void UpdateAgentDropdownList()
    {
        agentOptions.Clear();
        foreach (var agent in ROSAgentManager.Instance.agentPrefabs)
        {
            agentOptions.Add(agent.name);
        }
    }

    public List<string> GetAgentOptions()
    {
        return agentOptions;
    }

    public void OnRunClick()
    {
        Ros.Bridge.canConnect = true;

        bool allConnected = true;
        foreach (var agent in ROSAgentManager.Instance.activeAgents)
        {
            if (agent.Bridge.Status != Ros.Status.Connected)
            {
                allConnected = false;
                break;
            }
        }

        if (!allConnected && !StaticConfigManager.Instance.staticConfig.initialized)
        {
            if (Input.GetKey(KeyCode.LeftShift) == false)
            {
                StartCoroutine(HideErrorAfter(1.0f));
                return;
            }
        }

        PlayerPrefs.SetString("SELECTED_MAP", loadableSceneNames[MapDropdown.value]);
        ROSAgentManager.Instance.SaveAgents();

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

        if (FindObjectOfType<SimulatorManager>() == null)
        {
            GameObject go = Instantiate(Resources.Load("Managers/SimulatorManager", typeof(GameObject))) as GameObject;
        }

        ROSAgentManager.Instance.RemoveDevModeAgents();

        var agentListCanvas = Instantiate(UserInterfaceAgentList);
        var agentList = agentListCanvas.transform.FindDeepChild("Content"); // TODO needs to change !!! asap

        float height = 0;
        if (ROSAgentManager.Instance.activeAgents.Count > 1)
        {
            height = agentListCanvas.transform.FindDeepChild("AgentList").GetComponent<RectTransform>().rect.height; // TODO needs to change !!! asap
        }
        else
        {
            agentListCanvas.enabled = false;
        }

        // TODO: update spawn position from static config
        Vector3 defaultSpawnPosition = new Vector3(1.0f, 0.018f, 0.7f);
        Quaternion defaultSpawnRotation = Quaternion.identity;

        var spawnInfos = FindObjectsOfType<SpawnInfo>();
        var spawnInfoList = spawnInfos.ToList();
        spawnInfoList.Reverse();

        for (int i = 0; i < ROSAgentManager.Instance.activeAgents.Count; i++)
        {
            var agentImage = Instantiate(AgentUI, agentList);
            agentImage.transform.FindDeepChild("Address").GetComponent<Text>().text = ROSAgentManager.Instance.activeAgents[i].PrettyAddress;
            var ilocal = i;
            var button = agentImage.GetComponent<Button>();
            button.onClick.AddListener(() =>
            {
                UserInterfaceSetup.ChangeFocusUI(ROSAgentManager.Instance.activeAgents[ilocal]);
                SteeringWheelInputController.ChangeFocusSteerWheel(ROSAgentManager.Instance.activeAgents[ilocal].Agent.GetComponentInChildren<SteeringWheelInputController>());
            });

            var agentSetup = ROSAgentManager.Instance.activeAgents[i].agentType;
            var spawnPos = defaultSpawnPosition;
            var spawnRot = defaultSpawnRotation;
            if (spawnInfoList.Count > 0)
            {
                spawnPos = spawnInfoList[spawnInfoList.Count - 1].transform.position;
                spawnRot = spawnInfoList[spawnInfoList.Count - 1].transform.rotation;
                spawnInfoList.RemoveAt(spawnInfoList.Count - 1);
            }

            GameObject bot = new GameObject();

            if (StaticConfigManager.Instance.staticConfig.initialized)
            {
                var gps = agentSetup.gameObject.transform.GetComponentInChildren<GpsDevice>();

                var pos = StaticConfigManager.Instance.staticConfig.vehicles[i].position;
                if (pos.e != 0.0 || pos.n != 0.0)
                {
                    spawnPos = gps.GetPosition(pos.e, pos.n);
                    spawnPos.y = pos.h;
                    var rot = StaticConfigManager.Instance.staticConfig.vehicles[i].orientation;
                    spawnRot = Quaternion.Euler(rot.r, rot.y, rot.p);
                }
                bot = Instantiate(agentSetup == null ? ROSAgentManager.Instance.agentPrefabs[0].gameObject : agentSetup.gameObject, spawnPos, spawnRot);
            }
            else
            {
                bot = Instantiate(agentSetup == null ? ROSAgentManager.Instance.agentPrefabs[0].gameObject : agentSetup.gameObject, spawnPos - new Vector3(0.25f * i, 0, 0), spawnRot); // TODO better system
            }

            AnalyticsManager.Instance?.EgoStartEvent(agentSetup == null ? ROSAgentManager.Instance.agentPrefabs[0].gameObject.name : agentSetup.gameObject.name);

            var bridgeConnector = ROSAgentManager.Instance.activeAgents[i];

            var uiObject = Instantiate(UserInterfaceAgent);
            uiObject.GetComponent<RfbClient>().Address = ROSAgentManager.Instance.activeAgents[i].Address;
            var ui = uiObject.transform;
            ui.GetComponent<UserInterfaceSetup>().agent = bot;

            if (bot.name.Contains("duckiebot"))
            {
                HelpScreenUpdate helpScreen = uiObject.GetComponent<HelpScreenUpdate>();
                helpScreen.Help = helpScreen.DuckieHelp;
                helpScreen.agentsText = helpScreen.duckieText;
            }

            // offset for multiple vehicle UI
            RectTransform rect = uiObject.GetComponent<UserInterfaceSetup>().MainPanel;
            if (rect != null)
            {
                rect.offsetMax = new Vector2(0, rect.offsetMax.y - height);
            }
            bridgeConnector.UiObject = uiObject;
            bridgeConnector.UiButton = agentImage;
            bridgeConnector.BridgeStatus = uiObject.GetComponent<UserInterfaceSetup>().BridgeStatus;

            bot.GetComponent<AgentSetup>().Setup(ui.GetComponent<UserInterfaceSetup>(), bridgeConnector, StaticConfigManager.Instance.staticConfig.initialized ? StaticConfigManager.Instance.staticConfig.vehicles[i] : null);

            bot.GetComponent<AgentSetup>().FollowCamera.gameObject.SetActive(i == 0);
            button.image.sprite = bot.GetComponent<AgentSetup>().agentUISprite;

            uiObject.enabled = i == 0;
            var colors = button.colors;
            colors.normalColor = i == 0 ? new Color(1, 1, 1) : new Color(0.8f, 0.8f, 0.8f);
            button.colors = colors;

            var name = new GameObject($"agent_{i}_name");
            name.transform.parent = agentListCanvas.transform.FindDeepChild("Panel").transform;
            bridgeConnector.UiName = name.AddComponent<Text>();
            bridgeConnector.UiName.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            bridgeConnector.UiName.text = ROSAgentManager.Instance.activeAgents[i].PrettyAddress;
            bridgeConnector.UiName.fontSize = 16;
            bridgeConnector.UiName.fontStyle = FontStyle.Bold;
            bridgeConnector.UiName.horizontalOverflow = HorizontalWrapMode.Overflow;
            bridgeConnector.UiName.verticalOverflow = VerticalWrapMode.Overflow;

            bridgeConnector.Agent = bot;
        }

        UserInterfaceSetup.ChangeFocusUI(ROSAgentManager.Instance.activeAgents[0]);
        SteeringWheelInputController.ChangeFocusSteerWheel(ROSAgentManager.Instance.activeAgents[0].Agent.GetComponentInChildren<SteeringWheelInputController>());
        ROSAgentManager.Instance?.SetCurrentActiveAgent(ROSAgentManager.Instance.activeAgents[0]);


        //destroy spawn information after use
        foreach (var spawnInfo in spawnInfos)
        {
            Destroy(spawnInfo.gameObject);
        }

        //Configure shadow settings due to huge difference between different cars
        bool useRealSizeSetting = false;
        for (int i = 0; i < ROSAgentManager.Instance.activeAgents.Count; i++)
        {
            var agentSetup = ROSAgentManager.Instance.activeAgents[i].agentType;

            if (agentSetup.GetComponentInChildren<SimpleCarController>() == null)
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

    public static void AssignBridge(GameObject agent, Ros.Bridge bridge)
    {
        var components = agent.GetComponentsInChildren(typeof(Component));
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
        RunButton.interactable = ROSAgentManager.Instance.activeAgents.Count > 0;
    }

    public void AddAgent(RosBridgeConnector connector)
    {
        //if (connector == null)
        //    connector = ROSAgentManager.Instance.Add();

        var agentConnectInfo = Instantiate(connectTemplateUI, ScrollArea.transform);

        var addressField = agentConnectInfo.bridgeAddress;
        var agentOptionField = agentConnectInfo.agentOptions;
        agentOptionField.AddOptions(GetAgentOptions());

        if (connector.Port == RosBridgeConnector.DefaultPort)
        {
            addressField.text = connector.Address;
        }
        else
        {
            addressField.text = $"{connector.Address}:{connector.Port}";
        }

        agentOptionField.value = connector.agentType == null ? 0 : ROSAgentManager.Instance.agentPrefabs.IndexOf(connector.agentType);

        addressField.onValueChanged.AddListener((value) =>
        {
            var splits = value.Split(new char[] { ':' }, 2);
            if (splits.Length == 2)
            {
                int port;
                if (int.TryParse(splits[1], out port))
                {
                    connector.Address = splits[0];
                    connector.Port = port;
                    connector.Disconnect();
                }
            }
            else if (splits.Length == 1)
            {
                connector.Address = splits[0];
                connector.Port = RosBridgeConnector.DefaultPort;
                connector.Disconnect();
            }
        });

        agentOptionField.onValueChanged.AddListener((index) =>
        {
            connector.agentType = ROSAgentManager.Instance.agentPrefabs[index];
            connector.Disconnect();
        });

        if (connector.agentType == null)
        {
            connector.agentType = ROSAgentManager.Instance.agentPrefabs[0];
        }
        connector.BridgeStatus = agentConnectInfo.transform.Find("ConnectionStatus").GetComponent<Text>();
        connector.MenuObject = agentConnectInfo.gameObject;

        transform.SetAsLastSibling();

        RunButtonInteractiveCheck();
    }

    public void AddAgent()
    {
        var connector = ROSAgentManager.Instance.Add();

        var agentConnectInfo = Instantiate(connectTemplateUI, ScrollArea.transform);

        var addressField = agentConnectInfo.bridgeAddress;
        var agentOptionField = agentConnectInfo.agentOptions;
        agentOptionField.AddOptions(GetAgentOptions());

        if (connector.Port == RosBridgeConnector.DefaultPort)
        {
            addressField.text = connector.Address;
        }
        else
        {
            addressField.text = $"{connector.Address}:{connector.Port}";
        }

        agentOptionField.value = connector.agentType == null ? 0 : ROSAgentManager.Instance.agentPrefabs.IndexOf(connector.agentType);

        addressField.onValueChanged.AddListener((value) =>
        {
            var splits = value.Split(new char[] { ':' }, 2);
            if (splits.Length == 2)
            {
                int port;
                if (int.TryParse(splits[1], out port))
                {
                    connector.Address = splits[0];
                    connector.Port = port;
                    connector.Disconnect();
                }
            }
            else if (splits.Length == 1)
            {
                connector.Address = splits[0];
                connector.Port = RosBridgeConnector.DefaultPort;
                connector.Disconnect();
            }
        });

        agentOptionField.onValueChanged.AddListener((index) =>
        {
            connector.agentType = ROSAgentManager.Instance.agentPrefabs[index];
            connector.Disconnect();
        });

        if (connector.agentType == null)
        {
            connector.agentType = ROSAgentManager.Instance.agentPrefabs[0];
        }
        connector.BridgeStatus = agentConnectInfo.transform.Find("ConnectionStatus").GetComponent<Text>();
        connector.MenuObject = agentConnectInfo.gameObject;

        transform.SetAsLastSibling();

        RunButtonInteractiveCheck();
    }
}
