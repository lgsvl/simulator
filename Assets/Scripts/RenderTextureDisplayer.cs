/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class RenderTextureDisplayer : MonoBehaviour
{
    public RenderTexture renderTexture;
    public Camera renderCamera;
    private RawImage UIImage;
    private float initViewHeightDelta;

    void Start()
    {
        UIImage = GetComponent<RawImage>();
        initViewHeightDelta = UIImage.rectTransform.sizeDelta.y;
    }

    void Update()
    {
        if (renderCamera != null)
        {
            renderTexture = renderCamera.targetTexture;
        }

        if (UIImage.texture != renderTexture)
        {
            UIImage.texture = renderTexture;
        }
    }

    public void SwitchResolution(int w, int h)
    {
        UIImage.rectTransform.sizeDelta = new Vector2((float)w / (float)h * initViewHeightDelta, initViewHeightDelta);
    }
}