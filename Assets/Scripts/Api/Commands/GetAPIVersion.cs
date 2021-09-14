/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using Simulator.Utilities;
using UnityEngine;

namespace Simulator.Api.Commands
{
    class GetAPIVersion : ICommand
    {
        public string Name => "simulator/apiversion";

        public void Execute(JSONNode args)
        {
            ApiManager.Instance.SendResult(this, BundleConfig.Versions[BundleConfig.BundleTypes.API]);
        }
    }
}
