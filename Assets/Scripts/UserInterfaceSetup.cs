/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UserInterfaceSetup : MonoBehaviour
{
    public static List<UserInterfaceSetup> Instances { get; private set; }
    public static UserInterfaceSetup FocusUI { get; private set; } //a flag to remember which UI is in focus
    public static StaticConfig staticConfig;

    public RectTransform MainPanel;
    public GameObject cameraViewScrollView;
    public RectTransform CameraPreviewPanel;
    public Text BridgeStatus;
    public Toggle SensorEffectsToggle;
    public Toggle TrafficToggle;
    public Toggle PedestriansToggle;
    public Toggle SteerwheelFeedback;
    public RenderTextureDisplayer CameraPreview;
    public RenderTextureDisplayer ColorSegmentPreview;
    public DuckiebotPositionResetter PositionReset;
    public Toggle HighQualityRendering;
    public Text errorContent;
    public GameObject exitScreen;

    public GameObject[] obstacleVehicles;
    public float obstacleDistance = 20f;
    private bool isInObstacleMode = false;
    private GameObject currentObstacle;

    private void Awake()
    {
        if (Instances == null)
        {
            Instances = new List<UserInterfaceSetup>();
            FocusUI = this;
        }
        Instances.Add(this);
    }

    public void CheckStaticConfigTraffic()
    {
        if (staticConfig.initialized)
        {
            TrafSpawner.Instance.spawnDensity = staticConfig.initial_configuration.traffic_density;
            TrafficToggle.isOn = staticConfig.initial_configuration.enable_traffic;
            PedestriansToggle.isOn = staticConfig.initial_configuration.enable_pedestrian;

            var weatherController = DayNightEventsController.Instance.weatherController;
            weatherController.rainIntensity = staticConfig.initial_configuration.rain_intensity;
            weatherController.fogIntensity = staticConfig.initial_configuration.fog_intensity;
            weatherController.roadWetness = staticConfig.initial_configuration.road_wetness;
            DayNightEventsController.Instance.currentHour = staticConfig.initial_configuration.time_of_day;
            DayNightEventsController.Instance.freezeTimeOfDay = staticConfig.initial_configuration.freeze_time_of_day;

            DayNightEventsController.Instance.RefreshControls();
        }
    }

    private void OnDestroy()
    {
        Instances.Remove(this);
    }

    private void Start()
    {
    }

    private void Update()
    {
        // if not focus UI, don't update
        if (this != FocusUI) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            exitScreen.SetActive(!exitScreen.activeInHierarchy);
        }

        if (Input.GetKeyDown(KeyCode.F5))
        {
            // save pos/rot
            SaveAutoPositionRotation();
        }

        if (Input.GetKeyDown(KeyCode.F9))
        {
            // load saved pos and rot and apply to controller transform
            LoadAutoPositionRotation();
        }

        if (Input.GetKeyDown(KeyCode.F10))
        {
            // move car in front of user vehicle
            ToggleNPCObstacleToUser();
        }

        ToggleCameraViewScrollView();

        CheckStateErrors();
    }

    private void ToggleCameraViewScrollView()
    {
        bool isAllCamerasOff = true;
        for (int i = 0; i < CameraPreviewPanel.childCount; i++)
        {
            if (CameraPreviewPanel.GetChild(i).gameObject.activeSelf)
            {
                isAllCamerasOff = false;
                break;
            }

        }
        cameraViewScrollView.gameObject.SetActive(!isAllCamerasOff);
    }

    private void CheckStateErrors()
    {
        errorContent.text = ""; //clear

        var steerwheels = FindObjectsOfType<SteeringWheelInputController>();
        foreach (var steerwheel in steerwheels)
        {
            if (steerwheel.stateFail != "")
            {
                errorContent.text += $"{steerwheel.stateFail}\n";
            }
        }
    }

    public void AddTweakables(UserInterfaceTweakables tweakables)
    {
        int childCount = MainPanel.childCount;
        foreach (var UIElement in tweakables.Elements)
        {
            UIElement.transform.SetParent(MainPanel);
            UIElement.transform.SetSiblingIndex(childCount-1);
            UIElement.transform.localScale = Vector3.one;
        }
        foreach(var UIElement in tweakables.CameraElements)
        {
            UIElement.transform.SetParent(CameraPreviewPanel);
            UIElement.transform.localScale = Vector3.one;
        }
    }

    public void ToggleTweakable(UserInterfaceTweakables tweakables, string type, bool state)
    {
        foreach (var UIElement in tweakables.Elements)
        {
            if (UIElement.name.Contains(type))
            {
                Toggle toggle = UIElement.GetComponent<Toggle>();
                if (toggle != null)
                    toggle.isOn = state;
            }
        }
        
    }

    public void RemoveTweakables(UserInterfaceTweakables tweakables)
    {
        foreach (var UIElement in tweakables.Elements)
        {
            Destroy(UIElement.gameObject);
        }
        foreach (var UIElement in tweakables.CameraElements)
        {
            Destroy(UIElement.gameObject);
        }
    }

    public static void ChangeFocusUI(RosBridgeConnector connector, RosRobots robots)
    {
        for (int k = 0; k < robots.Robots.Count; k++)
        {
            var robotConnector = robots.Robots[k];
            bool isFocus = robotConnector == connector;
            robotConnector.UiObject.enabled = isFocus;
            var b = robotConnector.UiButton.GetComponent<Button>();
            var c = b.colors;
            c.normalColor = isFocus ? new Color(1, 1, 1) : new Color(0.8f, 0.8f, 0.8f);
            b.colors = c;
            var robotSetup = robotConnector.Robot.GetComponent<RobotSetup>();
            robotSetup.FollowCamera.gameObject.SetActive(isFocus);
            robotSetup.FollowCamera.enabled = isFocus;
            var inputControllers = robotConnector.Robot.GetComponentsInChildren<IInputController>().ToList();
            if (isFocus)
            {
                FocusUI = robotSetup.UI;
                inputControllers.ForEach(i => i.Enable());

                // TODO move to gameobject based
                SimulatorManager.Instance?.SetCurrentActiveRobot(robotSetup.gameObject);
            }
            else                
            {
                inputControllers.ForEach(i => i.Disable());
            }
        }

        VehicleList.Instances?.ForEach(x => x.ToggleDisplay(FocusUI.MainPanel.gameObject.activeSelf)); //hack
    }

    #region save pos/rot
    public void SaveAutoPositionRotation()
    {
        if (PositionReset.RobotController == null)
        {
            Debug.LogError("Missing PositionReset RobotController!");
            return;
        }

        PlayerPrefs.SetString("AUTO_POSITION", PositionReset.RobotController.transform.position.ToString());
        PlayerPrefs.SetString("AUTO_ROTATION", PositionReset.RobotController.transform.rotation.eulerAngles.ToString());
    }

    public void LoadAutoPositionRotation()
    {
        if (PositionReset.RobotController == null)
        {
            Debug.LogError("Missing PositionReset RobotController!");
            return;
        }
        // calls method passing pos and rot saved instead of init position and rotation. Init pos and rot are still used on reset button in UI
        Vector3 tempPos = StringToVector3(PlayerPrefs.GetString("AUTO_POSITION", Vector3.zero.ToString()));
        Quaternion tempRot = Quaternion.Euler(StringToVector3(PlayerPrefs.GetString("AUTO_ROTATION", Vector3.zero.ToString())));
        PositionReset.RobotController.ResetSavedPosition(tempPos, tempRot);
    }
    #endregion

    #region obstacle
    public void ToggleNPCObstacleToUser()
    {
        if (PositionReset.RobotController == null)
        {
            Debug.Log("Error returning PositionReset.RobotController!");
            return;
        }

        // static obstacle NPC
        if (obstacleVehicles.Length == 0)
        {
            Debug.Log("No obstacle vehicles in pool!");
            return;
        }

        isInObstacleMode = !isInObstacleMode;
        if (isInObstacleMode)
        {
            Vector3 spawnPos = PositionReset.RobotController.transform.position + PositionReset.RobotController.transform.forward * obstacleDistance;
            currentObstacle = Instantiate(obstacleVehicles[(int)Random.Range(0, obstacleVehicles.Length)], spawnPos, PositionReset.RobotController.transform.rotation);
        }
        else
        {
            if (currentObstacle != null)
                Destroy(currentObstacle);
        }
    }
    #endregion

    #region utilities
    protected Vector3 StringToVector3(string str)
    {
        Vector3 tempVector3 = Vector3.zero;

        if (str.StartsWith("(") && str.EndsWith(")"))
            str = str.Substring(1, str.Length - 2);

        // split the items
        string[] sArray = str.Split(',');

        // store as a Vector3
        if (!string.IsNullOrEmpty(str))
            tempVector3 = new Vector3(float.Parse(sArray[0]), float.Parse(sArray[1]), float.Parse(sArray[2]));

        return tempVector3;
    }
    #endregion
}
