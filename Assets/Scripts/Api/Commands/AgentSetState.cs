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

                var tr = obj.transform;
                tr.SetPositionAndRotation(position, Quaternion.Euler(rotation));

                var rb = obj.GetComponent<Rigidbody>();
                rb.velocity = velocity;
                rb.angularVelocity = angular_velocity;
                

                ApiManager.Instance.SendResult(client, JSONNull.CreateOrGet());
            }
            else
            {
                ApiManager.Instance.SendError(client, $"Agent '{uid}' not found");
            }
        }
    }
}
