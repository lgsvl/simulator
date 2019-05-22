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
    class VehicleSetNPCPhysics : ICommand
    {
        public string Name { get { return "vehicle/set_npc_physics"; } }

        public void Execute(JSONNode args)
        {
            // var uid = args["uid"].Value;

            //GameObject obj;
            // if (ApiManager.Instance.Agents.TryGetValue(uid, out obj))
            // {
            var isPhysicsSimple = args["isPhysicsSimple"].AsBool;

                //var npc = obj.GetComponent<NPCControllerComponent>();
                // if (npc == null)
                // {
                //     ApiManager.Instance.SendError($"Agent '{uid}' is not a NPC agent");
                //     return;
                // }

                // npc.SetPhysicsMode(isPhysicsSimple);
            NPCManager.Instance.isSimplePhysics = isPhysicsSimple;
            ApiManager.Instance.SendResult();
            // }
            // else
            // {
            //     ApiManager.Instance.SendError($"Agent '{uid}' not found");
            // }
        }
    }
}