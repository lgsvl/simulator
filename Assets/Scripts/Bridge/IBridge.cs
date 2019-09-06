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
    public enum Status
    {
        Disconnected,
        Connecting,
        Connected,
        Disconnecting,
    }

    public class TopicUIData
    {
        public string Topic;
        public string Type;
        public int Count;
        public int StartCount;
        public float ElapsedTime;
        public float Frequency;
    }

    public interface IBridge
    {
        Status Status { get; }

        void Connect(string address, int port);
        void Disconnect();

        IWriter<T> AddWriter<T>(string topic) where T : class;
        void AddReader<T>(string topic, Action<T> callback) where T : class;

        void AddService<Argument, Result>(string topic, Func<Argument, Result> callback);

        void Update();

        List<TopicUIData> TopicSubscriptions { get; set; }
        List<TopicUIData> TopicPublishers { get; set; }
    }
}
