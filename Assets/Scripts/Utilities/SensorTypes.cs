/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Simulator.Sensors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Simulator.Sensors.Postprocessing;

namespace Simulator.Utilities
{
    public class SensorConfig
    {
        public string Name;
        public string AssetGuid;
        public string[] Types;
        public List<SensorParam> Parameters;
    }

    public class SensorParam
    {
        public string Name;
        public string Type;
        public object DefaultValue;
        public string[] Values;
        public float? Min;
        public float? Max;
        public string Unit;
    }

    public static class SensorTypes
    {
        // when updating this, please update also SetupSensors in AgentManager
        static readonly Dictionary<Type, string> Typemap = new Dictionary<Type, string>()
        {
            { typeof(bool), "bool" },
            { typeof(int), "int" },
            { typeof(float), "float" },
            { typeof(string), "string" },
        };

        public static SensorConfig GetConfig(object sb)
        {
            var parameters = new List<SensorParam>();
            var sensorType = sb.GetType().GetCustomAttribute<SensorType>();
            if (sensorType == null)
            {
                throw new Exception($"Sensor Configuration Error: {sb.GetType().ToString()} is missing SensorType Attribute");
            }

            var sbType = sb.GetType();

            foreach (var info in sbType.GetRuntimeFields().Where(field => field.IsDefined(typeof(SensorParameter), true)))
            {
                if (info.FieldType.IsEnum)
                {
                    int i = (int)info.GetValue(sb);
                    var f = new SensorParam()
                    {
                        Name = info.Name,
                        Type = "enum",
                        DefaultValue = Enum.GetNames(info.FieldType)[(int)info.GetValue(sb)],
                        Values = Enum.GetNames(info.FieldType),
                    };

                    parameters.Add(f);
                }
                else if (info.FieldType == typeof(Color))
                {
                    var value = info.GetValue(sb);
                    var f = new SensorParam()
                    {
                        Name = info.Name,
                        Type = "color",
                        DefaultValue = value == null ? null : "#" + ColorUtility.ToHtmlStringRGBA((Color)value),
                    };

                    parameters.Add(f);
                }
                else if (info.FieldType.IsGenericType && info.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var type = info.FieldType;
                    var attr = info.GetCustomAttribute<SensorParameter>();

                    var f = new SensorParam()
                    {
                        Name = info.Name,
                        Type = type.Name,
                        DefaultValue = attr?.GetDefaultInstanceJToken(type)
                    };

                    parameters.Add(f);
                }
                else
                {
                    SensorParam f;
                    if (!Typemap.ContainsKey(info.FieldType))
                    {
                        var type = info.FieldType;
                        var attr = info.GetCustomAttribute<SensorParameter>();

                        f = new SensorParam()
                        {
                            Name = info.Name,
                            Type = type.Name,
                            DefaultValue = attr?.GetDefaultInstanceJToken(type)
                        };
                    }
                    else
                    {
                        var range = info.GetCustomAttribute<RangeAttribute>();
                        f = new SensorParam()
                        {
                            Name = info.Name,
                            Type = Typemap[info.FieldType],
                            DefaultValue = info.GetValue(sb),
                            Min = range != null ? (float?) range.min : null,
                            Max = range != null ? (float?) range.max : null,
                        };
                    }

                    parameters.Add(f);
                }
            }

            if (typeof(CameraSensorBase).IsAssignableFrom(sbType))
            {
                const string ppName = DefaultPostprocessingAttribute.PostprocessingFieldName;
                if (parameters.Any(x => x.Name == ppName))
                    throw new Exception($"Serialized `{ppName}` field marked with {nameof(SensorParameter)} attribute conflicts with postprocessing data for {typeof(CameraSensorBase)}.");

                var ppAttr = sbType.GetCustomAttribute<DefaultPostprocessingAttribute>();
                var param = new SensorParam
                {
                    Name = ppName,
                    Type = typeof(List<PostProcessData>).Name,
                    DefaultValue = ppAttr != null ? ppAttr.GetDefaultInstanceJToken() : DefaultPostprocessingAttribute.GetEmptyInstanceJToken()
                };

                parameters.Add(param);
            }

            return new SensorConfig()
            {
                Name = sensorType.Name,
                Types = sensorType.RequiredTypes.Select(t => t.ToString()).ToArray(),
                Parameters = parameters,
            };
        }

        public static List<SensorConfig> ListSensorFields(List<SensorBase> SensorPrefabs)
        {
            if (SensorPrefabs == null)
            {
                return new List<SensorConfig>();
            }

            var sensorData = new List<SensorConfig>();
            foreach (var go in SensorPrefabs)
            {
                try
                {
                    var sensors = go.gameObject.GetComponents<SensorBase>();
                    if (sensors.Length != 1)
                    {
                        throw new Exception($"Sensor Configuration Error: {go.name} has {sensors.Length} sensors rather than 1");
                    }
                    foreach (var sb in sensors)
                    {
                        sensorData.Add(GetConfig(sb));
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            return sensorData;
        }
    }
}
