/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

public static class PngEncoder
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void png_rw_ptr(IntPtr png, IntPtr data, IntPtr size);

    [DllImport("libpng")]
    public static extern IntPtr png_get_libpng_ver(IntPtr png_ptr);

    [DllImport("libpng")]
    static extern IntPtr png_create_write_struct(IntPtr user_png_ver, IntPtr error_ptr, IntPtr error_fn, IntPtr warn_fn);

    [DllImport("libpng")]
    static extern IntPtr png_get_io_ptr(IntPtr png_ptr);

    [DllImport("libpng")]
    static extern void png_set_write_fn(IntPtr png_ptr, IntPtr io_ptr, IntPtr write_data_fn, IntPtr output_flush_fn);

    [DllImport("libpng")]
    static extern IntPtr png_create_info_struct(IntPtr png_ptr);

    [DllImport("libpng")]
    static extern void png_set_IHDR(IntPtr png_ptr, IntPtr info_ptr, uint width, uint height, int bit_depth, int color_type, int interlace_method, int compression_method, int filter_method);

    [DllImport("libpng")]
    static unsafe extern void png_set_rows(IntPtr png_ptr, IntPtr info_ptr, byte** row_pointers);

    [DllImport("libpng")]
    static extern void png_write_png(IntPtr png_ptr, IntPtr info_ptr, int transforms, IntPtr @params);

    [DllImport("libpng")]
    static extern void png_destroy_write_struct(ref IntPtr png_ptr_ptr, ref IntPtr info_ptr_ptr);

    const int PNG_TRANSFORM_IDENTITY = 0;

    const int PNG_COLOR_MASK_PALETTE = 1;
    const int PNG_COLOR_MASK_COLOR = 2;
    const int PNG_COLOR_MASK_ALPHA = 4;

    const int PNG_COLOR_TYPE_GRAY = 0;
    const int PNG_COLOR_TYPE_PALETTE = PNG_COLOR_MASK_COLOR | PNG_COLOR_MASK_PALETTE;
    const int PNG_COLOR_TYPE_RGB = PNG_COLOR_MASK_COLOR;
    const int PNG_COLOR_TYPE_RGB_ALPHA = PNG_COLOR_MASK_COLOR | PNG_COLOR_MASK_ALPHA;
    const int PNG_COLOR_TYPE_GRAY_ALPHA = PNG_COLOR_MASK_ALPHA;
    const int PNG_COLOR_TYPE_RGBA = PNG_COLOR_TYPE_RGB_ALPHA;

    const int PNG_INTERLACE_NONE = 0;
    const int PNG_COMPRESSION_TYPE_DEFAULT = 0;
    const int PNG_FILTER_TYPE_DEFAULT = 0;

    class Writer
    {
        public byte[] Data;
        public int Size = 0;
        public bool Overflow = false;
    }

    static void PngWrite(IntPtr png, IntPtr data, IntPtr size)
    {
        var handle = GCHandle.FromIntPtr(png_get_io_ptr(png));
        var writer = handle.Target as Writer;

        if (writer.Overflow)
        {
            return;
        }

        long count = size.ToInt64();
        if (writer.Size + count > writer.Data.Length)
        {
            writer.Overflow = true;
            return;
        }

        Marshal.Copy(data, writer.Data, writer.Size, (int)count);
        writer.Size += (int)count;
    }

    static png_rw_ptr PngWriteDelegate = PngWrite;

    public static int Encode(byte[] data, int width, int height, int components, byte[] result)
    {
        var writer = new Writer() { Data = result };
        var handle = GCHandle.Alloc(writer, GCHandleType.Pinned);

        IntPtr version = png_get_libpng_ver(IntPtr.Zero);

        IntPtr png = png_create_write_struct(version, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        IntPtr info = IntPtr.Zero;
        try
        {
            info = png_create_info_struct(png);

            int color_type = 0;
            if (components == 1)
            {
                color_type = PNG_COLOR_TYPE_GRAY;
            }
            else if (components == 3)
            {
                color_type = PNG_COLOR_TYPE_RGB;
            }
            else if (components == 4)
            {
                color_type = PNG_COLOR_TYPE_RGBA;
            }
            else
            {
                Debug.Assert(false);
            }

            png_set_IHDR(png, info, (uint)width, (uint)height, 8, color_type,
                PNG_INTERLACE_NONE, PNG_COMPRESSION_TYPE_DEFAULT, PNG_FILTER_TYPE_DEFAULT);

            png_set_write_fn(png,
                GCHandle.ToIntPtr(handle),
                Marshal.GetFunctionPointerForDelegate(PngWriteDelegate),
                IntPtr.Zero);

            unsafe
            {
                fixed (byte* dataptr = data)
                {
                    byte** rows = stackalloc byte*[height];
                    for (int y = 0; y < height; y++)
                    {
                        rows[y] = dataptr + (height - 1 - y) * width * components;
                    }
                    png_set_rows(png, info, rows);
                    png_write_png(png, info, PNG_TRANSFORM_IDENTITY, IntPtr.Zero);
                }
            }

            return writer.Overflow ? -1 : writer.Size;
        }
        finally
        {
            png_destroy_write_struct(ref png, ref info);
        }
    }
}
