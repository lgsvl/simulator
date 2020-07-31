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
            var sim = SimulatorManager.Instance;

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
                var frameLimit = (int)(timeLimit / Time.fixedDeltaTime);
                api.FrameLimit = api.CurrentFrame + frameLimit;
            }
            else
            {
                api.FrameLimit = 0;
            }

            SIM.LogAPI(SIM.API.SimulationRun, timeLimit.ToString());
            if (sim.NPCManager.startTime == 0f)
                sim.NPCManager.startTime = sim.CurrentTime;

            sim.AnalysisManager.AnalysisInit();
        }
    }
}
