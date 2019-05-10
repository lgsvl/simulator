/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;

namespace Simulator.Bridge.Ros.LGSVL
{
    [MessageType("lgsvl_srvs/Int")]
    public struct Int
    {
        public int data;
    }

    [MessageType("lgsvl_srvs/String")]
    public struct String
    {
        public string str;
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
        public double score;

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
        public double score;

        public BoundingBox3D bbox;
        public Twist velocity;
    }

    [MessageType("lgsvl_msgs/Detection3DArray")]
    public struct Detection3DArray
    {
        public Header header;
        public List<Detection3D> detections;
    }
}
