/**
 * Copyright (c) 2019-2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using Simulator.Bridge.Data;

namespace Simulator.Bridge.Cyber
{
    [BridgeName("CyberRT")]
    public class CyberBridgeFactory : IBridgeFactory
    {
        public IBridgeInstance CreateInstance() => new CyberBridgeInstance();

        public void Register(IBridgePlugin plugin)
        {
            RegPublisher<ImageData, apollo.drivers.CompressedImage>(plugin, CyberConversions.ConvertFrom);
            RegPublisher<PointCloudData, apollo.drivers.PointCloud>(plugin, CyberConversions.ConvertFrom);
            RegPublisher<Detected3DObjectData, apollo.perception.PerceptionObstacles>(plugin, CyberConversions.ConvertFrom);
            RegPublisher<Detected2DObjectData, apollo.common.Detection2DArray>(plugin, CyberConversions.ConvertFrom);
            RegPublisher<DetectedRadarObjectData, apollo.drivers.ContiRadar>(plugin, CyberConversions.ConvertFrom);
            RegPublisher<CanBusData, apollo.canbus.Chassis>(plugin, CyberConversions.ConvertFrom);
            RegPublisher<GpsData, apollo.drivers.gnss.GnssBestPose>(plugin, CyberConversions.ConvertFrom);
            RegPublisher<GpsOdometryData, apollo.localization.Gps>(plugin, CyberConversions.ConvertFrom);
            RegPublisher<GpsInsData, apollo.drivers.gnss.InsStat>(plugin, CyberConversions.ConvertFrom);
            RegPublisher<ImuData, apollo.drivers.gnss.Imu>(plugin, CyberConversions.ConvertFrom);
            RegPublisher<CorrectedImuData, apollo.localization.CorrectedImu>(plugin, CyberConversions.ConvertFrom);
            RegPublisher<SignalDataArray, apollo.perception.TrafficLightDetection>(plugin, CyberConversions.ConvertFrom);
            RegPublisher<ClockData, apollo.cyber.proto.Clock>(plugin, CyberConversions.ConvertFrom);
            RegPublisher<LaneLinesData, apollo.perception.PerceptionLanes>(plugin, CyberConversions.ConvertFrom);

            RegSubscriber<VehicleControlData, apollo.control.ControlCommand>(plugin, CyberConversions.ConvertTo);
            RegSubscriber<Detected2DObjectArray, apollo.common.Detection2DArray>(plugin, CyberConversions.ConvertTo);
            RegSubscriber<Detected3DObjectArray, apollo.perception.PerceptionObstacles>(plugin, CyberConversions.ConvertTo);
        }

        public void RegPublisher<DataType, BridgeType>(IBridgePlugin plugin, Func<DataType, BridgeType> converter)
        {
            plugin.AddType<DataType>(typeof(BridgeType).Name);
            plugin.AddPublisherCreator(
                (instance, topic) =>
                {
                    var cyberInstance = instance as CyberBridgeInstance;
                    cyberInstance.AddPublisher<BridgeType>(topic);
                    var writer = new CyberWriter<BridgeType>(cyberInstance, topic);
                    return new Publisher<DataType>((data, completed) => writer.Write(converter(data), completed));
                }
            );
        }

        public void RegSubscriber<DataType, BridgeType>(IBridgePlugin plugin, Func<BridgeType, DataType> converter)
        {
            plugin.AddType<DataType>(typeof(BridgeType).Name);
            plugin.AddSubscriberCreator<DataType>(
                (instance, topic, callback) => (instance as CyberBridgeInstance).AddSubscriber<BridgeType>(topic,
                    rawData => callback(converter(CyberSerialization.Unserialize<BridgeType>(rawData)))
                )
            );
        }
    }
}
