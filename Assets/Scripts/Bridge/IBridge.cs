/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;

namespace Simulator.Bridge
{
    public enum Status
    {
        Disconnected,
        Connecting,
        Connected,
        Disconnecting,
    }

    public interface IBridge
    {
        Status Status { get; }

        void Connect(string address, int port);
        void Disconnect();

        IWriter<T> AddWriter<T>(string topic);
        void AddReader<T>(string topic, Action<T> callback);
        void AddService<Argument, Result>(string topic, Func<Argument, Result> callback);

        void Update();
    }
}
