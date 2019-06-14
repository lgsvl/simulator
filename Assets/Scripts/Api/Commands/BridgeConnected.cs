/**
* Copyright (c) 2019 LG Electronics, Inc.
*
* This software contains code licensed as described in LICENSE.
*
*/

using SimpleJSON;
using UnityEngine;
using Simulator.Bridge;
using Simulator.Components;

namespace Api.Commands
{
    class BridgeConnected : ICommand
    {
        public string Name { get { return "vehicle/bridge/connected"; } }

        public void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;
            var api = SimulatorManager.Instance.ApiManager;

            if (api.Agents.TryGetValue(uid, out GameObject obj))
            {
                var bridge = obj.GetComponentInChildren<BridgeClient>();
                if (bridge == null)
                {
                    api.SendError($"Agent '{uid}' is missing bridge client");
                }
                else
                {
                    var result = new JSONBool(bridge.BridgeStatus == Status.Connected);
                    api.SendResult(result);
                }
            }
            else
            {
                api.SendError($"Agent '{uid}' not found");
            }
        }
    }
}
