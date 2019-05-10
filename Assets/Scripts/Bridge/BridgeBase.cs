/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Generic;

namespace Simulator.Bridge
{
    public enum BridgeStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Disconnecting,
    }

    public abstract class BridgeBase
    {
        public struct Topic
        {
            public string Name;
            public string Type;
        }
        public List<Topic> TopicSubscriptions = new List<Topic>();
        public List<Topic> TopicPublishers = new List<Topic>();

        public abstract void Connect(string address, int port);
        public abstract void Disconnect();
        public abstract void Update();

        public abstract void SendAsync(byte[] data, Action completed = null);
        public abstract void AddReader<T>(string topic, Action<T> callback);
        public abstract WriterBase<T> AddWriter<T>(string topic);

        public abstract void AddService<Args, Result>(string service, Func<Args, Result> callback);

        protected void FinishConnecting()
        {
            OnConnectedImpl?.Invoke();
            Status = Bridge.BridgeStatus.Connected;
        }

        public event Action OnConnected
        {
            add
            {
                if (Status == Bridge.BridgeStatus.Connected)
                {
                    value();
                }
                OnConnectedImpl += value;
            }
            remove
            {
                OnConnectedImpl -= value;
            }
        }
        event Action OnConnectedImpl;

        public BridgeStatus Status { get; set; }
        public string Address { get; set; }
        public int Port { get; set; }
    }
}
