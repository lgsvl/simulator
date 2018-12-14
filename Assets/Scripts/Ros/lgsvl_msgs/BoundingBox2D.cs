/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


using System.Collections.Generic;

namespace Ros
{
    [MessageType("lgsvl_msgs/BoundingBox2D")]
    public struct BoundingBox2D
    {
        public float x;
        public float y;
        
        public float width;
        public float height;
    }
}