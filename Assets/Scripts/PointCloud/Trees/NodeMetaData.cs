/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.PointCloud.Trees
{
    using System;
    using System.IO.MemoryMappedFiles;
    using System.Text;
    using UnityEngine;

    /// <summary>
    /// Serializable struct containing meta data of a single node.
    /// </summary>
    [Serializable]
    public struct NodeMetaData
    {
        /// <summary>
        /// Unique identifier of a node. Describes position in tree hierarchy.
        /// </summary>
        public string Identifier;

        /// <summary>
        /// Amount of points present in represented node.
        /// </summary>
        public int PointCount;

        /// <summary>
        /// Center of this node's bounds.
        /// </summary>
        public Vector3 BoundsCenter;

        /// <summary>
        /// Size of this node's bounds.
        /// </summary>
        public Vector3 BoundsSize;

        public int GetByteSize()
        {
            return sizeof(int) + sizeof(float) * 6 + sizeof(byte) + Encoding.ASCII.GetByteCount(Identifier);
        }

        public static NodeMetaData Read(MemoryMappedViewAccessor accessor, ref long position)
        {
            accessor.Read(position, out byte idLength);
            position += sizeof(byte);

            var idBytes = new byte[idLength];
            accessor.ReadArray(position, idBytes, 0, idLength);
            var identifier = Encoding.ASCII.GetString(idBytes);

            position += idLength;
            accessor.Read(position, out int pointCount);
            position += sizeof(int);

            accessor.Read(position, out float cX);
            position += sizeof(float);
            accessor.Read(position, out float cY);
            position += sizeof(float);
            accessor.Read(position, out float cZ);
            position += sizeof(float);

            accessor.Read(position, out float sX);
            position += sizeof(float);
            accessor.Read(position, out float sY);
            position += sizeof(float);
            accessor.Read(position, out float sZ);
            position += sizeof(float);

            var result = new NodeMetaData
            {
                Identifier = identifier,
                PointCount = pointCount,
                BoundsCenter = new Vector3(cX, cY, cZ),
                BoundsSize = new Vector3(sX, sY, sZ)
            };

            return result;
        }

        public void Write(MemoryMappedViewAccessor accessor, ref long position)
        {
            var bytes = Encoding.ASCII.GetBytes(Identifier);

            accessor.Write(position, (byte) bytes.Length);
            position += sizeof(byte);

            accessor.WriteArray(position, bytes, 0, bytes.Length);
            position += bytes.Length;

            accessor.Write(position, PointCount);
            position += sizeof(int);

            accessor.Write(position, BoundsCenter.x);
            position += sizeof(float);
            accessor.Write(position, BoundsCenter.y);
            position += sizeof(float);
            accessor.Write(position, BoundsCenter.z);
            position += sizeof(float);

            accessor.Write(position, ref BoundsSize.x);
            position += sizeof(float);
            accessor.Write(position, ref BoundsSize.y);
            position += sizeof(float);
            accessor.Write(position, ref BoundsSize.z);
            position += sizeof(float);
        }
    }
}