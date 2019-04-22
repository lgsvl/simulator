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
    class VehicleSetHazards : ICommand
    {
        public string Name { get { return "vehicle/set_hazards"; } }

        public void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;
            var isOn = args["isOn"].AsBool;

            GameObject obj;
            if (ApiManager.Instance.Agents.TryGetValue(uid, out obj))
            {
                var npc = obj.GetComponent<NPCControllerComponent>();
                if (npc == null)
                {
                    ApiManager.Instance.SendError($"Agent '{uid}' is not a NPC agent");
                    return;
                }
                
                npc.ForceNPCHazards(isOn);

                ApiManager.Instance.SendResult();
            }
            else
            {
                ApiManager.Instance.SendError($"Agent '{uid}' not found");
            }
        }
    }
}