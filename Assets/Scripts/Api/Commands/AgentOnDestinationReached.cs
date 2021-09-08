/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using UnityEngine;

namespace Simulator.Api.Commands
{
    class AgentOnDestinationReached : ICommand
    {
        public string Name => "agent/on_destination_reached";

        public void Execute(JSONNode args)
        {
            var api = ApiManager.Instance;
            var uid = args["uid"].Value;

            if (api.Agents.TryGetValue(uid, out GameObject obj))
            {
                api.Destinations.Add(obj);
                api.SendResult(this);
            }
            else
            {
                api.SendError(this, $"Agent '{uid}' not found");
            }
        }
    }
}
