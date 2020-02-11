/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

﻿#pragma warning disable 0649

using System.Collections.Generic;

namespace Simulator.Bridge.Ros.Autoware
{
    // Autoware-specific messages
    [MessageType("autoware_msgs/steer_cmd")]
    public class steer_cmd
    {
        public Header header;
        public int steer;
    }

    [MessageType("autoware_msgs/accel_cmd")]
    public class accel_cmd
    {
        public Header header;
        public int accel;
    }

    [MessageType("autoware_msgs/brake_cmd")]
    public class brake_cmd
    {
        public Header header;
        public int brake;
    }

    [MessageType("autoware_msgs/lamp_cmd")]
    public class lamp_cmd
    {
        public Header header;
        public int l;
        public int r;
    }

    [MessageType("autoware_msgs/ControlCommand")]
    public class ControlCommand
    {
        public double linear_velocity;
        public double linear_acceleration;
        public double steering_angle;
    }

    [MessageType("autoware_msgs/VehicleCmd")]
    public class VehicleCmd
    {
        public Header header;
        public steer_cmd _steer_cmd;
        public accel_cmd _accel_cmd;
        public brake_cmd _brake_cmd;
        public lamp_cmd _lamp_cmd;
        public uint gear;
        public uint mode;
        public TwistStamped twist_cmd;
        public ControlCommand ctrl_cmd;
        public uint emergency;
    }

    [MessageType("autoware_msgs/DetectedObject")]
    public class DetectedObject
    {
        public Header header;
        public uint id;
        public string label;
        public double score;  // Score as defined by the detection, Optional
        public ColorRGBA color;  // Define this object specific color

        // 3D Bounding Box
        public string space_frame;  // 3D Space coordinate frame of the object, required if pose and dimensions are defined
        public Pose pose;
        public Vector3 dimensions;
        public Vector3 variance;
        public Twist velocity;
        public Twist acceleration;

        public PointCloud2 pointcloud;
        
        // public PolygonStamped convex_hull;
        // public LaneArray candidate_trajectories;

        public bool pose_reliable;
        public bool velocity_reliable;
        public bool acceleration_reliable;

        // 2D Rect
        public string image_frame;
        public int x;
        public int y;
        public int width;
        public int height;
        public double angle;

        public Image roi_image;

        // Indicator information
        public uint indicator_state;  // INDICATOR_LEFT = 0, INDICATOR_RIGHT = 1, INDICATOR_BOTH = 2, INDICATOR_NONE = 3

        // Behavior state of the detected object
        public uint behavior_state;  // FORWARD_STATE = 0, STOPPING_STATE = 1, BRANCH_LEFT_STATE = 2, BRANCH_RIGHT_STATE = 3, YIELDING_STATE = 4, ACCELERATING_STATE = 5, SLOWDOWN_STATE = 6

        public List<string> user_defined_info;
    }

    [MessageType("autoware_msgs/DetectedObjectArray")]
    public class DetectedObjectArray
    {
        public Header header;
        public DetectedObject[] objects;
    }

    [MessageType("autoware_auto_msgs/VehicleControlCommand")]
    public class VehicleControlCommand
    {
        public Time stamp;
        public float long_accel_mps2;
        public float front_wheel_angle_rad;
        public double rear_wheel_angle_rad;
    }

    [MessageType("autoware_auto_msgs/VehicleStateCommand")]
    public class VehicleStateCommand
    {
        public Time stamp;
        public byte blinker;
        public byte headlight;
        public byte wiper;
        public byte gear;
        public byte mode;
        public bool hand_brake;
        public bool horn;
        public bool autonomous;
    }

    [MessageType("autoware_auto_msgs/VehicleStateReport")]
    public class VehicleStateReport
    {
        public Time stamp;
        public byte fuel;
        public byte blinker;
        public byte headlight;
        public byte wiper;
        public byte gear;
        public byte mode;
        public bool hand_brake;
        public bool horn;
    }

    [MessageType("autoware_auto_msgs/VehicleOdometry")]
    public class VehicleOdometry
    {
        public Time stamp;
        public float velocity_mps;
        public float front_wheel_angle_rad;
        public float rear_wheel_angle_rad;
    }

    [MessageType("autoware_auto_msgs/RawControlCommand")]
    public class RawControlCommand
    {
        public Time stamp;
        public uint throttle;
        public uint brake;
        public int front_steer;
        public int rear_steer;
    }
}
