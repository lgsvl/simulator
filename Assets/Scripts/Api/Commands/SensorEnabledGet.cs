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
    class SensorEnabledGet : ICommand
    {
        public string Name => "sensor/enabled/get";

        public void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;

            Component sensor;
            if (ApiManager.Instance.Sensors.TryGetValue(uid, out sensor))
            {
                bool enabled = sensor.gameObject.activeSelf;
                ApiManager.Instance.SendResult(new JSONBool(enabled));
            }
            else
            {
                ApiManager.Instance.SendError($"Sensor '{uid}' not found");
            }
        }
    }
}
