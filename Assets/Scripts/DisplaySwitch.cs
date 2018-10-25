/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DisplaySwitch : MonoBehaviour
{
    public List<GameObject> gameObjects;
    [SerializeField]
    private KeyCode switchKeyCode = KeyCode.Space;

    public RectTransform MainPanel;
    public RenderTextureDisplayer CameraPreview;
    public RenderTextureDisplayer ColorSegmentPreview;
    public Toggle MainCameraToggle;
    public Toggle ColorSegmentCameraToggle;

    protected virtual void Update ()
    {
        if (Input.GetKeyDown(switchKeyCode))
        {
            Switch();
        }
    }

    public void Switch()
    {
        foreach (var go in gameObjects)
        {
            go.SetActive(!go.activeSelf);
        }

        if (MainCameraToggle.isOn)
        {
            CameraPreview.gameObject.SetActive(!CameraPreview.gameObject.activeSelf);
        }
        else
        {
            CameraPreview.gameObject.SetActive(false);
        }

        if (ColorSegmentCameraToggle.isOn)
        {
            ColorSegmentPreview.gameObject.SetActive(!ColorSegmentPreview.gameObject.activeSelf);
        }
        else
        {
            ColorSegmentPreview.gameObject.SetActive(false);
        }
    }
}
