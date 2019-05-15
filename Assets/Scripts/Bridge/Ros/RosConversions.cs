/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Linq;
using Simulator.Bridge.Data;
using Simulator.Bridge.Ros.LGSVL;

namespace Simulator.Bridge.Ros
{
    static class Conversions
    {
        public static CompressedImage ConvertFrom(ImageData data)
        {
            return new CompressedImage()
            {
                header = new Header()
                {
                    seq = data.Sequence,
                    stamp = Time.Now(), // TODO: time should be virtual Unity time, not real world time
                    frame_id = data.Frame,
                },
                format = "jpeg",
                data = new PartialByteArray()
                {
                    Array = data.Bytes,
                    Length = data.Length,
                },
            };
        }

        public static Detection3DArray ConvertFrom(Detected3DObjectData data)
        {
            return new Detection3DArray()
            {
                header = new Header()
                {
                    seq = data.Sequence,
                    stamp = Time.Now(),
                    frame_id = data.Frame,
                },
                detections = data.Data.Select(d => new Detection3D()
                {
                    id = d.Id,
                    label = d.Label,
                    score = d.Score,
                    bbox = new BoundingBox3D()
                    {
                        position = new Pose()
                        {
                            position = new Point()
                            {
                                x = d.Position.x,
                                y = d.Position.y,
                                z = d.Position.z
                            },
                            orientation = new Quaternion()
                            {
                                x = d.Rotation.x,
                                y = d.Rotation.y,
                                z = d.Rotation.z,
                                w = d.Rotation.w,
                            },
                        },
                        size = new Vector3()
                        {
                            x = d.Scale.x,
                            y = d.Scale.y,
                            z = d.Scale.z
                        }
                    },
                    velocity = new Twist()
                    {
                        linear = new Vector3()
                        {
                            x = d.LinearVelocity.x,
                            y = d.LinearVelocity.y,
                            z = d.LinearVelocity.z,
                        },
                        angular = new Vector3()
                        {
                            x = d.AngularVelocity.x,
                            y = d.AngularVelocity.y,
                            z = d.AngularVelocity.z
                        },
                    }
                }).ToList(),
            };
        }

        public static Detected3DObjectArray ConvertTo(Detection3DArray data)
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
                        LinearVelocity = new UnityEngine.Vector3((float)obj.velocity.linear.x, 0, 0),
                        AngularVelocity = new UnityEngine.Vector3(0, 0, (float)obj.velocity.angular.z),
                    }).ToArray(),
            };
        }

        static Point ConvertToPoint(UnityEngine.Vector3 v)
        {
            return new Point() { x = v.x, y = v.y, z = v.z };
        }

        static Vector3 ConvertToVector(UnityEngine.Vector3 v)
        {
            return new Vector3() { x = v.x, y = v.y, z = v.z };
        }

        static Quaternion Convert(UnityEngine.Quaternion q)
        {
            return new Quaternion() { x = q.x, y = q.y, z = q.z, w = q.w };
        }

        static UnityEngine.Vector3 Convert(Point p)
        {
            return new UnityEngine.Vector3((float)p.x, (float)p.y, (float)p.z);
        }

        static UnityEngine.Vector3 Convert(Vector3 v)
        {
            return new UnityEngine.Vector3((float)v.x, (float)v.y, (float)v.z);
        }

        static UnityEngine.Quaternion Convert(Quaternion q)
        {
            return new UnityEngine.Quaternion((float)q.x, (float)q.y, (float)q.z, (float)q.w);
        }
    }
}
