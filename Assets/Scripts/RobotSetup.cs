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
    public InputController InputCtrl;
    public List<Camera> Cameras;
    public Camera TelephotoCam;
    private Vector2 TelephotoCamOriginRes = new Vector2(640, 480);
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

        if (InputCtrl != null)
        {
            ui.SideCameras.onValueChanged.AddListener(InputCtrl.SideCamToggleValueChanged);
        }

        if (TelephotoCam != null)
        {
            ui.TelephotoCamera.onValueChanged.AddListener(enabled =>
            {
                TelephotoCam.enabled = enabled;
                TelephotoCam.GetComponent<VideoToROS>().enabled = enabled;
            });

            ui.TelephotoCameraHD.onValueChanged.AddListener(enabled =>
            {
                if (enabled)
                {
                    TelephotoCam.GetComponent<VideoToROS>().SwitchResolution(1920, 1080); //HD
                }
                else
                {
                    TelephotoCam.GetComponent<VideoToROS>().SwitchResolution(Mathf.RoundToInt(TelephotoCamOriginRes.x), Mathf.RoundToInt(TelephotoCamOriginRes.y));
                }
            });

            TelephotoCamOriginRes = new Vector2(TelephotoCam.targetTexture.width, TelephotoCam.targetTexture.height);
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
        ui.CameraPreview.renderCamera = Cameras[0].GetComponent<Camera>();
        ui.PositionReset.RobotController = CarController;
        ui.MainCameraToggle.Camera = Cameras[0].GetComponent<Camera>();

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
                    c.GetComponent<VideoToROS>().ValueChangeCallback(int.Parse(value));
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

        foreach (var item in NeedsBridge)
        {
            if (item == null)
            {
                continue;
            }
            var a = item as Ros.IRosClient;
            a.OnRosBridgeAvailable(bridge);
        }
        //NeedsBridge.ForEach(b => 
        //{
        //    (b as Ros.IRosClient).OnRosBridgeAvailable(bridge);
        //});
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
