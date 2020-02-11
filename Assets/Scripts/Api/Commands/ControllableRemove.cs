/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using Simulator.Controllable;

namespace Simulator.Api.Commands
{
    class ControllableRemove : ICommand
    {
        public string Name => "simulator/controllable_remove";

        public void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;
            var api = ApiManager.Instance;

            if (api.Controllables.TryGetValue(uid, out IControllable obj))
            {
                SimulatorManager.Instance.ControllableManager.RemoveControllable(uid, obj);
                api.SendResult();
            }
            else
            {
                api.SendError($"Agent '{uid}' not found");
            }
        }
    }
}
