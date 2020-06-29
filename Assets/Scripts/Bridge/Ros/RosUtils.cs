/**
 * Copyright(c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Text;
using System.Collections.Generic;

namespace Simulator.Bridge.Ros
{
    static class RosUtils
    {
        public static bool IsGenericList(this Type type)
            => type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(List<>));

        public static bool IsNullable(this Type type)
            => Nullable.GetUnderlyingType(type) != null;

        public static void AppendEscaped(this StringBuilder sb, string text)
        {
            foreach (char c in text)
            {
                switch (c)
                {
                    case '\\': sb.Append('\\'); sb.Append('\\'); break;
                    case '\"': sb.Append('\\'); sb.Append('"'); break;
                    case '\n': sb.Append('\\'); sb.Append('n'); break;
                    case '\r': sb.Append('\\'); sb.Append('r'); break;
                    case '\t': sb.Append('\\'); sb.Append('t'); break;
                    case '\b': sb.Append('\\'); sb.Append('b'); break;
                    case '\f': sb.Append('\\'); sb.Append('f'); break;
                    default: sb.Append(c); break;
                }
            }
        }

        public static string GetMessageType<BridgeType>()
        {
            var type = typeof(BridgeType);

            string name;
            if (RosSerialization.BuiltinMessageTypes.TryGetValue(type, out name))
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
