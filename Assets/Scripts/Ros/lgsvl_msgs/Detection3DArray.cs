/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


using System.Collections.Generic;

namespace Ros
{
    [MessageType("lgsvl_msgs/Detection3DArray")]
    public struct Detection3DArray
    {
        public Header header;
        public List<Detection3D> detections;
    }
}