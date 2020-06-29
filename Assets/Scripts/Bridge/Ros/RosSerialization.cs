/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using SimpleJSON;

namespace Simulator.Bridge.Ros
{
    // fill either Array & Length, or set Base64 string
    public class PartialByteArray
    {
        public byte[] Array;
        public int Length;

        public string Base64;
    }

    static class RosSerialization
    {
        public static readonly Dictionary<Type, string> BuiltinMessageTypes = new Dictionary<Type, string>
        {
            { typeof(bool), "std_msgs/Bool" },
            { typeof(sbyte), "std_msgs/Int8" },
            { typeof(short), "std_msgs/Int16" },
            { typeof(int), "std_msgs/Int32" },
            { typeof(long), "std_msgs/Int64" },
            { typeof(byte), "std_msgs/UInt8" },
            { typeof(ushort), "std_msgs/UInt16" },
            { typeof(uint), "std_msgs/UInt32" },
            { typeof(ulong), "std_msgs/UInt64" },
            { typeof(float), "std_msgs/Float32" },
            { typeof(double), "std_msgs/Float64" },
            { typeof(string), "std_msgs/String" },
        };

        static bool CheckBasicType(Type type)
        {
            if (type.IsNullable())
            {
                type = Nullable.GetUnderlyingType(type);
            }

            if (BuiltinMessageTypes.ContainsKey(type))
            {
                return true;
            }
            if (type == typeof(string))
            {
                return true;
            }
            if (type.IsEnum)
            {
                return true;
            }
            return false;
        }

        static void SerializeInternal(object message, Type type, StringBuilder sb)
        {
            if (type.IsNullable())
            {
                type = Nullable.GetUnderlyingType(type);
            }

            if (type == typeof(string))
            {
                var str = message as string;

                sb.Append('"');
                if (!string.IsNullOrEmpty(str))
                {
                    sb.AppendEscaped(str);
                }
                sb.Append('"');
            }
            else if (type.IsEnum)
            {
                var etype = type.GetEnumUnderlyingType();
                SerializeInternal(Convert.ChangeType(message, etype), etype, sb);
            }
            else if (BuiltinMessageTypes.ContainsKey(type))
            {
                if (type == typeof(bool))
                {
                    sb.Append(string.Format(CultureInfo.InvariantCulture, "{0}", message).ToLowerInvariant());
                }
                else
                {
                    sb.Append(string.Format(CultureInfo.InvariantCulture, "{0}", message));
                }
            }
            else if (type == typeof(PartialByteArray))
            {
                var arr = message as PartialByteArray;
                sb.Append('"');
                if (arr.Base64 == null)
                {
                    sb.Append(Convert.ToBase64String(arr.Array, 0, arr.Length));
                }
                else
                {
                    sb.Append(arr.Base64);
                }
                sb.Append('"');
            }
            else if (type.IsArray)
            {
                if (type.GetElementType() == typeof(byte))
                {
                    sb.Append('"');
                    sb.Append(Convert.ToBase64String((byte[])message));
                    sb.Append('"');
                }
                else
                {
                    var array = message as Array;
                    var elementType = type.GetElementType();
                    sb.Append('[');
                    for (int i = 0; i < array.Length; i++)
                    {
                        SerializeInternal(array.GetValue(i), elementType, sb);
                        if (i < array.Length - 1)
                        {
                            sb.Append(',');
                        }
                    }
                    sb.Append(']');
                }
            }
            else if (type.IsGenericList())
            {
                var list = message as IList;
                var elementType = type.GetGenericArguments()[0];
                sb.Append('[');
                for (int i = 0; i < list.Count; i++)
                {
                    SerializeInternal(list[i], elementType, sb);
                    if (i < list.Count - 1)
                    {
                        sb.Append(',');
                    }
                }
                sb.Append(']');
            }
            else if (type == typeof(Ros.Time))
            {
                var t = message as Ros.Time;
                sb.AppendFormat("{{\"secs\":{0},\"nsecs\":{1}}}", (uint)t.secs, (uint)t.nsecs);
            }
            else
            {
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

                sb.Append('{');

                for (int i = 0; i < fields.Length; i++)
                {
                    var field = fields[i];
                    var fieldType = field.FieldType;
                    var fieldValue = field.GetValue(message);
                    if (fieldValue != null)
                    {
                        sb.Append('"');
                        sb.Append(field.Name);
                        sb.Append('"');
                        sb.Append(':');
                        SerializeInternal(fieldValue, fieldType, sb);
                        sb.Append(',');
                    }
                }

                if (sb[sb.Length - 1] == ',')
                {
                    sb.Remove(sb.Length - 1, 1);
                }

                sb.Append('}');
            }
        }

