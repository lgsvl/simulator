/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


 namespace Ros
 {
     [MessageType("geometry_msgs/TwistWithCovariance")]
     public struct TwistWithCovariance
     {
         public Twist twist;
         public double[] covariance;
     }
 }