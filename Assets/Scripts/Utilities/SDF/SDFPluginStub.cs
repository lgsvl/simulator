/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;
using UnityEngine;

public class SDFPluginStub : MonoBehaviour
{
    static protected Dictionary<string, Type> pluginParsers = null;
    public string data;

    void CheckParsers()
    {
        if (pluginParsers == null)
        {
            pluginParsers = new Dictionary<string, Type>();
#if UNITY_EDITOR
            try
            {
                var assembly = Assembly.Load("Simulator.Controllables");
                foreach (var type in assembly.GetTypes())
                {
                    var attr = type.GetCustomAttribute<SDFPluginParser>();
                    if (typeof(SDFParserBase).IsAssignableFrom(type) && attr != null)
                    {
                        Debug.Log("found assembly plugin " + attr.PluginName);
                        pluginParsers.Add(attr.PluginName, type);
                    }
                }
            }
            catch { }
#else
            foreach (var controllable in Simulator.Web.Config.Controllables)
            {
                if (controllable.Value == null)
                {
                    Debug.LogError("null controllable: " + controllable.Key);
                    continue;
                }
                var assemblyTypes = controllable.Value.GetType().Assembly.GetTypes();
                foreach (var type in assemblyTypes)
                {
                    var attr = type.GetCustomAttribute<SDFPluginParser>();
                    Debug.Log($"checking controllable {controllable.Key} type: {type.Name} has attr {attr != null} is assignable {typeof(SDFParserBase).IsAssignableFrom(type)} subclass {type.IsSubclassOf(typeof(SDFParserBase))}");
                    if (typeof(SDFParserBase).IsAssignableFrom(type) && attr != null)
                    {
                        Debug.Log("found plugin " + attr.PluginName);
                        pluginParsers.Add(attr.PluginName, type);
                    }
                }
            }
#endif
        }
    }

    void Awake()
    {
        CheckParsers();
        var doc = XDocument.Parse(data);
        var pluginElement = doc.Root;

        var pluginName = pluginElement.Attribute("name")?.Value;
        if (pluginParsers.TryGetValue(pluginName, out Type parserType))
        {
            SDFParserBase parser = (SDFParserBase)Activator.CreateInstance(parserType);
            parser.Parse(pluginElement, gameObject);
        }
        else
        {
            Debug.LogWarning("plugin not implemented: " + pluginElement);
        }
    }
}
