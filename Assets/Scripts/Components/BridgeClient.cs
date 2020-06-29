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
        public string Connection { get; private set; }

        public Status BridgeStatus => Bridge == null ? Status.Disconnected : Bridge.Status;

        public BridgeInstance Bridge { get; private set; }

        long ConnectTime;
        bool Disconnected = true;

        public void Init(BridgePlugin plugin)
        {
            Bridge = new BridgeInstance(plugin);
        }

        public void Connect(string connection)
        {
            if (BridgeStatus != Status.Disconnected)
            {
                Bridge.Disconnect();
                Disconnected = true;
            }

            Connection = connection;
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

            if (Connection == null)
            {
                return;
            }

            if (BridgeStatus == Status.Disconnected)
            {
                if (Stopwatch.GetTimestamp() > ConnectTime || ConnectTime == 0 || Time.timeScale == 0f)
                {
                    Disconnected = false;
                    Bridge.Connect(Connection);
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
