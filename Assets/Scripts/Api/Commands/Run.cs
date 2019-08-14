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
    class Run : ICommand
    {
        public string Name => "simulator/run";

        public void Execute(JSONNode args)
        {
            var api = ApiManager.Instance;

            var timeScale = args["time_scale"];
            if (timeScale == null || timeScale.IsNull)
            {
                api.TimeScale = 1f;
            }
            else
            {
                api.TimeScale = timeScale.AsFloat;
            }

            SimulatorManager.SetTimeScale(api.TimeScale);

            var timeLimit = args["time_limit"].AsFloat;
            if (timeLimit != 0)
            {
                api.TimeLimit = api.CurrentTime + timeLimit;
            }
            else
            {
                api.TimeLimit = 0.0;
            }

            SIM.LogAPI(SIM.API.SimulationRun, timeLimit.ToString());
        }
    }
}
