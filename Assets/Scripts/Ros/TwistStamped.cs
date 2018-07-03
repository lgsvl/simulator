/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿namespace Ros
{
    [MessageType("geometry_msgs/TwistStamped")]
    public struct TwistStamped
    {
        public Header header;
        public Twist twist;
    }
}
