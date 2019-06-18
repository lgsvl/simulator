/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using UnityEngine;
using Simulator.Sensors;

namespace Simulator.Api.Commands
{
    class SensorLidarSave : ICommand
    {
        public string Name { get { return "sensor/lidar/save"; } }

        public void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;
            var api = SimulatorManager.Instance.ApiManager;

            if (api.Sensors.TryGetValue(uid, out Component sensor))
            {
                if (sensor is LidarSensor)
                {
                    var lidar = sensor as LidarSensor;
                    var path = args["path"].Value;

                    var result = lidar.Save(path);
                    api.SendResult(result);
                }
                else
                {
                    api.SendError($"Sensor '{uid}' is not a lidar sensor");
                }
            }
            else
            {
                api.SendError($"Sensor '{uid}' not found");
            }
        }
    }
}
