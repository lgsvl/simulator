/**
 * Copyright (c) 2019-2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

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
        protected object DefaultInstance;
        private readonly string DefaultJson;

        public SensorParameter()
        {
        }

        /// <summary>
        /// Defines default instance for field with this attribute as deserialized JSON object.
        /// </summary>
        /// <param name="defaultJson">JSON representing object matching the field type.</param>
        public SensorParameter(string defaultJson)
        {
            DefaultJson = defaultJson;
        }

        /// <summary>
        /// Defines default instance for field with this attribute as newly instantiated object.
        /// </summary>
        /// <param name="defaultItemType">Type of the object.</param>
        /// <param name="constructorArguments">Constructor arguments.</param>
        public SensorParameter(Type defaultItemType, params object[] constructorArguments)
        {
            DefaultInstance = Activator.CreateInstance(defaultItemType, constructorArguments);
        }

        private string GetDefaultInstanceJson(Type fieldType)
        {
            if (DefaultJson != null)
                return DefaultJson;

            object instance;

            if (DefaultInstance == null)
            {
                var constructor = fieldType.GetConstructor(Type.EmptyTypes);

                // No parameterless constructor
                if (constructor == null)
                    return null;

                instance = Activator.CreateInstance(fieldType);
            }
            else
                instance = DefaultInstance;

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(instance, fieldType,
                Newtonsoft.Json.Formatting.None,
                new Newtonsoft.Json.JsonSerializerSettings {TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto});

            // Auto type name handling is used only in sensor configuration. $type field will be stripped when
            // simulation config is deserialized, before actual sensor config is parsed. Built-in field is renamed to
            // something else to prevent this.
            return json.Replace("$type", "$_type");
        }

        /// <summary>
        /// Returns default instance as defined by this attribute, represented as <see cref="Newtonsoft.Json.Linq.JToken"/> object.
        /// </summary>
        /// <param name="fieldType">Type of the field using this attribute.</param>
        /// <returns></returns>
        public Newtonsoft.Json.Linq.JToken GetDefaultInstanceJToken(Type fieldType)
        {
            var json = GetDefaultInstanceJson(fieldType);
            return json == null ? null : Newtonsoft.Json.Linq.JToken.Parse(json);
        }

        /// <summary>
        /// Returns default instance as defined by this attribute.
        /// </summary>
        /// <param name="sensorType">Type of the sensor using this attribute.</param>
        /// <param name="fieldType">Type of the field using this attribute.</param>
        /// <returns></returns>
        public object GetDefaultInstance(Type sensorType, Type fieldType)
        {
            if (DefaultInstance != null)
                return DefaultInstance;

            var jObjStr = GetDefaultInstanceJson(fieldType);
            if (jObjStr == null)
                return null;

            // Revert change from GetDefaultInstanceJson
            jObjStr = jObjStr.Replace("$_type", "$type");

            // If sensor's assembly is named after its type, we're loading asset bundle with renamed assembly
            // Type was serialized before renaming - update types from that assembly to reflect this change
            if (sensorType.Assembly.GetName().Name == sensorType.Name)
                jObjStr = Regex.Replace(jObjStr, "(\"\\$type\"\\:[^,]*, )(Simulator.Sensors)", $"$1{sensorType.Name}");

            var instance = Newtonsoft.Json.JsonConvert.DeserializeObject(jObjStr, fieldType,
                new Newtonsoft.Json.JsonSerializerSettings {TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto});

            return instance;
        }
    }

    public class SensorListParameter : SensorParameter
    {
        /// <summary>
        /// Defines default instance for field with this attribute as list populated with default values of given types.
        /// </summary>
        /// <param name="defaultListType">Type of the field using this attribute.</param>
        /// <param name="defaultElementTypes">Types of elements that the list will be populated with. Default values will be used.</param>
        /// <exception cref="Exception"><see cref="defaultListType"/> is not a generic list.</exception>
        public SensorListParameter(Type defaultListType, params Type[] defaultElementTypes)
        {
            if (!defaultListType.IsGenericType || defaultListType.GetGenericTypeDefinition() != typeof(List<>))
                throw new Exception($"{nameof(defaultListType)} argument in {nameof(SensorListParameter)} has to be a generic list.");

            var instance = Activator.CreateInstance(defaultListType);
            var addMethod = instance.GetType().GetMethod("Add");

            foreach (var itemType in defaultElementTypes)
            {
                var item = Activator.CreateInstance(itemType);
                addMethod?.Invoke(instance, new[] {item});
            }

            DefaultInstance = instance;
        }

        /// <summary>
        /// Defines default instance for field with this attribute as bool list populated with given default values.
        /// </summary>
        /// <param name="defaultValues">Values that the list will be populated with.</param>
        public SensorListParameter(params bool[] defaultValues)
        {
            DefaultInstance = defaultValues.ToList();
        }

        /// <summary>
        /// Defines default instance for field with this attribute as int list populated with given default values.
        /// </summary>
        /// <param name="defaultValues">Values that the list will be populated with.</param>
        public SensorListParameter(params int[] defaultValues)
        {
            DefaultInstance = defaultValues.ToList();
        }

        /// <summary>
        /// Defines default instance for field with this attribute as float list populated with given default values.
        /// </summary>
        /// <param name="defaultValues">Values that the list will be populated with.</param>
        public SensorListParameter(params float[] defaultValues)
        {
            DefaultInstance = defaultValues.ToList();
        }

        /// <summary>
        /// Defines default instance for field with this attribute as string list populated with given default values.
        /// </summary>
        /// <param name="defaultValues">Values that the list will be populated with.</param>
        public SensorListParameter(params string[] defaultValues)
        {
            DefaultInstance = defaultValues.ToList();
        }
    }

    public enum MeasurementType
    {
        Distance,
        Velocity,
        Acceleration,
        Jerk,
        Angle,
        RightHandPos,
        LeftHandPos,
        Count,
        Fps,
        Input,
        Gear,
        Longitude,
        Latitude,
        Altitude,
        Northing,
        Easting,
        MapURL,
        FilePath,
        Duration,
        Misc,
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
