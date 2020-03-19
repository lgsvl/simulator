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
                var path = args["path"].Value;
                var quality = args["quality"].AsInt;
                var compression = args["compression"].AsInt;

                if (sensor is ColorCameraSensor)
                {
                    var camera = sensor as ColorCameraSensor;
                    bool result = camera.Save(path, quality, compression);
                    api.SendResult(this, result);
                }
                else if (sensor is SegmentationCameraSensor)
                {
                    var camera = sensor as SegmentationCameraSensor;
                    bool result = camera.Save(path, quality, compression);
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
