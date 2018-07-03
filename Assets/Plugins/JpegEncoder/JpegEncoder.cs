/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


using System.Runtime.InteropServices;

#if UNITY_2018_1_OR_NEWER
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
#endif

public class JpegEncoder
{
    [DllImport("JpegEncoder")]
    static extern unsafe int JpegEncoder_Encode(void* source, int width, int height, int components, int quality, void* output, int maxout);

#if UNITY_2018_1_OR_NEWER
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
#endif

    public static int Encode(byte[] data, int width, int height, int components, int quality, byte[] result)
    {
        unsafe
        {
            fixed (byte* inPtr = data)
            fixed (byte* outPtr = result)
            {
                return JpegEncoder_Encode(inPtr, width, height, components, quality, outPtr, result.Length);
            }
        }
    }
}
