/**
 * Copyright (c) 2019-2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */
using System;
namespace Simulator.Utilities
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SensorType : Attribute
    {
        public string Name;
        public Type[] RequiredTypes;

        public SensorType(string name, Type[] requiredTypes)
        {
            Name = name;
            RequiredTypes = requiredTypes;
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class SensorParameter : Attribute
    {
    }
}
