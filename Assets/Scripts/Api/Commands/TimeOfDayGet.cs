/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;

namespace Simulator.Api.Commands
{
    class TimeOfDayGet : ICommand
    {
        public string Name { get { return "environment/time/get"; } }

        public void Execute(JSONNode args)
        {
            var env = SimulatorManager.Instance.EnvironmentEffectsManager;
            var api = SimulatorManager.Instance.ApiManager;

            if (env == null)
            {
                api.SendError("Environment Effects Manager not found. Is the scene loaded?");
                return;
            }

            api.SendResult(new JSONNumber(env.currentTimeOfDay));
        }
    }
}
