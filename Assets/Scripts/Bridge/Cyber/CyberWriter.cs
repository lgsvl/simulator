/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Generic;
using Google.Protobuf;

namespace Simulator.Bridge.Cyber
{
    enum Op : byte
    {
        RegisterDesc = 1,
        AddReader = 2,
        AddWriter = 3,
        Publish = 4,
    }

    public class Writer<T> : WriterBase<T>
    {
        BridgeBase Bridge;
        string Topic;

        public Writer(BridgeBase bridge, string topic)
        {
            Bridge = bridge;
            Topic = topic;
        }

        public void Publish(T message, Action completed = null)
        {
            var msg = (message as IMessage).ToByteArray();
            var topicb = System.Text.Encoding.ASCII.GetBytes(Topic);

            var data = new List<byte>(128);
            data.Add((byte)Op.Publish);
            data.Add((byte)(Topic.Length >> 0));
            data.Add((byte)(Topic.Length >> 8));
            data.Add((byte)(Topic.Length >> 16));
            data.Add((byte)(Topic.Length >> 24));
            data.AddRange(topicb);
            data.Add((byte)(msg.Length >> 0));
            data.Add((byte)(msg.Length >> 8));
            data.Add((byte)(msg.Length >> 16));
            data.Add((byte)(msg.Length >> 24));
            data.AddRange(msg);

            Bridge.SendAsync(data.ToArray(), completed);
        }

    }
}
