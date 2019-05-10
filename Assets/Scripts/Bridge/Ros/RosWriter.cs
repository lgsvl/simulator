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
    public class Writer<T> : WriterBase<T>
    {
        Bridge Bridge;
        string Topic;

        public Writer(Bridge bridge, string topic)
        {
            Bridge = bridge;
            Topic = topic;
        }

        public void Publish(T message, Action completed = null)
        {
            var sb = new StringBuilder(128);
            sb.Append('{');
            {
                sb.Append("\"op\":\"publish\",");

                sb.Append("\"topic\":\"");
                sb.Append(Topic);
                sb.Append("\",");

                sb.Append("\"msg\":");
                Bridge.Serialize(Bridge.Version, sb, typeof(T), message);
            }
            sb.Append('}');

            byte[] data = Encoding.ASCII.GetBytes(sb.ToString());

            Bridge.SendAsync(data, completed);
        }
    }
}
