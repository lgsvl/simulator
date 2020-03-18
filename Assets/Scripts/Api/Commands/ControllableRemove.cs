/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using Simulator.Controllable;
using Simulator.Network.Core.Identification;

namespace Simulator.Api.Commands
{
    class ControllableRemove : IDistributedCommand
    {
        public string Name => "simulator/controllable_remove";

        public void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;
            var api = ApiManager.Instance;

            if (api.Controllables.TryGetValue(uid, out IControllable obj))
            {
                SimulatorManager.Instance.ControllableManager.RemoveControllable(uid, obj);
                api.SendResult(this);
            }
            else
            {
                api.SendError(this, $"Agent '{uid}' not found");
            }
        }
    }
}
