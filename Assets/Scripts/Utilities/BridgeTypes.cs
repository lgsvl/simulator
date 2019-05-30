/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Simulator.Bridge;

namespace Simulator.Utilities
{
    public static class BridgeTypes
    {
        public static List<IBridgeFactory> GetBridgeTypes()
        {
            return (
                from a in AppDomain.CurrentDomain.GetAssemblies()
                where !a.IsDynamic
                from t in GetExportedTypesSafe(a)
                where typeof(IBridgeFactory).IsAssignableFrom(t) && !t.IsAbstract && !t.ContainsGenericParameters
                select (IBridgeFactory)Activator.CreateInstance(t)
            ).ToList();
        }

        static Type[] GetExportedTypesSafe(Assembly asm)
        {
            try
            {
                return asm.GetExportedTypes();
            }
            catch
            {
                return Array.Empty<Type>();
            }
        }
    }
}