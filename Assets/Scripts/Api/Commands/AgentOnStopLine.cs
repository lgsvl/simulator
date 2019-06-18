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
    class AgentOnStopLine : ICommand
    {
        public string Name { get { return "agent/on_stop_line"; } }

        public void Execute(JSONNode args)
        {
            var api = SimulatorManager.Instance.ApiManager;
            var uid = args["uid"].Value;
            
            if (api.Agents.TryGetValue(uid, out GameObject obj))
            {
                api.StopLine.Add(obj);
                api.SendResult();
            }
            else
            {
                api.SendError($"Agent '{uid}' not found");
            }
        }
    }
}
