/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Runtime.InteropServices;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Simulator.Plugins
{
    public class JpegEncoder
    {
        enum Subsample : int
        {
            Samp444 = 0,
            Samp422,
            Samp420,
            Gray,
            Samp440,
            Samp411,
        }

        enum PixelFormat : int
        {
            RGB = 0,
            BGR,
            RGBX,
            BGRX,
            XBGR,
            XRGB,
            GRAY,
            RGBA,
            BGRA,
            ABGR,
            ARGB,
            CMYK,
            Unknown = -1,
        }

        [System.Flags]
        enum Flags : int
        {
            BottomUp = 2,
            FastUpsample = 256,
            NoRealloc = 1024,
            FastDCT = 2048,
            AccurateDCT = 4096,
            StopOnWarning = 8192,
            Progressive = 16384,
        }

        [DllImport("turbojpeg")]
        static extern IntPtr tjInitCompress();

        [DllImport("turbojpeg")]
        static extern int tjDestroy(IntPtr handle);

        [DllImport("turbojpeg")]
        static extern int tjCompress2(IntPtr handle, IntPtr input,
            int width, int pitch, int height, PixelFormat pixelFormat,
            ref IntPtr output, ref ulong size, Subsample subsample, int quality, Flags flags);

        public static int Encode(NativeArray<byte> data, int width, int height, int components, int quality, byte[] result)
        {
            IntPtr handle = tjInitCompress();
            try
            {
                // careful with this, on Windows C long is 32-bit, but we use C# long which is always 64-bit
                // but as long as Windows is little-endian, it'll work fine
                ulong bufferSize = (ulong)result.Length;

                unsafe
                {
                    fixed (byte* output = result)
                    {
                        IntPtr buffer = (IntPtr)output;

                        int ok = tjCompress2(
                            handle,
                            (IntPtr)data.GetUnsafeReadOnlyPtr(),
                            width,
                            width * components,
                            height,
                            components == 3 ? PixelFormat.RGB : PixelFormat.RGBX,
                            ref buffer,
                            ref bufferSize,
                            quality == 100 ? Subsample.Samp444 : Subsample.Samp420,
                            quality,
                            Flags.BottomUp | Flags.NoRealloc);
                        return (ok == 0) ? (int)bufferSize : -1;
                    }
                }
            }
            finally
            {
                tjDestroy(handle);
            }
        }
    }
}
