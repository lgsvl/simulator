using System;
using System.Collections.Generic;
using System.Text;

namespace Comm
{
    namespace Ros
    {
        public class RosWriter<T> : Writer<T>
        {
            Bridge Bridge;
            string Topic;
            public RosWriter(Bridge bridge, string topic)
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
                    RosBridge.Serialize(Bridge.Version, sb, typeof(T), message);
                }
                sb.Append('}');

                byte[] data = Encoding.ASCII.GetBytes(sb.ToString());

                Bridge.SendAsync(data, completed);
            }
        }
    }
}   