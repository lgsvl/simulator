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
    class ControllableCurrentStateGet : ICommand
    {
        public string Name => "controllable/current_state/get";

        public void Execute(JSONNode args)
        {
            var api = ApiManager.Instance;
            var uid = args["uid"].Value;

            if (api.Controllables.TryGetValue(uid, out IControllable controllable))
            {
                JSONObject result = new JSONObject();
                result.Add("state", controllable.CurrentState);
                api.SendResult(this, result);
            }
            else
            {
                api.SendError(this, $"Controllable '{uid}' not found");
            }
        }
    }
}
