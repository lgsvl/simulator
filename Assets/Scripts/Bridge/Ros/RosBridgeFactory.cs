/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using Simulator.Bridge.Data;
// NOTE: DO NOT add using "Ros.Ros", "Ros.Apollo" or "Ros.Lgsvl" namespaces here to avoid
// NOTE: confusion between types. Keep them fully qualified in this file.

namespace Simulator.Bridge.Ros
{
    public abstract class RosBridgeFactoryBase : IBridgeFactory
    {
        public IBridgeInstance CreateInstance() => new RosBridgeInstance();

        public virtual void Register(IBridgePlugin plugin)
        {
            // point cloud is special, as we use special writer for performance reasons
            plugin.AddType<PointCloudData>(RosUtils.GetMessageType<Ros.PointCloud2>());
            plugin.AddPublisherCreator<PointCloudData>(
                (instance, topic) =>
                {
                    var rosInstance = instance as RosBridgeInstance;
                    rosInstance.AddPublisher<Ros.PointCloud2>(topic);
                    var writer = new RosPointCloudWriter(rosInstance, topic);
                    return new Publisher<PointCloudData>((data, completed) => writer.Write(data, completed));
                }
            );

            RegPublisher<ImageData, Ros.CompressedImage>(plugin, Conversions.ConvertFrom);
            RegPublisher<Detected2DObjectData, Lgsvl.Detection2DArray>(plugin, Conversions.ConvertFrom);
            RegPublisher<ClockData, Ros.Clock>(plugin, Conversions.ConvertFrom);

            RegSubscriber<VehicleStateData, Lgsvl.VehicleStateDataRos>(plugin, Conversions.ConvertTo);
            RegSubscriber<Detected2DObjectArray, Lgsvl.Detection2DArray>(plugin, Conversions.ConvertTo);
            RegSubscriber<Detected3DObjectArray, Lgsvl.Detection3DArray>(plugin, Conversions.ConvertTo);

            // std_srvs/Empty
            RegService<EmptySrv, Ros.Empty, EmptySrv, Ros.Empty>(plugin, Conversions.ConvertTo, Conversions.ConvertFrom);

            // std_srvs/SetBool
            RegService<SetBoolSrv, Ros.SetBool, SetBoolSrv, Ros.SetBoolResponse>(plugin, Conversions.ConvertTo, Conversions.ConvertFrom);

            // std_srvs/Trigger
            RegService<EmptySrv, Ros.Empty, TriggerSrv, Ros.Trigger>(plugin, Conversions.ConvertTo, Conversions.ConvertFrom);
        }

        public void RegPublisher<DataType, BridgeType>(IBridgePlugin plugin, Func<DataType, BridgeType> converter)
        {
            plugin.AddType<DataType>(RosUtils.GetMessageType<BridgeType>());
            plugin.AddPublisherCreator<DataType>(
                (instance, topic) =>
                {
                    var rosInstance = instance as RosBridgeInstance;
                    rosInstance.AddPublisher<BridgeType>(topic);
                    var writer = new RosWriter<BridgeType>(rosInstance, topic);
                    return new Publisher<DataType>((data, completed) => writer.Write(converter(data), completed));
                }
            );
        }

        public void RegSubscriber<DataType, BridgeType>(IBridgePlugin plugin, Func<BridgeType, DataType> converter)
        {
            plugin.AddType<DataType>(RosUtils.GetMessageType<BridgeType>());
            plugin.AddSubscriberCreator< DataType>(
                (instance, topic, callback) => (instance as RosBridgeInstance).AddSubscriber<BridgeType>(topic,
                    rawData => callback(converter(RosSerialization.Unserialize<BridgeType>(rawData)))
                )
            );
        }

        public void RegService<ArgDataType, ArgBridgeType, ResDataType, ResBridgeType>(IBridgePlugin plugin, Func<ArgBridgeType, ArgDataType> argConverter, Func<ResDataType, ResBridgeType> resConverter)
        {
            plugin.AddServiceCreator<ArgDataType, ResDataType>(
                (instance, topic, service) =>
                {
                    // this callback is called every time sensor registers service on different topic
                    (instance as RosBridgeInstance).AddService<ArgBridgeType>(topic,
                        (rawArg, resultCb) =>
                        {
                            // this callback is called every time websocket receives message from rosbridge
                            var arg = RosSerialization.Unserialize<ArgBridgeType>(rawArg);
                            var argData = argConverter(arg);
                            service(argData, resData =>
                            {
                                // this callback is called from sensor service callback to return result data back to rosbridge
                                var res = resConverter(resData);
                                resultCb(res);
                            });
                        }
                    );
                }
            );
        }
    }

    [BridgeName("ROS")]
    public class RosBridgeFactory : RosBridgeFactoryBase
    {
        public override void Register(IBridgePlugin plugin)
        {
            base.Register(plugin);

            // gps data is special, because it actually sends two Ros.Sentence messages for each data point from simulator
            plugin.AddType<GpsData>(RosUtils.GetMessageType<Ros.Sentence>());
            plugin.AddPublisherCreator(
                (instance, topic) =>
                {
                    var rosInstance = instance as RosBridgeInstance;
                    rosInstance.AddPublisher<Ros.Sentence>(topic);
                    var writer = new RosNmeaWriter(rosInstance, topic);
                    return new Publisher<GpsData>((data, completed) => writer.Write(data, completed));
                }
            );

            RegPublisher<DetectedRadarObjectData, Lgsvl.DetectedRadarObjectArray>(plugin, Conversions.ROS2ConvertFrom);
            RegPublisher<GpsOdometryData, Ros.Odometry>(plugin, Conversions.ConvertFrom);
            RegPublisher<ImuData, Ros.Imu>(plugin, Conversions.ConvertFrom);
            RegPublisher<Detected3DObjectData, Lgsvl.Detection3DArray>(plugin, Conversions.ConvertFrom);
            RegPublisher<SignalDataArray, Lgsvl.SignalArray>(plugin, Conversions.ConvertFrom);

            RegSubscriber<VehicleControlData, Autoware.VehicleCmd>(plugin, Conversions.ConvertTo);
        }
    }

    [BridgeName("ROS Apollo")]
    public class RosApolloBridgeFactory : RosBridgeFactoryBase
    {
        public override void Register(IBridgePlugin plugin)
        {
            base.Register(plugin);

            RegPublisher<DetectedRadarObjectData, Apollo.Drivers.ContiRadar>(plugin, Conversions.ConvertFrom);
            RegPublisher<CanBusData, Apollo.ChassisMsg>(plugin, Conversions.ConvertFrom);
            RegPublisher<GpsData, Apollo.GnssBestPose>(plugin, Conversions.ConvertFrom);
            RegPublisher<GpsOdometryData, Apollo.Gps>(plugin, Conversions.ApolloConvertFrom);
            RegPublisher<ImuData, Apollo.Imu>(plugin, Conversions.ApolloConvertFrom);
            RegPublisher<CorrectedImuData, Apollo.CorrectedImu>(plugin, Conversions.ApolloConvertFrom);
            RegPublisher<Detected3DObjectData, Apollo.Perception.PerceptionObstacles>(plugin, Conversions.ApolloConvertFrom);
            RegPublisher<SignalDataArray, Apollo.Perception.TrafficLightDetection>(plugin, Conversions.ApolloConvertFrom);

            RegSubscriber<VehicleControlData, Apollo.control_command>(plugin, Conversions.ConvertTo);
        }
    }
}
