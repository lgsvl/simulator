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
    class AgentGetState : ICommand
    {
        public string Name { get { return "agent/get_state"; } }

        public void Execute(string client, JSONNode args)
        {
            var uid = args["uid"].Value;

            GameObject obj;
            if (ApiManager.Instance.Agents.TryGetValue(uid, out obj))
            {
                var tr = obj.transform;
                var rb = obj.GetComponent<Rigidbody>();

                var transform = new JSONObject();
                transform.Add("position", tr.position);
                transform.Add("rotation", tr.rotation.eulerAngles);

                var result = new JSONObject();
                result.Add("transform", transform);
                result.Add("velocity", rb.velocity);
                result.Add("angular_velocity", rb.angularVelocity);

                ApiManager.Instance.SendResult(client, result);
            }
            else
            {
                ApiManager.Instance.SendError(client, $"Agent '{uid}' not found");
            }
        }
    }
}
