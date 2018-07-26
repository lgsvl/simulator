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
    private AutoPlatform Platform;

    public RobotController CarController;
    public InputController SideCameras;
    public List<Camera> Cameras;
    public LidarSensor LidarSensor;
    public GpsDevice GpsDevice;
    public Camera FollowCamera;

    public List<Component> NeedsBridge;

    public void Setup(UserInterfaceSetup ui, RosBridgeConnector connector)
    {
        var bridge = connector.Bridge;
        Platform = connector.Platform; //
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

        if (SideCameras)
        {
            ui.SideCameras.onValueChanged.AddListener(SideCameras.SideCamToggleValueChanged);
        }

        ui.Lidar.onValueChanged.AddListener(LidarSensor.Enable);
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

        NeedsBridge.ForEach(b => (b as Ros.IRosClient).OnRosBridgeAvailable(bridge));
    }
}
