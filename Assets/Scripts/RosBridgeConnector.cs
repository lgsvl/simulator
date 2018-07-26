/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;
using UnityEngine.UI;

public enum AutoPlatform
{
    Apollo = 0,
    Autoware,
}

public class RosBridgeConnector
{
    public const int DefaultPort = 9090;

    public GameObject Robot;       // actual bot object in scene
    public GameObject MenuObject;  // main menu panel
    public Canvas UiObject;        // settings panel in scene
    public GameObject UiButton;    // robot selection icon in scene
    public Text UiName;            // floating name

    public string Address = "localhost";
    public int Port = DefaultPort;
    public int Version = 1;
    public AutoPlatform Platform = AutoPlatform.Apollo;

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

    public RosBridgeConnector()
    {
        Bridge = new Ros.Bridge();
    }

    public RosBridgeConnector(string address, int port, int version, AutoPlatform platform) : this()
    {
        Address = address;
        Port = port;
        Version = version;
        Platform = platform;
    }

    public void Disconnect()
    {
        connectTime = Time.time + 1.0f;
        Bridge.Close();
    }

    public void Update()
    {
        if (Bridge.Status == Ros.Status.Disconnected)
        {
            if (!string.IsNullOrEmpty(Address) && (Time.time > connectTime || connectTime == 0.0f))
            {
                Bridge.Connect(Address, Port, Version);
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

        if (Robot != null)
        {
            Vector3 pos = Camera.main.WorldToScreenPoint(Robot.transform.position);
            var mainTransform = Robot.transform.Find("Main");
            if (mainTransform != null)
            {
                pos = Camera.main.WorldToScreenPoint(Robot.transform.Find("Main").transform.position);
            }

            pos.y -= 75.0f; // pixels
            UiName.transform.position = pos;
        }
    }
}
