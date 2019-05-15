/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Linq;
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

        public static Apollo.Common.Detection3DArray ConvertFrom(Detected3DObjectData data)
        {
            var r = new Apollo.Common.Detection3DArray()
            {
                Header = new Apollo.Common.Header()
                {
                    SequenceNum = data.Sequence,
                    FrameId = data.Frame,
                    TimestampSec = 0.0, // TODO: time
                },
            };

            foreach (var obj in data.Data)
            {
                r.Detections.Add(new Apollo.Common.Detection3D()
                {
                    Header = r.Header,
                    Id = obj.Id,
                    Label = obj.Label,
                    Score = obj.Score,
                    Bbox = new Apollo.Common.BoundingBox3D()
                    {
                        Position = new Apollo.Common.Pose()
                        {
                            Position = ConvertToPoint(obj.Position),
                            Orientation = Convert(obj.Rotation),
                        },
                        Size = ConvertToVector(obj.Scale),
                    },
                    Velocity = new Apollo.Common.Twist()
                    {
                        Linear = ConvertToVector(obj.LinearVelocity),
                        Angular = ConvertToVector(obj.LinearVelocity),
                    },
                });
            }

            return r;
        }

        public static Detected3DObjectArray ConvertTo(Apollo.Common.Detection3DArray data)
        {
            return new Detected3DObjectArray()
            {
                Data = data.Detections.Select(obj =>
                    new Detected3DObject()
                    {
                        Id = obj.Id,
                        Label = obj.Label,
                        Score = obj.Score,
                        Position = Convert(obj.Bbox.Position.Position),
                        Rotation = Convert(obj.Bbox.Position.Orientation),
                        Scale = Convert(obj.Bbox.Size),
                        LinearVelocity = Convert(obj.Velocity.Linear),
                        AngularVelocity = Convert(obj.Velocity.Angular),
                    }).ToArray(),
            };
        }

        static Apollo.Common.Point3D ConvertToPoint(Vector3 v)
        {
            return new Apollo.Common.Point3D() { X = v.x, Y = v.y, Z = v.z };
        }

        static Apollo.Common.Vector3 ConvertToVector(Vector3 v)
        {
            return new Apollo.Common.Vector3() { X = v.x, Y = v.y, Z = v.z };
        }

        static Apollo.Common.Quaternion Convert(Quaternion q)
        {
            return new Apollo.Common.Quaternion() { Qx = q.x, Qy = q.y, Qz = q.z, Qw = q.w };
        }

        static Vector3 Convert(Apollo.Common.Point3D p)
        {
            return new Vector3((float)p.X, (float)p.Y, (float)p.Z);
        }

        static Vector3 Convert(Apollo.Common.Vector3 v)
        {
            return new Vector3((float)v.X, (float)v.Y, (float)v.Z);
        }

        static Quaternion Convert(Apollo.Common.Quaternion q)
        {
            return new Quaternion((float)q.Qx, (float)q.Qy, (float)q.Qz, (float)q.Qw);
        }
    }
}
