/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Diagnostics;
using System.Text;

namespace Simulator.Bridge.Ros2
{
    public class Writer<T> : IWriter<T>
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
            int topicLength = Topic.Length;
            int messageLength = Serialization.GetLength(message);

            var bytes = new byte[1 + 4 + topicLength + 4 + messageLength];
            bytes[0] = (byte)BridgeOp.Publish;

            bytes[1] = (byte)(topicLength >> 0);
            bytes[2] = (byte)(topicLength >> 8);
            bytes[3] = (byte)(topicLength >> 16);
            bytes[4] = (byte)(topicLength >> 24);
            Buffer.BlockCopy(Topic, 0, bytes, 5, topicLength);

            bytes[5 + topicLength] = (byte)(messageLength >> 0);
            bytes[6 + topicLength] = (byte)(messageLength >> 8);
            bytes[7 + topicLength] = (byte)(messageLength >> 16);
            bytes[8 + topicLength] = (byte)(messageLength >> 24);
            int written = Serialization.Serialize(message, bytes, 9 + topicLength);

            Debug.Assert(written == messageLength, "Something is terribly wrong if this is not true");

            Bridge.SendAsync(bytes, completed, TopicString);
        }
    }

    class Writer<From, To> : IWriter<From>
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
