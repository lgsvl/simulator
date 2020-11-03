/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using System.Globalization;

namespace Simulator.Api.Commands
{
    class DateTimeGet : ICommand
    {
        public string Name => "simulator/datetime/get";

        public void Execute(JSONNode args)
        {
            var api = ApiManager.Instance;
            var env = SimulatorManager.Instance.EnvironmentEffectsManager;

            if (env == null)
            {
                api.SendError(this, "Environment Effects Manager not found. Is the scene loaded?");
                return;
            }

            string format = "G";
            var culture = CultureInfo.CreateSpecificCulture("de-DE");
            var result = new JSONString(env.CurrentDateTime.ToString(format, culture));
            api.SendResult(this, result);
        }
    }
}