/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
*/

using System.Collections.Generic;

namespace Ros
{
    [MessageType("sensor_msgs/LaserScan")]
    public struct LaserScan
    {
        public Header header;
        public float angle_min;
        public float angle_max;
        public float angle_increment;
        public float time_increment;
        public float scan_time;
        public float range_min;
        public float range_max;
        public float[] ranges;
        public float[] intensities;
    }
}