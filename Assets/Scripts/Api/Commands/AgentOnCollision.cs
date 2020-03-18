/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using UnityEngine;

namespace Simulator.Api.Commands
{
    class AgentOnCollision : ICommand
    {
        public string Name => "agent/on_collision";

        public void Execute(JSONNode args)
        {
            var api = ApiManager.Instance;
            var uid = args["uid"].Value;
            
            if (api.Agents.TryGetValue(uid, out GameObject obj))
            {
                api.Collisions.Add(obj);
                api.SendResult(this);
                SIM.LogAPI(SIM.API.OnCollisionSet, obj.tag);
            }
            else
            {
                api.SendError(this, $"Agent '{uid}' not found");
            }
        }
    }
}
