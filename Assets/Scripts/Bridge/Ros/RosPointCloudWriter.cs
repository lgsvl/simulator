/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using Simulator.Bridge.Data;

namespace Simulator.Bridge.Ros
{
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
                    stamp = Conversions.ConvertTime(data.Time),
                    frame_id = data.Frame,
                },
                height = 1,
                width = (uint)count,
                fields = new []
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
                    Array = Buffer,
                    Length = count * 32,
                },
                is_dense = true,
            };

            OriginalWriter.Write(msg, completed);
        }
    }
}
