using System.Collections.Generic;

namespace Ros
{
    [MessageType("pb_msgs/Header")]
    public struct ApolloHeader
    {
        public double timestamp_sec;
        public string module_name;
        public int sequence_num;
        public int lidar_timestamp;
        public int camera_timestamp;
        public int radar_timestamp;
        public int version;
    }

    [MessageType("pb_msgs/Point3D")]
    public struct Point3D
    {
        public double x;
        public double y;
	    public double z;
    }

    [MessageType("pb_msgs/Imu")]
    public struct Imu
    {
        public ApolloHeader header;
        public double measurement_time;
        public float measurement_span;
        public Point3D linear_acceleration;
        public Point3D angular_velocity;
    }

    [MessageType("pb_msgs/GnssBestPose")]
    public struct GnssBestPose
    {
        public ApolloHeader header;
        public double measurement_time;
        public int sol_status;
        public int sol_type;

        public double latitude;  // in degrees
        public double longitude;  // in degrees
        public double height_msl;  // height above mean sea level in meters
        public float undulation;  // undulation = height_wgs84 - height_msl
        public int datum_id;  // datum id number
        public float latitude_std_dev;  // latitude standard deviation (m)
        public float longitude_std_dev;  // longitude standard deviation (m)
        public float height_std_dev;  // height standard deviation (m)
        public string base_station_id;  // base station id
        public float differential_age;  // differential position age (sec)
        public float solution_age;  // solution age (sec)
        public int num_sats_tracked;  // number of satellites tracked
        public int num_sats_in_solution;  // number of satellites used in solution
        public int num_sats_l1;  // number of L1/E1/B1 satellites used in solution
        public int num_sats_multi;  // number of multi-frequency satellites used in solution
        public int reserved;  // reserved
        public int extended_solution_status;  // extended solution status - OEMV and
                                                    // greater only
        public int galileo_beidou_used_mask;
        public int gps_glonass_used_mask;
    }

    [MessageType("pb_msgs/Quaternion")]
    public struct ApolloQuaternion
    {
        public double qx;
        public double qy;
        public double qz;
        public double qw;
    }

    // A point in the map reference frame. The map defines an origin, whose
    // coordinate is (0, 0, 0).
    // Most modules, including localization, perception, and prediction, generate
    // results based on the map reference frame.
    // Currently, the map uses Universal Transverse Mercator (UTM) projection. See
    // the link below for the definition of map origin.
    //   https://en.wikipedia.org/wiki/Universal_Transverse_Mercator_coordinate_system
    // The z field of PointENU can be omitted. If so, it is a 2D location and we do
    // not care its height.
    [MessageType("pb_msgs/PointENU")]
    public struct PointENU {
      public double x;  // East from the origin, in meters.
      public double y;  // North from the origin, in meters.
      public double z;  // Up from the WGS-84 ellipsoid, in
                        // meters.
    }

    [MessageType("pb_msgs/Pose")]
    public struct ApolloPose
    {
        // Position of the vehicle reference point (VRP) in the map reference frame.
        // The VRP is the center of rear axle.
        public PointENU position;        // GPS

        // A quaternion that represents the rotation from the IMU coordinate
        // (Right/Forward/Up) to the
        // world coordinate (East/North/Up).
        public ApolloQuaternion orientation;   // GPS

        // Linear velocity of the VRP in the map reference frame.
        // East/north/up in meters per second.
        public Point3D linear_velocity;  // GPS

        // Linear acceleration of the VRP in the map reference frame.
        // East/north/up in meters per second.
        public Point3D linear_acceleration; //// IMU

        // Angular velocity of the vehicle in the map reference frame.
        // Around east/north/up axes in radians per second.
        public Point3D angular_velocity; //// IMU

        // Heading
        // The heading is zero when the car is facing East and positive when facing North.
        public double heading;

        // Linear acceleration of the VRP in the vehicle reference frame.
        // Right/forward/up in meters per square second.
        public Point3D linear_acceleration_vrf;

        // Angular velocity of the VRP in the vehicle reference frame.
        // Around right/forward/up axes in radians per second.
        public Point3D angular_velocity_vrf;

        // Roll/pitch/yaw that represents a rotation with intrinsic sequence z-x-y.
        // in world coordinate (East/North/Up)
        // The roll, in (-pi/2, pi/2), corresponds to a rotation around the y-axis.
        // The pitch, in [-pi, pi), corresponds to a rotation around the x-axis.
        // The yaw, in [-pi, pi), corresponds to a rotation around the z-axis.
        // The direction of rotation follows the right-hand rule.
        public Point3D euler_angles; //// IMU
    }

    [MessageType("pb_msgs/Gps")]
    public struct Gps
    {
        public ApolloHeader header;

        // Localization message: from GPS or localization
        public ApolloPose localization;
    }

    [MessageType("pb_msgs/CorrectedImu")]
    public struct CorrectedImu
    {
        public ApolloHeader header;

        // Inertial Measurement Unit(IMU)
        public ApolloPose imu;
    }

    namespace Apollo
    {
        namespace Drivers
        {
            namespace Conti_Radar
            {
                public enum OutputType
                {
                    OUTPUT_TYPE_NONE = 0,
                    OUTPUT_TYPE_OBJECTS = 1,
                    OUTPUT_TYPE_CLUSTERS = 2,
                    OUTPUT_TYPE_ERROR = 3,
                }

                public enum RcsThreshold
                {
                    RCS_THRESHOLD_STANDARD = 0,
                    RCS_THRESHOLD_HIGH_SENSITIVITY = 1,
                    RCS_THRESHOLD_ERROR = 2,
                }
            }

            public struct ContiRadarObs
            {
                public ApolloHeader header;
                public bool clusterortrack;
                public int obstacle_id;
                public double longitude_dist;
                public double lateral_dist;
                public double longitude_vel;
                public double lateral_vel;
                public double rcs;
                public int dynprop;
                public double longitude_dist_rms;
                public double lateral_dist_rms;
                public double longitude_vel_rms;
                public double lateral_vel_rms;
                public double probexist;
                public int meas_state;
                public double longitude_accel;
                public double lateral_accel;
                public double oritation_angle;
                public double longitude_accel_rms;
                public double lateral_accel_rms;
                public double oritation_angle_rms;
                public double length;
                public double width;
                public int obstacle_class;
            }

            public struct ClusterListStatus_600
            {
                public int near;
                public int far;
                public int meas_counter;
                public int interface_version;
            }
            public struct ObjectListStatus_60A
            {
                public int nof_objects;
                public int meas_counter;
                public int interface_version;
            }
                        
            public struct RadarState_201
            {                
                public uint max_distance;
                public uint radar_power;
                public Conti_Radar.OutputType output_type;
                public Conti_Radar.RcsThreshold rcs_threshold;
                public bool send_quality;
                public bool send_ext_info;
            }

            [MessageType("pb_msgs/ContiRadar")]
            public struct ContiRadar
            {
                public ApolloHeader header;
                public List<ContiRadarObs> contiobs;
                public RadarState_201 radar_state;//
                public ClusterListStatus_600 cluster_list_status;//
                public ObjectListStatus_60A object_list_status;///
            }
        }
    }
}
