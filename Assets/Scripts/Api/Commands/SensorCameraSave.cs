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
    class SensorCameraSave : ICommand
    {
        public string Name => "sensor/camera/save";

        public void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;
            var api = ApiManager.Instance;

            if (api.Sensors.TryGetValue(uid, out Component sensor))
            {
                var path = args["path"].Value;
                var quality = args["quality"].AsInt;
                var compression = args["compression"].AsInt;

                if (sensor is ColorCameraSensor)
                {
                    var camera = sensor as ColorCameraSensor;
                    bool result = camera.Save(path, quality, compression);
                    api.SendResult(result);
                }
                else if (sensor is SemanticCameraSensor)
                {
                    var camera = sensor as SemanticCameraSensor;
                    bool result = camera.Save(path, quality, compression);
                    api.SendResult(result);
                }
                else
                {
                    api.SendError($"Sensor '{uid}' is not a camera sensor");
                }
            }
            else
            {
                api.SendError($"Sensor '{uid}' not found");
            }
        }
    }
}
