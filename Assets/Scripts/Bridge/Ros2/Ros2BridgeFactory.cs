/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using Simulator.Bridge.Data;
// NOTE: DO NOT add using "Ros2.Ros" or "Ros2.Lgsvl" namespaces here to avoid
// NOTE: confusion between types. Keep them fully qualified in this file.

namespace Simulator.Bridge.Ros2
{
    [BridgeName("ROS2")]
    public class Ros2BridgeFactory : IBridgeFactory
    {
        public IBridgeInstance CreateInstance() => new Ros2BridgeInstance();

        public void Register(IBridgePlugin plugin)
        {
            // point cloud is special, as we use special writer for performance reasons
            plugin.AddType<PointCloudData>(Ros2Utils.GetMessageType<Ros.PointCloud2>());
            plugin.AddPublisherCreator(
                (instance, topic) =>
                {
                    var ros2Instance = instance as Ros2BridgeInstance;
                    ros2Instance.AddPublisher<Ros.PointCloud2>(topic);
                    var writer = new Ros2PointCloudWriter(ros2Instance, topic);
                    return new Publisher<PointCloudData>((data, completed) => writer.Write(data, completed));
                }
            );

            RegPublisher<ImageData, Ros.CompressedImage>(plugin, Ros2Conversions.ConvertFrom);
            RegPublisher<Detected3DObjectData, Lgsvl.Detection3DArray>(plugin, Ros2Conversions.ConvertFrom);
            RegPublisher<Detected2DObjectData, Lgsvl.Detection2DArray>(plugin, Ros2Conversions.ConvertFrom);
            RegPublisher<SignalDataArray, Lgsvl.SignalArray>(plugin, Ros2Conversions.ConvertFrom);
            RegPublisher<CanBusData, Lgsvl.CanBusData>(plugin, Ros2Conversions.ConvertFrom);
            RegPublisher<UltrasonicData, Lgsvl.Ultrasonic>(plugin, Ros2Conversions.ConvertFrom);
            RegPublisher<GpsData, Ros.NavSatFix>(plugin, Ros2Conversions.ConvertFrom);
            RegPublisher<GpsOdometryData, Ros.Odometry>(plugin, Ros2Conversions.ConvertFrom);
            RegPublisher<ImuData, Ros.Imu>(plugin, Ros2Conversions.ConvertFrom);
            RegPublisher<ClockData, Ros.Clock>(plugin, Ros2Conversions.ConvertFrom);

            RegSubscriber<VehicleStateData, Lgsvl.VehicleStateData>(plugin, Ros2Conversions.ConvertTo);
            RegSubscriber<VehicleControlData, Lgsvl.VehicleControlData>(plugin, Ros2Conversions.ConvertTo);
            RegSubscriber<Detected2DObjectArray, Lgsvl.Detection2DArray>(plugin, Ros2Conversions.ConvertTo);
            RegSubscriber<Detected3DObjectArray, Lgsvl.Detection3DArray>(plugin, Ros2Conversions.ConvertTo);
        }

        public void RegPublisher<DataType, BridgeType>(IBridgePlugin plugin, Func<DataType, BridgeType> converter)
        {   
            plugin.AddType<DataType>(Ros2Utils.GetMessageType<BridgeType>());
            plugin.AddPublisherCreator(
                (instance, topic) =>
                {
                    var ros2Instance = instance as Ros2BridgeInstance;
                    ros2Instance.AddPublisher<BridgeType>(topic);
                    var writer = new Ros2Writer<BridgeType>(ros2Instance, topic);
                    return new Publisher<DataType>((data, completed) => writer.Write(converter(data), completed));
                }
            );
        }

        public void RegSubscriber<DataType, BridgeType>(IBridgePlugin plugin, Func<BridgeType, DataType> converter)
        {
            plugin.AddType<DataType>(Ros2Utils.GetMessageType<BridgeType>());
            plugin.AddSubscriberCreator<DataType>(
                (instance, topic, callback) => (instance as Ros2BridgeInstance).AddSubscriber<BridgeType>(topic,
                    rawData => callback(converter(Ros2Serialization.Unserialize<BridgeType>(rawData)))
                )
            );
        }
    }
}
