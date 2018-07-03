/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿namespace Ros
{
    public struct PartialByteArray
    {
        public byte[] Array;
        public int Length;
    }

    [MessageType("sensor_msgs/CompressedImage")]
    public struct CompressedImage
    {
        public Header header;
        public string format;
        public PartialByteArray data;
    }
}
