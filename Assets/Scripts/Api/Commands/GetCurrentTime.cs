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
        public string Name => "simulator/current_time";

        public void Execute(JSONNode args)
        {
            var api = ApiManager.Instance;
            var result = new JSONNumber(api.CurrentTime);
            api.SendResult(this, result);
        }
    }
}
