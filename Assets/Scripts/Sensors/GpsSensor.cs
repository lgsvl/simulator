/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Simulator.Bridge;
using Simulator.Bridge.Data;
using Simulator.Map;
using Simulator.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Simulator.Sensors.UI;

namespace Simulator.Sensors
{
    [SensorType("GPS Device", new[] { typeof(GpsData) })]
    public class GpsSensor : SensorBase
    {
        [SensorParameter]
        [Range(1f, 100f)]
        public float Frequency = 12.5f;

        [SensorParameter]
        public bool IgnoreMapOrigin = false;

        Queue<Tuple<double, Action>> MessageQueue =
            new Queue<Tuple<double, Action>>();

        bool Destroyed = false;
        bool IsFirstFixedUpdate = true;
        double LastTimestamp;

        float NextSend;
        uint SendSequence;

        IBridge Bridge;
        IWriter<GpsData> Writer;

        MapOrigin MapOrigin;
        
        public override SensorDistributionType DistributionType => SensorDistributionType.LowLoad;

        private void Awake()
        {
            MapOrigin = MapOrigin.Find();
        }

        public void Start()
        {
            Task.Run(Publisher);
        }

        void OnDestroy()
        {
            Destroyed = true;
        }

        public override void OnBridgeSetup(IBridge bridge)
        {
            Bridge = bridge;
            Writer = Bridge.AddWriter<GpsData>(Topic);
        }

        void Publisher()
        {
            var nextPublish = Stopwatch.GetTimestamp();

            while (!Destroyed)
            {
                long now = Stopwatch.GetTimestamp();
                if (now < nextPublish)
                {
                    Thread.Sleep(0);
                    continue;
                }

                Tuple<double, Action> msg = null;
                lock (MessageQueue)
                {
                    if (MessageQueue.Count > 0)
                    {
                        msg = MessageQueue.Dequeue();
                    }
                }

                if (msg != null)
                {
                    try
                    {
                        msg.Item2();
                    }
                    catch
                    {
                    }
                    nextPublish = now + (long)(Stopwatch.Frequency / Frequency);
                    LastTimestamp = msg.Item1;
                }
            }
        }

        void FixedUpdate()
        {
            if (MapOrigin == null || Bridge == null || Bridge.Status != Status.Connected)
            {
                return;
            }

            if (IsFirstFixedUpdate)
            {
                lock (MessageQueue)
                {
                    MessageQueue.Clear();
                }
                IsFirstFixedUpdate = false;
            }

            var time = SimulatorManager.Instance.CurrentTime;
            if (time < LastTimestamp)
            {
                return;
            }

            var location = MapOrigin.GetGpsLocation(transform.position, IgnoreMapOrigin);

            var data = new GpsData()
            {
                Name = Name,
                Frame = Frame,
                Time = SimulatorManager.Instance.CurrentTime,
                Sequence = SendSequence++,

                IgnoreMapOrigin = IgnoreMapOrigin,
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                Altitude = location.Altitude,
                Northing = location.Northing,
                Easting = location.Easting,
                Orientation = transform.rotation,
            };
            
            lock (MessageQueue)
            {
                MessageQueue.Enqueue(Tuple.Create(time, (Action)(() => Writer.Write(data))));
            }
        }

        void Update()
        {
            IsFirstFixedUpdate = true;
        }

        public Api.Commands.GpsData GetData()
        {
            var location = MapOrigin.GetGpsLocation(transform.position);

            var data = new Api.Commands.GpsData
            {
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                Easting = location.Easting,
                Northing = location.Northing,
                Altitude = location.Altitude,
                Orientation = -transform.rotation.eulerAngles.y
            };
            return data;
        }

        public override void OnVisualize(Visualizer visualizer)
        {
            UnityEngine.Debug.Assert(visualizer != null);

            var location = MapOrigin.GetGpsLocation(transform.position, IgnoreMapOrigin);

            var graphData = new Dictionary<string, object>()
            {
                {"Ignore MapOrigin", IgnoreMapOrigin},
                {"Latitude", location.Latitude},
                {"Longitude", location.Longitude},
                {"Altitude", location.Altitude},
                {"Northing", location.Northing},
                {"Easting", location.Easting},
                {"Orientation", transform.rotation}
            };
            visualizer.UpdateGraphValues(graphData);
        }

        public override void OnVisualizeToggle(bool state)
        {
            //
        }
    }
}
