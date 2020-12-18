/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using Simulator.Sensors;
using Simulator.Utilities;
using System.Reflection;

namespace Simulator.Api.Commands
{
    class SensorVideoRecord : SensorCommand
    {
        public override string Name => "sensor/video/record";

        public override void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;
            var api = ApiManager.Instance;

            SensorBase sensor = null;
            if (SimulatorManager.InstanceAvailable)
            {
                sensor = SimulatorManager.Instance.Sensors.GetSensor(uid);
            }

            if (sensor != null)
            {
                var isStart = args["is_start"].AsBool;
                var filename = args["filename"].Value;

                var sensorType = sensor.GetType().GetCustomAttribute<SensorType>();
                if (sensorType.Name == "Video Recording")
                {
                    bool result;

                    if (isStart)
                    {
                        result = (bool)sensor.GetType().GetMethod("StartRecording").Invoke(sensor, new object[] { filename });
                    }
                    else
                    {
                        result = (bool)sensor.GetType().GetMethod("StopRecording").Invoke(sensor, null);
                    }

                    api.SendResult(this, result);
                }
                else
                {
                    api.SendError(this, $"Sensor '{uid}' is not a video recording sensor");
                }
            }
            else
            {
                api.SendError(this, $"Sensor '{uid}' not found");
            }
        }
    }
}
