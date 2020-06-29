/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

namespace Simulator.Bridge
{
    public class BridgePlugin : IBridgePlugin
    {
        // private

        Dictionary<Type, string> BridgeTypes = new Dictionary<Type, string>();
        Dictionary<Type, Delegate> Subscribers = new Dictionary<Type, Delegate>();
        Dictionary<Type, Delegate> Publishers = new Dictionary<Type, Delegate>();
        Dictionary<(Type, Type), Delegate> Services = new Dictionary<(Type, Type), Delegate>();

        // constructor

        public BridgePlugin(IBridgeFactory factory)
        {
            Factory = factory;
            factory.Register(this);
        }

        // public interface

        public IBridgeFactory Factory { get; private set; }

        public string Name => BridgePlugins.GetNameFromFactory(Factory.GetType());

        public string[] GetSupportedDataTypes()
        {
            return BridgeTypes.Keys.Select(type => type.Name).ToArray();
        }

        public string GetBridgeType<DataType>()
        {
            BridgeTypes.TryGetValue(typeof(DataType), out var bridgeType);
            return bridgeType;
        }

        public SubscriberCreator<DataType> GetCreateSubscriber<DataType>()
        {
            Subscribers.TryGetValue(typeof(DataType), out var subscriber);
            return subscriber as SubscriberCreator<DataType>;
        }

        public PublisherCreator<DataType> GetCreatePublisher<DataType>()
        {
            Publishers.TryGetValue(typeof(DataType), out var publisher);
            return publisher as PublisherCreator<DataType>;
        }

        public ServiceCreator<ArgDataType, ResDataType> GetCreateService<ArgDataType, ResDataType>()
        {
            Services.TryGetValue((typeof(ArgDataType), typeof(ResDataType)), out var service);
            return service as ServiceCreator<ArgDataType, ResDataType>;
        }

        // interface implementation

        public void AddType<Type>(string bridgeTypeName)
        {
            var bridgeType = typeof(Type);
            if (!BridgeTypes.ContainsKey(bridgeType))
            {
                BridgeTypes.Add(bridgeType, bridgeTypeName);
            }
        }

        public void AddSubscriberCreator<DataType>(SubscriberCreator<DataType> subscriber)
        {
            var dataType = typeof(DataType);
            if (Subscribers.ContainsKey(dataType))
            {
                Debug.LogWarning($"Ignoring duplicate data type {dataType.Name} for subscriber");
                return;
            }
            Subscribers.Add(dataType, subscriber);
        }

        public void AddPublisherCreator<DataType>(PublisherCreator<DataType> publisher)
        {
            var dataType = typeof(DataType);
            if (Subscribers.ContainsKey(dataType))
            {
                Debug.LogWarning($"Ignoring duplicate data type {dataType.Name} for publisher");
                return;
            }
            Publishers.Add(dataType, publisher);
        }
        
        public void AddServiceCreator<ArgDataType, ResDataType>(ServiceCreator<ArgDataType, ResDataType> service)
        {
            var key = (typeof(ArgDataType), typeof(ResDataType));
            if (Services.ContainsKey(key))
            {
                Debug.LogWarning($"Ignoring duplicate arg/result types ({key.Item1.Name}, {key.Item2.Name}) for service");
                return;
            }
            Services.Add(key, service);
        }
    }

    public static class BridgePlugins
    {
        public static Dictionary<string, BridgePlugin> All { get; private set; } = new Dictionary<string, BridgePlugin>();

        public static BridgePlugin Get(string name)
        {
            if (All.TryGetValue(name, out var plugin))
            {
                return plugin;
            }
            return null;
        }

        public static void Add(IBridgeFactory factory)
        {
            var name = GetNameFromFactory(factory.GetType());
            if (All.ContainsKey(name))
            {
                Debug.LogError($"Bridge {name} already registered, ignoring duplicate(?) plugin");
            }
            else
            {
                All.Add(name, new BridgePlugin(factory));
            }
        }

        public static void Load()
        {
            foreach (var factoryType in GetBridgeFactories())
            {
                var factory = (IBridgeFactory)Activator.CreateInstance(factoryType);
                Add(factory);
            }
        }

        public static IEnumerable<Type> GetBridgeFactories()
        {
            return
                from a in AppDomain.CurrentDomain.GetAssemblies()
                where !a.IsDynamic
                from t in GetExportedTypesSafe(a)
                where typeof(IBridgeFactory).IsAssignableFrom(t) && !t.IsAbstract && !t.ContainsGenericParameters
                select t;
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

        public static string GetNameFromFactory(Type factoryType)
        {
            var attrib = factoryType.GetCustomAttribute<BridgeNameAttribute>();
            if (attrib == null)
            {
                throw new Exception($"Bridge {factoryType.Name} does not have {nameof(BridgeNameAttribute)} attribute!");
            }
            return attrib.Name;
        }
    }
}
