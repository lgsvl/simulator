﻿/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using global::Apollo;

namespace Ros
{
    [MessageType("pb_msgs/Header")]
    public struct ApolloHeader
    {
        public double? timestamp_sec;
        public string module_name;
        public uint? sequence_num;
        public ulong? lidar_timestamp;
        public ulong? camera_timestamp;
        public ulong? radar_timestamp;
        public uint? version;
    }

    [MessageType("pb_msgs/Point3D")]
    public struct Point3D
    {
        public double? x;
        public double? y;
	    public double? z;
    }

    [MessageType("pb_msgs/Imu")]
    public struct Imu
    {
        public ApolloHeader? header;
        public double? measurement_time;
        public float? measurement_span;
        public Point3D? linear_acceleration;
        public Point3D? angular_velocity;
    }

    [MessageType("pb_msgs/GnssBestPose")]
    public struct GnssBestPose
    {
        public ApolloHeader? header;
        public double? measurement_time;
        public int? sol_status;
        public int? sol_type;

        public double? latitude;  // in degrees
        public double? longitude;  // in degrees
        public double? height_msl;  // height above mean sea level in meters
        public float? undulation;  // undulation = height_wgs84 - height_msl
        public int? datum_id;  // datum id number
        public float? latitude_std_dev;  // latitude standard deviation (m)
        public float? longitude_std_dev;  // longitude standard deviation (m)
        public float? height_std_dev;  // height standard deviation (m)
        public string base_station_id;  // base station id
        public float? differential_age;  // differential position age (sec)
        public float? solution_age;  // solution age (sec)
        public int? num_sats_tracked;  // number of satellites tracked
        public int? num_sats_in_solution;  // number of satellites used in solution
        public int? num_sats_l1;  // number of L1/E1/B1 satellites used in solution
        public int? num_sats_multi;  // number of multi-frequency satellites used in solution
        public int? reserved;  // reserved
        public int? extended_solution_status;  // extended solution status - OEMV and
                                                    // greater only
        public int? galileo_beidou_used_mask;
        public int? gps_glonass_used_mask;
    }

    [MessageType("pb_msgs/Quaternion")]
    public struct ApolloQuaternion
    {
        public double? qx;
        public double? qy;
        public double? qz;
        public double? qw;
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
    public struct PointENU
    {
        public double? x;  // East from the origin, in meters.
        public double? y;  // North from the origin, in meters.
        public double? z;  // Up from the WGS-84 ellipsoid, in
                           // meters.
        public PointENU(double x, double y)
        {
            this.x = x;
            this.y = y;
            this.z = null;
        }

        public PointENU(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }

    [MessageType("pb_msgs/Pose")]
    public struct ApolloPose
    {
        // Position of the vehicle reference point (VRP) in the map reference frame.
        // The VRP is the center of rear axle.
        public PointENU? position;        // GPS

        // A quaternion that represents the rotation from the IMU coordinate
        // (Right/Forward/Up) to the
        // world coordinate (East/North/Up).
        public ApolloQuaternion? orientation;   // GPS

        // Linear velocity of the VRP in the map reference frame.
        // East/north/up in meters per second.
        public Point3D? linear_velocity;  // GPS

        // Linear acceleration of the VRP in the map reference frame.
        // East/north/up in meters per second.
        public Point3D? linear_acceleration; //// IMU

        // Angular velocity of the vehicle in the map reference frame.
        // Around east/north/up axes in radians per second.
        public Point3D? angular_velocity; //// IMU

        // Heading
        // The heading is zero when the car is facing East and positive when facing North.
        public double? heading;

        // Linear acceleration of the VRP in the vehicle reference frame.
        // Right/forward/up in meters per square second.
        public Point3D? linear_acceleration_vrf;

        // Angular velocity of the VRP in the vehicle reference frame.
        // Around right/forward/up axes in radians per second.
        public Point3D? angular_velocity_vrf;

        // Roll/pitch/yaw that represents a rotation with intrinsic sequence z-x-y.
        // in world coordinate (East/North/Up)
        // The roll, in (-pi/2, pi/2), corresponds to a rotation around the y-axis.
        // The pitch, in [-pi, pi), corresponds to a rotation around the x-axis.
        // The yaw, in [-pi, pi), corresponds to a rotation around the z-axis.
        // The direction of rotation follows the right-hand rule.
        public Point3D? euler_angles; //// IMU
    }

    [MessageType("pb_msgs/Gps")]
    public struct Gps
    {
        public ApolloHeader? header;

        // Localization message: from GPS or localization
        public ApolloPose? localization;
    }

    [MessageType("pb_msgs/CorrectedImu")]
    public struct CorrectedImu
    {
        public ApolloHeader? header;

