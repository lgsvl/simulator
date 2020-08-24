/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using UnityEngine;

    /// <summary>
    /// Class that caches the class types available in the Simulator
    /// </summary>
    public static class ReflectionCache
    {
        /// <summary>
        /// Caches class types available in the Simulator accessible by their fullname
        /// </summary>
        private static readonly Dictionary<string, Type> typesByName = new Dictionary<string, Type>();

        static ReflectionCache()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (var i = 0; i < assemblies.Length; i++)
            {
                var assembly = assemblies[i];
                if (!assembly.ManifestModule.Name.Contains("Simulator"))
                    continue;
                try
                {
                    foreach (var definedType in assembly.GetTypes())
                    {
                        if (string.IsNullOrEmpty(definedType.FullName)) continue;

                        if (!typesByName.ContainsKey(definedType.FullName))
                            typesByName.Add(definedType.FullName, definedType);
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    foreach (Exception inner in ex.LoaderExceptions)
                    {
                        Debug.LogError(inner.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves class type available in the Simulator application of the given fullname
        /// </summary>
        /// <param name="fullname">Fullname of the requested type</param>
        /// <returns>Class type available in the Simulator application of the given fullname</returns>
        public static Type GetType(string fullname)
        {
            return string.IsNullOrEmpty(fullname) ? null : typesByName[fullname];
        }

        /// <summary>
        /// Finds all the class types available in the Simulator application which fulfills the predicate
        /// </summary>
        /// <param name="filter">Predicate that has to be fulfilled by the type to be included in the result list</param>
        /// <returns>Class types available in the Simulator application which fulfills the predicate</returns>
        public static List<Type> FindTypes(Func<Type, bool> filter)
        {
            return typesByName.Values.Where(type => filter == null || filter.Invoke(type)).ToList();
        }
    }
}