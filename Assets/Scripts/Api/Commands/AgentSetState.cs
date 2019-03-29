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
    class AgentSetState : ICommand
    {
        public string Name { get { return "agent/set_state"; } }

        public void Execute(string client, JSONNode args)
        {
            var uid = args["uid"].Value;

            GameObject obj;
            if (ApiManager.Instance.Agents.TryGetValue(uid, out obj))
            {
                var position = args["state"]["transform"]["position"].ReadVector3();
                var rotation = args["state"]["transform"]["rotation"].ReadVector3();
                var velocity = args["state"]["velocity"].ReadVector3();
                var angular_velocity = args["state"]["angular_velocity"].ReadVector3();

                var ped = obj.GetComponent<PedestrianComponent>();
                if (ped == null)
                {
                    var tr = obj.transform;
                    tr.SetPositionAndRotation(position, Quaternion.Euler(rotation));

                    var rb = obj.GetComponent<Rigidbody>();
                    rb.velocity = velocity;
                    rb.angularVelocity = angular_velocity;
                }
                else
                {
                    var agent = ped.GetComponent<NavMeshAgent>();
                    agent.Warp(position);
                    agent.transform.rotation = Quaternion.Euler(rotation);
                    agent.velocity = velocity;
                }

                ApiManager.Instance.SendResult(client, JSONNull.CreateOrGet());
            }
            else
            {
                ApiManager.Instance.SendError(client, $"Agent '{uid}' not found");
            }
        }
    }
}
