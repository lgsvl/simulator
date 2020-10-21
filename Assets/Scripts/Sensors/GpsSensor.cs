/**
 * Copyright (c) 2018-2020 LG Electronics, Inc.
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
using System.Collections;
using Simulator.Analysis;

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

        BridgeInstance Bridge;
        Publisher<GpsData> Publish;

        MapOrigin MapOrigin;
        private Vector3 startPosition;
        private Quaternion startRotation;

        public override SensorDistributionType DistributionType => SensorDistributionType.LowLoad;

        private void Awake()
        {
            MapOrigin = MapOrigin.Find();
            startPosition = transform.position;
            startRotation = transform.rotation;
        }

        public void Start()
        {
            Task.Run(Publisher);
        }

        void OnDestroy()
        {
            Destroyed = true;
        }

        public override void OnBridgeSetup(BridgeInstance bridge)
        {
            Bridge = bridge;
            Publish = Bridge.AddPublisher<GpsData>(Topic);
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
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogException(ex, this);
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

            var orientation = transform.rotation;
            orientation.Set(-orientation.z, orientation.x, -orientation.y, orientation.w); // converting to right handed xyz

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
                Orientation = orientation,
            };

            lock (MessageQueue)
            {
                MessageQueue.Enqueue(Tuple.Create(time, (Action)(() =>
                {
                    if (Bridge != null && Bridge.Status == Status.Connected)
                    {
                        Publish(data);
                    }
                })));
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

            var orientation = transform.rotation;
            orientation.Set(-orientation.z, orientation.x, -orientation.y, orientation.w); // converting to right handed xyz

            var graphData = new Dictionary<string, object>()
            {
                {"Ignore MapOrigin", IgnoreMapOrigin},
                {"Latitude", location.Latitude},
                {"Longitude", location.Longitude},
                {"Altitude", location.Altitude},
                {"Northing", location.Northing},
                {"Easting", location.Easting},
                {"Orientation", orientation}
            };
            visualizer.UpdateGraphValues(graphData);
        }

        public override void OnVisualizeToggle(bool state)
        {
            //
        }

        public override void SetAnalysisData()
        {
            var startLocation = MapOrigin.GetGpsLocation(startPosition, IgnoreMapOrigin);
            var location = MapOrigin.GetGpsLocation(transform.position, IgnoreMapOrigin);


            SensorAnalysisData = new List<AnalysisReportItem>
            {
                new AnalysisReportItem {
                    name = "Start Latitude",
                    type = "latitude",
                    value = startLocation.Latitude
                },
                new AnalysisReportItem {
                    name = "Start Longitude",
                    type = "longitude",
                    value = startLocation.Longitude
                },
                new AnalysisReportItem {
                    name = "Start Altitude",
                    type = "altitude",
                    value = startLocation.Altitude
                },
                new AnalysisReportItem {
                    name = "Start Northing",
                    type = "northing",
                    value = startLocation.Northing
                },
                new AnalysisReportItem {
                    name = "Start Easting",
                    type = "easting",
                    value = startLocation.Easting
                },
                new AnalysisReportItem {
                    name = "Start Map Url",
                    type = "mapUrl",
                    value =  $"https://www.google.com/maps/search/?api=1&query={startLocation.Latitude},{startLocation.Longitude}"
                },
                new AnalysisReportItem {
                    name = "Latitude",
                    type = "latitude",
                    value = location.Latitude
                },
                new AnalysisReportItem {
                    name = "Longitude",
                    type = "longitude",
                    value = location.Longitude
                },
                new AnalysisReportItem {
                    name = "Altitude",
                    type = "altitude",
                    value = location.Altitude
                },
                new AnalysisReportItem {
                    name = "Northing",
                    type = "northing",
                    value = location.Northing
                },
                new AnalysisReportItem {
                    name = "Easting",
                    type = "easting",
                    value = location.Easting
                },
                new AnalysisReportItem {
                    name = "Map Url",
                    type = "mapUrl",
                    value =  $"https://www.google.com/maps/search/?api=1&query={location.Latitude},{location.Longitude}"
                },
            };
        }
    }
}
