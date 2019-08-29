/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using ProtoBuf;

namespace Simulator.Bridge.Cyber
{
    public class Writer<T> : IWriter<T> where T : class
    {
        Bridge Bridge;
        string TopicString;
        byte[] Topic;

        public Writer(Bridge bridge, string topic)
        {
            Bridge = bridge;
            TopicString = topic;
            Topic = Encoding.ASCII.GetBytes(topic);
        }

        public void Write(T message, Action completed = null)
        {
            byte[] msg;
            using (var stream = new MemoryStream(4096))
            {
                Serializer.Serialize(stream, message);
                msg = stream.ToArray();
            }

            var data = new List<byte>(1024);
            data.Add((byte)BridgeOp.Publish);
            data.Add((byte)(Topic.Length >> 0));
            data.Add((byte)(Topic.Length >> 8));
            data.Add((byte)(Topic.Length >> 16));
            data.Add((byte)(Topic.Length >> 24));
            data.AddRange(Topic);
            data.Add((byte)(msg.Length >> 0));
            data.Add((byte)(msg.Length >> 8));
            data.Add((byte)(msg.Length >> 16));
            data.Add((byte)(msg.Length >> 24));
            data.AddRange(msg);

            Bridge.SendAsync(data.ToArray(), completed, TopicString);
        }
    }

    class Writer<From, To> : IWriter<From>
        where From : class
        where To : class
    {
        Writer<To> OriginalWriter;
        Func<From, To> Convert;

        public Writer(Bridge bridge, string topic, Func<From, To> convert)
        {
            OriginalWriter = new Writer<To>(bridge, topic);
            Convert = convert;
        }

        public void Write(From message, Action completed)
        {
            To converted = Convert(message);
            OriginalWriter.Write(converted, completed);
        }
    }
}
