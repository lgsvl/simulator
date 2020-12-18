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
    class SensorCameraSave : SensorCommand
    {
        public override string Name => "sensor/camera/save";

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
                var path = args["path"].Value;
                var quality = args["quality"].AsInt;
                var compression = args["compression"].AsInt;

                if (sensorType.Name == "Color Camera")
                {
                    bool result = (bool)sensor.GetType().GetMethod("Save").Invoke(sensor, new object[] { path, quality, compression});
                    api.SendResult(this, result);
                }
                else if (sensorType.Name == "Segmentation Camera")
                {
                    bool result = (bool)sensor.GetType().GetMethod("Save").Invoke(sensor, new object[] { path, quality, compression });
                    api.SendResult(this, result);
                }
                else
                {
                    api.SendError(this, $"Sensor '{uid}' is not a camera sensor");
                }
            }
            else
            {
                api.SendError(this, $"Sensor '{uid}' not found");
            }
        }
    }
}
