/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.IO;
using System.Collections.Generic;
using ProtoBuf;
using Google.Protobuf.Reflection;

namespace Simulator.Bridge.Cyber
{
    public partial class Bridge : IBridge
    {
        static Dictionary<string, string> NameByMsgType;
        static Dictionary<string, Tuple<byte[], FileDescriptorProto>> DescriptorByName;

        static FileDescriptorSet DeserializeFileDescriptorSet(string value)
        {
            var bytes = Convert.FromBase64String(ApolloFileDescriptorSet.Value);

            using (var stream = new MemoryStream(bytes))
            {
                return Serializer.Deserialize<FileDescriptorSet>(stream);
            }
        }

        static byte[] SerializeFileDescriptor(FileDescriptorProto descriptor)
        {
            using (var stream = new MemoryStream(4096))
            {
                Serializer.Serialize(stream, descriptor);
                return stream.ToArray();
            }
        }

        static Bridge()
        {
            NameByMsgType = new Dictionary<string, string>();
            DescriptorByName = new Dictionary<string, Tuple<byte[], FileDescriptorProto>>();

            var set = DeserializeFileDescriptorSet(ApolloFileDescriptorSet.Value);

            foreach (var descriptor in set.Files)
            {
                var descriptorName = descriptor.Name;
                var data = SerializeFileDescriptor(descriptor);

                if (!DescriptorByName.ContainsKey(descriptorName))
                {
                    DescriptorByName.Add(descriptorName, Tuple.Create(data, descriptor));
                }

                foreach (var msgType in descriptor.MessageTypes)
                {
                    var fullMsgType = $"{descriptor.Package}.{msgType.Name}";
                    if (!NameByMsgType.ContainsKey(fullMsgType))
                    {
                        NameByMsgType.Add(fullMsgType, descriptorName);
                    }
                }
            }
        }
    }
}
