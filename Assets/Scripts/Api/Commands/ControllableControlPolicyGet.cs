/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using SimpleJSON;
using Simulator.Controllable;

namespace Simulator.Api.Commands
{
    using Utilities;

    class ControllableControlPolicyGet : ICommand
    {
        public string Name => "controllable/control_policy/get";

        public void Execute(JSONNode args)
        {
            var api = ApiManager.Instance;
            var manager = SimulatorManager.Instance.ControllableManager;
            var uid = args["uid"].Value;

            if (manager.TryGetControllable(uid, out IControllable controllable))
            {
                JSONObject result = new JSONObject();
                result.Add("control_policy", controllable.SerializeControlPolicy());
                api.SendResult(this, result);
            }
            else
            {
                api.SendError(this, $"Controllable '{uid}' not found");
            }
        }
    }
}
