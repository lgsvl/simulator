/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Text;
using Simulator.Bridge.Data;
using UnityEngine;

namespace Simulator.Bridge.Ros
{
    public class RosWriter<BridgeType>
    {
        RosBridgeInstance Instance;
        string Topic;

        public RosWriter(RosBridgeInstance instance, string topic)
        {
            Instance = instance;
            Topic = topic;
        }

        public void Write(BridgeType message, Action completed)
        {
            var sb = new StringBuilder(4096);
            sb.Append('{');
            {
                sb.Append("\"op\":\"publish\",");

                sb.Append("\"topic\":\"");
                sb.Append(Topic);
                sb.Append("\",");

                sb.Append("\"msg\":");
                try
                {
                    RosSerialization.Serialize(message, sb);
                }
                catch (Exception ex)
                {
                    // explicit logging of exception because this method is often called
                    // from background threads for which Unity does not log exceptions
                    Debug.LogException(ex);
                    throw;
                }
            }
            sb.Append('}');

            var data = sb.ToString();
            Instance.SendAsync(data, completed);
        }
    }

    class RosPointCloudWriter
    {
        RosWriter<Ros.PointCloud2> Writer;

        byte[] Buffer;
        static readonly Ros.PointField[] PointFields = new[]
        {
            new Ros.PointField()
            {
                name = "x",
                offset = 0,
                datatype = 7,
                count = 1,
            },
            new Ros.PointField()
            {
                name = "y",
                offset = 4,
                datatype = 7,
                count = 1,
            },
            new Ros.PointField()
            {
                name = "z",
                offset = 8,
                datatype = 7,
                count = 1,
            },
            new Ros.PointField()
            {
                name = "intensity",
                offset = 16,
                datatype = 2,
                count = 1,
            },
            new Ros.PointField()
            {
                name = "timestamp",
                offset = 24,
                datatype = 8,
                count = 1,
            },
        };

        public RosPointCloudWriter(RosBridgeInstance instance, string topic)
        {
            Writer = new RosWriter<Ros.PointCloud2>(instance, topic);
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

            var msg = new Ros.PointCloud2()
            {
                header = new Ros.Header()
                {
                    seq = data.Sequence,
                    stamp = Conversions.ConvertTime(data.Time),
                    frame_id = data.Frame,
                },
                height = 1,
                width = (uint)count,
                fields = PointFields,
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

            Writer.Write(msg, completed);
        }
    }

    class RosNmeaWriter
    {
        RosWriter<Ros.Sentence> Writer;

        const float Accuracy = 0.01f; // just a number to report
        const double Height = 0; // sea level to WGS84 ellipsoid

        public RosNmeaWriter(RosBridgeInstance instance, string topic)
        {
            Writer = new RosWriter<Ros.Sentence>(instance, topic);
        }

        public void Write(GpsData message, Action completed)
        {
            char latitudeS = message.Latitude < 0 ? 'S' : 'N';
            char longitudeS = message.Longitude < 0 ? 'W' : 'E';
            double lat = Math.Abs(message.Latitude);
            double lon = Math.Abs(message.Longitude);

            lat = Math.Floor(lat) * 100 + (lat % 1) * 60.0f;
            lon = Math.Floor(lon) * 100 + (lon % 1) * 60.0f;

            var dt = DateTimeOffset.FromUnixTimeMilliseconds((long)(message.Time * 1000.0)).UtcDateTime;
            var utc = dt.ToString("HHmmss.fff");

            var gga = string.Format("GPGGA,{0},{1:0.000000},{2},{3:0.000000},{4},{5},{6},{7},{8:0.000000},M,{9:0.000000},M,,",
                utc,
                lat, latitudeS,
                lon, longitudeS,
                1, // GPX fix
                10, // sattelites tracked
                Accuracy,
                message.Altitude,
                Height);

            var angles = message.Orientation.eulerAngles;
            float roll = -angles.z;
            float pitch = -angles.x;
            float yaw = angles.y;

            var qq = string.Format("QQ02C,INSATT,V,{0},{1:0.000},{2:0.000},{3:0.000},",
                utc,
                roll,
                pitch,
                yaw);

            // http://www.plaisance-pratique.com/IMG/pdf/NMEA0183-2.pdf
            // 5.2.3 Checksum Field

            byte ggaChecksum = 0;
            for (int i = 0; i < gga.Length; i++)
            {
                ggaChecksum ^= (byte)gga[i];
            }

            byte qqChecksum = 0;
            for (int i = 0; i < qq.Length; i++)
            {
                qqChecksum ^= (byte)qq[i];
            }

            var ggaMessage = new Ros.Sentence()
            {
                header = new Ros.Header()
                {
                    stamp = Conversions.ConvertTime(message.Time),
                    seq = 2 * message.Sequence + 0,
                    frame_id = message.Frame,
                },
                sentence = "$" + gga + "*" + ggaChecksum.ToString("X2"),
            };
            Writer.Write(ggaMessage, null);

            var qqMessage = new Ros.Sentence()
            {
                header = new Ros.Header()
                {
                    stamp = ggaMessage.header.stamp,
                    seq = 2 * message.Sequence + 1,
                    frame_id = message.Frame,
                },
                sentence = qq + "@" + qqChecksum.ToString("X2"),

            };
            Writer.Write(qqMessage, completed);
        }
    }
}
