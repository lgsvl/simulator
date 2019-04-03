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
    class SensorEnabledSet : ICommand
    {
        public string Name { get { return "sensor/enabled/set"; } }

        public void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;

            Component sensor;
            if (ApiManager.Instance.Sensors.TryGetValue(uid, out sensor))
            {
                bool enabled = args["enabled"].AsBool;

                if (sensor is VideoToROS)
                {
                    var camera = sensor as VideoToROS;
                    camera.Enable(enabled);
                }
                else if (sensor is LidarSensor)
                {
                    var lidar = sensor as LidarSensor;
                    lidar.enabled = enabled;
                }
                else if (sensor is ImuSensor)
                {
                    var imu = sensor as ImuSensor;
                    imu.enabled = imu.IsEnabled = enabled;
                }
                else if (sensor is GpsDevice)
                {
                    var gps = sensor as GpsDevice;
                    gps.enabled = gps.PublishMessage = enabled;
                }
                else if (sensor is RadarSensor)
                {
                    var radar = sensor as RadarSensor;
                    radar.Enable(enabled);
                }
                else if (sensor is CanBus)
                {
                    var canbus = sensor as CanBus;
                    canbus.enabled = enabled;
                }

                ApiManager.Instance.SendResult();
            }
            else
            {
                ApiManager.Instance.SendError($"Sensor '{uid}' not found");
            }
        }
    }
}
