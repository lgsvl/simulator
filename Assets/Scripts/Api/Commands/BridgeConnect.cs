/**
* Copyright (c) 2019 LG Electronics, Inc.
*
* This software contains code licensed as described in LICENSE.
*
*/

using SimpleJSON;
using UnityEngine;
using UnityEngine.AI;

namespace Api.Commands
{
    class BridgeConnect : ICommand
    {
        public string Name { get { return "vehicle/bridge/connect"; } }

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
                    var address = args["address"].Value;
                    var port = args["port"].AsInt;

                    setup.Connector.Connect(address, port);

                    ApiManager.Instance.SendResult();
                }
            }
            else
            {
                ApiManager.Instance.SendError($"Agent '{uid}' not found");
            }
        }
    }
}
