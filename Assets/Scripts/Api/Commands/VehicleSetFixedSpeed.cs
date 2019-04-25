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
    class VehicleSetFixedSpeed : ICommand
    {
        public string Name { get { return "vehicle/set_fixed_speed"; } }

        public void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;
            var isCruise = args["isCruise"].AsBool;

            GameObject obj;
            if (ApiManager.Instance.Agents.TryGetValue(uid, out obj))
            {
                var vc = obj.GetComponent<VehicleController>();
                if (isCruise)
                {
                    var speed = args["speed"].AsFloat;
                    vc.EnableCruiseControl(speed);
                }
                else
                {
                    vc.DisableCruiseControl();
                }
                
                ApiManager.Instance.SendResult();
            }
            else
            {
                ApiManager.Instance.SendError($"Agent '{uid}' not found");
            }
        }
    }
}