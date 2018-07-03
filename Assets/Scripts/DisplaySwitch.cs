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
    public KeyCode switchKeyCode = KeyCode.Space;

    public RectTransform MainPanel;
    public RenderTextureDisplayer RenderTexture;
    public Toggle MainCameraToggle;

    void Update ()
    {
        if (Input.GetKeyDown(switchKeyCode))
        {
            foreach (var go in gameObjects)
            {
                go.SetActive(!go.activeSelf);
            }

            if (MainCameraToggle.isOn)
            {
                RenderTexture.gameObject.SetActive(!RenderTexture.gameObject.activeSelf);
            }
            else
            {
                RenderTexture.gameObject.SetActive(false);
            }
        }
    }
}
