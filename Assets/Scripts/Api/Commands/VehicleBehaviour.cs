/**
 * Copyright (c) 2019-2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using UnityEngine;
using System;
using System.Reflection;

namespace Simulator.Api.Commands
{
    class VehicleBehaviour : ICommand
    {
        public string Name => "vehicle/behaviour";

        public void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;
            var behaviour = args["behaviour"].Value;
            var api = ApiManager.Instance;

            if (!Simulator.Web.Config.NPCBehaviours.ContainsKey(behaviour))
            {
                api.SendError(this, $"could not find behaviour '{behaviour}'");
                return;
            }
            Type behaviourType = Simulator.Web.Config.NPCBehaviours[behaviour];
            if (behaviourType == null)
            {
                api.SendError(this, $"could not find behaviour '{behaviour}'");
                return;
            }

            if (api.Agents.TryGetValue(uid, out GameObject obj))
            {
                var npc = obj.GetComponent<NPCController>();
                if (npc == null)
                {
                    api.SendError(this, $"Agent '{uid}' is not a NPC agent");
                    return;
                }

                MethodInfo method = typeof(NPCController).GetMethod("SetBehaviour");
                MethodInfo generic = method.MakeGenericMethod(behaviourType);
                generic.Invoke(npc, null);

                api.SendResult(this);
            }
            else
            {
                api.SendError(this, $"Agent '{uid}' not found");
            }
        }
    }
}

