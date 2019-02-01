/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;
using UnityEngine.UI;

public enum ROSTargetEnvironment
{
    APOLLO,
    AUTOWARE,
    DUCKIETOWN_ROS1,
    DUCKIETOWN_ROS2,
    LGSVL,
}

public class RosBridgeConnector
{
    public const int DefaultPort = 9090;

    public GameObject Agent;       // actual bot object in scene
    public GameObject MenuObject;  // main menu panel
    public Canvas UiObject;        // settings panel in scene
    public GameObject UiButton;    // robot selection icon in scene
    public Text UiName;            // floating name

    public string Address = "localhost";
    public int Port = DefaultPort;
    public AgentSetup agentType;

    public string PrettyAddress
    {
        get
        {
            if (Port == DefaultPort)
            {
                return Address;
            }
            return $"{Address}:{Port}";
        }
    }

    public Text BridgeStatus;

    public Ros.Bridge Bridge { get; private set; }

    float connectTime = 0.0f;
    bool isDisconnected = true;

    public RosBridgeConnector()
    {
        Bridge = new Ros.Bridge();
    }

    public RosBridgeConnector(string address, int port, AgentSetup type) : this()
    {
        Address = address;
        Port = port;
        agentType = type;
    }

    public void Disconnect()
    {
        connectTime = Time.time + 1.0f;
        Bridge.Close();
    }

    public void Update()
    {
        if (Bridge.Status != Ros.Status.Disconnected)
        {
            isDisconnected = false;
        }

        if (!isDisconnected && Bridge.Status == Ros.Status.Disconnected)
        {
            connectTime = Time.time + 1.0f;
            isDisconnected = true;
        }

        if (Bridge.Status == Ros.Status.Disconnected && Ros.Bridge.canConnect)
        {
            if (!string.IsNullOrEmpty(Address) && (Time.time > connectTime || connectTime == 0.0f))
            {
                isDisconnected = false;
                Bridge.Connect(Address, Port, agentType.GetRosVersion());
            }
            else
            {
                return;
            }
        }

        Bridge.Update();

        if (BridgeStatus != null)
        {
            BridgeStatus.text = Bridge.Status.ToString();
        }

        if (Agent != null)
        {
            Vector3 pos = Camera.main.WorldToScreenPoint(Agent.transform.position);
            var mainTransform = Agent.transform.Find("Main");
            if (mainTransform != null)
            {
                pos = Camera.main.WorldToScreenPoint(Agent.transform.Find("Main").transform.position);
            }

            pos.y -= 75.0f; // pixels
            UiName.transform.position = pos;
        }
    }
}
