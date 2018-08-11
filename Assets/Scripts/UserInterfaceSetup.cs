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
    public InputField WheelScale;
    public Toggle SideCameras;
    public InputField CameraFramerate;
    public Scrollbar CameraSaturation;
    public Text BridgeStatus;
    public Toggle Lidar;
    public Toggle Radar;
    public Toggle Gps;
    public RenderTextureDisplayer CameraPreview;
    public DuckiebotPositionResetter PositionReset;
    public ToggleMainCamera MainCameraToggle;
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
