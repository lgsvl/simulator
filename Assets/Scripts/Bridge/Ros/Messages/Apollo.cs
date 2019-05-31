/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;

namespace Simulator.Bridge.Ros.Apollo
{
    [MessageType("pb_msgs/Header")]
    public class Header
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
    public class Point3D
    {
        public double? x;
        public double? y;
        public double? z;
    }
   
    [MessageType("pb_msgs/GnssBestPose")]
    public class GnssBestPose
    {
        public Header header;
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
    public class Quaternion
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
    public class PointENU
    {
        public double? x;  // East from the origin, in meters.
        public double? y;  // North from the origin, in meters.
        public double? z;  // Up from the WGS-84 ellipsoid, in meters.
    }

    [MessageType("pb_msgs/Pose")]
    public class Pose
    {
        // Position of the vehicle reference point (VRP) in the map reference frame.
        // The VRP is the center of rear axle.
        public PointENU position;        // GPS

        // A quaternion that represents the rotation from the IMU coordinate
        // (Right/Forward/Up) to the
        // world coordinate (East/North/Up).
        public Quaternion orientation;   // GPS

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
        public double? heading;

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
    public class Gps
    {
        public Header header;

        // Localization message: from GPS or localization
        public Pose localization;
    }

    [MessageType("pb_msgs/CorrectedImu")]
    public class CorrectedImu
    {
        public Header header;

        // Inertial Measurement Unit(IMU)
        public Pose imu;
    }

    [MessageType("pb_msgs/ControlCommand")]
    public class control_command
    {
        public Header header;
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
   
    public class LatencyStats
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

    public class Debug
    {
        public SimpleLongitudinalDebug simple_lon_debug;
        public SimpleLateralDebug simple_lat_debug;
        public InputDebug input_debug;
        public SimpleMPCDebug simple_mpc_debug;
    }

    public class SimpleLongitudinalDebug
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

    public class SimpleLateralDebug
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

    public class InputDebug
    {
        public Header localization_header;
        public Header canbus_header;
        public Header trajectory_header;
    }

    public class SimpleMPCDebug
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

    public class PadMessage
    {
        public Header header;
        public Apollo.Chassis.DrivingMode driving_mode;
        public DrivingAction action;
    }

    [MessageType("pb_msgs/ADCTrajectory")]
    public class ADCTrajectory
    {
        public Header header;
        public double total_path_length;
        public double total_path_time;
        public List<TrajectoryPoint> trajectory_point;
        public Estop estop;
        public PathPoint path_point;
        public bool is_replan;
        public Apollo.Chassis.GearPosition gear;
        public DecisionResult decision;
        public LatencyStats latency_stats;
        public Header routing_header;
        public  Apollo.Planning.Debug debug;
    }

    public class TrajectoryPoint
    {
        public PathPoint path_point;
        public double v;
        public double a;
        public double relative_time;
    }

    public class PathPoint
    {
        public double x;
        public double y;
        public double z;
        public double theta;
        public double kappa;
        public double s;
        public double dkappa;
        public double ddkappa;
        public string lane_id;
        public double x_derivative;
        public double y_derivative;
    }

    public class Estop
    {
        public bool is_estop;
        public string reason;
    }

    public class DecisionResult
    {
        public MainDecision main_decision;
        public ObjectDecisions object_decision;
        public Apollo.Common.VehicleSignal vehicle_signal;
    }

    public class MainDecision
    {
        public MainMissionComplete mission_complete;
        public MainNotReady not_ready;
        public MainParking parking;
    }

    public class MainMissionComplete
    {
        public PointENU stop_point;
        public double stop_heading;
    }

    public class MainNotReady
    {
        public string reason;
    }

    public class MainParking
    {
    }

    public class ObjectDecisions
    {
        public List<ObjectDecision> decisions;
    }

    public class ObjectDecision
    {
        public string id;
        public int perception_id;
        public List<ObjectDecisionType> object_decision;
    }

