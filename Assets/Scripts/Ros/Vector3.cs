/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿namespace Ros
{
    [MessageType("geometry_msgs/Vector3")]
    public struct Vector3
    {
        public double x;
        public double y;
        public double z;
        public Vector3(double i, double j, double k)
        {
            x = i;
            y = j;
            z = k;
        }
    }
}
