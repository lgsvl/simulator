/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using Simulator.Utilities;
using UnityEngine;

namespace Api.Commands
{
    class GetVersion : ICommand
    {
        public string Name { get { return "simulator/version"; } }

        public void Execute(JSONNode args)
        {
            var api = SimulatorManager.Instance.ApiManager;
            var info = Resources.Load<BuildInfo>("BuildInfo");
            var result = new JSONString(info.Version);
            api.SendResult(result);
        }
    }
}
