/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public enum DashStateTypes
{
    Ignition,
    Wiper,
    Lights,
    ParkingBrake,
    Shift
};

public class DashStateMissive : Missive
{
    public DashStateTypes type = DashStateTypes.Ignition;
    public int state = 0;
}

public class DashUIManager : MonoBehaviour
{
    #region Singleton
    private static DashUIManager _instance = null;
    public static DashUIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<DashUIManager>();
                if (_instance == null)
                    Debug.LogError("<color=red>DashUIManager Not Found!</color>");
            }
            return _instance;
        }
    }
    #endregion

    #region vars
    [Header("Dash Settings", order = 0)]
    public GameObject dashUI;
    public Color initColor;
    public Color enabledColor;
    public Color disabledColor;

    public Image ignitionImage;
    public Image wiperImage;
    public Image lightsImage;
    public Image parkingBrakeImage;
    public Image shiftImage;

    public Sprite[] wiperStateSprites;
    public Sprite[] lightsStateSprites;
    public Sprite[] shiftStateSprites;

    [Space(5, order = 0)]
    [Header("Menu", order = 1)]
    public Color activePanelColor;
    public GameObject settingsUIPanel;

    public GameObject[] settingsUIPanels;
    public Image[] settingsUIButtonImages;
    public int currentSettingsUIPanelIndex = 0;

    [Space(5, order = 0)]
    [Header("Agent Settings", order = 1)]
    public Color activeAgentUIColor;
    public Color inactiveAgentUIColor;
    public Sprite defaultAgentUISprite;
    public GameObject agentUIPrefab;
    public Transform agentUIButtonHolder;
    public Text agentAddress;
    public Text agentConnectorData;

    // CES
    public DashUIComponent dash;

    #endregion

    #region mono
    private void Awake()
    {
        if (_instance == null)
            _instance = this;

        if (_instance != this)
            DestroyImmediate(gameObject);
    }

    private void Start()
    {
        InitSettingsUI();

        // TODO not working need better way
        //yield return new WaitUntil(() => FindObjectOfType<ROSAgentManager>() != null);
        //yield return new WaitUntil(() => ROSAgentManager.Instance.activeAgents != null);
        //if (ROSAgentManager.Instance.GetCurrentActiveAgent().name.Contains("duckiebot"))
        //{
        //    ToggleUI(false);
        //}
    }

    private void OnEnable()
    {
        Missive.AddListener<DashStateMissive>(OnDashStateChange);
        dash = FindObjectOfType<DashUIComponent>();
    }

    private void OnDisable()
    {
        Missive.RemoveListener<DashStateMissive>(OnDashStateChange);
        dash = null;
    }

    private void OnApplicationQuit()
    {
        _instance = null;
        DestroyImmediate(gameObject);
    }
    #endregion

    #region toggles
    public void ToggleUI(bool state)
    {
        dashUI.SetActive(state);
    }

    private void ToggleIgnitionUI(int state)
    {
        ignitionImage.color = Convert.ToBoolean(state) ? enabledColor : initColor;
        dash?.SetDashIgnitionUI(ignitionImage.color);
    }

    private void ToggleWiperUI(int state)
    {
        wiperImage.color = state == 0 ? initColor : enabledColor;
        switch (state)
        {
            case 0:
                wiperImage.sprite = wiperStateSprites[0];
                break;
            case 1:
                wiperImage.sprite = wiperStateSprites[1];
                break;
            case 2:
                wiperImage.sprite = wiperStateSprites[2];
                break;
            case 3:
                wiperImage.sprite = wiperStateSprites[3];
                break;
            default:
                Debug.Log("Wiper state out of range!!!");
                break;
        }
        dash?.SetDashWiperUI(wiperImage.color, wiperImage.sprite);
    }

    private void ToggleLightsUI(int state)
    {
        lightsImage.color = state == 0 ? initColor : enabledColor;
        switch (state)
        {
            case 0:
                lightsImage.sprite = lightsStateSprites[0];
                break;
            case 1:
                lightsImage.sprite = lightsStateSprites[0];
                break;
            case 2:
                lightsImage.sprite = lightsStateSprites[1];
                break;
            default:
                Debug.Log("Lights state out of range!!!");
                break;
        }
        dash?.SetDashLightsUI(lightsImage.color, lightsImage.sprite);
    }

    private void ToggleParkingBrakeUI(int state)
    {
        parkingBrakeImage.color = Convert.ToBoolean(state) ? enabledColor : initColor;
        dash?.SetDashParkingBrakeUI(parkingBrakeImage.color);
    }

    private void ToggleShiftUI(int state)
    {
        shiftImage.color = Convert.ToBoolean(state) ? enabledColor : disabledColor;
        shiftImage.sprite = Convert.ToBoolean(state) ? shiftStateSprites[0] : shiftStateSprites[1];
        dash?.SetDashShiftUI(shiftImage.color, shiftImage.sprite);
    }
    #endregion

    #region dash buttons
    public void IgnitionOnClick()
    {
        ROSAgentManager.Instance?.GetCurrentActiveAgent()?.GetComponent<VehicleController>()?.ToggleIgnition();
        EventSystem.current.SetSelectedGameObject(null);
    }

    public void WiperOnClick()
    {
        ROSAgentManager.Instance?.GetCurrentActiveAgent()?.GetComponent<VehicleController>()?.IncrementWiperState();
        EventSystem.current.SetSelectedGameObject(null);
    }

    public void LightsOnClick()
    {
        ROSAgentManager.Instance?.GetCurrentActiveAgent()?.GetComponent<VehicleController>()?.ChangeHeadlightMode();
        EventSystem.current.SetSelectedGameObject(null);
    }

    public void ParkingBrakeOnClick()
    {
        ROSAgentManager.Instance?.GetCurrentActiveAgent()?.GetComponent<VehicleController>()?.ToggleHandBrake();
        EventSystem.current.SetSelectedGameObject(null);
    }

    public void ShiftOnClick()
    {
        ROSAgentManager.Instance?.GetCurrentActiveAgent()?.GetComponent<VehicleController>()?.ToggleShift();
        EventSystem.current.SetSelectedGameObject(null);
    }
    #endregion

    #region settings
    public void ToggleSettingsUI()
    {
        settingsUIPanel.SetActive(!settingsUIPanel.activeInHierarchy);
    }

    public void ToggleSettingsUI(bool state)
    {
        settingsUIPanel.SetActive(state);
    }

    public void InitSettingsUI()
    {
        ToggleSettingsUI(false);
        currentSettingsUIPanelIndex = 0;
        for (int i = 0; i < settingsUIButtonImages.Length; i++)
        {
            settingsUIButtonImages[i].color = initColor;
        }
        for (int i = 0; i < settingsUIPanels.Length; i++)
        {
            settingsUIPanels[i].SetActive(false);
        }
        settingsUIButtonImages[currentSettingsUIPanelIndex].color = activePanelColor;
        settingsUIPanels[currentSettingsUIPanelIndex].SetActive(true);
    }

    public void SettingsPanelOnClick(int index)
    {
        settingsUIButtonImages[currentSettingsUIPanelIndex].color = initColor;
        settingsUIPanels[currentSettingsUIPanelIndex].SetActive(false);
        currentSettingsUIPanelIndex = index;
        settingsUIButtonImages[currentSettingsUIPanelIndex].color = activePanelColor;
        settingsUIPanels[currentSettingsUIPanelIndex].SetActive(true);
    }
    #endregion

    // TODO what is this and why is it in DashUI? remove?
    //#region settings robots
    //public void InitRobotSettings(AgentSetup setup, RosBridgeConnector connector)
    //{
    //    if (setup == null) return;

    //    GameObject go = Instantiate(robotUIPrefab, robotUIButtonHolder);
    //    go.transform.GetChild(0).GetComponent<Image>().sprite = setup.agentUISprite ?? defaultRobotUISprite;
    //    robotAddress.text = connector != null ? connector.PrettyAddress : "ROSBridgeConnector Missing!";
    //    robotConnectorData.text = GetRobotConnectorData(connector);
    //    Button button = go.GetComponent<Button>();
    //    ColorBlock tempCB = button.colors;
    //    tempCB.normalColor = inactiveRobotUIColor;
    //}

    //public void SetRobotSettings(AgentSetup setup, RosBridgeConnector connector)
    //{
    //    if (setup == null) return;

    //    robotAddress.text = connector != null ? connector.PrettyAddress : "ROSBridgeConnector Missing!";
    //    robotConnectorData.text = GetRobotConnectorData(connector);
    //    //Button button = go.GetComponent<Button>();
    //    //ColorBlock tempCB = button.colors;
    //    //tempCB.normalColor = inactiveRobotUIColor;
    //}

    //private string GetRobotConnectorData(RosBridgeConnector connector)
    //{
    //    if (SimulatorManager.Instance == null) return "SimulatorManager Missing!";
    //    if (connector == null) return "ROSBridgeConnector Missing!";

    //    StringBuilder sb = new StringBuilder();
    //    sb.Append("\nAvailable ROS Bridges:\n\n");
    //    if (SimulatorManager.Instance.IsFoci())
    //    {
    //        if (connector.Bridge.Status == Ros.Status.Connected)
    //        {
    //            sb.AppendLine($"{connector.PrettyAddress}");
    //            foreach (var topic in connector.Bridge.TopicPublishers)
    //            {
    //                sb.AppendLine($"PUB: {topic.Name} ({topic.Type})");
    //            }
    //            foreach (var topic in connector.Bridge.TopicSubscriptions)
    //            {
    //                sb.AppendLine($"SUB: {topic.Name} ({topic.Type})");
    //            }
    //        }
    //        else
    //        {
    //            sb.AppendLine($"{connector.PrettyAddress} ({connector.Bridge.Status})");
    //        }
    //    }


    //    return sb.ToString();
    //}
    //#endregion

    #region settings camera

    #endregion

    #region settings sensors

    #endregion

    #region settings environment

    #endregion

    #region settings help

    #endregion

    #region missives
    private void OnDashStateChange(DashStateMissive missive)
    {
        switch (missive.type)
        {
            case DashStateTypes.Ignition:
                ToggleIgnitionUI(missive.state);
                break;
            case DashStateTypes.Wiper:
                ToggleWiperUI(missive.state);
                break;
            case DashStateTypes.Lights:
                ToggleLightsUI(missive.state);
                break;
            case DashStateTypes.ParkingBrake:
                ToggleParkingBrakeUI(missive.state);
                break;
            case DashStateTypes.Shift:
                ToggleShiftUI(missive.state);
                break;
            default:
                Debug.Log("Dash state out of range");
                break;
        }
    }
    #endregion
}