        // Inertial Measurement Unit(IMU)
        public ApolloPose? imu;
    }

    [MessageType("pb_msgs/ControlCommand")]
    public struct control_command
    {
        public ApolloHeader header;
        public double throttle;
        public double brake;
        public double steering_rate;
        public double steering_target;
        public bool parking_brake;
        public double speed;
        public double acceleration;
        public bool reset_model;
        public bool engine_on_off;
        public double trajectory_fraction;
        public Apollo.Chassis.DrivingMode driving_mode;
        public Apollo.Chassis.GearPosition gear_position;
        public Debug debug;
        public Apollo.Common.VehicleSignal signal;
        public LatencyStats latency_stats;
        public PadMessage pad_msg;
        public Apollo.Common.EngageAdvise engage_advice;
        public bool is_in_safe_mode;

        // depricated fields
        public bool left_turn;
        public bool right_turn;
        public bool high_beam;
        public bool low_beam;
        public bool horn;
        public TurnSignal turn_signal;
    }
   
    public struct LatencyStats
    {
        public double total_time_ms;
        public List<double> controller_time_ms;
        public bool total_time_exceeded;
    }

    public enum TurnSignal
    {
        TURN_NONE = 0,
        TURN_LEFT = 1,
        TURN_RIGHT = 2,
    }

    // (TODO) fix these optional fields for control topic.

    public struct Debug
    {
        public SimpleLongitudinalDebug simple_lon_debug;
        public SimpleLateralDebug simple_lat_debug;
        public InputDebug input_debug;
        public SimpleMPCDebug simple_mpc_debug;
    }

    public struct SimpleLongitudinalDebug
    {
        public double station_reference;
        public double station_error;
        public double station_error_limited;
        public double preview_station_error;
        public double speed_reference;
        public double speed_error;
        public double speed_controller_input_limited;
        public double preview_speed_reference;
        public double preview_speed_error;
        public double preview_acceleration_reference;
        public double acceleration_cmd_closeloop;
        public double acceleration_cmd;
        public double acceleration_lookup;
        public double speed_lookup;
        public double calibration_value;
        public double throttle_cmd;
        public double brake_cmd;
        public bool is_full_stop;
        public double slope_offset_compensation;
        public double current_station;
        public double path_remain;
    }

    public struct SimpleLateralDebug
    {
        public double lateral_error;
        public double ref_heading;
        public double heading;
        public double heading_error;
        public double heading_error_rate;
        public double lateral_error_rate;
        public double curvature;
        public double steer_angle;
        public double steer_angle_feedforward;
        public double steer_angle_lateral_contribution;
        public double steer_angle_lateral_rate_contribution;
        public double steer_angle_heading_contribution;
        public double steer_angle_heading_rate_contribution;
        public double steer_angle_feedback;
        public double steering_position;
        public double ref_speed;
        public double steer_angle_limited;   
    }

    public struct InputDebug
    {
        public ApolloHeader localization_header;
        public ApolloHeader canbus_header;
        public ApolloHeader trajectory_header;
    }

    public struct SimpleMPCDebug
    {
        public double lateral_error;
        public double ref_heading;
        public double heading;
        public double heading_error;
        public double heading_error_rate;
        public double lateral_error_rate;
        public double curvature;
        public double steer_angle;
        public double steer_angle_feedforward;
        public double steer_angle_lateral_contribution;
        public double steer_angle_lateral_rate_contribution;
        public double steer_angle_heading_contribution;
        public double steer_angle_heading_rate_contribution;
        public double steer_angle_feedback;
        public double steering_position;
        public double ref_speed;
        public double steer_angle_limited;
        public double station_reference;
        public double station_error;
        public double speed_reference;
        public double speed_error;
        public double acceleration_reference;
        public bool is_full_stop;
        public double station_feedback;
        public double speed_feedback;
        public double acceleration_cmd_closeloop;
        public double acceleration_cmd;
        public double acceleration_lookup;
        public double speed_lookup;
        public double calibration_value;
        public List<double> matrix_q_updated;     // matrix_q_updated_ size = 6
        public List<double> matrix_r_updated;    // matrix_r_updated_ size = 2
    }

    public enum DrivingAction
    {
        STOP = 0,
        START = 1,
        RESET = 2,
    }

    public struct PadMessage
    {
        public ApolloHeader header;
        public Apollo.Chassis.DrivingMode driving_mode;
        public DrivingAction action;
    }

    namespace Apollo
    {   
        namespace Common
        {
            public struct VehicleSignal
            {
                public TurnSignal? turn_signal;
                public bool? high_beam;
                public bool? low_beam;
                public bool? horn;
                public bool? emergency_light;
            }

