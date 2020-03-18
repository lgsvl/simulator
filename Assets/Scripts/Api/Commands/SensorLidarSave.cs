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
    class SensorLidarSave : SensorCommand
    {
        public override string Name => "sensor/lidar/save";

        public override void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;
            var api = ApiManager.Instance;

            SensorBase sensor = null;
            if (SimulatorManager.InstanceAvailable)
                sensor = SimulatorManager.Instance.Sensors.GetSensor(uid);
            if (sensor!=null)
            {
                if (sensor is LidarSensor lidar)
                {
                    var path = args["path"].Value;

                    var result = lidar.Save(path);
                    api.SendResult(this, result);
                }
                else
                {
                    api.SendError(this, $"Sensor '{uid}' is not a lidar sensor");
                }
            }
            else
            {
                api.SendError(this, $"Sensor '{uid}' not found");
            }
        }
    }
}
