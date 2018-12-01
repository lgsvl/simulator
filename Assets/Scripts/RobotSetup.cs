/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;
using UnityEngine.PostProcessing;
using System.Collections.Generic;

public class RobotSetup : MonoBehaviour
{
    public ROSTargetEnvironment TargetRosEnv;
    public Sprite robotUISprite;
    public RobotController CarController;

    public Camera FollowCamera;

    [HideInInspector]
    public Camera MainCam;

    public GroundTruthSensor GroundTruthSensor;

    public CameraSettingsManager CameraMan;

    public List<Component> NeedsBridge;

    public RosBridgeConnector Connector { get; private set; }
    public UserInterfaceSetup UI { get; private set; }

    public UserInterfaceTweakables Tweakables;

    public virtual void Setup(UserInterfaceSetup ui, RosBridgeConnector connector)
    {
        Connector = connector;
        UI = ui;
        var bridge = connector.Bridge;
        ui.PositionReset.RobotController = CarController;

        if (GroundTruthSensor != null)
        {
            ui.GroundTruthToggle.onValueChanged.AddListener(GroundTruthSensor.Enable);
        }

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
        
        ui.HighQualityRendering.onValueChanged.AddListener(enabled =>
        {
            FollowCamera.GetComponent<PostProcessingBehaviour>().enabled = enabled;
            CameraMan?.SetHighQualityRendering(enabled);
        });

        ui.TrafficToggle.onValueChanged.AddListener(enabled =>
        {
            if (enabled)
            {
                FindObjectOfType<TrafSpawner>()?.ReSpawnTrafficCars();
            }
            else
            {
                FindObjectOfType<TrafSpawner>()?.KillTrafficCars();
            }

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

        ui.PedestriansToggle.onValueChanged.AddListener(enabled =>
        {
            if (enabled)
            {
                FindObjectOfType<PedestrianManager>()?.SpawnNPCPedestrians();
            }
            else
            {
                FindObjectOfType<PedestrianManager>()?.KillNPCPedestrians();
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
    }

    public void DevUICleanup(UserInterfaceSetup ui)
    {
        ui?.RemoveTweakables(Tweakables);
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
}
