/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;
using UnityEngine.UI;

public class InitializeTrainingScript : MonoBehaviour
{
    void Start ()
    {
        GetComponentInParent<Toggle>().isOn = MenuScript.IsTrainingMode;
    }
}
