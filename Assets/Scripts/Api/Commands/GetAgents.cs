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
    class GetAgents : ICommand
    {
        public string Name { get { return "simulator/get_agents"; } }

        public void Execute(string client, JSONNode args)
        {
            JSONArray result = new JSONArray();
            foreach (var a in ApiManager.Instance.Agents)
            {
                var uid = a.Key;
                var obj = a.Value;

                AgentType type = AgentType.Unknown;
                if (obj.GetComponent<VehicleController>() != null)
                {
                    type = AgentType.Ego;
                }
                else if (obj.GetComponent<NPCControllerComponent>() != null)
                {
                    type = AgentType.Npc;
                }
                else
                {
                    Debug.Assert(false);
                }

                var j = new JSONObject();
                j.Add("type", new JSONNumber((int)type));
                j.Add("uid", new JSONString(uid));

                result[result.Count] = j;
            }

            ApiManager.Instance.SendResult(client, result);
        }
    }
}
