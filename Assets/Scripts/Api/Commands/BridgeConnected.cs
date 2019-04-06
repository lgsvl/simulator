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
    class BridgeConnected : ICommand
    {
        public string Name { get { return "vehicle/bridge/connected"; } }

        public void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;

            GameObject obj;
            if (ApiManager.Instance.Agents.TryGetValue(uid, out obj))
            {
                var setup = obj.GetComponent<AgentSetup>();
                if (setup == null)
                {
                    ApiManager.Instance.SendError($"Agent '{uid}' is not an EGO vehicle");
                }
                else
                {
                    var result = new JSONBool(setup.Connector.Bridge.Status == Comm.BridgeStatus.Connected);
                    ApiManager.Instance.SendResult(result);
                }
            }
            else
            {
                ApiManager.Instance.SendError($"Agent '{uid}' not found");
            }
        }
    }
}
