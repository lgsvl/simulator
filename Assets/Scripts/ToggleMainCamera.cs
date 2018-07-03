/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;
using UnityEngine.UI;

public class ToggleMainCamera : MonoBehaviour
{
    public Camera Camera;
    public RenderTextureDisplayer TextureDisplay;

    void Start()
    {
        var toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(enabled =>
        {
            Camera.enabled = enabled;
            Camera.GetComponent<VideoToROS>().enabled = enabled;
            TextureDisplay.gameObject.SetActive(enabled);
        });
    }
}
