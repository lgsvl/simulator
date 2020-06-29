/**
 * Copyright(c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;

namespace Simulator.Bridge
{
    public class ByteArray
    {
        public byte[] Data { get; private set; }
        public int Count { get; private set; }

        public ByteArray(int capacity = 4096)
        {
            Data = new byte[capacity];
        }

        public void Append(byte[] bytes, int offset, int count)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }
            if (offset >= bytes.Length)
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            if (offset + count > bytes.Length)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            if (Count + count > Data.Length)
            {
                var newSize = Count + count;

                // round up to next 4KB
                newSize = (newSize + 4095) & ~4095;

                var bigger = new byte[newSize];
                Array.Copy(Data, bigger, Count);
                Data = bigger;
            }
            Array.Copy(bytes, offset, Data, Count, count);
            Count += count;
        }

        public void RemoveFirst(int count)
        {
            if (count >= Count)
            {
                Count = 0;
                return;
            }
            Array.Copy(Data, count, Data, 0, Count - count);
            Count -= count;
        }
    }
}
