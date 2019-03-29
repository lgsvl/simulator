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
    class AgentGetBoundinBox : ICommand
    {
        public string Name { get { return "agent/get_bounding_box"; } }

        public void Execute(string client, JSONNode args)
        {
            var uid = args["uid"].Value;

            GameObject obj;
            if (ApiManager.Instance.Agents.TryGetValue(uid, out obj))
            {
                var bounds = new Bounds();

                var vc = obj.GetComponent<VehicleController>();
                if (vc != null)
                {
                    var collider = vc.carCenter.GetComponent<BoxCollider>();
                    bounds.center = collider.center;
                    bounds.size = collider.size;
                }

                var npc = obj.GetComponent<NPCControllerComponent>();
                if (npc != null)
                {
                    var collider = npc.GetComponent<BoxCollider>();
                    bounds.center = collider.center;
                    bounds.size = collider.size;
                }

                var result = new JSONObject();
                result.Add("min", bounds.min);
                result.Add("max", bounds.max);
                ApiManager.Instance.SendResult(client, result);
           }
            else
            {
                ApiManager.Instance.SendError(client, $"Agent '{uid}' not found");
            }
        }
    }
}
