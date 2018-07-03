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

    void Start()
    {
        UIImage = GetComponent<RawImage>();
    }

    void Update()
    {
        if (renderTexture == null)
        {
            if (renderCamera != null)
            {
                renderTexture = renderCamera.targetTexture;
            }
        }

        if (renderTexture != null)
        {
            if (UIImage.texture != renderTexture)
            {
                UIImage.texture = renderTexture;
            }
        }
    }
}