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

        public void Write(T message, Action completed = null, Type type = null)
        {
            if (type == null)
            {
                type = typeof(T);
            }

            var sb = new StringBuilder(1024);
            sb.Append('{');
            {
                sb.Append("\"op\":\"publish\",");

                sb.Append("\"topic\":\"");
                sb.Append(Topic);
                sb.Append("\",");

                sb.Append("\"msg\":");
                Bridge.Serialize(message, type, sb);
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
        Bridge Bridge;
        Writer<To> OriginalWriter;
        Func<From, To> Convert;

        public Writer(Bridge bridge, string topic, Func<From, To> convert)
        {
            Bridge = bridge;
            OriginalWriter = new Writer<To>(Bridge, topic);
            Convert = convert;
        }

        public void Write(From message, Action completed = null, Type type = null)
        {
            if (type == null)
            {
                if (BridgeConfig.bridgeConverters.ContainsKey(typeof(From)))
                {
                    type = BridgeConfig.bridgeConverters[typeof(From)].GetOutputType(Bridge);
                }
                else
                {
                    type = typeof(To);
                }
            }

            To converted = Convert(message);
            OriginalWriter.Write(converted, completed, type);
        }
    }
}
