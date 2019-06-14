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
    class PedestrianWalkRandomly : ICommand
    {
        public string Name { get { return "pedestrian/walk_randomly"; } }

        public void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;
            var enable = args["enable"].AsBool;
            var api = SimulatorManager.Instance.ApiManager;

            if (api.Agents.TryGetValue(uid, out GameObject obj))
            {
                var ped = obj.GetComponent<PedestrianController>();
                if (ped == null)
                {
                    api.SendError($"Agent '{uid}' is not a pedestrian");
                    return;
                }

                ped.WalkRandomly(enable);

                api.SendResult();
            }
            else
            {
                api.SendError($"Agent '{uid}' not found");
            }
        }
    }
}
