/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


using System.Collections.Generic;

namespace Ros
{
    [MessageType("lgsvl_msgs/BoundingBox3D")]
    public struct BoundingBox3D
    {
        public Pose position;
        public Vector3 size;
    }
}