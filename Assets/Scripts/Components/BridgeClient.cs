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
    using System;
    using Debug = UnityEngine.Debug;

    public class BridgeClient : MonoBehaviour
    {
        public string Connection { get; private set; }

        public Status BridgeStatus => Bridge == null ? Status.Disconnected : Bridge.Status;

        public BridgeInstance Bridge { get; private set; }

        long ConnectTime;
        bool Disconnected = true;

        static long reconnectInterval = Stopwatch.Frequency * 3;

        public void Init(BridgePlugin plugin)
        {
            Bridge = new BridgeInstance(plugin);
        }

        public void Connect(string connection)
        {
            if (BridgeStatus != Status.Disconnected && BridgeStatus != Status.UnexpectedlyDisconnected)
            {
                Bridge.Disconnect();
                Disconnected = true;
            }

            Connection = connection;
            ConnectTime = 0;
        }

        public void Update()
        {
            bool disconnectedStatus = BridgeStatus == Status.Disconnected || BridgeStatus == Status.UnexpectedlyDisconnected;

            if (!disconnectedStatus)
            {
                Disconnected = false;
            }

            if (!Disconnected && disconnectedStatus)
            {
                ConnectTime = Stopwatch.GetTimestamp() + reconnectInterval;
                Disconnected = true;
            }

            if (Connection == null)
            {
                return;
            }

            // do not reconnect in non interactive mode
            if (BridgeStatus == Status.UnexpectedlyDisconnected && Loader.Instance.CurrentSimulation.Interactive == false)
            {
                Loader.Instance.reportStatus(SimulatorStatus.Error, "Bridge socket was unexpectedly disconnected");
                Bridge.Disconnect();
                Disconnected = true;
            }
            else if (disconnectedStatus)
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
            Bridge.Disconnect();
        }
    }
}
