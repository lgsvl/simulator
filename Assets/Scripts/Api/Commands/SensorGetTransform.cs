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

    class SensorGetTransform : SensorCommand
    {
        public override string Name => "sensor/transform/get";

        public override void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;
            var api = ApiManager.Instance;

            SensorBase sensor = null;
            if (SimulatorManager.InstanceAvailable)
                sensor = SimulatorManager.Instance.Sensors.GetSensor(uid);
            if (sensor!=null)
            {
                var tr = sensor.transform;
                var pos = tr.localPosition;
                var rot = tr.localRotation.eulerAngles;

                var result = new JSONObject();
                result.Add("position", pos);
                result.Add("rotation", rot);

                api.SendResult(this, result);
            }
            else
            {
                api.SendError(this, $"Sensor '{uid}' not found");
            }
        }
    }
}
