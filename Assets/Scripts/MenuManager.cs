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

    public Dropdown MapDropdown;
    private List<string> agentOptions = new List<string>();
    private List<Sprite> MapSprites = new List<Sprite>();
    
    public Text leftShiftText;

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
        
        if (FindObjectOfType<ROSAgentManager>() == null)
        {
            GameObject clone = GameObject.Instantiate(Resources.Load("Managers/ROSAgentManager", typeof(GameObject))) as GameObject;
            clone.GetComponent<ROSAgentManager>().currentMode = StartModeTypes.Menu;
            clone.name = "ROSAgentManager";
        }

        StaticConfigManager scM = FindObjectOfType<StaticConfigManager>();
        if (scM != null)
        {
            scM.UnloadStaticConfigBundles();
            Destroy(scM.gameObject);
        }
    }

    public IEnumerator Start()
    {
        yield return new WaitUntil(() => ROSAgentManager.Instance.isAgentsLoaded);

        leftShiftText.text = "Hold Left-Shift and Click Run for Standalone Mode";
        runButtonImage = RunButton.GetComponent<Image>();
        origRunButtonColor = runButtonImage.color;

        RosBridgeConnector.canConnect = false;
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
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.LeftShift))
            leftShiftText.text = "Standalone Mode Ready";
        else
            leftShiftText.text = "Hold Left-Shift and Click Run for Standalone Mode";
    }
    
    public void ShowFreeRoaming()
    {
        AnalyticsManager.Instance?.MenuButtonEvent("FreeRoaming");
        Activate(FreeRoamingPanel);
        RosBridgeConnector.canConnect = true;
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
        RosBridgeConnector.canConnect = false;
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

    public void LoadScene(string name, Action cb)
    {
        AnalyticsManager.Instance?.MapStartEvent(name);

        // TODO: add nice loading progress to both async operations (bundle and scene loading)
        var loader = SceneManager.LoadSceneAsync(name);
        loader.completed += SceneLoadFinished;

        if (cb != null)
        {
            loader.completed += op => cb();
        }
    }

    public void OnRunClick()
    {
        RosBridgeConnector.canConnect = true;

        bool allConnected = true;
        foreach (var agent in ROSAgentManager.Instance.activeAgents)
        {
            if (agent.Bridge.Status != Comm.BridgeStatus.Connected)
            {
                allConnected = false;
                break;
            }
        }

        if (!allConnected)
        {
            if (Input.GetKey(KeyCode.LeftShift) == false)
            {
                StartCoroutine(HideErrorAfter(1.0f));
                return;
            }
        }

        var selectedSceneName = loadableSceneNames[MapDropdown.value];

        RunButton.interactable = false;
        PlayerPrefs.SetString("SELECTED_MAP", selectedSceneName);
        ROSAgentManager.Instance.SaveAgents();

        LoadScene(selectedSceneName, null);
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

        ROSAgentManager.Instance.RemoveDevModeAgents(); // remove ui and go's of agents left in scene
    }

    public static void AssignBridge(GameObject agent, Comm.Bridge bridge)
    {
        var components = agent.GetComponentsInChildren(typeof(Component));
        foreach (var component in components)
        {
            var ros = component as Comm.BridgeClient;
            if (ros != null)
            {
                ros.OnBridgeAvailable(bridge);
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
        var agentSetup = ROSAgentManager.Instance.agentPrefabs[0];
        var connector = new RosBridgeConnector(agentSetup);
        ROSAgentManager.Instance.Add(connector);

        var agentConnectInfo = Instantiate(connectTemplateUI, ScrollArea.transform);

        var addressField = agentConnectInfo.bridgeAddress;
        var agentOptionField = agentConnectInfo.agentOptions;
        agentOptionField.AddOptions(GetAgentOptions());

        addressField.text = connector.Address;

        agentOptionField.value = ROSAgentManager.Instance.agentPrefabs.IndexOf(connector.agentType);

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

        connector.BridgeStatus = agentConnectInfo.transform.Find("ConnectionStatus").GetComponent<Text>();
        connector.MenuObject = agentConnectInfo.gameObject;

        transform.SetAsLastSibling();

        RunButtonInteractiveCheck();
    }
}
