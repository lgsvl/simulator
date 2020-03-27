/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Unity.Collections;

namespace Simulator.Utilities
{
    public static class ByteArrayUtils 
    {
        // Helper function to fill in an unsinged 32-bit integer at specific position in a byte array.
        public static void FillInFourBytes(uint value, byte[] data, int byteIndex)
        {
            // The simulator can only run on little endian platforms.
            // So we assume the endianness is always little endian.
            data[byteIndex] = (byte)(value & 0xFF);
            value >>= 8;
            data[byteIndex + 1] = (byte)(value & 0xFF);
            value >>= 8;
            data[byteIndex + 2] = (byte)(value & 0xFF);
            value >>= 8;
            data[byteIndex + 3] = (byte)(value & 0xFF);
        }

        // Helper function to fill in an unsigned 16-bit integer at specific position in a native byte array.
        public static void FillInTowBytes(ushort value, NativeArray<byte> data, int byteIndex)
        {
            // The simulator can only run on little endian platforms.
            // So we assume the endianness is always little endian.
            data[byteIndex] = (byte)(value & 0xFF);
            value >>= 8;
            data[byteIndex + 1] = (byte)(value & 0xFF);
        }

        // Helper function to fill in an unsinged 32-bit integer at specific position in a native byte array.
        public static void FillInFourBytes(uint value, NativeArray<byte> data, int byteIndex)
        {
            // The simulator can only run on little endian platforms.
            // So we assume the endianness is always little endian.
            data[byteIndex] = (byte)(value & 0xFF);
            value >>= 8;
            data[byteIndex + 1] = (byte)(value & 0xFF);
            value >>= 8;
            data[byteIndex + 2] = (byte)(value & 0xFF);
            value >>= 8;
            data[byteIndex + 3] = (byte)(value & 0xFF);
        }
    }
}