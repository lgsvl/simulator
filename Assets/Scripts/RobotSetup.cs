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

    public RobotController CarController;
    public Camera MainCam;
    public List<Camera> SideCams;
    public Camera TelephotoCam;
    public ImuSensor imuSensor;
    public LidarSensor LidarSensor;
    public RadarSensor RidarSensor;
    public GpsDevice GpsDevice;
    public Camera FollowCamera;

    public List<Component> NeedsBridge;

    public void Setup(UserInterfaceSetup ui, RosBridgeConnector connector)
    {
        var bridge = connector.Bridge;
        ui.WheelScale.onValueChanged.AddListener(value =>
        {
            try
            {
                CarController.SetWheelScale(float.Parse(value));
            }
            catch (System.Exception)
            {
                Debug.Log("ROS Wheel Force Scaler: Please input valid number!");
            }
        });

        if (MainCam != null)
        {
            ui.CameraPreview.renderCamera = ui.CameraPreview.renderCamera == null ? MainCam : ui.CameraPreview.renderCamera;
            MainCam.GetComponent<VideoToROS>().Init();
            ui.MainCameraToggle.onValueChanged.AddListener(enabled =>
            {
                MainCam.enabled = enabled;
                MainCam.GetComponent<VideoToROS>().enabled = enabled;
                ui.CameraPreview.gameObject.SetActive(enabled);
            });
            MainCam.GetComponent<VideoToROS>().Init();
        }

        SideCams.ForEach(cam =>
        {
            cam.GetComponent<VideoToROS>().Init();
            ui.SideCameraToggle.onValueChanged.AddListener(enabled =>
            {
                cam.enabled = enabled;
                cam.GetComponent<VideoToROS>().enabled = enabled;
            });
            cam.GetComponent<VideoToROS>().Init();
        });

        if (TelephotoCam != null)
        {
            TelephotoCam.GetComponent<VideoToROS>().Init();
            ui.TelephotoCamera.onValueChanged.AddListener(enabled =>
            {
                TelephotoCam.enabled = enabled;
                TelephotoCam.GetComponent<VideoToROS>().enabled = enabled;
            });
        }

        if (imuSensor != null)
        {
            ui.Imu.onValueChanged.AddListener(imuSensor.Enable);
        }

        if (LidarSensor != null)
        {
            ui.Lidar.onValueChanged.AddListener(LidarSensor.Enable);
        }

        if (RidarSensor != null)
        {
            ui.Radar.onValueChanged.AddListener(RidarSensor.Enable);
        }

        ui.Gps.onValueChanged.AddListener(enabled => GpsDevice.PublishMessage = enabled);
        ui.PositionReset.RobotController = CarController;

        var Cameras = new List<Camera>();
        Cameras.Add(MainCam);
        Cameras.Add(TelephotoCam);
        Cameras.AddRange(SideCams);

        ui.HDToggle.onValueChanged.AddListener(enabled =>
        {
            ui.CameraPreview.renderTexture = null;
            if (enabled)
            {
                Cameras.ForEach(cam =>
                {
                    if (cam != null)
                    {
                        cam.GetComponent<VideoToROS>().SwitchResolution(1920, 1080); //HD
                    }                    
                });                
            }
            else
            {
                Cameras.ForEach(cam =>
                {
                    if (cam != null)
                    {
                        cam.GetComponent<VideoToROS>().SwitchResolution();
                    }
                });
            }
        });

        Cameras.ForEach(c =>
        {
            var pp = c.GetComponent<PostProcessingListener>();
            if (pp != null)
            {
                ui.CameraSaturation.onValueChanged.AddListener(x =>
                {
                    pp.SetSaturationValue(x);
                });
            }

            ui.CameraFramerate.onValueChanged.AddListener(value =>
            {
                try
                {
                    c.GetComponent<VideoToROS>().FPSChangeCallback(int.Parse(value));
                }
                catch (System.Exception)
                {
                    Debug.Log("Duckiebot Cam FPS: Please input valid number!");
                }
            });

            var ppb = c.GetComponent<PostProcessingBehaviour>();
            if (ppb != null)
            {
                ui.HighQualityRendering.onValueChanged.AddListener(enabled => ppb.enabled = enabled);
            }
        });

        ui.HighQualityRendering.onValueChanged.AddListener(enabled => FollowCamera.GetComponent<PostProcessingBehaviour>().enabled = enabled);

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
}
