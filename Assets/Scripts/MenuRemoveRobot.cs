/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;
using UnityEngine.UI;

public class MenuRemoveRobot : MonoBehaviour
{
    public RosRobots Robots;
    public GameObject rootGO;

    private void Awake()
    {
        Robots = GameObject.FindObjectOfType<RosRobots>();
    }

    public void RemoveRobot()
    {
        if (Robots == null) return;

        Robots.Remove(rootGO);
        Destroy(rootGO);
    }
}
