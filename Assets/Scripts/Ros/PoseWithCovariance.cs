/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


 namespace Ros
 {
     [MessageType("geometry_msgs/PoseWithCovariance")]
     public struct PoseWithCovariance
     {
         public Pose pose;
         public double[] covariance;
     }
 }