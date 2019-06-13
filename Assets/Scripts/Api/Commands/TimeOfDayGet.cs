/**
* Copyright (c) 2019 LG Electronics, Inc.
*
* This software contains code licensed as described in LICENSE.
*
*/

using SimpleJSON;

namespace Api.Commands
{
    class TimeOfDayGet : ICommand
    {
        public string Name { get { return "environment/time/get"; } }

        public void Execute(JSONNode args)
        {
            var env = SimulatorManager.Instance.EnvironmentEffectsManager;
            if (env == null)
            {
                ApiManager.Instance.SendError("Environment Effects Manager not found. Is the scene loaded?");
                return;
            }

            ApiManager.Instance.SendResult(new JSONNumber(env.currentTimeOfDay));
        }
    }
}
