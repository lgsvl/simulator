/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
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
    class ControllableControlPolicySet : ICommand
    {
        public string Name => "controllable/control_policy/set";

        public void Execute(JSONNode args)
        {
            var api = ApiManager.Instance;
            var manager = SimulatorManager.Instance.ControllableManager;
            var uid = args["uid"].Value;

            if (manager.TryGetControllable(uid, out IControllable controllable))
            {
                List<ControlAction> controlActions;
                string errorMsg;
                if (args["control_policy"].IsArray)
                    controlActions = controllable.ParseControlPolicy(args["control_policy"].AsArray, out errorMsg);
                else 
                    controlActions = controllable.ParseControlPolicy(JSONNode.Parse(args["control_policy"].Value), out errorMsg);
                if (controlActions == null)
                {
                    api.SendError(this, errorMsg);
                    return;
                }

                controllable.CurrentControlPolicy = controlActions;
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
