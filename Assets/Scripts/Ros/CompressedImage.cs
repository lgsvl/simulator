/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿namespace Ros
{
    // fill either Array & Length, or (only for ROS1) set Base64 string
    public struct PartialByteArray
    {
        public byte[] Array;
        public int Length;

        public string Base64;
    }

    [MessageType("sensor_msgs/CompressedImage")]
    public struct CompressedImage
    {
        public Header header;
        public string format;
        public PartialByteArray data;
    }
}
