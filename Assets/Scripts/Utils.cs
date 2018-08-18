/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


using System.Collections.Generic;
using UnityEngine;

public static class Utils
{
    public static Transform FindDeepChild(this Transform parent, string name)
    {
        var result = parent.Find(name);
        if (result != null)
        {
            return result;
        }

        foreach (Transform child in parent)
        {
            result = child.FindDeepChild(name);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }

    public static bool IsGenericList(this System.Type type) => type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(List<>));

    public static bool IsNullable(this System.Type type) => System.Nullable.GetUnderlyingType(type) != null;

    public static object TypeDefaultValue(this System.Type type)
    {
        if (type.IsValueType)
            return System.Activator.CreateInstance(type);

        return null;
    }
}

namespace Apollo
{
    public static class Utils
    {
        public interface IOneOf
        {
            void Clear();
            KeyValuePair<string, object> GetOne();
        }

        public interface IOneOf<T> : IOneOf where T : IOneOf<T> { }
    }
}
