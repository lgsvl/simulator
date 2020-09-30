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

        public void Disconnect()
        {
            Bridge.Disconnect();

            ConnectTime = Stopwatch.GetTimestamp() + reconnectInterval;
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
            if (BridgeStatus == Status.UnexpectedlyDisconnected
                && ConnectionManager.instance != null
                && Loader.Instance.CurrentSimulation.Interactive == false)
            {
                if (ConnectionManager.instance != null && Loader.Instance.CurrentSimulation.Interactive == false)
                {
                    ConnectionManager.instance.UpdateStatus("Error", Loader.Instance.CurrentSimulation.Id, "Bridge socket was unexpectedly disconnected");
                }
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
            Disconnect();
        }
    }
}
