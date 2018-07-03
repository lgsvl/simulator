/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿namespace Ros
{
    [MessageType("geometry_msgs/Twist")]
    public struct Twist
    {
        public Vector3 linear;
        public Vector3 angular;
    }
}
