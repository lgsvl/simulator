/**
* Copyright (c) 2019 LG Electronics, Inc.
*
* This software contains code licensed as described in LICENSE.
*
*/

using SimpleJSON;
using UnityEngine;

namespace Api.Commands
{
    class SensorGetTransform : ICommand
    {
        public string Name { get { return "sensor/transform/get"; } }

        public void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;
            var api = SimulatorManager.Instance.ApiManager;

            if (api.Sensors.TryGetValue(uid, out Component sensor))
            {
                var tr = sensor.transform;
                var pos = tr.localPosition;
                var rot = tr.localRotation.eulerAngles;

                var result = new JSONObject();
                result.Add("position", tr.localPosition);
                result.Add("rotation", tr.localRotation.eulerAngles);

                api.SendResult(result);
            }
            else
            {
                api.SendError($"Sensor '{uid}' not found");
            }
        }
    }
}
