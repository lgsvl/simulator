/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(UserInterfaceSetup))]
public class DisplaySwitch : MonoBehaviour
{
    [SerializeField]
    private KeyCode switchKeyCode = KeyCode.Space;
    public RectTransform MainPanel;
    public RectTransform LGWatermark;
    private UserInterfaceSetup UI;

    void Start()
    {
        UI = GetComponent<UserInterfaceSetup>();
    }

    protected virtual void Update ()
    {
        if (Input.GetKeyDown(switchKeyCode))
        {
            Switch();
        }
    }

    public void Switch()
    {
        MainPanel.gameObject.SetActive(!MainPanel.gameObject.activeSelf);
        SyncUIComponents();
        VehicleList.Instances.ForEach(x => x.ToggleDisplay(UserInterfaceSetup.FocusUI.MainPanel.gameObject.activeSelf)); //hack
    }

    //make all ui components match its expected state when main panel is on/off
    //This is also needed when you set the toggle in the code, which could result in incorrect ui components display states
    public void SyncUIComponents()
    {
        bool isOn = MainPanel.gameObject.activeSelf;
        UI.CameraPreview.gameObject.SetActive(isOn);
        UI.ColorSegmentPreview.gameObject.SetActive(isOn);
        LGWatermark.gameObject.SetActive(!isOn);
    }
}
