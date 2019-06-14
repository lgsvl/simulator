/**
* Copyright (c) 2019 LG Electronics, Inc.
*
* This software contains code licensed as described in LICENSE.
*
*/

using SimpleJSON;
using UnityEngine;
using Simulator.Sensors;

namespace Api.Commands
{
    class VehicleSetFixedSpeed : ICommand
    {
        public string Name { get { return "vehicle/set_fixed_speed"; } }

        public void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;
            var isCruise = args["isCruise"].AsBool;
            var api = SimulatorManager.Instance.ApiManager;
            
            if (api.Agents.TryGetValue(uid, out GameObject obj))
            {
                var ccs = obj.GetComponentInChildren<CruiseControlSensor>();

                if (isCruise)
                    ccs.CruiseSpeed = args["speed"].AsFloat;
                else
                    ccs.CruiseSpeed = 0f;
                
                api.SendResult();
            }
            else
            {
                api.SendError($"Agent '{uid}' not found");
            }
        }
    }
}