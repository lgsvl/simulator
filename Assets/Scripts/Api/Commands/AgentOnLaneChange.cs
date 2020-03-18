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
    class AgentOnLaneChange : ICommand
    {
        public string Name => "agent/on_lane_change";

        public void Execute(JSONNode args)
        {
            var api = ApiManager.Instance;
            var uid = args["uid"].Value;

            if (api.Agents.TryGetValue(uid, out GameObject obj))
            {
                api.LaneChange.Add(obj);
                api.SendResult(this);
                SIM.LogAPI(SIM.API.OnLaneChanged);
            }
            else
            {
                api.SendError(this, $"Agent '{uid}' not found");
            }
        }
    }
}
