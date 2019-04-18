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

            GameObject obj;
            if (ApiManager.Instance.Agents.TryGetValue(uid, out obj))
            {
                var npc = obj.GetComponent<NPCControllerComponent>();
                if (npc == null)
                {
                    ApiManager.Instance.SendError($"Agent '{uid}' is not a NPC agent");
                    return;
                }

                npc.ForceLaneChange(isLeft);

                ApiManager.Instance.SendResult();
            }
            else
            {
                ApiManager.Instance.SendError($"Agent '{uid}' not found");
            }
        }
    }
}