/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using UnityEngine;
using Simulator.Components;

namespace Simulator.Api.Commands
{
    class BridgeConnect : ICommand
    {
        public string Name => "vehicle/bridge/connect";

        public void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;
            var address = args["address"].Value;
            var port = args["port"].AsInt;

            var api = ApiManager.Instance;

            if (api.Agents.TryGetValue(uid, out GameObject obj))
            {
                var bridge = obj.GetComponentInChildren<BridgeClient>();
                if (bridge == null)
                {
                    api.SendError($"Agent '{uid}' is missing bridge client");
                }
                else
                {
                    bridge.Connect(address, port);
                    api.SendResult();
                }
            }
            else
            {
                api.SendError($"Agent '{uid}' not found");
            }
        }
    }
}
