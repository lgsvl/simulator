/**
 * Copyright (c) 2019-2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace Simulator.Bridge
{
    public delegate void Subscriber<DataType>(DataType message);
    public delegate void Publisher<DataType>(DataType message, Action completed = null);
    public delegate ResDataType Service<ArgDataType, ResDataType>(ArgDataType message);

    public class BridgeInstance
    {
        public Status Status => Instance.Status;

        public IList<TopicData> PublisherData { get; private set; } = new List<TopicData>();
        public IList<TopicData> SubscriberData { get; private set; }  = new List<TopicData>();

        public ConcurrentQueue<Action> Actions { get; private set; } = new ConcurrentQueue<Action>();

        class SubscriberCallbacks
        {
            public List<Delegate> Callbacks = new List<Delegate>();
        }

        Dictionary<string, SubscriberCallbacks> Subscribers = new Dictionary<string, SubscriberCallbacks>();
        HashSet<string> Services = new HashSet<string>();

        BridgePlugin Plugin;
        IBridgeInstance Instance;

        public BridgeInstance(BridgePlugin plugin)
        {
            Plugin = plugin;
            Instance = plugin.Factory.CreateInstance();
        }

        public void Connect(string connection)
        {
            Instance.Connect(connection);
        }

        public void Disconnect()
        {
            Instance.Disconnect();
        }

        public Publisher<DataType> AddPublisher<DataType>(string topic)
        {
            var bridgeType = Plugin.GetBridgeType<DataType>();
            if (bridgeType == null)
            {
                throw new Exception($"Unknown data type {typeof(DataType).Name} used for publisher in {Plugin.Name} bridge");
            }

            PublisherData.Add(new TopicData(topic, bridgeType));

            var pubCreator = Plugin.GetCreatePublisher<DataType>();
            if (pubCreator == null)
            {
                throw new NotSupportedException($"Publisher on {topic} topic for for {typeof(DataType).Name} not supported by {Plugin.Name} bridge");
            }

            var newPub = pubCreator(Instance, topic);

            return (DataType data, Action completed) =>
            {
                foreach (var p in PublisherData)
                {
                    if (p.Topic == topic)
                    {
                        p.Count += 1;
                    }
                }
                newPub(data, completed);
            };
        }

        public void AddSubscriber<DataType>(string topic, Subscriber<DataType> subscriber)
        {
            var bridgeType = Plugin.GetBridgeType<DataType>();
            if (bridgeType == null)
            {
                throw new Exception($"Unknown data type {typeof(DataType).Name} used for subscriber in {Plugin.Name} bridge");
            }

            SubscriberData.Add(new TopicData(topic, bridgeType));

            if (Subscribers.TryGetValue(topic, out var sub))
            {
                lock (sub)
                {
                    sub.Callbacks.Add(subscriber);
                }
                return;
            }

            sub = new SubscriberCallbacks();
            sub.Callbacks.Add(subscriber);

            var subCreator = Plugin.GetCreateSubscriber<DataType>();
            if (subCreator == null)
            {
                throw new NotSupportedException($"Subscriber on {topic} topic for for {typeof(DataType).Name} not supported by {Plugin.Name} bridge");
            }

            subCreator(Instance, topic, data =>
            {
                foreach (var s in SubscriberData)
                {
                    if (s.Topic == topic)
                    {
                        s.Count += 1;
                    }
                }

                lock (sub)
                {
                    sub.Callbacks.ForEach(cb => Actions.Enqueue(() => (cb as Subscriber<DataType>)(data)));
                }
            });

            Subscribers.Add(topic, sub);
        }

        public void AddService<ArgDataType, ResDataType>(string topic, Service<ArgDataType, ResDataType> callback)
        {
            if (Services.Contains(topic))
            {
                throw new Exception($"Topic {topic} already has service registered");
            }
            Services.Add(topic);

            var srvCreator = Plugin.GetCreateService<ArgDataType, ResDataType>();
            if (srvCreator == null)
            {
                throw new NotSupportedException($"Service on {topic} topic for for arg/result ({typeof(ArgDataType).Name}, {typeof(ResDataType).Name}) not supported by {Plugin.Name} bridge");
            }

            // TODO: if you want service statistics for UI, add them in this callback, look in AddPublisher method for example
            srvCreator(Instance, topic, (arg, result) =>
            {
                Actions.Enqueue(() => result(callback(arg)));
            });
        }

        public void Update()
        {
            if (Status != Status.Connected)
            {
                PublisherData.Clear();
                SubscriberData.Clear();

                while (Actions.TryDequeue(out var action))
                {
                }
            }

            foreach (var pub in PublisherData)
            {
                if (pub.ElapsedTime >= 1 && pub.Count > pub.StartCount)
                {
                    pub.Frequency = (pub.Count - pub.StartCount) / pub.ElapsedTime;
                    pub.StartCount = pub.Count;
                    pub.ElapsedTime = 0f;
                }
                else
                {
                    pub.ElapsedTime += Time.unscaledDeltaTime;
                }
            }

            foreach (var sub in SubscriberData)
            {
                if (sub.ElapsedTime >= 1 && sub.Count > sub.StartCount)
                {
                    sub.Frequency = (sub.Count - sub.StartCount) / sub.ElapsedTime;
                    sub.StartCount = sub.Count;
                    sub.ElapsedTime = 0f;
                }
                else
                {
                    sub.ElapsedTime += Time.unscaledDeltaTime;
                }
            }

            while (Actions.TryDequeue(out var action))
            {
                action();
            }
        }
    }
}
