/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿namespace Ros
{
    [MessageType("geometry_msgs/Pose")]
    public struct Pose
    {
        public Point position;
        public Quaternion orientation;
    }
}
