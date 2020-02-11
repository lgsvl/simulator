/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Simulator.Bridge.Data;

namespace Simulator.Bridge.Ros
{
    public abstract class RosBridgeFactoryBase : IBridgeFactory
    {
        public abstract string Name { get; }

        public virtual IEnumerable<Type> SupportedDataTypes => new[]
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
            typeof(EmptySrv),
            typeof(SetBoolSrv),
            typeof(TriggerSrv),
            typeof(VehicleStateData),
        };

        public abstract IBridge Create();
    }

    public class RosBridgeFactory : RosBridgeFactoryBase
    {
        public override string Name => "ROS";
        public override IBridge Create() => new Bridge(1);
    }

    public class RosApolloBridgeFactory : RosBridgeFactoryBase
    {
        public override string Name => "ROS Apollo";
        public override IBridge Create() => new Bridge(1, apollo: true);

        public override IEnumerable<Type> SupportedDataTypes => base.SupportedDataTypes.Concat(new[]
        {
            typeof(DetectedRadarObjectData),
        });
    }

    public class Ros2BridgeFactory : RosBridgeFactoryBase
    {
        public override string Name => "ROS2";
        public override IBridge Create() => new Bridge(2);
    }
}
