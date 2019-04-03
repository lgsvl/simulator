/**
* Copyright (c) 2019 LG Electronics, Inc.
*
* This software contains code licensed as described in LICENSE.
*
*/

using SimpleJSON;
using UnityEngine;

namespace Api.Commands
{
    class SensorEnabledGet : ICommand
    {
        public string Name { get { return "sensor/enabled/get"; } }

        public void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;

            Component sensor;
            if (ApiManager.Instance.Sensors.TryGetValue(uid, out sensor))
            {
                bool enabled = false;

                if (sensor is VideoToROS)
                {
                    var camera = sensor as VideoToROS;
                    enabled = camera.enabled;
                }
                else if (sensor is LidarSensor)
                {
                    var lidar = sensor as LidarSensor;
                    enabled = lidar.enabled;
                }
                else if (sensor is ImuSensor)
                {
                    var imu = sensor as ImuSensor;
                    enabled = imu.enabled && imu.IsEnabled;
                }
                else if (sensor is GpsDevice)
                {
                    var gps = sensor as GpsDevice;
                    enabled = gps.enabled && gps.PublishMessage;
                }
                else if (sensor is RadarSensor)
                {
                    var radar = sensor as RadarSensor;
                    enabled = radar.enabled && radar.IsEnabled;
                }
                else if (sensor is CanBus)
                {
                    var canbus = sensor as CanBus;
                    enabled = canbus.enabled;
                }

                ApiManager.Instance.SendResult(new JSONBool(enabled));
            }
            else
            {
                ApiManager.Instance.SendError($"Sensor '{uid}' not found");
            }
        }
    }
}
