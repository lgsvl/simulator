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
    class SensorEnabledSet : SensorCommand
    {
        public override string Name => "sensor/enabled/set";

        public override void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;

            SensorBase sensor = null;
            if (SimulatorManager.InstanceAvailable)
                sensor = SimulatorManager.Instance.Sensors.GetSensor(uid);
            if (sensor!=null)
            {
                bool enabled = args["enabled"].AsBool;
                sensor.gameObject.SetActive(enabled);

                ApiManager.Instance.SendResult(this);
            }
            else
            {
                ApiManager.Instance.SendError(this, $"Sensor '{uid}' not found");
            }
        }
    }
}
