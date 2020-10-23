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

    public enum MeasurementType
    {
        distance,
        velocity,
        acceleration,
        jerk,
        angle,
        rightHandPos,
        leftHandPos,
        count,
        fps,
        Input,
        gear,
        Longitude,
        Latitude,
        altitude,
        northing,
        easting,
        mapURL,
        filePath,
        duration,
        misc,
    }
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class AnalysisMeasurement : Attribute
    {
        public AnalysisMeasurement(MeasurementType type, string name) {
            Type = type;
            Name = name;
        }
        public AnalysisMeasurement(MeasurementType type) {
            Type = type;
            Name = null;
        }
        public string Name { get; private set; }
        public MeasurementType Type { get; private set; }
    }
}
