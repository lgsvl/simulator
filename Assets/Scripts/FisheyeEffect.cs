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

    public void OnPostRender() 
    {
        float oneOverBaseSize = 80.0f / 512.0f;

        float ar = (Screen.width * 1.0f) / (Screen.height * 1.0f);

        fisheyeMaterial.SetVector ("intensity", new Vector4 (strengthX * ar * oneOverBaseSize, strengthY * oneOverBaseSize, strengthX * ar * oneOverBaseSize, strengthY * oneOverBaseSize));
        FullScreenQuad(fisheyeMaterial);
    }

    public static void FullScreenQuad(Material renderMat)
    {
        GL.PushMatrix();
        for (var i = 0; i < renderMat.passCount; ++i)
        {
            renderMat.SetPass(i);

            GL.LoadOrtho();
            GL.Begin(GL.QUADS); // Quad
            GL.Color(new Color(1f, 1f, 1f, 1f));
            GL.MultiTexCoord(0, new Vector3(0f, 0f, 0f));
            GL.Vertex3(0, 0, 0);
            GL.MultiTexCoord(0, new Vector3(0f, 1f, 0f));
            GL.Vertex3(0, 1, 0);
            GL.MultiTexCoord(0, new Vector3(1f, 1f, 0f));
            GL.Vertex3(1, 1, 0);
            GL.MultiTexCoord(0, new Vector3(1f, 0f, 0f));
            GL.Vertex3(1, 0, 0);
            GL.End();
        }
        GL.PopMatrix();
    }
}