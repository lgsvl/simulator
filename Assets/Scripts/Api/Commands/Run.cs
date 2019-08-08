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

            var time_limit = args["time_limit"].AsFloat;
            if (time_limit != 0)
            {
                api.TimeLimit = api.CurrentTime + time_limit;
            }
            else
            {
                api.TimeLimit = 0.0;
            }

            var framerate = args["framerate"];
            if (framerate == null || framerate.IsNull)
            {
                api.Realtime = true;
            }
            else
            {
                api.TargetFrameRate = framerate;
                api.Realtime = false;
            }

            SimulatorManager.SetTimeScale(1.0f);
            SIM.LogAPI(SIM.API.SimulationRun, time_limit.ToString());
        }
    }
}
