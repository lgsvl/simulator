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
            SceneManager.LoadScene("Menu");

            var robots = FindObjectOfType<RosRobots>();
            if (robots != null)
            {

                robots.Disconnect();
                Destroy(robots.gameObject);
            }

            // TODO: unload only loaded map bundle, not everything
            AssetBundle.UnloadAllAssetBundles(true);
        });
    }
}
