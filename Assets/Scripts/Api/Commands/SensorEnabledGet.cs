/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using Simulator.Sensors;

namespace Simulator.Api.Commands
{

    class SensorEnabledGet : SensorCommand
    {
        public override string Name => "sensor/enabled/get";

        public override void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;

            SensorBase sensor = null;
            if (SimulatorManager.InstanceAvailable)
                sensor = SimulatorManager.Instance.Sensors.GetSensor(uid);
            if (sensor!=null)
            {
                bool enabled = sensor.gameObject.activeSelf;
                ApiManager.Instance.SendResult(this, new JSONBool(enabled));
            }
            else
            {
                ApiManager.Instance.SendError(this, $"Sensor '{uid}' not found");
            }
        }
    }
}