    public class ObjectDecisionType
    {
        public ObjectIgnore ignore;
        public ObjectStop stop;
        public ObjectFollow follow;
        public ObjectYield yield;
        public ObjectOvertake overtake;
        public ObjectNudge nudge;
        public ObjectSidePass sidepass;
        public ObjectAvoid avoid;
    }

    public class ObjectIgnore
    {
    }

    public class ObjectStop
    {
        public StopReasonCode reason_code;
        public double distance_s;
        public PointENU stop_point;
        public double stop_heading;
        public List<string> wait_for_obstacle;
    }

    public enum StopReasonCode
    {
        STOP_REASON_HEAD_VEHICLE = 1,
        STOP_REASON_DESTINATION = 2,
        STOP_REASON_PEDESTRIAN = 3,
        STOP_REASON_OBSTACLE = 4,
        STOP_REASON_PREPARKING = 5,
        STOP_REASON_SIGNAL = 100, // only for red signal
        STOP_REASON_STOP_SIGN = 101,
        STOP_REASON_YIELD_SIGN = 102,
        STOP_REASON_CLEAR_ZONE = 103,
        STOP_REASON_CROSSWALK = 104,
        STOP_REASON_CREEPER = 105,
        STOP_REASON_REFERENCE_END = 106, // end of the reference_line
        STOP_REASON_YELLOW_SIGNAL = 107, // yellow signal
        STOP_REASON_PULL_OVER = 108, // pull over
    }

    public class ObjectFollow
    {
        public double distance_s;
        public PointENU fence_point;
        public double fence_heading;
    }

    public class ObjectYield
    {
        public double distance_s;
        public PointENU fence_point;
        public double fence_heading;
        public double time_buffer;
    }

    public class ObjectOvertake
    {
        public double distance_s;
        public PointENU fence_point;
        public double fence_heading;
        public double time_buffer;
    }

    public class ObjectNudge
    {
        public NudgeType type;
        public double distance_l;
    }

    public enum NudgeType
    {
        LEFT_NUDGE = 1,  // drive from the left side of the obstacle
        RIGHT_NUDGE = 2,  // drive from the right side of the obstacle
        NO_NUDGE = 3,  // No nudge is set.
    }

    public class ObjectSidePass
    {
        public SidePassType type;
    }
    
    public enum SidePassType
    {
        LEFT = 1,
        RIGHT = 2,
    }

    public class ObjectAvoid
    {
    }

    [MessageType("pb_msgs/Imu")]
    public class Imu
    {
        public Header header;
        public double? measurement_time;
        public float? measurement_span;
        public Point3D linear_acceleration;
        public Point3D angular_velocity;
    }

    namespace Common
    {
        public class StatusPb
        {
            public ErrorCode error_code;
            public string msg;
        }

        public enum ErrorCode
        {
            // No error, reutrns on success.
            OK = 0,

            // Control module error codes start from here.
            CONTROL_ERROR = 1000,
            CONTROL_INIT_ERROR = 1001,
            CONTROL_COMPUTE_ERROR = 1002,

            // Canbus module error codes start from here.
            CANBUS_ERROR = 2000,
            CAN_CLIENT_ERROR_BASE = 2100,
            CAN_CLIENT_ERROR_OPEN_DEVICE_FAILED = 2101,
            CAN_CLIENT_ERROR_FRAME_NUM = 2102,
            CAN_CLIENT_ERROR_SEND_FAILED = 2103,
            CAN_CLIENT_ERROR_RECV_FAILED = 2104,

            // Localization module error codes start from here.
            LOCALIZATION_ERROR = 3000,
            LOCALIZATION_ERROR_MSG = 3100,
            LOCALIZATION_ERROR_LIDAR = 3200,
            LOCALIZATION_ERROR_INTEG = 3300,
            LOCALIZATION_ERROR_GNSS = 3400,

            // Perception module error codes start from here.
            PERCEPTION_ERROR = 4000,
            PERCEPTION_ERROR_TF = 4001,
            PERCEPTION_ERROR_PROCESS = 4002,
            PERCEPTION_FATAL = 4003,

            // Prediction module error codes start from here.
            PREDICTION_ERROR = 5000,

