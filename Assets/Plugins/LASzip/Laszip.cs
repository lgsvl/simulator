/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Runtime.InteropServices;

namespace Simulator.Plugins
{
    public class Laszip : IDisposable
    {
        IntPtr Handle;
        bool Opened;

        unsafe LaszipPoint* PointPtr;

        public long Count { get; private set; }
        public bool HasColor { get; private set; }

        public double MinX { get; private set; }
        public double MinY { get; private set; }
        public double MinZ { get; private set; }

        public double MaxX { get; private set; }
        public double MaxY { get; private set; }
        public double MaxZ { get; private set; }

        double ScaleX;
        double ScaleY;
        double ScaleZ;

        double OffsetX;
        double OffsetY;
        double OffsetZ;

        public struct Point
        {
            public double X;
            public double Y;
            public double Z;
            public ushort Intensity;
            public ushort Red;
            public ushort Green;
            public ushort Blue;
        }

        public Laszip(string filename)
        {
            Check(Native.laszip_create(out Handle));

            var flags = DecompressFlags.XY | DecompressFlags.Z | DecompressFlags.Intensity | DecompressFlags.RGB;
            Check(Native.laszip_decompress_selective(Handle, flags));

            bool isCompressed;
            Check(Native.laszip_open_reader(Handle, filename, out isCompressed));
            Opened = true;

            IntPtr headerPtr;
            Check(Native.laszip_get_header_pointer(Handle, out headerPtr));
            try
            {
                var header = Marshal.PtrToStructure<LaszipHeader>(headerPtr);

                MinX = header.MinX;
                MinY = header.MinY;
                MinZ = header.MinZ;

                MaxX = header.MaxX;
                MaxY = header.MaxY;
                MaxZ = header.MaxZ;

                ScaleX = header.ScaleX;
                ScaleY = header.ScaleY;
                ScaleZ = header.ScaleZ;

                OffsetX = header.OffsetX;
                OffsetY = header.OffsetY;
                OffsetZ = header.OffsetZ;

                HasColor =
                    header.PointDataFormat == 2 ||
                    header.PointDataFormat == 3 ||
                    header.PointDataFormat == 5 ||
                    header.PointDataFormat == 7 ||
                    header.PointDataFormat == 8 ||
                    header.PointDataFormat == 10;

                Count = header.PointDataCount != 0 ? (long)header.PointDataCount : (long)header.PointDataCountLong;
            }
            finally
            {
                Native.laszip_clean(headerPtr);
            }

            unsafe
            {
                Check(Native.laszip_get_point_pointer(Handle, out PointPtr));
            }
        }

        public Point GetNextPoint()
        {
            Check(Native.laszip_read_point(Handle));

            var pt = new Point();
            unsafe
            {
                pt.X = PointPtr->X * ScaleX + OffsetX;
                pt.Y = PointPtr->Y * ScaleY + OffsetY;
                pt.Z = PointPtr->Z * ScaleZ + OffsetZ;
                pt.Intensity = PointPtr->Intensity;
                if (HasColor)
                {
                    pt.Red = PointPtr->Red;
                    pt.Green = PointPtr->Green;
                    pt.Blue = PointPtr->Blue;
                }
            }
            return pt;
        }

        public void Dispose()
        {
            if (Opened)
            {
                Native.laszip_close_reader(Handle);
            }

            if (Handle != IntPtr.Zero)
            {
                Native.laszip_destroy(Handle);
            }
        }

        void Check(int code)
        {
            if (code == 0)
            {
                return;
            }

            IntPtr error;
            if (Native.laszip_get_error(Handle, out error) != 0)
            {
                try
                {
                    throw new Exception(Marshal.PtrToStringAnsi(error));
                }
                finally
                {
                    Native.laszip_clean(error);
                }
            }
            else
            {
                throw new Exception("Unknown laszip error");
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct LaszipHeader
        {
            public ushort FileSourceId;
            public ushort GlobalEncoding;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] Guid;
            public byte VersionMajor;
            public byte VersionMinor;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] SystemIdentifier;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] GeneratingSoftware;
            public ushort CreateDayOfYear;
            public ushort CreateYear;
            public ushort HeaderSize;
            public uint PointDataOffset;
            public uint VarRecCount;
            public byte PointDataFormat;
            public byte PaddingByte;
            public ushort PointDataSize;
            public uint PointDataCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            public uint[] PointCountReturn;
            public double ScaleX;
            public double ScaleY;
            public double ScaleZ;
            public double OffsetX;
            public double OffsetY;
            public double OffsetZ;
            public double MaxX;
            public double MinX;
            public double MaxY;
            public double MinY;
            public double MaxZ;
            public double MinZ;
            public ulong WaveformDataOffset;
            public ulong ExtendedVarOffset;
            public uint ExtendedVarCount;
            public ulong PointDataCountLong;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
            public ulong[] PointCountReturnLong;

