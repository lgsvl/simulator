/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Simulator.Utilities
{
    public class PcdWriter : IDisposable
    {
        public int Count { get; private set; }
        public long Size => Stream.Length;

        FileStream Stream;
        long CountOffset1;
        long CountOffset2;

        byte[] Buffer = new byte[4 + 4 + 4 + 1];

        public PcdWriter(string filename)
        {
            Stream = File.Create(filename, 1024 * 1024);

            Write($"VERSION 0.7\n");
            Write($"FIELDS x y z intensity\n");
            Write($"SIZE 4 4 4 1\n");
            Write($"TYPE F F F U\n");
            Write($"COUNT 1 1 1 1\n");
            Write($"WIDTH ");
            CountOffset1 = Stream.Position;
            Write($"##########\n");
            Write($"HEIGHT 1\n");
            Write($"VIEWPOINT 0 0 0 1 0 0 0\n");
            Write($"POINTS ");
            CountOffset2 = Stream.Position;
            Write($"##########\n");
            Write($"DATA binary\n");
        }

        public void Dispose()
        {
            Stream.Seek(CountOffset1, SeekOrigin.Begin);
            Write($"{Count}");

            Stream.Seek(CountOffset2, SeekOrigin.Begin);
            Write($"{Count}");

            Stream.Dispose();
        }

        public void Write(Vector3 position, float intensity)
        {
            Debug.Assert(Count < int.MaxValue);

            unsafe
            {
                fixed (byte* ptr = Buffer)
                {
                    *(Vector3*)ptr = position;
                    ptr[4 + 4 + 4] = (byte)(intensity * 255);
                }
            }

            Stream.Write(Buffer, 0, Buffer.Length);
            Count++;
        }

        void Write(string s)
        {
            var bytes = Encoding.ASCII.GetBytes(s);
            Stream.Write(bytes, 0, bytes.Length);
        }
    }
}