            // Planning module error codes start from here
            PLANNING_ERROR = 6000,

            // HDMap module error codes start from here
            HDMAP_DATA_ERROR = 7000,

            // Routing module error codes
            ROUTING_ERROR = 8000,
            ROUTING_ERROR_REQUEST = 8001,
            ROUTING_ERROR_RESPONSE = 8002,
            ROUTING_ERROR_NOT_READY = 8003,

            // Indicates an input has been exhausted.
            END_OF_INPUT = 9000,

            // HTTP request error codes.
            HTTP_LOGIC_ERROR = 10000,
            HTTP_RUNTIME_ERROR = 10001,

            // Relative Map error codes.
            RELATIVE_MAP_ERROR = 11000, // general relative map error code
            RELATIVE_MAP_NOT_READY = 11001,

            // Driver error codes.
            DRIVER_ERROR_GNSS = 12000,
            DRIVER_ERROR_VELODYNE = 13000,
        }

        public class VehicleSignal
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

        public class EngageAdvise
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

        public class ChassisGPS
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

        public enum GpsQuality
        {
            FIX_NO = 0,
            FIX_2D = 1,
            FIX_3D = 2,
            FIX_INVALID = 3,
        }


    }

    // Chassis related topic used as feedback for the control module.
    [MessageType("pb_msgs/Chassis")]
    public class ChassisMsg
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
        public Header header;
        public int? chassis_error_mask;
        public Common.VehicleSignal signal;
        public Chassis.ChassisGPS chassis_gps;
        public Common.EngageAdvise engage_advice;

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

        public class ContiRadarObs
        {
            public Header header;
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

        public class ClusterListStatus_600
        {
            public int? near;
            public int? far;
            public int? meas_counter;
            public int? interface_version;
        }

        public class ObjectListStatus_60A
        {
            public int? nof_objects;
            public int? meas_counter;
            public int? interface_version;
        }

        public class RadarState_201
        {
            public uint? max_distance;
            public uint? radar_power;
            public Conti_Radar.OutputType? output_type;
            public Conti_Radar.RcsThreshold? rcs_threshold;
            public bool? send_quality;
            public bool? send_ext_info;
        }

        [MessageType("pb_msgs/ContiRadar")]
        public class ContiRadar
        {
            public Header header;
            public List<ContiRadarObs> contiobs;
            public RadarState_201 radar_state;
            public ClusterListStatus_600 cluster_list_status;
            public ObjectListStatus_60A object_list_status;
        }
    }

    namespace Planning
    {
        public class Debug
        {
            public PlanningData planning_data;
        }

        public class PlanningData
        {
            public Apollo.Localization.LocalizationEstimate adc_position;
            public ChassisMsg chassis;
            // public Routing.RoutingResponse routing;
            public TrajectoryPoint init_point;
            // public Path path;
            // public SpeedPlan speed_plan;
            // ....
        }
    }

    namespace Localization
    {
        public class LocalizationEstimate
        {
            public Header header;
            public Pose pose;
            public Uncertainty uncertainty;
            public double measurement_time;
            public List<TrajectoryPoint> trajectory_point;
        }

        public class Uncertainty
        {
            public Point3D position_std_dev;
            public Point3D orientation_std_dev;
            public Point3D linear_velocity_std_dev;
            public Point3D linear_acceleration_std_dev;
            public Point3D angular_velocity_std_dev;
        }
    }

    namespace Routing
    {
        [MessageType("pb_msgs/RoutingRequest")]
        public class RoutingRequest
        {
            public Header header;
            public List<LaneWayPoint> waypoint;
            public List<LaneSegment> blacklisted_lane;
            public List<string> blacklisted_road;
            public bool broadcast;
        }

        public class LaneSegment
        {
            public string id;
            public double start_s;
            public double end_s;
        }

        public class LaneWayPoint
        {
            public string id;
            public double s;
            public PointENU pose;
        }

        [MessageType("pb_msgs/RoutingResponse")]
        public class RoutingResponse
        {
            public Header header;

            public Common.StatusPb status;
        }
    }
}
