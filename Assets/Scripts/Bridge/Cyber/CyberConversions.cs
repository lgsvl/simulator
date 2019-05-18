/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using Google.Protobuf;
using Simulator.Bridge.Data;

namespace Simulator.Bridge.Cyber
{
    static class Conversions
    {
        public static Apollo.Drivers.CompressedImage ConvertFrom(ImageData data)
        {
            return new Apollo.Drivers.CompressedImage()
            {
                Header = new Apollo.Common.Header()
                {
                    TimestampSec = 0.0, // TODO: publish time
                    SequenceNum = data.Sequence,
                    Version = 1,
                    Status = new Apollo.Common.StatusPb()
                    {
                        ErrorCode = Apollo.Common.ErrorCode.Ok,
                    },
                    FrameId = data.Frame,
                },
                MeasurementTime = 0.0, // TODO
                FrameId = data.Frame,
                Format = "jpg",

                Data = ByteString.CopyFrom(data.Bytes, 0, data.Bytes.Length),
            };
        }

        public static Apollo.Drivers.PointCloud ConvertFrom(PointCloudData data)
        {
            var msg = new Apollo.Drivers.PointCloud()
            {
                Header = new Apollo.Common.Header()
                {
                    TimestampSec = 0.0, // TODO: publish time
                    SequenceNum = data.Sequence,
                    Version = 1,
                    Status = new Apollo.Common.StatusPb()
                    {
                        ErrorCode = Apollo.Common.ErrorCode.Ok,
                    },
                    FrameId = data.Frame,
                },
                FrameId = "lidar128",
                IsDense = false,
                MeasurementTime = 0.0, // TODO: time
                Height = 1,
                Width = (uint)data.Points.Length, // TODO is this right?
            };

            for (int i = 0; i < data.Points.Length; i++)
            {
                var point = data.Points[i];
                if (point == Vector4.zero)
                {
                    continue;
                }

                var pos = new Vector3(point.x, point.y, point.z);
                float intensity = point.w;

                pos = data.Transform.MultiplyPoint3x4(pos);

                msg.Point.Add(new Apollo.Drivers.PointXYZIT()
                {
                    X = pos.x,
                    Y = pos.y,
                    Z = pos.z,
                    Intensity = (byte)(intensity * 255),
                    Timestamp = (ulong)0, // TODO
                });
            };

            return msg;
        }
    }
}
