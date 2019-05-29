/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Generic;
using Simulator.Bridge.Data;

namespace Simulator.Bridge.Ros
{
    public abstract class RosBridgeFactoryBase : IBridgeFactory
    {
        public abstract string Name { get; }

        public IEnumerable<Type> SupportedDataTypes
        {
            get
            {
                return new[]
                {
                    typeof(ImageData),
                    typeof(PointCloudData),
                    typeof(Detected3DObjectData),
                    typeof(Detected3DObjectArray),
                };
            }
        }

        public abstract IBridge Create();
    }

    public class RosBridgeFactory1 : RosBridgeFactoryBase
    {
        public override string Name => "ROS1";
        public override IBridge Create() => new Bridge(1);
    }

    public class RosBridgeFactory2 : RosBridgeFactoryBase
    {
        public override string Name => "ROS2";
        public override IBridge Create() => new Bridge(2);
    }
}
