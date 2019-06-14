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
            var api = SimulatorManager.Instance.ApiManager;

            if (api.Agents.TryGetValue(uid, out GameObject obj))
            {
                // TODO
                Debug.LogWarning("TODO Bridge Connect API");
                //var setup = obj.GetComponent<AgentSetup>();
                //if (setup == null)
                //{
                //    api.SendError($"Agent '{uid}' is not an EGO vehicle");
                //}
                //else
                //{
                //    var address = args["address"].Value;
                //    var port = args["port"].AsInt;
                    
                //    setup.Connector.Connect(address, port);

                //    api.SendResult();
                //}
            }
            else
            {
                api.SendError($"Agent '{uid}' not found");
            }
        }
    }
}