            public enum Advice 
            {
                UNKNOWN = 0,
                DISALLOW_ENGAGE = 1,
                READY_TO_ENGAGE = 2,
                KEEP_ENGAGED = 3,
                PREPARE_DISENGAGE = 4,
            }

            public struct EngageAdvise
            {
                public Advice? advice;
                public string reason;
            }
        }

        namespace Chassis
        {
            public enum DrivingMode
            {
                COMPLETE_MANUAL = 0,
                COMPLETE_AUTO_DRIVE = 1,
                AUTO_STEER_ONLY = 2,
                AUTO_SPEED_ONLY = 3,
                EMERGENCY_MODE = 4,
            }

            public enum ErrorCode
            {
                NO_ERROR = 0,
                CMD_NOT_IN_PERIOD = 1,
                CHASSIS_ERROR = 2,
                MANUAL_INTERVENTION = 3,
                CHASSIS_CAN_NOT_IN_PERIOD = 4,
                UNKNOWN_ERROR = 5,
            }

            public enum GearPosition
            {
                GEAR_NEUTRAL = 0,
                GEAR_DRIVE = 1,
                GEAR_REVERSE = 2,
                GEAR_PARKING = 3,
                GEAR_LOW = 4,
                GEAR_INVALID = 5,
                GEAR_NONE = 6,
            }

            public struct ChassisGPS
            {
                public double? latitude;
                public double? longitude;
                public bool? gps_valid;
                public int? year;
                public int? month;
                public int? day;
                public int? hours;
                public int? minutes;
                public int? seconds;
                public double? compass_direction;
                public double? pdop;
                public bool? is_gps_fault;
                public bool? is_inferred;
                public double? altitude;
                public double? heading;
                public double? hdop;
                public double? vdop;
                public GpsQuality? quality;
                public int? num_satellites;
                public double? gps_speed;
            }

            public enum GpsQuality {
                FIX_NO = 0,
                FIX_2D = 1,
                FIX_3D = 2,
                FIX_INVALID = 3,
            }


        }

        // Chassis related topic used as feedback for the control module.
        [MessageType("pb_msgs/Chassis")]
        public struct ChassisMsg
        {
            public bool engine_started;
            public float? engine_rpm;
            public float? speed_mps;
            public float? odometer_m;
            public int? fuel_range_m;
            public float? throttle_percentage;
            public float? brake_percentage;
            public float? steering_percentage;
            public float? steering_torque_nm;
            public bool? parking_brake;
            public bool? high_beam_signal;
            public bool? low_beam_signal;
            public bool? left_turn_signal;
            public bool? right_turn_signal;
            public bool? horn;
            public bool? wiper;
            public bool? disengage_status;
            public Chassis.DrivingMode? driving_mode;
            public Chassis.ErrorCode? error_code;
            public Chassis.GearPosition? gear_location;
            public double? steering_timestamp;
            public ApolloHeader? header;
            public int? chassis_error_mask;
            public Common.VehicleSignal? signal;
            public Chassis.ChassisGPS? chassis_gps;
            public Common.EngageAdvise? engage_advice;

        }


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
                public ApolloHeader? header;
                public bool? clusterortrack;
                public int? obstacle_id;
                public double longitude_dist;
                public double lateral_dist;
                public double longitude_vel;
                public double lateral_vel;
                public double? rcs;
                public int? dynprop;
                public double? longitude_dist_rms;
                public double? lateral_dist_rms;
                public double? longitude_vel_rms;
                public double? lateral_vel_rms;
                public double? probexist;
                public int? meas_state;
                public double? longitude_accel;
                public double? lateral_accel;
                public double? oritation_angle;
                public double? longitude_accel_rms;
                public double? lateral_accel_rms;
                public double? oritation_angle_rms;
                public double? length;
                public double? width;
                public int? obstacle_class;
            }

            public struct ClusterListStatus_600
            {
                public int? near;
                public int? far;
                public int? meas_counter;
                public int? interface_version;
            }
            public struct ObjectListStatus_60A
            {
                public int? nof_objects;
                public int? meas_counter;
                public int? interface_version;
            }
                        
            public struct RadarState_201
            {                
                public uint? max_distance;
                public uint? radar_power;
                public Conti_Radar.OutputType? output_type;
                public Conti_Radar.RcsThreshold? rcs_threshold;
                public bool? send_quality;
                public bool? send_ext_info;
            }

            [MessageType("pb_msgs/ContiRadar")]
            public struct ContiRadar
            {
                public ApolloHeader? header;
                public List<ContiRadarObs> contiobs;
                public RadarState_201? radar_state;
                public ClusterListStatus_600? cluster_list_status;
                public ObjectListStatus_60A? object_list_status;
            }
        }
    }
}
