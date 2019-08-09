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
    class Continue : ICommand
    {
        public string Name => "simulator/continue";

        public void Execute(JSONNode args)
        {
            SimulatorManager.Instance.SetTimeScale(1.0f);
        }
    }
}
