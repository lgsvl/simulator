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
    public RectTransform cameraViewPanel;
    public RectTransform LGWatermark;
    private UserInterfaceSetup UI;

    private int state = 1;

    void Start()
    {
        state = 1;
        MainPanel.gameObject.SetActive(true);
        cameraViewPanel.gameObject.SetActive(true);
        LGWatermark.gameObject.SetActive(false);
        DashUIManager.Instance?.ToggleUI(true);

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
        state = state < 2 ? state + 1 : 0;
        if (state == 0)
        {
            MainPanel.gameObject.SetActive(false);
            cameraViewPanel.gameObject.SetActive(false);
            LGWatermark.gameObject.SetActive(true);
            DashUIManager.Instance?.ToggleUI(false);
        }
        else if (state == 1)
        {
            MainPanel.gameObject.SetActive(true);
            cameraViewPanel.gameObject.SetActive(true);
            LGWatermark.gameObject.SetActive(false);
            DashUIManager.Instance?.ToggleUI(true);
        }
        else if (state == 2)
        {
            MainPanel.gameObject.SetActive(false);
            cameraViewPanel.gameObject.SetActive(true);
            LGWatermark.gameObject.SetActive(false);
            DashUIManager.Instance?.ToggleUI(true);
        }
        // TODO fix
        //VehicleList.Instances?.ForEach(x => x.ToggleDisplay(UserInterfaceSetup.FocusUI.MainPanel.gameObject.activeSelf)); //hack
    }

    public void ToggleDashView()
    {
        MainPanel.gameObject.SetActive(false);
        cameraViewPanel.gameObject.SetActive(false);
        LGWatermark.gameObject.SetActive(true);
        DashUIManager.Instance?.ToggleUI(false);
    }

    public void ToggleOutOfDashView()
    {
        if (state == 0)
        {
            MainPanel.gameObject.SetActive(false);
            cameraViewPanel.gameObject.SetActive(false);
            LGWatermark.gameObject.SetActive(true);
            DashUIManager.Instance?.ToggleUI(false);
        }
        else if (state == 1)
        {
            MainPanel.gameObject.SetActive(true);
            cameraViewPanel.gameObject.SetActive(true);
            LGWatermark.gameObject.SetActive(false);
            DashUIManager.Instance?.ToggleUI(true);
        }
        else if (state == 2)
        {
            MainPanel.gameObject.SetActive(false);
            cameraViewPanel.gameObject.SetActive(true);
            LGWatermark.gameObject.SetActive(false);
            DashUIManager.Instance?.ToggleUI(true);
        }
    }
}
