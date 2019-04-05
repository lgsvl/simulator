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
    
    public GameObject AgentUI;
    public Canvas UserInterfaceAgent;
    public Canvas UserInterfaceAgentList;

    Transform AgentList;
    Canvas AgentListCanvas;

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
        {
            GameObject clone = GameObject.Instantiate(Resources.Load("Managers/ROSAgentManager", typeof(GameObject))) as GameObject;
            clone.GetComponent<ROSAgentManager>().currentMode = StartModeTypes.Dev;
            clone.name = "ROSAgentManager";
        }
    }

    private void Start()
    {
        SpawnManagers();
        InitScene();
    }

    public void DespawnVehicle(RosBridgeConnector connector)
    {
        Destroy(connector.UiObject.gameObject);
        Destroy(connector.UiName.gameObject);
        Destroy(connector.UiButton);
    }

    public void SpawnVehicle(Vector3 position, Quaternion rotation, RosBridgeConnector connector, VehicleConfig staticConfig, float height = 0.0f)
    {
        var agentImage = Instantiate(AgentUI, AgentList);
        agentImage.transform.FindDeepChild("Address").GetComponent<Text>().text = connector.PrettyAddress;
        var button = agentImage.GetComponent<Button>();
        button.onClick.AddListener(() =>
        {
            UserInterfaceSetup.ChangeFocusUI(connector);
            SteeringWheelInputController.ChangeFocusSteerWheel(connector.Agent.GetComponentInChildren<SteeringWheelInputController>());
        });

        var agentSetup = connector.agentType;

        GameObject bot = Instantiate(agentSetup == null ? ROSAgentManager.Instance.agentPrefabs[0].gameObject : agentSetup.gameObject, position, rotation); // TODO better system

        AnalyticsManager.Instance?.EgoStartEvent(agentSetup == null ? ROSAgentManager.Instance.agentPrefabs[0].gameObject.name : agentSetup.gameObject.name);

        var uiObject = Instantiate(UserInterfaceAgent);
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
        connector.UiObject = uiObject;
        connector.UiButton = agentImage;
        connector.BridgeStatus = uiObject.GetComponent<UserInterfaceSetup>().BridgeStatus;

        bot.GetComponent<AgentSetup>().Setup(ui.GetComponent<UserInterfaceSetup>(), connector, staticConfig);

        bot.GetComponent<AgentSetup>().FollowCamera.gameObject.SetActive(false);
        button.image.sprite = bot.GetComponent<AgentSetup>().agentUISprite;

        //uiObject.enabled = i == 0;
        var colors = button.colors;
        //colors.normalColor = i == 0 ? new Color(1, 1, 1) : new Color(0.8f, 0.8f, 0.8f);
        button.colors = colors;

        var name = new GameObject();
        name.transform.parent = AgentListCanvas.transform.FindDeepChild("Panel").transform;
        connector.UiName = name.AddComponent<Text>();
        connector.UiName.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        connector.UiName.text = connector.PrettyAddress;
        connector.UiName.fontSize = 16;
        connector.UiName.fontStyle = FontStyle.Bold;
        connector.UiName.horizontalOverflow = HorizontalWrapMode.Overflow;
        connector.UiName.verticalOverflow = VerticalWrapMode.Overflow;

        connector.Agent = bot;
    }

    Vector3 GetPosition(ROSTargetEnvironment targetEnv, double easting, double northing)
    {
        MapOrigin mapOrigin = GameObject.Find("/MapOrigin").GetComponent<MapOrigin>();

        if (targetEnv == ROSTargetEnvironment.APOLLO || targetEnv == ROSTargetEnvironment.APOLLO35)
        {
            easting += 500000;
        }
        easting -= mapOrigin.OriginEasting;
        northing -= mapOrigin.OriginNorthing;

        float x = (float)easting;
        float z = (float)northing;

        if (targetEnv == ROSTargetEnvironment.AUTOWARE)
            return new Vector3(x, 0, z);
        return Quaternion.Euler(0f, -mapOrigin.Angle, 0f) * new Vector3(x, 0, z);
    }

    private void InitScene()
    {
        if (ROSAgentManager.Instance.currentMode == StartModeTypes.Dev) return;

        AgentListCanvas = Instantiate(UserInterfaceAgentList);
        AgentList = AgentListCanvas.transform.FindDeepChild("Content"); // TODO needs to change !!! asap

        float height = 0;
        if (ROSAgentManager.Instance.activeAgents.Count > 1)
        {
            height = AgentListCanvas.transform.FindDeepChild("AgentList").GetComponent<RectTransform>().rect.height; // TODO needs to change !!! asap
        }
        else
        {
            AgentListCanvas.enabled = false;
        }

        // TODO: update spawn position from static config
        Vector3 defaultSpawnPosition = new Vector3(1.0f, 0.018f, 0.7f);
        Quaternion defaultSpawnRotation = Quaternion.identity;

        var spawnInfos = FindObjectsOfType<SpawnInfo>();
        var spawnInfoList = spawnInfos.ToList();
        spawnInfoList.Reverse();

        RosBridgeConnector first = null;

        for (int i = 0; i < ROSAgentManager.Instance.activeAgents.Count; i++)
        {
            var connector = ROSAgentManager.Instance.activeAgents[i];

            var agentSetup = connector.agentType;
            var spawnPos = defaultSpawnPosition;
            var spawnRot = defaultSpawnRotation;
            if (spawnInfoList.Count > 0)
            {
                spawnPos = spawnInfoList[spawnInfoList.Count - 1].transform.position;
                spawnRot = spawnInfoList[spawnInfoList.Count - 1].transform.rotation;
                spawnInfoList.RemoveAt(spawnInfoList.Count - 1);
            }
            spawnPos -= new Vector3(0.25f * i, 0, 0);

            if (FindObjectOfType<StaticConfigManager>() != null
                && StaticConfigManager.Instance.staticConfig.initialized
                && ROSAgentManager.Instance.currentMode == StartModeTypes.StaticConfig)
            {
                var staticConfig = StaticConfigManager.Instance.staticConfig.vehicles[i];

                var pos = staticConfig.position;
                if (pos.e != 0.0f || pos.n != 0.0f)
                {
                    spawnPos = GetPosition(connector.agentType.TargetRosEnv, pos.e, pos.n);
                    spawnPos.y = pos.h;
                    var rot = staticConfig.orientation;
                    spawnRot = Quaternion.Euler(rot.r, rot.y, rot.p);
                }
                SpawnVehicle(spawnPos, spawnRot, connector, staticConfig, height);
            }
            else
            {
                SpawnVehicle(spawnPos, spawnRot, connector, null, height);
            }

            if (first == null)
            {
                first = connector;
            }
        }

        if (first != null)
        {
            first.Agent.GetComponent<AgentSetup>().FollowCamera.gameObject.SetActive(true);
            UserInterfaceSetup.ChangeFocusUI(first);
            SteeringWheelInputController.ChangeFocusSteerWheel(first.Agent.GetComponentInChildren<SteeringWheelInputController>());
            ROSAgentManager.Instance?.SetCurrentActiveAgent(first);
        }

        InitGlobalShadowSettings(); // TODO better way for small maps
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
