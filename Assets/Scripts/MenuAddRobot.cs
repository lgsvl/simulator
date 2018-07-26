/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MenuAddRobot : MonoBehaviour
{
    public GameObject ScrollArea;
    public BridgeConnectionUI connectTemplateUI;
    public RosRobots Robots;
    public Button RunButton;

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            Add(Robots.Add());
        });
    }

    public void Add(RosBridgeConnector robot)
    {
        var robotConnectInfo = Instantiate<BridgeConnectionUI>(connectTemplateUI, ScrollArea.transform);

        var addressField = robotConnectInfo.bridgeAddress;
        var versionField = robotConnectInfo.ROSVersion;
        var platformField = robotConnectInfo.ADPlatform;

        if (robot.Port == RosBridgeConnector.DefaultPort)
        {
            addressField.text = robot.Address;
        }
        else
        {
            addressField.text = $"{robot.Address}:{robot.Port}";
        }
        versionField.value = robot.Version == 1 ? 0 : 1;
        platformField.value = robot.Platform == AutoPlatform.Apollo ? 0 : 1;

        addressField.onValueChanged.AddListener((value) =>
        {
            var splits = value.Split(new char[] { ':' }, 2);
            if (splits.Length == 2)
            {
                int port;
                if (int.TryParse(splits[1], out port))
                {
                    robot.Address = splits[0];
                    robot.Port = port;
                    robot.Disconnect();
                }
            }
            else if (splits.Length == 1)
            {
                robot.Address = splits[0];
                robot.Port = RosBridgeConnector.DefaultPort;
                robot.Disconnect();
            }
        });

        versionField.onValueChanged.AddListener((index) =>
        {
            robot.Version = index == 0 ? 1 : 2;
            robot.Disconnect();
        });

        platformField.onValueChanged.AddListener((index) =>
        {
            robot.Platform = index == 0 ? AutoPlatform.Apollo : AutoPlatform.Autoware;
            robot.Disconnect();
        });

        robot.BridgeStatus = robotConnectInfo.transform.Find("ConnectionStatus").GetComponent<Text>();
        robot.MenuObject = robotConnectInfo.gameObject;

        transform.SetAsLastSibling();

        RunButton.interactable = Robots.Robots.Count > 0;
    }
}
