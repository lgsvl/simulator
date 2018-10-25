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
    public Camera ColorSegmentCam;
    public ImuSensor imuSensor;
    public LidarSensor LidarSensor;
    public RadarSensor RidarSensor;
    public GpsDevice GpsDevice;
    public Camera FollowCamera;

    public DepthCameraEnabler DepthCameraEnabler;

    public List<Component> NeedsBridge;

    public RosBridgeConnector Connector { get; private set; }

    public virtual void Setup(UserInterfaceSetup ui, RosBridgeConnector connector)
    {
        Connector = connector;
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

        if (FollowCamera != null)
        {
            ui.SensorEffectsToggle.onValueChanged.AddListener(on =>
            {
                if (on)
                {
                    FollowCamera.cullingMask = MainCam.cullingMask | 1 << LayerMask.NameToLayer("Sensor Effects");
                }
                else
                {
                    FollowCamera.cullingMask = MainCam.cullingMask & ~(1 << LayerMask.NameToLayer("Sensor Effects"));
                }
            });
        }

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
        }

        SideCams.ForEach(cam =>
        {
            cam.GetComponent<VideoToROS>().Init();
            ui.SideCameraToggle.onValueChanged.AddListener(enabled =>
            {
                cam.enabled = enabled;
                cam.GetComponent<VideoToROS>().enabled = enabled;
            });
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

        if (ColorSegmentCam != null)
        {
            var segmentColorer = FindObjectOfType<SegmentColorer>();
            if (segmentColorer != null)
            {
                segmentColorer.ApplyToCamera(ColorSegmentCam);

                ColorSegmentCam.GetComponent<VideoToROS>().Init();
                ui.ColorSegmentPreview.renderCamera = ColorSegmentCam;
                ui.ColorSegmentCamera.onValueChanged.AddListener(enabled =>
                {
                    ColorSegmentCam.enabled = enabled;
                    ColorSegmentCam.GetComponent<VideoToROS>().enabled = enabled;
                    ui.ColorSegmentPreview.gameObject.SetActive(enabled);
                });
            }
        }

        if (DepthCameraEnabler != null)
        {
            DepthCameraEnabler.TextureDisplay = ui.ColorSegmentPreview;
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
        if (MainCam != null)
        {
            Cameras.Add(MainCam);
        }
        if (TelephotoCam != null)
        {
            Cameras.Add(TelephotoCam);
        }
        if (SideCams != null)
        {
            Cameras.AddRange(SideCams);
        }

        ui.HDToggle.onValueChanged.AddListener(enabled =>
        {
            ui.CameraPreview.renderTexture = null;
            if (enabled)
            {
                Cameras.ForEach(cam =>
                {
                    if (cam != null)
                    {
                        var vs = cam.GetComponent<VideoToROS>();
                        vs.SwitchResolution(1920, 1080); //HD
                        ui.CameraPreview.SwitchResolution(vs.videoResolution.Item1, vs.videoResolution.Item2);
                    }
                });
            }
            else
            {
                Cameras.ForEach(cam =>
                {
                    if (cam != null)
                    {
                        var vs = cam.GetComponent<VideoToROS>();
                        vs.SwitchResolution();
                        ui.CameraPreview.SwitchResolution(vs.videoResolution.Item1, vs.videoResolution.Item2);
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
