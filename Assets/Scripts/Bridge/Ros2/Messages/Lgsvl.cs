/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using Simulator.Bridge.Ros2.Ros;

namespace Simulator.Bridge.Ros2.Lgsvl
{
    public enum Gear: byte
    {
        GEAR_NEUTRAL = 0,
        GEAR_DRIVE = 1,
        GEAR_REVERSE = 2,
        GEAR_PARKING = 3,
        GEAR_LOW = 4,
    }

    public enum BlinkerState: byte
    {
        BLINKERS_OFF = 0,
        BLINKERS_LEFT = 1,
        BLINKERS_RIGHT = 2,
        BLINKERS_HAZARD = 3,
    }

    public enum HeadlightState: byte
    {
        HEADLIGHTS_OFF = 0,
        HEADLIGHTS_LOW = 1,
        HEADLIGHTS_HIGH = 2,
    }

    public enum WiperState: byte
    {
        WIPERS_OFF = 0,
        WIPERS_LOW = 1,
        WIPERS_MED = 2,
        WIPERS_HIGH = 3,
    }

    public enum VehicleMode: byte
    {
        VEHICLE_MODE_COMPLETE_MANUAL = 0,
        VEHICLE_MODE_COMPLETE_AUTO_DRIVE = 1,
        VEHICLE_MODE_AUTO_STEER_ONLY = 2,
        VEHICLE_MODE_AUTO_SPEED_ONLY = 3,
        VEHICLE_MODE_EMERGENCY_MODE = 4,
    }

    public enum ObjectState
    {
        STATE_MOVING = 0,
        STATE_STATIONARY = 1,
    }

    [MessageType("lgsvl_msgs/BoundingBox2D")]
    public struct BoundingBox2D
    {
        public float x;
        public float y;
        public float width;
        public float height;
    }

    [MessageType("lgsvl_msgs/BoundingBox3D")]
    public struct BoundingBox3D
    {
        public Pose position;
        public Vector3 size;
    }

    [MessageType("lgsvl_msgs/Detection2D")]
    public struct Detection2D
    {
        public Header header;
        public uint id;
        public string label;
        public float score;
        public BoundingBox2D bbox;
        public Twist velocity;
    }

    [MessageType("lgsvl_msgs/Detection2DArray")]
    public struct Detection2DArray
    {
        public Header header;
        public List<Detection2D> detections;
    }

    [MessageType("lgsvl_msgs/Detection3D")]
    public struct Detection3D
    {
        public Header header;
        public uint id;
        public string label;
        public float score;
        public BoundingBox3D bbox;
        public Twist velocity;
    }

    [MessageType("lgsvl_msgs/Detection3DArray")]
    public struct Detection3DArray
    {
        public Header header;
        public List<Detection3D> detections;
    }

    [MessageType("lgsvl_msgs/Signal")]
    public struct Signal
    {
        public Header header;
        public uint id;
        public string label;
        public float score;
        public BoundingBox3D bbox;
    }

    [MessageType("lgsvl_msgs/SignalArray")]
    public struct SignalArray
    {
        public Header header;
        public List<Signal> signals;
    }

    [MessageType("lgsvl_msgs/CanBusData")]
    public struct CanBusData
    {
        public Header header;

        public float speed_mps;
        public float throttle_pct;  // 0 to 1
        public float brake_pct;     // 0 to 1
        public float steer_pct;     // -1 to 1
        public bool parking_brake_active;
        public bool high_beams_active;
        public bool low_beams_active;
        public bool hazard_lights_active;
        public bool fog_lights_active;
        public bool left_turn_signal_active;
        public bool right_turn_signal_active;
        public bool wipers_active;
        public bool reverse_gear_active;
        public Gear selected_gear;
        public bool engine_active;
        public float engine_rpm;
        public double gps_latitude;
        public double gps_longitude;
        public double gps_altitude;
        public Quaternion orientation;
        public Vector3 linear_velocities;
    }

    [MessageType("lgsvl_msgs/VehicleControlData")]
    public struct VehicleControlData
    {
        public Header header;

        public float acceleration_pct;          // 0 to 1
        public float braking_pct;               // 0 to 1
        public float target_wheel_angle;        // radians
        public float target_wheel_angular_rate; // radians / second
        public Gear target_gear;
    }

    [MessageType("lgsvl_msgs/VehicleStateData")]
    public struct VehicleStateData
    {
        public Header header;

        public BlinkerState blinker_state;
        public HeadlightState headlight_state;
        public WiperState wiper_state;
        public Gear current_gear;
        public VehicleMode vehicle_mode;
        public bool hand_brake_active;
        public bool horn_active;
        public bool autonomous_mode_active;
    }

    [MessageType("lgsvl_msgs/DetectedRadarObject")]
    public struct DetectedRadarObject
    {
        public int id;

        public Vector3 sensor_aim;
        public Vector3 sensor_right;
        public Point sensor_position;
        public Vector3 sensor_velocity;
        public double sensor_angle;

        public Point object_position;
        public Vector3 object_velocity;
        public Point object_relative_position;
        public Vector3 object_relative_velocity;
        public Vector3 object_collider_size;
        public byte object_state;
        public bool new_detection;
    }

    [MessageType("lgsvl_msgs/DetectedRadarObjectArray")]
    public struct DetectedRadarObjectArray
    {
        public Header header;

        public List<DetectedRadarObject> objects;
    }

    [MessageType("lgsvl_msgs/Ultrasonic")]
    public struct Ultrasonic
    {
        public Header header;

        public float minimum_distance;
    }
}
