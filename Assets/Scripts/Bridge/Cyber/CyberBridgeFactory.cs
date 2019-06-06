/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Generic;
using Simulator.Bridge.Data;

namespace Simulator.Bridge.Cyber
{
    public class CyberBridgeFactory : IBridgeFactory
    {
        public string Name => "CyberRT";

        public IEnumerable<Type> SupportedDataTypes => new[]
        {
            typeof(ImageData),
            typeof(PointCloudData),
            typeof(Detected3DObjectData),
            typeof(Detected3DObjectArray),
            typeof(CanBusData),
            typeof(GpsData),
            typeof(GpsOdometryData),
            typeof(GpsInsData),
            typeof(VehicleControlData),
        };

        public IBridge Create() => new Bridge();
    }
}
