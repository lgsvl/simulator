/**
* Copyright (c) 2019 LG Electronics, Inc.
*
* This software contains code licensed as described in LICENSE.
*
*/

using SimpleJSON;
using UnityEngine;
using Simulator.Sensors;

namespace Api.Commands
{
    class SensorLidarSave : ICommand
    {
        public string Name { get { return "sensor/lidar/save"; } }

        public void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;

            Component sensor;
            if (ApiManager.Instance.Sensors.TryGetValue(uid, out sensor))
            {
                if (sensor is LidarSensor)
                {
                    var lidar = sensor as LidarSensor;
                    var path = args["path"].Value;

                    var result = lidar.Save(path);
                    ApiManager.Instance.SendResult(result);
                }
                else
                {
                    ApiManager.Instance.SendError($"Sensor '{uid}' is not a lidar sensor");
                }
            }
            else
            {
                ApiManager.Instance.SendError($"Sensor '{uid}' not found");
            }
        }
    }
}
