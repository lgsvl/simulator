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
        public string Name { get { return "sensor/get_transform"; } }

        public void Execute(string client, JSONNode args)
        {
            var uid = args["uid"].Value;

            Component sensor;
            if (ApiManager.Instance.Sensors.TryGetValue(uid, out sensor))
            {
                var tr = sensor.transform;
                var pos = tr.localPosition;
                var rot = tr.localRotation.eulerAngles;

                var result = new JSONObject();
                result.Add("position", tr.localPosition);
                result.Add("rotation", tr.localRotation.eulerAngles);

                ApiManager.Instance.SendResult(client, result);
            }
            else
            {
                ApiManager.Instance.SendError(client, $"Sensor '{uid}' not found");
            }
        }
    }
}
