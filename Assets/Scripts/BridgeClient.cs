/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using System.Diagnostics;

using Simulator.Bridge;
using System;
using System.Collections.Generic;

namespace Simulator
{
    public enum BridgeType
    {
        None,
        Ros1,
        Ros2,
        ApolloRos,
        CyberRT,
    };

    public enum MessageType
    {
        AUTOWARE,
        APOLLO,
        LGSVL,
        TUGBOT,
        DUCKIEBOT
    };

    public class BridgeClient
    {
        public string Address { get; private set; }
        public int Port { get; private set; }

        public string PrettyAddress => $"{Address}:{Port}";

        public Status BridgeStatus => Bridge == null ? Status.Disconnected : Bridge.Status;

        public IBridge Bridge { get; private set; }
        MessageType MessageType;

        long ConnectTime;
        bool Disconnected = true;

        public BridgeClient(string address, int port, BridgeType type)
        {
            Address = address;
            Port = port;

            if (type == BridgeType.Ros1 || type == BridgeType.ApolloRos)
            {
                Bridge = new Bridge.Ros.Bridge(1);
            }
            else if (type == BridgeType.Ros2)
            {
                Bridge = new Bridge.Ros.Bridge(2);
            }
            else if (type == BridgeType.CyberRT)
            {
                Bridge = new Bridge.Cyber.Bridge();
            }
            else
            {
                throw new Exception("Unsupported bridge type");
            }
        }

        public void Connect(string address, int port)
        {
            if (BridgeStatus != Status.Disconnected)
            {
                Bridge.Disconnect();
                Disconnected = true;
            }

            Address = address;
            Port = port;

            ConnectTime = 0;
        }

        public void Disconnect()
        {
            Bridge.Disconnect();

            ConnectTime = Stopwatch.GetTimestamp() + Stopwatch.Frequency;
        }

        public void Update()
        {
            if (BridgeStatus != Status.Disconnected)
            {
                Disconnected = false;
            }

            if (!Disconnected && BridgeStatus == Status.Disconnected)
            {
                ConnectTime = Stopwatch.GetTimestamp() + Stopwatch.Frequency;
                Disconnected = true;
            }

            if (BridgeStatus == Status.Disconnected)
            {
                if (Stopwatch.GetTimestamp() > ConnectTime || ConnectTime == 0 || Time.timeScale == 0f)
                {
                    Disconnected = false;
                    Bridge.Connect(Address, Port);
                }
                else
                {
                    return;
                }
            }

            if (BridgeStatus == Status.Connected)
            {
                Bridge.Update();
            }
        }
    }
}
