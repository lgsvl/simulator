/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


using System.Collections.Generic;

namespace Ros
{
    [MessageType("geometry_msgs/PoseWithCovariance")]
    public struct PoseWithCovariance
    {
        public Pose pose;
        public double[] covariance;  // float64[36] covariance
    }
}