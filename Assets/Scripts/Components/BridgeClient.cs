/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using System.Diagnostics;

using Simulator.Bridge;

namespace Simulator.Components
{
    public class BridgeClient : MonoBehaviour
    {
        public string Address { get; private set; }
        public int Port { get; private set; }

        public string PrettyAddress => $"{Address}:{Port}";

        public Status BridgeStatus => Bridge == null ? Status.Disconnected : Bridge.Status;

        public IBridge Bridge { get; private set; }

        long ConnectTime;
        bool Disconnected = true;

        public void Init(IBridgeFactory factory)
        {
            Bridge = factory.Create();
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

            if (Address == null)
            {
                return;
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

        private void OnDestroy()
        {
            Disconnect();
        }
    }
}
