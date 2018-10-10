/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class FisheyeEffect : MonoBehaviour
{
    public float strengthX = 0.2f;
    public float strengthY = 0.2f;
    public Shader fishEyeShader;
    private Material fisheyeMaterial;

    // Base effects
    private Texture2D rt;

    public void Start () {
        fisheyeMaterial = new Material(fishEyeShader);
    }

    public void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        float oneOverBaseSize = 80.0f / 512.0f;

        float ar = (Screen.width * 1.0f) / (Screen.height * 1.0f);

        fisheyeMaterial.SetVector("intensity", new Vector4(strengthX * ar * oneOverBaseSize, strengthY * oneOverBaseSize, strengthX * ar * oneOverBaseSize, strengthY * oneOverBaseSize));

        Graphics.Blit(src, dst, fisheyeMaterial, 0);
    }
}