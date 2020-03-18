/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;

namespace Simulator.Api.Commands
{
    class GetCurrentFrame : ICommand
    {
        public string Name => "simulator/current_frame";

        public void Execute(JSONNode args)
        {
            var api = ApiManager.Instance;
            var result = new JSONNumber(api.CurrentFrame);
            api.SendResult(this, result);
        }
    }
}