            // laszip_U32 user_data_in_header_size;
            // laszip_U8* user_data_in_header;
            // laszip_vlr_struct* vlrs;
            // laszip_U32 user_data_after_header_size;
            // laszip_U8* user_data_after_header;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct LaszipPoint
        {
            public int X;
            public int Y;
            public int Z;
            public ushort Intensity;
            public byte Flags1;
            public byte Flags2;
            public sbyte ScanAngleRank;
            public byte UserData;
            public ushort PointSourceId;
            public short ExtendedScanAngle;
            public byte ExtendedFlags1;
            public byte ExtendedClassification;
            public byte ExtendedFlags2;

            public byte Dummy0;
            public byte Dummy1;
            public byte Dummy2;
            public byte Dummy3;
            public byte Dummy4;
            public byte Dummy5;
            public byte Dummy6;

            public double GpsTime;
            public ushort Red;
            public ushort Green;
            public ushort Blue;
            public ushort Alpha;
            
            // laszip_U8 wave_packet[29];
            // laszip_I32 num_extra_bytes;
            // laszip_U8* extra_bytes;
        }

        [Flags]
        enum DecompressFlags : uint
        {
            All = 0xffffffff,
            XY = 0,
            Z = 0x00000001,
            Classification = 0x00000002,
            Flags = 0x00000004,
            Intensity = 0x00000008,
            ScanAngle = 0x00000010,
            UserData = 0x00000020,
            PointSource = 0x00000040,
            GpsTime = 0x00000080,
            RGB = 0x00000100,
            NIR = 0x00000200,
            WavePacket = 0x00000400,
            Byte0 = 0x00010000,
            Byte1 = 0x00020000,
            Byte2 = 0x00040000,
            Byte3 = 0x00080000,
            Byte4 = 0x00100000,
            Byte5 = 0x00200000,
            Byte6 = 0x00400000,
            Byte7 = 0x00800000,
            ExtraBytes = 0xFFFF0000,
        }

        static class Native
        {
            [DllImport("laszip", CallingConvention = CallingConvention.Cdecl)]
            public static extern int laszip_create(out IntPtr ptr);

            [DllImport("laszip", CallingConvention = CallingConvention.Cdecl)]
            public static extern int laszip_get_error(IntPtr ptr, out IntPtr error);

            [DllImport("laszip", CallingConvention = CallingConvention.Cdecl)]
            public static extern int laszip_clean(IntPtr ptr);

            [DllImport("laszip", CallingConvention = CallingConvention.Cdecl)]
            public static extern int laszip_destroy(IntPtr ptr);

            [DllImport("laszip", CallingConvention = CallingConvention.Cdecl)]
            public static extern int laszip_get_header_pointer(IntPtr ptr, out IntPtr header);

            [DllImport("laszip", CallingConvention = CallingConvention.Cdecl)]
            public unsafe static extern int laszip_get_point_pointer(IntPtr ptr, out LaszipPoint* point);

            [DllImport("laszip", CallingConvention = CallingConvention.Cdecl)]
            public static extern int laszip_decompress_selective(IntPtr ptr, DecompressFlags flags);

            [DllImport("laszip", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public static extern int laszip_open_reader(IntPtr ptr, [MarshalAs(UnmanagedType.LPStr)] string filename, out bool isCompressed);

            [DllImport("laszip", CallingConvention = CallingConvention.Cdecl)]
            public static extern int laszip_read_point(IntPtr ptr);

            [DllImport("laszip", CallingConvention = CallingConvention.Cdecl)]
            public static extern int laszip_close_reader(IntPtr ptr);
        }
    }
}
