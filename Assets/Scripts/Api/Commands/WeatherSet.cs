/**
* Copyright (c) 2019 LG Electronics, Inc.
*
* This software contains code licensed as described in LICENSE.
*
*/

using SimpleJSON;

namespace Api.Commands
{
    class WeatherSet : ICommand
    {
        public string Name { get { return "environment/weather/set"; } }

        public void Execute(JSONNode args)
        {
            var env = SimulatorManager.Instance.EnvironmentEffectsManager;
            var api = SimulatorManager.Instance.ApiManager;
            if (env == null)
            {
                api.SendError("Environment Effects Manager not found. Is the scene loaded?");
                return;
            }

            env.rain = args["rain"].AsFloat;
            env.fog = args["fog"].AsFloat;
            env.wet = args["wetness"].AsFloat;

            api.SendResult();
        }
    }
}
