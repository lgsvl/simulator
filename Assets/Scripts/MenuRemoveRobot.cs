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
    public GameObject ScrollArea;
    public RosRobots Robots;
    public Button RunButton;

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            var target = transform.parent.gameObject;
            Robots.Remove(target);
            RunButton.interactable = Robots.Robots.Count > 0;
            Destroy(target);
        });
    }
}
