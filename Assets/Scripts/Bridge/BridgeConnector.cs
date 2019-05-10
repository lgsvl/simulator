/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;
using UnityEngine.UI;

namespace Simulator.Bridge
{
    public enum BridgeType
    {
        NONE,
        ROS1,
        ROS2,
        APOLLO_ROS,
        CYBER_RT
    };

    public enum MessageType
    {
        AUTOWARE,
        APOLLO,
        LGSVL,
        TUGBOT,
        DUCKIEBOT
    };

    public class BridgeConnector
    {
        public static bool canConnect = false; // THIS IS VERY VERY BAD!! PLEASE DONT USE GLOBAL VARIABLES :(
        public const int DefaultPort = 9090;

        public GameObject Agent;       // actual bot object in scene
        public GameObject MenuObject;  // main menu panel
        public Canvas UiObject;        // settings panel in scene
        public GameObject UiButton;    // agent selection icon in scene
        public Text UiName;            // floating name

        public string Address = "localhost";
        public int Port = DefaultPort;
        public AgentSetup agentType;
        private BridgeType BridgeType;
        private MessageType MessageType;

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

        public Text BridgeStatusText;

        public BridgeBase Bridge { get; private set; }

        float connectTime = 0.0f;
        bool isDisconnected = true;
        public BridgeConnector(AgentSetup type)
        {
            agentType = type;
            BridgeType = agentType.AgentBridgeType;

            CreateBridge();
        }

        public BridgeConnector(string address, int port, AgentSetup type) : this(type)
        {
            Address = address;
            Port = port;
        }

        public void Connect(string address, int port)
        {
            Address = address;
            Port = port;
        }

        public void Disconnect()
        {
            connectTime = Time.time + 1.0f;
            Bridge.Disconnect();
            if (BridgeType != agentType.AgentBridgeType)
            {
                CreateBridge();
                BridgeType = agentType.AgentBridgeType;
            }
        }

        public void Update()
        {
            if (Bridge.Status != BridgeStatus.Disconnected)
            {
                isDisconnected = false;
            }

            if (!isDisconnected && Bridge.Status == BridgeStatus.Disconnected)
            {
                connectTime = Time.time + 1.0f;
                isDisconnected = true;
            }

            if (Bridge.Status == BridgeStatus.Disconnected && BridgeConnector.canConnect)
            {
                if (!string.IsNullOrEmpty(Address) && (Time.time > connectTime || connectTime == 0.0f || Time.timeScale == 0.0f))
                {
                    isDisconnected = false;
                    Bridge.Connect(Address, Port);
                }
                else
                {
                    return;
                }
            }

            Bridge.Update();

            if (BridgeStatusText != null)
            {
                BridgeStatusText.text = Bridge.Status.ToString();
            }

            if (Agent != null) // TODO move to world canvas on agent
            {
                Vector3 pos = Camera.main.WorldToScreenPoint(Agent.transform.position);
                var mainTransform = Agent.transform.Find("Main");
                if (mainTransform != null)
                {
                    pos = Camera.main.WorldToScreenPoint(Agent.transform.Find("Main").transform.position);
                }

                pos.y -= 75.0f; // pixels
                if (UiName != null)
                    UiName.transform.position = pos;
            }
        }

        void CreateBridge()
        {
            if (agentType.AgentBridgeType == BridgeType.CYBER_RT)
            {
                Bridge = new Cyber.Bridge();
            }
            else if (agentType.AgentBridgeType == BridgeType.ROS1 || agentType.AgentBridgeType == BridgeType.APOLLO_ROS)
            {
                Bridge = new Ros.Bridge(1);
            }
            else if (agentType.AgentBridgeType == BridgeType.ROS2)
            {
                Bridge = new Ros.Bridge(2);
            }
            else
            {
                Bridge = null;

            }
        }
    }
}
