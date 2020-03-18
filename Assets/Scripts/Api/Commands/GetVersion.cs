/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using Simulator.Utilities;
using UnityEngine;

namespace Simulator.Api.Commands
{
    class GetVersion : ICommand
    {
        public string Name => "simulator/version";

        public void Execute(JSONNode args)
        {
            var info = Resources.Load<BuildInfo>("BuildInfo");
            var result = new JSONString(info == null ? "unknown" : info.Version);
            ApiManager.Instance.SendResult(this, result);
        }
    }
}
