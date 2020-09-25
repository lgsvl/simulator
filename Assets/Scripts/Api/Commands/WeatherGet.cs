/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;

namespace Simulator.Api.Commands
{
    class WeatherGet : ICommand
    {
        public string Name => "environment/weather/get";

        public void Execute(JSONNode args)
        {
            var api = ApiManager.Instance;
            var env = SimulatorManager.Instance.EnvironmentEffectsManager;
            if (env == null)
            {
                api.SendError(this, "Environment Effects Manager not found. Is the scene loaded?");
                return;
            }

            var result = new JSONObject();
            result.Add("rain", new JSONNumber(env.Rain));
            result.Add("fog", new JSONNumber(env.Fog));
            result.Add("wetness", new JSONNumber(env.Wet));
            result.Add("cloudiness", new JSONNumber(env.Cloud));
            result.Add("damage", new JSONNumber(env.Damage));

            api.SendResult(this, result);
        }
    }
}
