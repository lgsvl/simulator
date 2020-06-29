/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Bridge.Ros.Ros
{
    [MessageType("std_msgs/Time")]
    public class Time
    {
        public long secs;
        public uint nsecs;
    }

    [MessageType("rosgraph_msgs/Clock")]
    public class Clock
    {
        public Time clock;
    }

    [MessageType("std_msgs/ColorRGBA")]
    public class ColorRGBA
    {
        public double r;
        public double g;
        public double b;
        public double a;
    }

    [MessageType("sensor_msgs/CompressedImage")]
    public class CompressedImage
    {
        public Header header;
        public string format;
        public PartialByteArray data;
    }

    [MessageType("std_msgs/Header")]
    public class Header
    {
        public uint seq;
        public Time stamp;
        public string frame_id;
    }

    [MessageType("sensor_msgs/Image")]
    public class Image
    {
        public Header header;
        public uint height;
        public uint width;
        public string encoding;
        public byte is_bigendian;
        public uint step;
        public byte[] data;
    }

    [MessageType("sensor_msgs/Imu")]
    public class Imu
    {
        public Header header;
        public Quaternion orientation;
        public double[] orientation_covariance; // Row major about x, y, z axes
        public Vector3 angular_velocity;
        public double[] angular_velocity_covariance; // Row major about x, y, z axes
        public Vector3 linear_acceleration;
        public double[] linear_acceleration_covariance; // Row major about x, y, z axes
    }

    [MessageType("sensor_msgs/Joy")]
    public class Joy
    {
        public Header header;
        public float[] axes;
        public int[] buttons;
    }

    [MessageType("sensor_msgs/LaserScan")]
    public class LaserScan
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

    public enum NavFixStatus : sbyte
    {
        STATUS_NO_FIX = -1, // unable to fix position
        STATUS_FIX = 0, // unaugmented fix
        STATUS_SBAS_FIX = 1, // with satellite-based augmentation
        STATUS_GBAS_FIX = 2 // with ground-based augmentation
    }

    public enum GpsServisType : ushort
    {
        SERVICE_GPS = 1,
        SERVICE_GLONASS = 2,
        SERVICE_COMPASS = 4, // includes BeiDou.
        SERVICE_GALILEO = 8
    }

    [MessageType("sensor_msgs/NavSatStatus")]
    public class NavSatStatus
    {
        public NavFixStatus status;
        public GpsServisType service;
    }

    public enum CovarianceType : byte
    {
        COVARIANCE_TYPE_UNKNOWN = 0,
        COVARIANCE_TYPE_APPROXIMATED = 1,
        COVARIANCE_TYPE_DIAGONAL_KNOWN = 2,
        COVARIANCE_TYPE_KNOWN = 3
    }

    [MessageType("sensor_msgs/NavSatFix")]
    public class NavSatFix
    {
        public Header header;
        public NavSatStatus status;
        public double latitude;
        public double longitude;
        public double altitude;
        public double[] position_covariance;
        public CovarianceType position_covariance_type;
    }

    [MessageType("nav_msgs/Odometry")]
    public class Odometry
    {
        public Header header;
        public string child_frame_id;
        public PoseWithCovariance pose;
        public TwistWithCovariance twist;
    }

    [MessageType("geometry_msgs/Point")]
    public class Point
    {
        public double x;
        public double y;
        public double z;
    }

    [MessageType("sensor_msgs/PointCloud2")]
    public class PointCloud2
    {
        public Header header;
        public uint height;
        public uint width;
        public PointField[] fields;
        public bool is_bigendian;
        public uint point_step;
        public uint row_step;
        public PartialByteArray data;
        public bool is_dense;
    }

    [MessageType("sensor_msgs/PointField")]
    public class PointField
    {
        public const byte INT8 = 1;
        public const byte UINT8 = 2;
        public const byte INT16 = 3;
        public const byte UINT16 = 4;
        public const byte INT32 = 5;
        public const byte UINT32 = 6;
        public const byte FLOAT32 = 7;
        public const byte FLOAT64 = 8;

        public string name;
        public uint offset;
        public byte datatype;
        public uint count;
    }

    [MessageType("geometry_msgs/Pose")]
    public class Pose
    {
        public Point position;
        public Quaternion orientation;
    }

    [MessageType("geometry_msgs/PoseWithCovariance")]
    public class PoseWithCovariance
    {
        public Pose pose;
        public double[] covariance;  // float64[36] covariance
    }

    [MessageType("geometry_msgs/Quaternion")]
    public class Quaternion
    {
        public double x;
        public double y;
        public double z;
        public double w;
    }

    [MessageType("nmea_msgs/Sentence")]
    public class Sentence
    {
        public Header header;
        public string sentence;
    }

    [MessageType("geometry_msgs/Twist")]
    public class Twist
    {
        public Vector3 linear;
        public Vector3 angular;
    }

    [MessageType("geometry_msgs/TwistStamped")]
    public class TwistStamped
    {
        public Header header;
        public Twist twist;
    }

    [MessageType("geometry_msgs/TwistWithCovariance")]
    public class TwistWithCovariance
    {
        public Twist twist;
        public double[] covariance;  // float64[36] covariance
    }

    [MessageType("geometry_msgs/Vector3")]
    public class Vector3
    {
        public double x;
        public double y;
        public double z;
    }

    [MessageType("std_srvs/Empty")]
    public class Empty
    {
    }

    [MessageType("std_srvs/SetBool")]
    public class SetBool
    {
        public bool data;
    }

    [MessageType("std_srvs/SetBool")]
    public class SetBoolResponse
    {
        public bool success;
        public string message;
    }

    [MessageType("std_srv/Trigger")]
    public class Trigger
    {
        public bool success;
        public string message;
    }
}
