/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;
using UnityEngine.UI;

public class FPSScript : MonoBehaviour
{
    public Text FPSText;

    float LastTime;
    int Frames;

    void Start()
    {
        LastTime = Time.time;
        Frames = 0;
    }

    void Update()
    {
        Frames++;
        float now = Time.time;
        if (now >= LastTime + 1.0f)
        {
            float delta = now - LastTime;
            LastTime = now;

            float fps = Frames / delta;
            FPSText.text = $"FPS: {fps:###0.0}";

            Frames = 0;
        }
    }
}
