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
    class VehicleEStop : ICommand
    {
        public string Name { get { return "vehicle/e_stop"; } }

        public void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;
            var isStop = args["isStop"].AsBool;

            GameObject obj;
            if (ApiManager.Instance.Agents.TryGetValue(uid, out obj))
            {
                var npc = obj.GetComponent<NPCControllerComponent>();
                if (npc == null)
                {
                    ApiManager.Instance.SendError($"Agent '{uid}' is not a NPC agent");
                    return;
                }

                npc.ForceEStop(isStop);

                ApiManager.Instance.SendResult();
            }
            else
            {
                ApiManager.Instance.SendError($"Agent '{uid}' not found");
            }
        }
    }
}