/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;

namespace Simulator.Api.Commands
{
    class TimeOfDaySet : ICommand
    {
        public string Name => "environment/time/set";

        public void Execute(JSONNode args)
        {
            var env = SimulatorManager.Instance.EnvironmentEffectsManager;
            var api = ApiManager.Instance;

            if (env == null)
            {
                api.SendError(this, "Environment Effects Manager not found. Is the scene loaded?");
                return;
            }

            env.currentTimeOfDay = args["time"].AsFloat;
            env.currentTimeOfDayCycle = args["fixed"].AsBool ? EnvironmentEffectsManager.TimeOfDayCycleTypes.Freeze : EnvironmentEffectsManager.TimeOfDayCycleTypes.Normal;

            api.SendResult(this);
        }
    }
}
