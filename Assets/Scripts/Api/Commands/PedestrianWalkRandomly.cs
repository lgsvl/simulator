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
    class PedestrianWalkRandomly : ICommand
    {
        public string Name => "pedestrian/walk_randomly";

        public void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;
            var enable = args["enable"].AsBool;
            var api = ApiManager.Instance;

            if (api.Agents.TryGetValue(uid, out GameObject obj))
            {
                var ped = obj.GetComponent<PedestrianController>();
                if (ped == null)
                {
                    api.SendError(this, $"Agent '{uid}' is not a pedestrian");
                    return;
                }

                ped.WalkRandomly(enable);

                api.SendResult(this);
            }
            else
            {
                api.SendError(this, $"Agent '{uid}' not found");
            }
        }
    }
}
