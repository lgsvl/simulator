/**
* Copyright (c) 2019 LG Electronics, Inc.
*
* This software contains code licensed as described in LICENSE.
*
*/

using SimpleJSON;
using UnityEngine;
using UnityEngine.AI;

namespace Api.Commands
{
    class WeatherGet : ICommand
    {
        public string Name { get { return "weather/get"; } }

        public void Execute(JSONNode args)
        {
            var env = EnvironmentEffectsManager.Instance;
            if (env == null)
            {
                ApiManager.Instance.SendError("Environment Effetcts Manager not found. Is the scene loaded?");
                return;
            }

            var result = new JSONObject();
            result.Add("rain", new JSONNumber(env.rainIntensitySlider.value));
            result.Add("fog", new JSONNumber(env.fogIntensitySlider.value));
            result.Add("wetness", new JSONNumber(env.roadWetnessSlider.value));

            ApiManager.Instance.SendResult(result);
        }
    }
}
