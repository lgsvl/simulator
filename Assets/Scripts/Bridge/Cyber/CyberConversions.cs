/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Linq;
using UnityEngine;
using Simulator.Bridge.Data;

namespace Simulator.Bridge.Cyber
{
    static class Conversions
    {
        static byte[] ActualBytes(byte[] array, int length)
        {
            byte[] result = new byte[length];
            Buffer.BlockCopy(array, 0, result, 0, length);
            return result;
        }

        public static apollo.drivers.CompressedImage ConvertFrom(ImageData data)
        {
            return new apollo.drivers.CompressedImage()
            {
                header = new apollo.common.Header()
                {
                    timestamp_sec = 0.0, // TODO: publish time
                    sequence_num = data.Sequence,
                    version = 1,
                    status = new apollo.common.StatusPb()
                    {
                        error_code = apollo.common.ErrorCode.OK,
                    },
                    frame_id = data.Frame,
                },
                measurement_time = 0.0, // TODO
                frame_id = data.Frame,
                format = "jpg",

                data = ActualBytes(data.Bytes, data.Length),
            };
        }

        public static apollo.drivers.PointCloud ConvertFrom(PointCloudData data)
        {
            var msg = new apollo.drivers.PointCloud()
            {
                header = new apollo.common.Header()
                {
                    timestamp_sec = 0.0, // TODO: publish time
                    sequence_num = data.Sequence,
                    version = 1,
                    status = new apollo.common.StatusPb()
                    {
                        error_code = apollo.common.ErrorCode.OK,
                    },
                    frame_id = data.Frame,
                },
                frame_id = data.Frame,
                is_dense = false,
                measurement_time = 0.0, // TODO: time
                height = 1,
                width = (uint)data.Points.Length, // TODO is this right?
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

                msg.point.Add(new apollo.drivers.PointXYZIT()
                {
                    x = pos.x,
                    y = pos.y,
                    z = pos.z,
                    intensity = (byte)(intensity * 255),
                    timestamp = (ulong)0, // TODO
                });
            };

            return msg;
        }

        public static apollo.common.Detection3DArray ConvertFrom(Detected3DObjectData data)
        {
            var r = new apollo.common.Detection3DArray()
            {
                header = new apollo.common.Header()
                {
                    sequence_num = data.Sequence,
                    frame_id = data.Frame,
                    timestamp_sec = 0.0, // TODO: time
                },
            };

            foreach (var obj in data.Data)
            {
                r.detections.Add(new apollo.common.Detection3D()
                {
                    header = r.header,
                    id = obj.Id,
                    label = obj.Label,
                    score = obj.Score,
                    bbox = new apollo.common.BoundingBox3D()
                    {
                        position = new apollo.common.Pose()
                        {
                            position = ConvertToPoint(obj.Position),
                            orientation = Convert(obj.Rotation),
                        },
                        size = ConvertToVector(obj.Scale),
                    },
                    velocity = new apollo.common.Twist()
                    {
                        linear = ConvertToVector(obj.LinearVelocity),
                        angular = ConvertToVector(obj.LinearVelocity),
                    },
                });
            }

            return r;
        }

        public static Detected3DObjectArray ConvertTo(apollo.common.Detection3DArray data)
        {
            return new Detected3DObjectArray()
            {
                Data = data.detections.Select(obj =>
                    new Detected3DObject()
                    {
                        Id = obj.id,
                        Label = obj.label,
                        Score = obj.score,
                        Position = Convert(obj.bbox.position.position),
                        Rotation = Convert(obj.bbox.position.orientation),
                        Scale = Convert(obj.bbox.size),
                        LinearVelocity = Convert(obj.velocity.linear),
                        AngularVelocity = Convert(obj.velocity.angular),
                    }).ToArray(),
            };
        }

        static apollo.common.Point3D ConvertToPoint(Vector3 v)
        {
            return new apollo.common.Point3D() { x = v.x, y = v.y, z = v.z };
        }

        static apollo.common.Vector3 ConvertToVector(Vector3 v)
        {
            return new apollo.common.Vector3() { x = v.x, y = v.y, z = v.z };
        }

        static apollo.common.Quaternion Convert(Quaternion q)
        {
            return new apollo.common.Quaternion() { qx = q.x, qy = q.y, qz = q.z, qw = q.w };
        }

        static Vector3 Convert(apollo.common.Point3D p)
        {
            return new Vector3((float)p.x, (float)p.y, (float)p.z);
        }

        static Vector3 Convert(apollo.common.Vector3 v)
        {
            return new Vector3((float)v.x, (float)v.y, (float)v.z);
        }

        static Quaternion Convert(apollo.common.Quaternion q)
        {
            return new Quaternion((float)q.qx, (float)q.qy, (float)q.qz, (float)q.qw);
        }
    }
}
