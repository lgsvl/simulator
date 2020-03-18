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
    class VehicleSetFixedSpeed : ICommand
    {
        public string Name => "vehicle/set_fixed_speed";

        public void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;
            var isCruise = args["isCruise"].AsBool;
            var api = ApiManager.Instance;

            if (api.Agents.TryGetValue(uid, out GameObject obj))
            {
                var ccs = obj.GetComponentInChildren<CruiseControlSensor>();
                if (ccs == null)
                {
                    api.SendError(this, $"Agent '{uid}' does not have CruiseControlSensor");
                    return;
                }

                ccs.CruiseSpeed = isCruise ? args["speed"].AsFloat : 0.0f;

                api.SendResult(this);
            }
            else
            {
                api.SendError(this, $"Agent '{uid}' not found");
            }
        }
    }
}