/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.PointCloud.Trees
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using ICSharpCode.SharpZipLib.Core;
    using ICSharpCode.SharpZipLib.Zip;

    /// <summary>
    /// <para>Helper class used for memory mapping point cloud data packed in uncompressed ZIP file.</para>
    /// <para>Implementation details based on <see cref="ICSharpCode.SharpZipLib.Zip.ZipFile"/>.</para>
    /// </summary>
    public class ZipTreeData
    {
        private class NodeRecord
        {
            public long offset;
            public long size;
        }

        private readonly long firstElementOffset;

        private Dictionary<string, NodeRecord> nodeRecords = new Dictionary<string, NodeRecord>();

        private static Encoding EncodingFromFlag(int flags) =>
            (flags & 2048) == 0
                ? Encoding.GetEncoding(ZipStrings.SystemDefaultCodePage)
                : Encoding.UTF8;

        private static string ConvertToString(int flags, byte[] data, int count) =>
            data != null
                ? EncodingFromFlag(flags).GetString(data, 0, count)
                : string.Empty;

        private static ushort ReadUshort(Stream stream)
        {
            var val0 = stream.ReadByte();
            if (val0 < 0)
                throw new EndOfStreamException("End of stream");

            var val1 = stream.ReadByte();
            if (val1 < 0)
                throw new EndOfStreamException("End of stream");
            
            return (ushort) ((uint) (ushort) val0 | (ushort) (val1 << 8));
        }

        private static int ReadShort(Stream stream)
        {
            var val0 = stream.ReadByte();
            if (val0 < 0)
                throw new EndOfStreamException();

            var val1 = stream.ReadByte();
            if (val1 < 0)
                throw new EndOfStreamException();

            return val0 | val1 << 8;
        }

        private static uint ReadUint(Stream stream) => ReadUshort(stream) | (uint) ReadUshort(stream) << 16;

        private static ulong ReadUlong(Stream stream) => ReadUint(stream) | (ulong) ReadUint(stream) << 32;

        private static int ReadInt(Stream stream) => ReadShort(stream) | ReadShort(stream) << 16;

        public ZipTreeData(string filePath, string dataHash)
        {
            var folderName = "pointcloud_" + dataHash;

            using (var stream = (Stream) File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var endLocation = stream.CanSeek
                    ? LocateBlockWithSignature(stream, 101010256, stream.Length, 22, ushort.MaxValue)
                    : throw new Exception("ZipFile stream must be seekable");
                if (endLocation < 0L)
                    throw new Exception("Cannot find central directory");
                var val0 = (int) ReadUshort(stream);
                var val1 = ReadUshort(stream);
                var length0 = (ulong) ReadUshort(stream);
                var val2 = (ulong) ReadUshort(stream);
                var val3 = (ulong) ReadUint(stream);
                var val4 = (long) ReadUint(stream);
                var val5 = (uint) ReadUshort(stream);
                if (val5 > 0U)
                {
                    var numArray = new byte[(int) val5];
                    StreamUtils.ReadFully(stream, numArray);
                }

                var flag1 = false;
                var flag2 = val0 == ushort.MaxValue || val1 == ushort.MaxValue || length0 == ushort.MaxValue ||
                            val2 == ushort.MaxValue || val3 == uint.MaxValue || val4 == uint.MaxValue;

                if (LocateBlockWithSignature(stream, 117853008, endLocation, 0, 4096) < 0L)
                {
                    if (flag2)
                        throw new Exception("Cannot find Zip64 locator");
                }
                else
                {
                    flag1 = true;
                    stream.Position += 4; // Ignored: uint
                    var val6 = ReadUlong(stream);
                    stream.Position += 4; // Ignored: uint
                    stream.Position = (long) val6;
                    if (ReadUint(stream) != 101075792U)
                        throw new Exception($"Invalid Zip64 Central directory signature at {val6:X}");
                    stream.Position += 20; // Ignored: ulong, ushort, ushort, uint, uint
                    length0 = ReadUlong(stream);
                    stream.Position += 8; // Ignored: ulong
                    val3 = ReadUlong(stream);
                    val4 = (long) ReadUlong(stream);
                }

                if (!flag1 && val4 < endLocation - (4L + (long) val3))
                {
                    firstElementOffset = endLocation - (4L + (long) val3 + val4);
                    if (firstElementOffset <= 0L)
                        throw new Exception("Invalid embedded zip archive");
                }

                stream.Seek(firstElementOffset + val4, SeekOrigin.Begin);
                for (ulong index = 0; index < length0; ++index)
                {
                    if (ReadUint(stream) != 33639248U)
                        throw new Exception("Wrong Central Directory signature");
                    stream.Position += 4; // Ignored: ushort, ushort
                    var flags = (int) ReadUshort(stream);
                    stream.Position += 14; // Ignored: ushort, uint, uint, uint
                    var ival0 = (long) ReadUint(stream);
                    var ival1 = (int) ReadUshort(stream);
                    var length1 = (int) ReadUshort(stream);
                    var ival2 = (int) ReadUshort(stream);
                    stream.Position += 8; // Ignored: ushort, ushort, uint
                    var ival3 = (long) ReadUint(stream);
                    var numArray = new byte[Math.Max(ival1, ival2)];
                    StreamUtils.ReadFully(stream, numArray, 0, ival1);
                    var name = ConvertToString(flags, numArray, ival1);

                    if (name.StartsWith(folderName))
                    {
                        name = name.Substring(folderName.Length + 1);

                        nodeRecords.Add(name, new NodeRecord()
                        {
                            offset = ival3,
                            size = ival0 & uint.MaxValue
                        });
                    }

                    if (length1 > 0)
                    {
                        var buffer = new byte[length1];
                        StreamUtils.ReadFully(stream, buffer);
                    }

                    if (ival2 > 0)
                        StreamUtils.ReadFully(stream, numArray, 0, ival2);
                }

                foreach (var nodeRecord in nodeRecords)
                {
                    var actualOffset = GetActualOffset(stream, nodeRecord.Value);
                    nodeRecord.Value.offset = actualOffset;
                }
            }
        }

        private long GetActualOffset(Stream stream, NodeRecord entry)
        {
            stream.Seek(firstElementOffset + entry.offset, SeekOrigin.Begin);
            if (ReadUint(stream) != 67324752U)
                throw new Exception($"Wrong local header signature @{(object) (firstElementOffset + entry.offset):X}");
            var val0 = (short) (ReadUshort(stream) & byte.MaxValue);
            var val1 = (short) ReadUshort(stream);
            stream.Position += 10; // Ignored: ushort, ushort, ushort, uint
            var val2 = (long) ReadUint(stream);
            var val3 = (long) ReadUint(stream);
            var length0 = (int) ReadUshort(stream);
            var length1 = (int) ReadUshort(stream);
            var numArray0 = new byte[length0];
            StreamUtils.ReadFully(stream, numArray0);
            var numArray1 = new byte[length1];
            StreamUtils.ReadFully(stream, numArray1);
            var zipExtraData = new ZipExtraData(numArray1);
            if (zipExtraData.Find(1))
            {
                val3 = zipExtraData.ReadLong();
                zipExtraData.ReadLong();
                if ((val1 & 8) != 0)
                {
                    if (val3 != -1L && val3 != entry.size)
                        throw new Exception("Size invalid for descriptor");
                }
            }
            else if (val0 >= 45 && ((uint) val3 == uint.MaxValue || (uint) val2 == uint.MaxValue))
                throw new Exception("Required Zip64 extended information missing");

            var val4 = length0 + length1;
            return firstElementOffset + entry.offset + 30L + val4;
        }

        public IEnumerable<string> EnumerateFiles => nodeRecords.Select(x => x.Key);

        public long GetEntryOffset(string id)
        {
            return nodeRecords[id].offset;
        }

        public long GetEntrySize(string id)
        {
            return nodeRecords[id].size;
        }

        private static long LocateBlockWithSignature(Stream stream, int signature, long endLocation, int minBlockSize,
            int maxVarData)
        {
            var val0 = endLocation - minBlockSize;
            if (val0 < 0L)
                return -1;
            var val1 = Math.Max(val0 - maxVarData, 0L);
            while (val0 >= val1)
            {
                stream.Seek(val0--, SeekOrigin.Begin);
                if (ReadInt(stream) == signature)
                    return stream.Position;
            }

            return -1;
        }
    }
}