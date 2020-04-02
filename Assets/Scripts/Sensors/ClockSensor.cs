/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Simulator.Bridge;
using Simulator.Bridge.Data;
using Simulator.Utilities;
using Simulator.Sensors.UI;

namespace Simulator.Sensors
{
    [SensorType("Clock", new[] { typeof(ClockData) })]
    public partial class ClockSensor : SensorBase
    {
        Queue<Tuple<double, float, Action>> MessageQueue =
            new Queue<Tuple<double, float, Action>>();

        IBridge Bridge;
        IWriter<ClockData> Writer;

        bool Destroyed = false;
        bool IsFirstFixedUpdate = true;
        double LastTimestamp;

        ClockData data;
        
        public override SensorDistributionType DistributionType => SensorDistributionType.LowLoad;

        public override void OnBridgeSetup(IBridge bridge)
        {
            Bridge = bridge;
            Writer = bridge.AddWriter<ClockData>(Topic);
        }

        public void Start()
        {
            Task.Run(Publisher);
        }

        void OnDestroy()
        {
            Destroyed = true;
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

                Tuple<double, float, Action> msg = null;
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
                        msg.Item3();
                    }
                    catch
                    {
                    }
                    nextPublish = now + (long)(Stopwatch.Frequency * msg.Item2);
                    LastTimestamp = msg.Item1;
                }
            }
        }

        void FixedUpdate()
        {
            if (IsFirstFixedUpdate)
            {
                if (Bridge != null && Bridge.Status == Status.Connected)
                {
                    lock (MessageQueue)
                    {
                        MessageQueue.Clear();
                    }
                }
                IsFirstFixedUpdate = false;
            }

            var time = SimulatorManager.Instance.CurrentTime;
            if (time < LastTimestamp)
            {
                return;
            }

            data = new ClockData()
            {
                Clock = time,
            };

            if (Bridge != null && Bridge.Status == Status.Connected)
            {
                lock (MessageQueue)
                {
                    MessageQueue.Enqueue(Tuple.Create(time, Time.fixedDeltaTime, (Action)(() => Writer.Write(data, null))));
                }
            }
        }

        void Update()
        {
            IsFirstFixedUpdate = true;
        }

        public override void OnVisualize(Visualizer visualizer)
        {
            UnityEngine.Debug.Assert(visualizer != null);

            if (data == null)
            {
                return;
            }

            var graphData = new Dictionary<string, object>()
            {
                {"Time", data.Clock},
                {"Fixed DeltaTime", Time.fixedDeltaTime}
            };
            visualizer.UpdateGraphValues(graphData);
        }

        public override void OnVisualizeToggle(bool state)
        {
            //
        }
    }
}
