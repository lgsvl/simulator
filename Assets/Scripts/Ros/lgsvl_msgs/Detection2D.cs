/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


using System.Collections.Generic;

namespace Ros
{
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
}