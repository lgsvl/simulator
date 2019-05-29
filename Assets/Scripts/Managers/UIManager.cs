/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public GameObject menuHolder;
    public GameObject controlsPanel;
    
    private void Start()
    {
        menuHolder?.SetActive(false);
        controlsPanel?.SetActive(false);
    }

    public void ToggleControlsUI()
    {
        if (controlsPanel.activeInHierarchy)
        {
            menuHolder?.SetActive(false);
            controlsPanel?.SetActive(false);
        }
        else
        {
            menuHolder?.SetActive(true);
            controlsPanel?.SetActive(true);
        }
    }
}
