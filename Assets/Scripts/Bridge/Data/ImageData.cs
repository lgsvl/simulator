/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Bridge.Data
{
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
}
