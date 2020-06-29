/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;

namespace Simulator.Bridge
{
    [AttributeUsage(AttributeTargets.Class)]
    public class BridgeNameAttribute : Attribute
    {
        public string Name { get; private set; }

        public BridgeNameAttribute(string name)
        {
            Name = name;
        }
    }

    public interface IBridgeFactory
    {
        IBridgeInstance CreateInstance();

        // called by simulator to allow bridge register builtin types/publishers/subscribers
        void Register(IBridgePlugin plugin);

        // called by sensors to register publisher & subscriber create methods for specific types
        void RegPublisher<DataType, BridgeType>(IBridgePlugin plugin, Func<DataType, BridgeType> converter);
        void RegSubscriber<DataType, BridgeType>(IBridgePlugin plugin, Func<BridgeType, DataType> converter);
    }
}
