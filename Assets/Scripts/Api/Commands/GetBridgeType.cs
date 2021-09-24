/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using UnityEngine;
using Simulator.Bridge;
using Simulator.Components;

namespace Simulator.Api.Commands
{
    class GetBridgeType : ICommand
    {
        public string Name => "vehicle/bridge/type";

        public void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;
            var api = ApiManager.Instance;

            if (api.Agents.TryGetValue(uid, out GameObject obj))
            {
                var bridge = obj.GetComponentInChildren<BridgeClient>();
                if (bridge == null)
                {
                    api.SendError(this, $"Agent '{uid}' is missing bridge client");
                }
                else
                {
                    var result = bridge.Bridge.Plugin.GetBridgeNameAttribute().Type;
                    api.SendResult(this, result);
                }
            }
            else
            {
                api.SendError(this, $"Agent '{uid}' not found");
            }
        }
    }
}
