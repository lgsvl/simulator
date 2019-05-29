using Simulator.Sensors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Simulator.Utilities
{
    public class SensorConfig
    {
        public string Name;
        public Type[] Types;
        public List<SensorParam> parameters = new List<SensorParam>();
    }

    public class SensorParam
    {
        public string Name;
        public string Type;
        public object DefaultValue;
        public string[] Values;
        public float? Min;
        public float? Max;
    }

    public static class SensorTypes
    {
        static Dictionary<Type, string> Typemap = new Dictionary<Type, string>()
        {
            { typeof(int), "int" },
            { typeof(float), "float" },
            { typeof(string), "string" },
        };

        public static List<SensorConfig> ListSensorFields(List<SensorBase> SensorPrefabs)
        {
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
                        var parameters = new List<SensorParam>();
                        var sensorType = sb.GetType().GetCustomAttribute<SensorType>();
                        if (sensorType == null)
                        {
                            throw new Exception($"Sensor Configuration Error: {sb.GetType().ToString()} is missing SensorType Attribute");
                        }
                        foreach (var info in sb.GetType().GetRuntimeFields().Where(field => field.IsDefined(typeof(SensorParameter), true)))
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
                            else
                            {
                                RangeAttribute range = info.GetCustomAttribute<RangeAttribute>();
                                var f = new SensorParam()
                                {
                                    Name = info.Name,
                                    Type = Typemap[info.FieldType],
                                    DefaultValue = info.GetValue(sb),
                                    Min = range != null ? (float?)range.min : null,
                                    Max = range != null ? (float?)range.max : null,
                                };

                                parameters.Add(f);
                            }
                        }

                        sensorData.Add(new SensorConfig()
                        {
                            Name = sensorType.Name,
                            Types = sensorType.RequiredTypes,
                            parameters = parameters,
                        });
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
