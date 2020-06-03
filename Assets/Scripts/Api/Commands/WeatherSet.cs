/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;

namespace Simulator.Api.Commands
{
    class WeatherSet : ICommand
    {
        public string Name => "environment/weather/set";

        public void Execute(JSONNode args)
        {
            var api = ApiManager.Instance;
            var env = SimulatorManager.Instance.EnvironmentEffectsManager;
            if (env == null)
            {
                api.SendError(this, "Environment Effects Manager not found. Is the scene loaded?");
                return;
            }

            env.Rain = args["rain"].AsFloat;
            env.Fog = args["fog"].AsFloat;
            env.Wet = args["wetness"].AsFloat;

            api.SendResult(this);
        }
    }
}
