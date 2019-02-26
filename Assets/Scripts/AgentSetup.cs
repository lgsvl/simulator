/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using UnityEngine.PostProcessing;
using System.Collections.Generic;

public class AgentSetup : MonoBehaviour
{
    //[System.NonSerialized]
    public string address = "localhost";
    //[System.NonSerialized]
    public int port = RosBridgeConnector.DefaultPort;

    public ROSTargetEnvironment TargetRosEnv;
    public Sprite agentUISprite;
    public AgentController agentController { get; set; }

    public Camera FollowCamera;

    [HideInInspector]
    public Camera MainCam;

    public CameraSettingsManager CameraMan;

    public List<Component> NeedsBridge;
    
    public RosBridgeConnector Connector { get; private set; }
    public UserInterfaceSetup UI { get; private set; }

    public UserInterfaceTweakables Tweakables;
    
    public Camera mainCamera { get; private set; }

    private void Awake()
    {
        agentController = GetComponent<AgentController>();
    }

    public virtual void Setup(UserInterfaceSetup ui, RosBridgeConnector connector, VehicleConfig config)
    {
        // needed for npc
        foreach (Transform child in transform)
        {
            if (child.CompareTag("MainCamera"))
                mainCamera = child.GetComponent<Camera>();
        }

        Connector = connector;
        UI = ui;
        var bridge = connector.Bridge;
        ui.agentController = agentController;

        ui.SensorEffectsToggle.onValueChanged.AddListener(enabled => ToggleSensorEffect(enabled));

        // ui.WheelScale.onValueChanged.AddListener(value =>
        // {
        //     try
        //     {
        //         CarController.SetWheelScale(float.Parse(value));
        //     }
        //     catch (System.Exception)
        //     {
        //         Debug.Log("ROS Wheel Force Scaler: Please input valid number!");
        //     }
        // });

        // Cameras.ForEach(c =>
        // {
        //     var pp = c.GetComponent<PostProcessingListener>();
        //     if (pp != null)
        //     {
        //         ui.CameraSaturation.onValueChanged.AddListener(x =>
        //         {
        //             pp.SetSaturationValue(x);
        //         });
        //     }
        // });

        ui.LowQualityRendering.onValueChanged.AddListener(useLowQuality =>
        {
            FollowCamera.GetComponent<PostProcessingBehaviour>().enabled = !useLowQuality;
            CameraMan?.SetHighQualityRendering(!useLowQuality);
        });
        ui.LowQualityRendering.onValueChanged.Invoke(ui.LowQualityRendering.isOn);

        ui.TrafficToggle.onValueChanged.AddListener(enabled =>
        {
            FindObjectOfType<NPCManager>()?.ToggleNPCS(enabled);

            //hack to sync toggle value among cars UIs
            {
                foreach (var otherUI in FindObjectsOfType<UserInterfaceSetup>())
                {
                    if (otherUI == ui)
                        continue;

                    var oldEvent = otherUI.TrafficToggle.onValueChanged;
                    otherUI.TrafficToggle.onValueChanged = new UnityEngine.UI.Toggle.ToggleEvent();
                    otherUI.TrafficToggle.isOn = enabled;
                    otherUI.TrafficToggle.onValueChanged = oldEvent;

                }
            }
        });

        ui.TrafficPhysicsModeToggle.onValueChanged.AddListener(enabled =>
        {
            FindObjectOfType<NPCManager>()?.ToggleNPCPhysicsMode(enabled);
            //hack to sync toggle value among cars UIs
            {
                foreach (var otherUI in FindObjectsOfType<UserInterfaceSetup>())
                {
                    if (otherUI == ui)
                        continue;

                    var oldEvent = otherUI.TrafficPhysicsModeToggle.onValueChanged;
                    otherUI.TrafficPhysicsModeToggle.onValueChanged = new UnityEngine.UI.Toggle.ToggleEvent();
                    otherUI.TrafficPhysicsModeToggle.isOn = enabled;
                    otherUI.TrafficPhysicsModeToggle.onValueChanged = oldEvent;
                }
            }
        });

        ui.PedestriansToggle.onValueChanged.AddListener(enabled =>
        {
            if (enabled)
            {
                FindObjectOfType<PedestrianManager>()?.SpawnPedestrians();
            }
            else
            {
                FindObjectOfType<PedestrianManager>()?.KillPedestrians();
            }

            //hack to sync toggle value among car UIs
            {
                foreach (var otherUI in FindObjectsOfType<UserInterfaceSetup>())
                {
                    if (otherUI == ui)
                        continue;

                    var oldEvent = otherUI.PedestriansToggle.onValueChanged;
                    otherUI.PedestriansToggle.onValueChanged = new UnityEngine.UI.Toggle.ToggleEvent();
                    otherUI.PedestriansToggle.isOn = enabled;
                    otherUI.PedestriansToggle.onValueChanged = oldEvent;
                }
            }
        });

        ui.SteerwheelFeedback.onValueChanged.AddListener(enabled =>
        {
            var steerwheels = FindObjectsOfType<SteeringWheelInputController>();
            foreach (var steerwheel in steerwheels)
            {
                steerwheel.useFeedback = enabled;
            }
        });

        foreach (var item in NeedsBridge)
        {
            if (item == null)
            {
                continue;
            }
            var a = item as Ros.IRosClient;
            a.OnRosBridgeAvailable(bridge);
        }

        ui.AddTweakables(Tweakables);

        if (config != null)
        {

            ui.ToggleTweakable(Tweakables, "Lidar", config.enable_lidar);
            ui.ToggleTweakable(Tweakables, "GPS", config.enable_gps);
            ui.ToggleTweakable(Tweakables, "IMU", config.enable_imu);
            ui.ToggleTweakable(Tweakables, "Main Camera", config.enable_main_camera);
            ui.ToggleTweakable(Tweakables, "Telephoto Camera", config.enable_telephoto_camera);

            ui.LowQualityRendering.isOn = !config.enable_high_quality_rendering;
            ui.SensorEffectsToggle.isOn = config.enable_sensor_effects;
        }

        if (config != null)
        {
            if (StaticConfigManager.Instance.staticConfig.initialized && ROSAgentManager.Instance.currentMode == StartModeTypes.StaticConfig)
                ui.CheckStaticConfigTraffic();
        }

        // CES
        CarInputController cc = GetComponent<CarInputController>();
        if (cc != null)
        {
            cc[InputEvent.TOGGLE_SENSOR_EFFECTS].Press += ToggleSensorEffect;
            cc[InputEvent.TOGGLE_TRAFFIC].Press += ToggleTraffic;
            cc[InputEvent.TOGGLE_UI].Press += ToggleUI;
        }
    }

