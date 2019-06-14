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
    class VehicleChangeLane : ICommand
    {
        public string Name { get { return "vehicle/change_lane"; } }

        public void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;
            var isLeft = args["isLeftChange"].AsBool;
            var api = SimulatorManager.Instance.ApiManager;

            if (api.Agents.TryGetValue(uid, out GameObject obj))
            {
                var npc = obj.GetComponent<NPCController>();
                if (npc == null)
                {
                    api.SendError($"Agent '{uid}' is not a NPC agent");
                    return;
                }

                npc.ForceLaneChange(isLeft);

                api.SendResult();
            }
            else
            {
                api.SendError($"Agent '{uid}' not found");
            }
        }
    }
}