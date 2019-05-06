/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ConfigData
{
    public string Name;
    public string Status;
    public int Id;
    public int? Cluster { get; set; }
    public string Map { get; set; }
    public string[] Vehicles { get; set; }
    public bool? ApiOnly { get; set; }
    public bool? Interactive { get; set; }
    public bool? OffScreen { get; set; }
    public DateTime? TimeOfDay { get; set; }
    public float? Rain { get; set; }
    public float? Fog { get; set; }
    public float? Wetness { get; set; }
    public float? Cloudiness { get; set; }
    public string MapName { get; set; }
    public List<GameObject> Agents { get; set; }
}

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

    public ConfigData currentConfigData;
    
    public AgentManager agentManagerPrefab;
    public MapManager mapManagerPrefab;
    public NPCManager npcManagerPrefab;
    public PedestrianManager pedestrianManagerPrefab;
    public EnvironmentEffectsManager environmentEffectsManagerPrefab;
    public CameraManager cameraManagerPrefab;
    public UIManager uiManagerPrefab;

    public AgentManager agentManager { get; private set; }
    public MapManager mapManager { get; private set; }
    public NPCManager npcManager { get; private set; }
    public PedestrianManager pedestrianManager { get; private set; }
    public CameraManager cameraManager { get; private set; }
    public EnvironmentEffectsManager environmentEffectsManager { get; private set; }
    public UIManager uiManager { get; private set; }

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
    }

    private void Start()
    {
        InitializeManagers();
    }

    private void InitializeManagers()
    {
        cameraManager = Instantiate(cameraManagerPrefab, transform);
        agentManager = Instantiate(agentManagerPrefab, transform);
        mapManager = Instantiate(mapManagerPrefab, transform);
        npcManager = Instantiate(npcManagerPrefab, transform);
        pedestrianManager = Instantiate(pedestrianManagerPrefab, transform);
        environmentEffectsManager = Instantiate(environmentEffectsManagerPrefab, transform);
        uiManager = Instantiate(uiManagerPrefab, transform);
    }
    
    public void LoadData(ConfigData data)
    {
        currentConfigData = data;
    }

    public void QuitSimulator()
    {
        Debug.Log("Quit Simulator");
    }

    //public void SpawnVehicle(Vector3 position, Quaternion rotation, RosBridgeConnector connector, VehicleConfig staticConfig, float height = 0.0f)
    //{
    //    var agentImage = Instantiate(AgentUI, AgentList);
    //    agentImage.transform.FindDeepChild("Address").GetComponent<Text>().text = connector.PrettyAddress;
    //    var button = agentImage.GetComponent<Button>();
    //    button.onClick.AddListener(() =>
    //    {
    //        UserInterfaceSetup.ChangeFocusUI(connector);
    //        SteeringWheelInputController.ChangeFocusSteerWheel(connector.Agent.GetComponentInChildren<SteeringWheelInputController>());
    //    });

    //    var agentSetup = connector.agentType;

    //    GameObject bot = Instantiate(agentSetup == null ? ROSAgentManager.Instance.agentPrefabs[0].gameObject : agentSetup.gameObject, position, rotation); // TODO better system

    //    AnalyticsManager.Instance?.EgoStartEvent(agentSetup == null ? ROSAgentManager.Instance.agentPrefabs[0].gameObject.name : agentSetup.gameObject.name);

    //    var uiObject = Instantiate(UserInterfaceAgent);
    //    var ui = uiObject.transform;
    //    ui.GetComponent<UserInterfaceSetup>().agent = bot;

    //    if (bot.name.Contains("duckiebot"))
    //    {
    //        HelpScreenUpdate helpScreen = uiObject.GetComponent<HelpScreenUpdate>();
    //        helpScreen.Help = helpScreen.DuckieHelp;
    //        helpScreen.agentsText = helpScreen.duckieText;
    //    }

    //    // offset for multiple vehicle UI
    //    RectTransform rect = uiObject.GetComponent<UserInterfaceSetup>().MainPanel;
    //    if (rect != null)
    //    {
    //        rect.offsetMax = new Vector2(0, rect.offsetMax.y - height);
    //    }
    //    connector.UiObject = uiObject;
    //    connector.UiButton = agentImage;
    //    connector.BridgeStatus = uiObject.GetComponent<UserInterfaceSetup>().BridgeStatus;

    //    bot.GetComponent<AgentSetup>().Setup(ui.GetComponent<UserInterfaceSetup>(), connector, staticConfig);

    //    bot.GetComponent<AgentSetup>().FollowCamera.gameObject.SetActive(false);
    //    button.image.sprite = bot.GetComponent<AgentSetup>().agentUISprite;

    //    //uiObject.enabled = i == 0;
    //    var colors = button.colors;
    //    //colors.normalColor = i == 0 ? new Color(1, 1, 1) : new Color(0.8f, 0.8f, 0.8f);
    //    button.colors = colors;

    //    var name = new GameObject();
    //    name.transform.parent = AgentListCanvas.transform.FindDeepChild("Panel").transform;
    //    connector.UiName = name.AddComponent<Text>();
    //    connector.UiName.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
    //    connector.UiName.text = connector.PrettyAddress;
    //    connector.UiName.fontSize = 16;
    //    connector.UiName.fontStyle = FontStyle.Bold;
    //    connector.UiName.horizontalOverflow = HorizontalWrapMode.Overflow;
    //    connector.UiName.verticalOverflow = VerticalWrapMode.Overflow;

    //    connector.Agent = bot;
    //}

    //Vector3 GetPosition(ROSTargetEnvironment targetEnv, double easting, double northing)
    //{
    //    MapOrigin mapOrigin = GameObject.Find("/MapOrigin").GetComponent<MapOrigin>();

    //    if (targetEnv == ROSTargetEnvironment.APOLLO || targetEnv == ROSTargetEnvironment.APOLLO35)
    //    {
    //        easting += 500000;
    //    }
    //    easting -= mapOrigin.OriginEasting;
    //    northing -= mapOrigin.OriginNorthing;

    //    float x = (float)easting;
    //    float z = (float)northing;

    //    if (targetEnv == ROSTargetEnvironment.AUTOWARE)
    //        return new Vector3(x, 0, z);
    //    return Quaternion.Euler(0f, -mapOrigin.Angle, 0f) * new Vector3(x, 0, z);
    //}

    //private void InitScene()
    //{
    //    if (ROSAgentManager.Instance.currentMode == StartModeTypes.Dev) return;

    //    AgentListCanvas = Instantiate(UserInterfaceAgentList);
    //    AgentList = AgentListCanvas.transform.FindDeepChild("Content"); // TODO needs to change !!! asap

    //    float height = 0;
    //    if (ROSAgentManager.Instance.activeAgents.Count > 1)
    //    {
    //        height = AgentListCanvas.transform.FindDeepChild("AgentList").GetComponent<RectTransform>().rect.height; // TODO needs to change !!! asap
    //    }
    //    else
    //    {
    //        AgentListCanvas.enabled = false;
    //    }

    //    // TODO: update spawn position from static config
    //    Vector3 defaultSpawnPosition = new Vector3(1.0f, 0.018f, 0.7f);
    //    Quaternion defaultSpawnRotation = Quaternion.identity;

    //    var spawnInfos = FindObjectsOfType<SpawnInfo>();
    //    var spawnInfoList = spawnInfos.ToList();
    //    spawnInfoList.Reverse();

    //    RosBridgeConnector first = null;

    //    var map = GameObject.Find("MapOrigin").GetComponent<MapOrigin>();

    //    for (int i = 0; i < ROSAgentManager.Instance.activeAgents.Count; i++)
    //    {
    //        var connector = ROSAgentManager.Instance.activeAgents[i];

    //        var agentSetup = connector.agentType;
    //        var spawnPos = defaultSpawnPosition;
    //        var spawnRot = defaultSpawnRotation;
    //        if (spawnInfoList.Count > 0)
    //        {
    //            spawnPos = spawnInfoList[spawnInfoList.Count - 1].transform.position;
    //            spawnRot = spawnInfoList[spawnInfoList.Count - 1].transform.rotation;
    //            spawnInfoList.RemoveAt(spawnInfoList.Count - 1);
    //        }
    //        spawnPos -= new Vector3(0.25f * i, 0, 0);

    //        if (FindObjectOfType<StaticConfigManager>() != null
    //            && StaticConfigManager.Instance.staticConfig.initialized
    //            && ROSAgentManager.Instance.currentMode == StartModeTypes.StaticConfig)
    //        {
    //            var staticConfig = StaticConfigManager.Instance.staticConfig.vehicles[i];

    //            var pos = staticConfig.position;
    //            if (pos.e != 0.0f || pos.n != 0.0f)
    //            {
    //                var position = new Vector3(pos.e, 0, pos.n);

    //                if (connector.agentType.TargetRosEnv == ROSTargetEnvironment.AUTOWARE)
    //                {
    //                    // Autoware does not use origin from map
    //                    position.x += map.OriginEasting - 500000;
    //                    position.z += map.OriginNorthing;
    //                }

    //                spawnPos = map.FromNorthingEasting(position.z, position.x);

    //                if (connector.agentType.TargetRosEnv == ROSTargetEnvironment.AUTOWARE)
    //                {
    //                    // Autoware does not use angle from map
    //                    spawnPos = Quaternion.Euler(0f, map.Angle, 0f) * spawnPos;
    //                }

    //                spawnPos.y = pos.h;
    //                var rot = staticConfig.orientation;
    //                spawnRot = Quaternion.Euler(rot.r, rot.y, rot.p);
    //            }
    //            SpawnVehicle(spawnPos, spawnRot, connector, staticConfig, height);
    //        }
    //        else
    //        {
    //            SpawnVehicle(spawnPos, spawnRot, connector, null, height);
    //        }

    //        if (first == null)
    //        {
    //            first = connector;
    //        }
    //    }

    //    if (first != null)
    //    {
    //        first.Agent.GetComponent<AgentSetup>().FollowCamera.gameObject.SetActive(true);
    //        UserInterfaceSetup.ChangeFocusUI(first);
    //        SteeringWheelInputController.ChangeFocusSteerWheel(first.Agent.GetComponentInChildren<SteeringWheelInputController>());
    //        ROSAgentManager.Instance?.SetCurrentActiveAgent(first);
    //    }

    //    InitGlobalShadowSettings(); // TODO better way for small maps
    //}

}
