/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
*/

namespace Ros
{
    [MessageType("sensor_msgs/Imu")]
    public struct Imu
    {
        public Header header;
        public Quaternion orientation;
        public double[] orientation_covariance; // Row major about x, y, z axes
        public Vector3 angular_velocity;
        public double[] angular_velocity_covariance; // Row major about x, y, z axes
        public Vector3 linear_acceleration;
        public double[] linear_acceleration_covariance; // Row major about x, y, z axes
    }
}