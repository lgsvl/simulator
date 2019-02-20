/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

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

    #region vars
    public GameObject[] managers;
    public GameObject rosAgentManager;

    public GameObject AgentUI;
    public Canvas UserInterfaceAgent;
    public Canvas UserInterfaceAgentList;

    //public KeyCode exitKey = KeyCode.Escape;
    //public KeyCode toggleUIKey = KeyCode.Space;
    //public KeyCode saveAgentPos = KeyCode.F5;
    //public KeyCode loadAgentPos = KeyCode.F9;
    //public KeyCode spawnObstacle = KeyCode.F10;
    //public KeyCode demo = KeyCode.F11;

    //public KeyCode exitKey = KeyCode.Escape; // d depth camera
    //public KeyCode exitKey = KeyCode.Escape; // h k traffic
    //public KeyCode exitKey = KeyCode.Escape; // f1 help
    //public KeyCode exitKey = KeyCode.Escape; // f12 tweaks
    //public KeyCode exitKey = KeyCode.Escape; // f2 camerafollow ???
    //public KeyCode exitKey = KeyCode.Escape; // vehicle inputs
    //public KeyCode exitKey = KeyCode.Escape; // left shift
    //public KeyCode exitKey = KeyCode.Escape; // m ros to video???
    #endregion

    #region mono
    private void Awake()
    {
        if (_instance == null)
            _instance = this;

        if (_instance != this)
        {
            DestroyImmediate(gameObject);
        }

        if (FindObjectOfType<AnalyticsManager>() == null)
            new GameObject("GA").AddComponent<AnalyticsManager>();

        if (FindObjectOfType<ROSAgentManager>() == null)
            Instantiate(rosAgentManager).GetComponent<ROSAgentManager>().isDevMode = true;
    }

    private void Start()
    {
        SpawnManagers();
        InitScene();
    }

    private void InitScene()
    {
        if (ROSAgentManager.Instance.isDevMode) return;

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

        InitGlobalShadowSettings(); // TODO better way for small maps

        UserInterfaceSetup.FocusUI.CheckStaticConfigTraffic();
    }

    public static void InitGlobalShadowSettings()
    {
        // duckie town maps shadow settings
        //QualitySettings.shadowDistance = 20f;
        //QualitySettings.shadowResolution = ShadowResolution.VeryHigh;

        QualitySettings.shadowDistance = 500f;
        QualitySettings.shadowResolution = ShadowResolution.High;
    }


    //private void Update()
    //{
    //    if (Input.GetKeyDown(exitKey))
    //    {

    //    }
    //    if (Input.GetKeyDown(toggleUIKey))
    //    {

    //    }
    //    if (Input.GetKeyDown(saveAgentPos))
    //    {

    //    }
    //    if (Input.GetKeyDown(loadAgentPos))
    //    {

    //    }
    //    if (Input.GetKeyDown(spawnObstacle))
    //    {

    //    }
    //    if (Input.GetKeyDown(demo))
    //    {

    //    }

    //    //CheckStateErrors();
    //}

    private void OnApplicationQuit()
    {
        _instance = null;
        DestroyImmediate(gameObject);
    }
    #endregion

    #region managers
    public void SpawnManagers()
    {
        foreach (var item in managers)
        {
            Instantiate(item);
        }
    }
    #endregion

    #region utilities
    //protected Vector3 StringToVector3(string str)
    //{
    //    Vector3 tempVector3 = Vector3.zero;

    //    if (str.StartsWith("(") && str.EndsWith(")"))
    //        str = str.Substring(1, str.Length - 2);

    //    // split the items
    //    string[] sArray = str.Split(',');

    //    // store as a Vector3
    //    if (!string.IsNullOrEmpty(str))
    //        tempVector3 = new Vector3(float.Parse(sArray[0]), float.Parse(sArray[1]), float.Parse(sArray[2]));

    //    return tempVector3;
    //}
    #endregion
}
