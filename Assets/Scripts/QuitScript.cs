/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

﻿using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class QuitScript : MonoBehaviour
{
    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            AnalyticsManager.Instance?.MapExitEvent(SceneManager.GetActiveScene().name);
            SceneManager.LoadScene("Menu", LoadSceneMode.Single);
            ROSAgentManager.Instance?.DisconnectAgents();
        });
    }


}
