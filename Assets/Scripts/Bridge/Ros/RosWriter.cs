/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Text;
using Simulator.Bridge.Data;

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
                Bridge.Serialize(message, typeof(T), sb);
            }
            sb.Append('}');

            byte[] data = Encoding.ASCII.GetBytes(sb.ToString());
            Bridge.SendAsync(data, completed);
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

    class PointCloudWriter : IWriter<PointCloudData>
    {
        Writer<PointCloud2> OriginalWriter;

        byte[] Buffer;

        public PointCloudWriter(Bridge bridge, string topic)
        {
            OriginalWriter = new Writer<PointCloud2>(bridge, topic);
        }

        public void Write(PointCloudData data, Action completed)
        {
            if (Buffer == null || Buffer.Length != data.Points.Length)
            {
                Buffer = new byte[32 * data.Points.Length];
            }

            int count = 0;
            unsafe
            {
                fixed (byte* ptr = Buffer)
                {
                    int offset = 0;
                    for (int i = 0; i < data.Points.Length; i++)
                    {
                        var point = data.Points[i];
                        if (point == UnityEngine.Vector4.zero)
                        {
                            continue;
                        }

                        var pos = new UnityEngine.Vector3(point.x, point.y, point.z);
                        float intensity = point.w;

                        *(UnityEngine.Vector3*)(ptr + offset) = data.Transform.MultiplyPoint3x4(pos);
                        *(ptr + offset + 16) = (byte)(intensity * 255);

                        offset += 32;
                        count++;
                    }
                }
            }

            var msg = new PointCloud2()
            {
                header = new Header()
                {
                    seq = data.Sequence,
                    stamp = Time.Now(), // TODO: time should be virtual Unity time, not real world time
                    frame_id = data.Frame,
                },
                height = 1,
                width = (uint)count,
                fields = new PointField[]
                {
                    new PointField()
                    {
                        name = "x",
                        offset = 0,
                        datatype = 7,
                        count = 1,
                    },
                    new PointField()
                    {
                        name = "y",
                        offset = 4,
                        datatype = 7,
                        count = 1,
                    },
                    new PointField()
                    {
                        name = "z",
                        offset = 8,
                        datatype = 7,
                        count = 1,
                    },
                    new PointField()
                    {
                        name = "intensity",
                        offset = 16,
                        datatype = 2,
                        count = 1,
                    },
                    new PointField()
                    {
                        name = "timestamp",
                        offset = 24,
                        datatype = 8,
                        count = 1,
                    },
                },
                is_bigendian = false,
                point_step = 32,
                row_step = (uint)count * 32,
                data = new PartialByteArray()
                {
                    Base64 = Convert.ToBase64String(Buffer, 0, count * 32),
                },
                is_dense = true,
            };

            OriginalWriter.Write(msg, completed);
        }
    }
}
