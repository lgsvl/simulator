/**
 * Copyright(c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using Simulator.Bridge.Data;

namespace Simulator.Bridge.Ros
{
    class RosNmeaWriter : IWriter<GpsData>
    {
        Writer<Sentence> Writer;

        const float Accuracy = 0.01f; // just a number to report
        const double Height = 0; // sea level to WGS84 ellipsoid

        public RosNmeaWriter(Bridge bridge, string topic)
        {
            Writer = new Writer<Sentence>(bridge, topic);
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

            var ggaMessage = new Sentence()
            {
                header = new Header()
                {
                    stamp = Conversions.ConvertTime(message.Time),
                    seq = 2 * message.Sequence + 0,
                    frame_id = message.Frame,
                },
                sentence = "$" + gga + "*" + ggaChecksum.ToString("X2"),
            };
            Writer.Write(ggaMessage, null);

            var qqMessage = new Sentence()
            {
                header = new Header()
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
