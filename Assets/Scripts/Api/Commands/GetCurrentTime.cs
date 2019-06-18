/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;

namespace Simulator.Api.Commands
{
    class GetCurrentTime : ICommand
    {
        public string Name { get { return "simulator/current_time"; } }

        public void Execute(JSONNode args)
        {
            var api = SimulatorManager.Instance.ApiManager;
            var result = new JSONNumber(api.CurrentTime);
            api.SendResult(result);
        }
    }
}
