/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿namespace Ros
{
    [MessageType("std_msgs/Header")]
    public struct Header
    {
        public uint seq;
        public Time stamp;
        public string frame_id;
    }
}
