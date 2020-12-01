/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using UnityEngine;

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

            env.Rain = Mathf.Clamp01(args["rain"].AsFloat);
            env.Fog = Mathf.Clamp01(args["fog"].AsFloat);
            env.Wet = Mathf.Clamp01(args["wetness"].AsFloat);
            env.Cloud = Mathf.Clamp01(args["cloudiness"].AsFloat);
            env.Damage = Mathf.Clamp01(args["damage"].AsFloat);

            api.SendResult(this);
        }
    }
}
