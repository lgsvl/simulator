/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;

namespace Simulator.Bridge
{
    // first argument is bridge instance for which to create subscriber
    // second argument is topic name
    // third argument is callback to call when message arrives on bridge
    // (callback will be invoked by bridge)
    public delegate void SubscriberCreator<DataType>(IBridgeInstance instance, string topic, Subscriber<DataType> suscriber);

    // first argument is bridge instance for which to create subscriber
    // second argument is topic name
    // returns delegate that sensor can call to send message to bridge
    // (delegate implementation should be non-blocking for best performance)
    public delegate Publisher<DataType> PublisherCreator<DataType>(IBridgeInstance instance, string topic);

    // first argument is bridge instance for which to create subscriber
    // second argument is topic name
    // third argument is callback to call when service call arrives on bridge
    //   callback will be invoked by bridge with two arguments:
    //   1) message receive from bridge
    //   2) delegate to call by sensor to send return message from service call to bridge
    //      (delegate implementation should be non-blocking for best performance)
    public delegate void ServiceCreator<ArgDataType, ResDataType>(IBridgeInstance instance, string topic, Action<ArgDataType, Action<ResDataType>> service);

    public interface IBridgePlugin
    {
        IBridgeFactory Factory { get; }

        // adds data type that bridge supports
        // bridgeTypeNanme is used only for displaying it in UI
        void AddType<DataType>(string bridgeTypeName);

        void AddSubscriberCreator<DataType>(SubscriberCreator<DataType> subscriber);
        void AddPublisherCreator<DataType>(PublisherCreator<DataType> publisher);
        void AddServiceCreator<ArgDataType, ResDataType>(ServiceCreator<ArgDataType, ResDataType> service);
    }
}
