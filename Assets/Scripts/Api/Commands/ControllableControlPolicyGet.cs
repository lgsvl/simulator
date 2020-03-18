/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using SimpleJSON;
using Simulator.Controllable;

namespace Simulator.Api.Commands
{
    class ControllableControlPolicyGet : ICommand
    {
        public string Name => "controllable/control_policy/get";

        public void Execute(JSONNode args)
        {
            var api = ApiManager.Instance;
            var uid = args["uid"].Value;

            if (api.Controllables.TryGetValue(uid, out IControllable controllable))
            {
                JSONObject result = new JSONObject();
                result.Add("control_policy", controllable.CurrentControlPolicy);
                api.SendResult(this, result);
            }
            else
            {
                api.SendError(this, $"Controllable '{uid}' not found");
            }
        }
    }
}
