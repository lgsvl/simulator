/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class SliderSynchronizer : MonoBehaviour
{
    public Scrollbar targetScrollbar;
    private Text targetText;

    public void Start()
    {
        targetScrollbar.onValueChanged.AddListener(delegate { ValueChangeCallback(); });
        targetText = GetComponent<Text>();
        ValueChangeCallback();
    }

    public void ValueChangeCallback()
    {
        targetText.text = (targetScrollbar.value * 2.0f).ToString("0.00");
    }
}
