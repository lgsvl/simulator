/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using System.Collections.Generic;
using Simulator.Utilities;
using Simulator.Controllable;
using Simulator.Network.Core.Identification;

namespace Simulator.Api.Commands
{
    class ControllableControlPolicySet : IDistributedCommand
    {
        public string Name => "controllable/control_policy/set";

        public void Execute(JSONNode args)
        {
            var api = ApiManager.Instance;
            var uid = args["uid"].Value;
            var controlPolicy = args["control_policy"].Value;

            if (api.Controllables.TryGetValue(uid, out IControllable controllable))
            {
                List<ControlAction> controlActions = controllable.ParseControlPolicy(controlPolicy, out string errorMsg);
                if (controlActions == null)
                {
                    api.SendError(this, errorMsg);
                    return;
                }

                controllable.CurrentControlPolicy = controlPolicy;
                controllable.Control(controlActions);

                api.SendResult(this);
            }
            else
            {
                api.SendError(this, $"Controllable '{uid}' not found");
            }
        }
    }
}
