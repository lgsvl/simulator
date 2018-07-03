/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;

public class SpawnInfo : MonoBehaviour
{
    public enum Type
    {
        Duckiebot,
        Sedan,
    }

    public Type type;

    public void ChangeGlobalSettings()
    {
        if (type == Type.Duckiebot)
        {
            MenuScript.InitGlobalSettings();
        }
        else if (type == Type.Sedan)
        {
            QualitySettings.shadowDistance = 500f;
            QualitySettings.shadowResolution = ShadowResolution.High;
        }
    }
}
