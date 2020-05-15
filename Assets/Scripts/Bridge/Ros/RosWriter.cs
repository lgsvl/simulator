/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Text;

namespace Simulator.Bridge.Ros
{
    public class Writer<T> : IWriter<T> where T : class
    {
        Bridge Bridge;
        string Topic;

        public Writer(Bridge bridge, string topic)
        {
            Bridge = bridge;
            Topic = topic;
        }

        public void Write(T message, Action completed = null)
        {
            var sb = new StringBuilder(1024);
            sb.Append('{');
            {
                sb.Append("\"op\":\"publish\",");

                sb.Append("\"topic\":\"");
                sb.Append(Topic);
                sb.Append("\",");

                sb.Append("\"msg\":");
                Bridge.Serialize(message, message.GetType(), sb);
            }
            sb.Append('}');

            byte[] data = Encoding.ASCII.GetBytes(sb.ToString());
            Bridge.SendAsync(data, completed, Topic);
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
