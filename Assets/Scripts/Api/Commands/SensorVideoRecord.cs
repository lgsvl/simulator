/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using Simulator.Sensors;

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

                if (sensor is VideoRecordingSensor recorder)
                {
                    bool result;

                    if (isStart)
                    {
                        result = recorder.StartRecording(filename);
                    }
                    else
                    {
                        result = recorder.StopRecording();
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
