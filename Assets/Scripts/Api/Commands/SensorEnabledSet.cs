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
    class SensorEnabledSet : ICommand
    {
        public string Name => "sensor/enabled/set";

        public void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;

            Component sensor;
            if (ApiManager.Instance.Sensors.TryGetValue(uid, out sensor))
            {
                bool enabled = args["enabled"].AsBool;
                sensor.gameObject.SetActive(enabled);

                ApiManager.Instance.SendResult();
            }
            else
            {
                ApiManager.Instance.SendError($"Sensor '{uid}' not found");
            }
        }
    }
}
