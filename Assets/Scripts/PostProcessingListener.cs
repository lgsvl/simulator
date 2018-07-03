/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;
using UnityEngine.PostProcessing;

[RequireComponent(typeof(PostProcessingBehaviour))]
public class PostProcessingListener : MonoBehaviour
{
    private PostProcessingBehaviour postProcBehaviour;

    void Start()
    {
        postProcBehaviour = GetComponent<PostProcessingBehaviour>();
    }

    public void SetSaturationValue(float value)
    {
        if (postProcBehaviour != null)
        {
            var settings = postProcBehaviour.profile.colorGrading.settings;
            settings.basic.saturation = value * 2.0f;
            postProcBehaviour.profile.colorGrading.settings = settings;
        }
    }
}
