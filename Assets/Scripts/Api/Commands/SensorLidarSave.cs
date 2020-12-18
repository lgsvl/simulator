/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using UnityEngine;
using Simulator.Sensors;
using System.Reflection;
using Simulator.Utilities;

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
                var sensorType = sensor.GetType().GetCustomAttribute<SensorType>();
                if (sensorType.Name == "Lidar")
                {
                    var path = args["path"].Value;

                    var result = (bool)sensor.GetType().GetMethod("Save").Invoke(sensor, new object[] { path });
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
