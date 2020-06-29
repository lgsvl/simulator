/**
 * Copyright(c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Generic;

namespace Simulator.Bridge.Ros2
{
    static class Ros2Utils
    {
        static readonly Dictionary<Type, string> BuiltinMessageTypes = new Dictionary<Type, string>
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

        public static string GetMessageType<BridgeType>()
        {
            var type = typeof(BridgeType);

            string name;
            if (BuiltinMessageTypes.TryGetValue(type, out name))
            {
                return name;
            }

            object[] attributes = type.GetCustomAttributes(typeof(MessageTypeAttribute), false);
            if (attributes == null || attributes.Length == 0)
            {
                throw new Exception($"Type {type.Name} does not have {nameof(MessageTypeAttribute)} attribute");
            }

            var attribute = attributes[0] as MessageTypeAttribute;
            return attribute.Type;
        }
    }
}
