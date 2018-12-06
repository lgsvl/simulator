/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;
using UnityEngine.UI;

public class SingleRosConnection : MonoBehaviour
{
    public string Address = "localhost";
    public int Port = RosBridgeConnector.DefaultPort;
    public RosBridgeConnector Connector { get; private set; }
    public UserInterfaceSetup uiPrefab;

    private RobotSetup robotSetup;
    private UserInterfaceSetup userInterface;
    private Text bridgeStatus;

    private void Awake()
    {
        if (FindObjectOfType<RosRobots>() != null)
        {
            robotSetup = FindObjectOfType<RobotSetup>();
            robotSetup.DevUICleanup(userInterface);
            Destroy(robotSetup.gameObject);
            Destroy(this.gameObject);
        }
    }

    void Start()
    {
        userInterface = Instantiate(uiPrefab);
        bridgeStatus = userInterface.BridgeStatus;

        Connector = new RosBridgeConnector();
        Connector.BridgeStatus = bridgeStatus;

        robotSetup.Setup(userInterface, Connector, null);

        string overrideAddress = System.Environment.GetEnvironmentVariable("ROS_BRIDGE_HOST");
        if (overrideAddress != null)
        {
            Address = overrideAddress;
        }

        Ros.Bridge.canConnect = true;
    }

    void Update()
    {
        if (Address != Connector.Address || Port != Connector.Port || robotSetup != Connector.robotType)
        {
            Connector.Disconnect();
        }

        Connector.Address = Address;
        Connector.Port = Port;
        Connector.robotType = robotSetup;

        Connector.Update();
    }
}
