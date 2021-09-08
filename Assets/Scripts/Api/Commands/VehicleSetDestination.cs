/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using UnityEngine;
using System.Linq;
using System.Reflection;
using Simulator.Utilities;
using Simulator.Sensors;

namespace Simulator.Api.Commands
{
    class VehicleSetDestination : ICommand
    {
        public string Name => "vehicle/set_destination";

        public void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;
            var position = args["transform"]["position"].ReadVector3();
            var rotation = args["transform"]["rotation"].ReadVector3();

            var api = ApiManager.Instance;
            if (api.Agents.TryGetValue(uid, out GameObject obj))
            {
                var sensors = obj.GetComponentsInChildren<SensorBase>();
                var destination_sensor = sensors.FirstOrDefault(s => s.GetType().GetCustomAttribute<SensorType>().Name == "Destination");

                if (destination_sensor == null)
                {
                    api.SendError(this, $"Agent '{uid}' does not have Destination Sensor");
                    return;
                }

                destination_sensor.GetType().GetMethod("SetDestination").Invoke(destination_sensor, new object[] { position, rotation });
                api.SendResult(this);
            }
            else
            {
                api.SendError(this, $"Agent '{uid}' not found");
            }
        }
    }
}
