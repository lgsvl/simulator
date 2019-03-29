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
    class AgentGetState : ICommand
    {
        public string Name { get { return "agent/get_state"; } }

        public void Execute(string client, JSONNode args)
        {
            var uid = args["uid"].Value;

            GameObject obj;
            if (ApiManager.Instance.Agents.TryGetValue(uid, out obj))
            {
                var result = new JSONObject();

                var ped = obj.GetComponent<PedestrianComponent>();
                if (ped == null)
                {
                    var tr = obj.transform;
                    var rb = obj.GetComponent<Rigidbody>();

                    var transform = new JSONObject();
                    transform.Add("position", tr.position);
                    transform.Add("rotation", tr.rotation.eulerAngles);

                    result.Add("transform", transform);
                    result.Add("velocity", rb.velocity);
                    result.Add("angular_velocity", rb.angularVelocity);
                }
                else
                {
                    var agent = ped.GetComponent<NavMeshAgent>();
                    var tr = agent.transform;

                    var transform = new JSONObject();
                    transform.Add("position", tr.position);
                    transform.Add("rotation", tr.rotation.eulerAngles);

                    result.Add("transform", transform);
                    result.Add("velocity", agent.velocity);
                    result.Add("angular_velocity", Vector3.zero);
                }

                ApiManager.Instance.SendResult(client, result);
            }
            else
            {
                ApiManager.Instance.SendError(client, $"Agent '{uid}' not found");
            }
        }
    }
}