        public static void Serialize(object message, StringBuilder sb)
        {
            var type = message.GetType();
            if (type == typeof(string))
            {
                var str = message as string;
                sb.Append("{");
                sb.Append("\"data\":");
                sb.Append('"');
                sb.AppendEscaped(str);
                sb.Append('"');
                sb.Append('}');
            }
            else if (BuiltinMessageTypes.ContainsKey(type))
            {
                sb.Append("{");
                sb.Append("\"data\":");
                sb.Append(string.Format(CultureInfo.InvariantCulture, "{0}", message));
                sb.Append('}');
            }
            else if (type == typeof(Ros.Time))
            {
                sb.Append("{");
                sb.Append("\"data\":");
                SerializeInternal(message, type, sb);
                sb.Append('}');
            }
            else
            {
                SerializeInternal(message, type, sb);
            }
        }

        static object UnserializeInternal(JSONNode node, Type type)
        {
            if (type == typeof(bool))
            {
                return node.AsBool;
            }
            else if (type == typeof(sbyte))
            {
                return short.Parse(node.Value);
            }
            else if (type == typeof(int))
            {
                return int.Parse(node.Value);
            }
            else if (type == typeof(long))
            {
                return long.Parse(node.Value);
            }
            else if (type == typeof(byte))
            {
                return byte.Parse(node.Value);
            }
            else if (type == typeof(ushort))
            {
                return ushort.Parse(node.Value);
            }
            else if (type == typeof(uint))
            {
                return uint.Parse(node.Value);
            }
            else if (type == typeof(ulong))
            {
                return ulong.Parse(node.Value);
            }
            else if (type == typeof(float))
            {
                return node.AsFloat;
            }
            else if (type == typeof(double))
            {
                return node.AsDouble;
            }
            else if (type == typeof(string))
            {
                return node.Value;
            }
            else if (type == typeof(PartialByteArray))
            {
                var nodeArr = node.AsArray;

                if (type.GetElementType() == typeof(byte) && nodeArr == null)
                {
                    var array = Convert.FromBase64String(node.Value);
                    return new PartialByteArray()
                    {
                        Array = array,
                        Length = array.Length,
                    };
                }
                else
                {
                    var array = new PartialByteArray()
                    {
                        Array = new byte[node.Count],
                        Length = node.Count,
                    };
                    for (int i = 0; i < node.Count; i++)
                    {
                        array.Array[i] = byte.Parse(nodeArr[i].Value);
                    }
                    return array;
                }
            }
            else if (type.IsArray)
            {
                var nodeArr = node.AsArray;

                if (type.GetElementType() == typeof(byte) && nodeArr == null)
                {
                    return Convert.FromBase64String(node.Value);
                }

                var arr = Array.CreateInstance(type.GetElementType(), node.Count);
                for (int i = 0; i < node.Count; i++)
                {
                    arr.SetValue(UnserializeInternal(nodeArr[i], type.GetElementType()), i);
                }
                return arr;
            }
            else if (type == typeof(Ros.Time))
            {
                var nodeObj = node.AsObject;
                return new Ros.Time()
                {
                    secs = uint.Parse(nodeObj["secs"].Value),
                    nsecs = uint.Parse(nodeObj["nsecs"].Value),
                };
            }
            else if (type.IsEnum)
            {
                var value = node.AsInt;
                var obj = Enum.ToObject(type, value);
                return obj;
            }
            else
            {
                var nodeObj = node.AsObject;
                var obj = Activator.CreateInstance(type);
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (nodeObj[field.Name] != null)
                    {
                        var fieldType = field.FieldType;
                        if (fieldType.IsNullable())
                        {
                            fieldType = Nullable.GetUnderlyingType(fieldType);
                        }
                        var value = UnserializeInternal(nodeObj[field.Name], fieldType);
                        field.SetValue(obj, value);

                    }
                }
                return obj;
            }
        }

        public static object Unserialize(JSONNode node, Type type)
        {
            if (BuiltinMessageTypes.ContainsKey(type) || type == typeof(Ros.Time))
            {
                return UnserializeInternal(node["data"], type);
            }
            return UnserializeInternal(node, type);
        }

        public static BridgeType Unserialize<BridgeType>(JSONNode node)
        {
            return (BridgeType)Unserialize(node, typeof(BridgeType));
        }

    }
}
