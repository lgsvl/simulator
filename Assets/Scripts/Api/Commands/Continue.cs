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
        public string Name { get { return "simulator/continue"; } }

        public void Execute(JSONNode args)
        {
            Time.timeScale = 1.0f;
        }
    }
}
