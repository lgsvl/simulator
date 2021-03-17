/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using System.Threading.Tasks;

namespace Simulator.Api.Commands
{
    class Reset : IDistributedCommand
    {
        public string Name => "simulator/reset";

        private static async Task ResetAsync(Reset sourceCommand)
        {
            var api = ApiManager.Instance;
            await api.Reset();
            ApiManager.Instance.SendResult(sourceCommand);
        }

        public void Execute(JSONNode args)
        {
            var nonBlockingTask = ResetAsync(this);
        }
    }
}
