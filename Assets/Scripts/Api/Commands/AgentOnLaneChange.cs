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
    class AgentOnLaneChange : ICommand
    {
        public string Name { get { return "agent/on_lane_change"; } }

        public void Execute(JSONNode args)
        {
            var api = ApiManager.Instance;
            var uid = args["uid"].Value;

            GameObject obj;
            if (api.Agents.TryGetValue(uid, out obj))
            {
                api.LaneChange.Add(obj);
                api.SendResult();
            }
            else
            {
                api.SendError($"Agent '{uid}' not found");
            }
        }
    }
}
