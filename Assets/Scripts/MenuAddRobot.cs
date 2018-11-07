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

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            Add(Robots.Add());
        });
    }

    public void Add(RosBridgeConnector connector)
    {
        var robotConnectInfo = Instantiate(connectTemplateUI, ScrollArea.transform);

        var addressField = robotConnectInfo.bridgeAddress;
        var robotOptionField = robotConnectInfo.robotOptions;
        robotOptionField.AddOptions(MenuScript.Instance.GetRobotOptions());

        if (connector.Port == RosBridgeConnector.DefaultPort)
        {
            addressField.text = connector.Address;
        }
        else
        {
            addressField.text = $"{connector.Address}:{connector.Port}";
        }

        robotOptionField.value = connector.robotType == null ? 0 : Robots.robotCandidates.IndexOf(connector.robotType);

        addressField.onValueChanged.AddListener((value) =>
        {
            var splits = value.Split(new char[] { ':' }, 2);
            if (splits.Length == 2)
            {
                int port;
                if (int.TryParse(splits[1], out port))
                {
                    connector.Address = splits[0];
                    connector.Port = port;
                    connector.Disconnect();
                }
            }
            else if (splits.Length == 1)
            {
                connector.Address = splits[0];
                connector.Port = RosBridgeConnector.DefaultPort;
                connector.Disconnect();
            }
        });

        robotOptionField.onValueChanged.AddListener((index) =>
        {
            connector.robotType = Robots.robotCandidates[index];
            connector.Disconnect();
        });

        if (connector.robotType == null)
        {
            connector.robotType = Robots.robotCandidates[0];
        }
        connector.BridgeStatus = robotConnectInfo.transform.Find("ConnectionStatus").GetComponent<Text>();
        connector.MenuObject = robotConnectInfo.gameObject;

        transform.SetAsLastSibling();

        MenuScript.Instance.RunButtonInteractiveCheck();
    }
}
