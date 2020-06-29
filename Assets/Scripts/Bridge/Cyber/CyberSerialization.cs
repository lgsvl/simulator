/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.IO;
using ProtoBuf;

namespace Simulator.Bridge.Cyber
{
    public static class CyberSerialization
    {
        public static ProtobufType Unserialize<ProtobufType>(ArraySegment<byte> data)
        {
            using (var stream = new MemoryStream(data.Array, data.Offset, data.Count, false))
            {
                return Serializer.Deserialize<ProtobufType>(stream);
            }
        }

        public static byte[] Serialize<ProtobufType>(ProtobufType message)
        {
            using (var stream = new MemoryStream(4096))
            {
                Serializer.Serialize(stream, message);
                return stream.ToArray();
            }
        }
    }
}
