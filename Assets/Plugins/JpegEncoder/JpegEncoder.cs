/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


using System.Runtime.InteropServices;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

public class JpegEncoder
{
    [DllImport("JpegEncoder")]
    static extern unsafe int JpegEncoder_Encode(void* source, int width, int height, int components, int quality, void* output, int maxout);

    public static int Encode(NativeArray<byte> data, int width, int height, int components, int quality, byte[] result)
    {
        unsafe
        {
            void* inPtr = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(data);
            fixed (byte* outPtr = result)
            {
                return JpegEncoder_Encode(inPtr, width, height, components, quality, outPtr, result.Length);
            }
        }
    }
}
