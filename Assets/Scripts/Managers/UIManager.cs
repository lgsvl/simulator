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
    
    private void Start()
    {
        menuHolder?.SetActive(false);
        Debug.Log("Init UI Manager");
    }
}
