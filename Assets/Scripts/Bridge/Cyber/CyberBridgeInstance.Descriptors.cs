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

// Protobuf Library Version (https://github.com/lgsvl/protobuf-net)
// $ protogen.exe --version
// protogen 2.4.2+gf6ca1f28a2

// To generate protobuf c# files
// $ mono protogen.exe --csharp_out=protobuf/cs +names=original +langver=3 -I. **/*.proto

// To generate DescriptorSet binary
// $ mono protogen.exe +names=original +langver=3 -oprotobuf/data.bin -I. **/*.proto

// Python script to convert DescriptorSet binary to string
/*
from glob import glob
from base64 import b64encode

apollo_path = "/path/to/apollo"
bin_file = os.path.join(apollo_path, "protobuf/data.bin")

with open(os.path.join(apollo_path, "protobuf/data.txt"), "w+") as fout:
    fout.write("public static readonly string Value = string.Concat(\n")
    with open(bin_file, "rb") as fin:
        b = b64encode(fin.read())
        arr = []
        while b:
            arr.append('    "{}"'.format(b[:60].decode('utf-8')))
            b = b[60:]
        fout.write(",\n".join(arr))
    fout.write('\n);')
 */

namespace Simulator.Bridge.Cyber
{
    public partial class CyberBridgeInstance : IBridgeInstance
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

        static List<byte[]> GetDescriptors<ProtobufType>()
        {
            var descriptorName = NameByMsgType[typeof(ProtobufType).ToString()];
            var descriptor = DescriptorByName[descriptorName].Item2;

            var descriptors = new List<byte[]>();
            GetDescriptors(descriptors, descriptor);
            return descriptors;
        }

        static void GetDescriptors(List<byte[]> descriptors, FileDescriptorProto descriptor)
        {
            foreach (var dependency in descriptor.Dependencies)
            {
                var desc = DescriptorByName[dependency].Item2;
                GetDescriptors(descriptors, desc);
            }
            var bytes = DescriptorByName[descriptor.Name].Item1;
            descriptors.Add(bytes);
        }

        static CyberBridgeInstance()
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
