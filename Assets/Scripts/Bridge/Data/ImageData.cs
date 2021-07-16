/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Bridge.Data
{
    using System;
    using Unity.Collections;

    // this is always Jpeg compressed image
    public class ImageData
    {
        public string Name;
        public string Frame;
        public double Time;
        public uint Sequence;

        public int Width;
        public int Height;

        public byte[] Bytes;
        public int Length;
    }

    public class UncompressedImageData : IThreadCachedBridgeData<UncompressedImageData>
    {
        public string Name;
        public string Frame;
        public double Time;
        public uint Sequence;

        public int Width;
        public int Height;

        public int Length;
        public string Encoding;
        public bool IsBigEndian;

        // NOTE: Due to relationship between native and managed arrays, this class provides two options to pass byte
        //       data. This saves one array copy if handled properly in bridge-side conversion.
        // If BridgeMessageDispatcher is used to publish data, it's best to only provide either NativeArray or Bytes,
        // with the other being null/default.
        // If BridgeMessageDispatcher is not used, it's best to either provide only Bytes (in which case bridge
        // shouldn't perform copy), or NativeArray plus reusable Bytes array with no actual data (in which case bridge
        // should perform copy, but won't allocate new managed array*).
        // * - only providing NativeArray might require allocation of managed array, unless bridge is using some pool
        public NativeArray<byte> NativeArray;
        public byte[] Bytes;

        public void CopyToCache(UncompressedImageData target)
        {
            target.Name = Name;
            target.Frame = Frame;
            target.Time = Time;
            target.Sequence = Sequence;

            target.Width = Width;
            target.Height = Height;

            target.Length = Length;
            target.Encoding = Encoding;
            target.IsBigEndian = IsBigEndian;

            if (target.Bytes == null || target.Bytes.Length != Length)
                target.Bytes = new byte[Length];

            if (NativeArray != default)
                NativeArray<byte>.Copy(NativeArray, 0, target.Bytes, 0, Length);
            else
                Array.Copy(Bytes, target.Bytes, Length);
        }

        public int GetHash()
        {
            // Arrays are always exact-length, so different sizes won't be compatible and will require new allocation.
            // Keep them separated in pool to allow for re-using already existing arrays.
            return Length;
        }
    }

    public class CameraInfoData
    {
        public string Name;
        public string Frame;
        public double Time;
        public uint Sequence;

        public int Width;
        public int Height;

        public double FocalLengthX;
        public double FocalLengthY;
        public double PrincipalPointX;
        public double PrincipalPointY;

        public float[] DistortionParameters;
    }
}