    public void AddToNeedsBridge(Component comp)
    {
        if (Connector.Bridge == null)
        {
            Debug.Log("Bridge instance is not available, can not add to needs bridge");
            return;
        }
        NeedsBridge.Add(comp);
        var a = comp as Ros.IRosClient;
        a.OnRosBridgeAvailable(Connector.Bridge);
    }

    public int GetRosVersion()
    {
        int rosVersion = 1;
        switch (TargetRosEnv)
        {
            case ROSTargetEnvironment.APOLLO:
                rosVersion = 1;
                break;
            case ROSTargetEnvironment.AUTOWARE:
                rosVersion = 1;
                break;
            case ROSTargetEnvironment.DUCKIETOWN_ROS1:
                rosVersion = 1;
                break;
            case ROSTargetEnvironment.DUCKIETOWN_ROS2:
                rosVersion = 2;
                break;
            case ROSTargetEnvironment.LGSVL:
                rosVersion = 2;
                break;
            default:
                break;
        }
        return rosVersion;
    }

    private void ToggleSensorEffect(bool state)
    {
        if (FollowCamera != null && MainCam != null)
            FollowCamera.cullingMask = (state) ? MainCam.cullingMask | 1 << LayerMask.NameToLayer("Sensor Effects") : MainCam.cullingMask & ~(1 << LayerMask.NameToLayer("Sensor Effects"));

    }

    // CES
    private void ToggleSensorEffect()
    {
        UI.SensorEffectsToggle.isOn = !UI.SensorEffectsToggle.isOn;
    }

    // CES
    private void ToggleTraffic()
    {
        if (!UI.TrafficToggle.isOn)
            UI.TrafficToggle.isOn = true;
        else
        {
            FindObjectOfType<NPCManager>()?.ToggleNPCS(true);
        }
    }

    // CES
    private void ToggleUI()
    {
        UI?.GetComponent<DisplaySwitch>().Switch();
    }

    public void RemoveTweakables()
    {
        foreach (var UIElement in Tweakables.Elements)
        {
            Destroy(UIElement.gameObject);
        }
        foreach (var UIElement in Tweakables.CameraElements)
        {
            Destroy(UIElement.gameObject);
        }
    }

    private void OnDestroy()
    {
        // CES
        CarInputController cc = GetComponent<CarInputController>();
        if (cc != null)
        {
            cc[InputEvent.TOGGLE_SENSOR_EFFECTS].Press -= ToggleSensorEffect;
            cc[InputEvent.TOGGLE_TRAFFIC].Press -= ToggleTraffic;
            cc[InputEvent.TOGGLE_UI].Press -= ToggleUI;
        }
    }
}
