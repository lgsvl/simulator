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
        public string Name => "environment/time/get";

        public void Execute(JSONNode args)
        {
            var env = SimulatorManager.Instance.EnvironmentEffectsManager;
            var api = ApiManager.Instance;

            if (env == null)
            {
                api.SendError(this, "Environment Effects Manager not found. Is the scene loaded?");
                return;
            }

            api.SendResult(this, new JSONNumber(env.currentTimeOfDay));
        }
    }
}
