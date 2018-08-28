/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;
using UnityEngine.UI;

public class UserInterfaceSetup : MonoBehaviour
{
    public RectTransform MainPanel;
    public Text BridgeStatus;
    public InputField WheelScale;
    public InputField CameraFramerate;
    public Scrollbar CameraSaturation;
    public Toggle MainCameraToggle;
    public Toggle SideCameraToggle;
    public Toggle TelephotoCamera;
    public Toggle HDToggle;
    public Toggle Imu;
    public Toggle Lidar;
    public Toggle Radar;
    public Toggle Gps;
    public Toggle TrafficToggle;
    public RenderTextureDisplayer CameraPreview;
    public DuckiebotPositionResetter PositionReset;
    public Toggle HighQualityRendering;
    public GameObject exitScreen;    

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            exitScreen.SetActive(!exitScreen.activeInHierarchy);
        }
    }
}
