/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Simulator.Utilities;

public class TriggersManager
{
    private static readonly Dictionary<string, Type> EffectorNameToType = new Dictionary<string, Type>();

    static TriggersManager()
    {
        var effectorTypes = ReflectionCache.FindTypes(type => type.IsSubclassOf(typeof(TriggerEffector)));
        foreach (var effectorType in effectorTypes)
        {
            if (Activator.CreateInstance(effectorType) is TriggerEffector effector)
                EffectorNameToType.Add(effector.TypeName, effectorType);
        }
    }
    
    public static List<Type> GetAllEffectorsTypes()
    {
        var types = EffectorNameToType.Values.ToList();
        return types;
    }

    public static TriggerEffector GetEffectorOfType(string typeName)
    {
        if (!EffectorNameToType.TryGetValue(typeName, out var effectorType))
            return null;
        return Activator.CreateInstance(effectorType) as TriggerEffector;
    }
}
