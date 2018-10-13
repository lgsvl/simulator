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

    public Text BridgeStatus;

    public RosBridgeConnector Connector { get; private set; }

    public RobotSetup Robot;
    public UserInterfaceSetup UserInterface;

    void Start()
    {
        Connector = new RosBridgeConnector();
        Connector.BridgeStatus = BridgeStatus;

        if (GameObject.Find("RosRobots") == null)
        {
            Robot.Setup(UserInterface, Connector);
        }
        else
        {
            Destroy(Robot.gameObject);
            Destroy(this);
            Destroy(GameObject.Find("UserInterface"));
        }

        string overrideAddress = System.Environment.GetEnvironmentVariable("ROS_BRIDGE_HOST");
        if (overrideAddress != null)
        {
            Address = overrideAddress;
        }

        Ros.Bridge.canConnect = true;
    }

    void Update()
    {
        if (Address != Connector.Address || Port != Connector.Port || Robot != Connector.robotType)
        {
            Connector.Disconnect();
        }

        Connector.Address = Address;
        Connector.Port = Port;
        Connector.robotType = Robot;

        Connector.Update();
    }
}
