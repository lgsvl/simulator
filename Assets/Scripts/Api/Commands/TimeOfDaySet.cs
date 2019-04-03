/**
* Copyright (c) 2019 LG Electronics, Inc.
*
* This software contains code licensed as described in LICENSE.
*
*/

using SimpleJSON;

namespace Api.Commands
{
    class TimeOfDaySet : ICommand
    {
        public string Name { get { return "environment/time/set"; } }

        public void Execute(JSONNode args)
        {
            var env = EnvironmentEffectsManager.Instance;
            if (env == null)
            {
                ApiManager.Instance.SendError("Environment Effetcts Manager not found. Is the scene loaded?");
                return;
            }

            env.timeOfDaySlider.value = args["time"].AsFloat;
            env.freezeTimeOfDay = args["fixed"].AsBool;

            ApiManager.Instance.SendResult();
        }
    }
}
