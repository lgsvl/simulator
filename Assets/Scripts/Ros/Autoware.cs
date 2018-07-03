/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿#pragma warning disable 0649

namespace Ros
{
    // Autoware-specific messages
    [MessageType("autoware_msgs/steer_cmd")]
    struct steer_cmd
    {
        public Header header;
        public int steer;
    }

    [MessageType("autoware_msgs/accel_cmd")]
    struct accel_cmd
    {
        public Header header;
        public int accel;
    }

    [MessageType("autoware_msgs/brake_cmd")]
    struct brake_cmd
    {
        public Header header;
        public int brake;
    }

    [MessageType("autoware_msgs/lamp_cmd")]
    struct lamp_cmd
    {
        public Header header;
        public int l;
        public int r;
    }

    [MessageType("autoware_msgs/ControlCommand")]
    struct ControlCommand
    {
        public double linear_velocity;
        public double steering_angle;
    }

    [MessageType("autoware_msgs/VehicleCmd")]
    struct VehicleCmd
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
}
