/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿namespace Ros
{
    [MessageType("sensor_msgs/Joy")]
    public struct Joy
    {
        public Header header;
        public float[] axes;
        public int[] buttons;
    }
}
