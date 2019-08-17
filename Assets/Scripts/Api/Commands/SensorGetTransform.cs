/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using UnityEngine;

namespace Simulator.Api.Commands
{
    class SensorGetTransform : ICommand
    {
        public string Name => "sensor/transform/get";

        public void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;
            var api = ApiManager.Instance;

            if (api.Sensors.TryGetValue(uid, out Component sensor))
            {
                var tr = sensor.transform;
                var pos = tr.localPosition;
                var rot = tr.localRotation.eulerAngles;

                var result = new JSONObject();
                result.Add("position", pos);
                result.Add("rotation", rot);

                api.SendResult(result);
            }
            else
            {
                api.SendError($"Sensor '{uid}' not found");
            }
        }
    }
}
