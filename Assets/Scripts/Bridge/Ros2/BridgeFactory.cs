/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Generic;
using Simulator.Bridge.Data;

namespace Simulator.Bridge.Ros2
{
    public class BridgeFactory : IBridgeFactory
    {
        public string Name => "ROS2";

        public IEnumerable<Type> SupportedDataTypes => new[]
        {
            typeof(ImageData),
            typeof(PointCloudData),
            typeof(Detected3DObjectData),
            typeof(Detected3DObjectArray),
            typeof(Detected2DObjectData),
            typeof(Detected2DObjectArray),
            typeof(CanBusData),
            typeof(GpsData),
            typeof(GpsOdometryData),
            typeof(VehicleControlData),
            typeof(ImuData),
            typeof(VehicleStateData),
        };

        public IBridge Create() => new Bridge();
    }
}
