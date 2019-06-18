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
    class VehicleFollowClosestLane : ICommand
    {
        public string Name { get { return "vehicle/follow_closest_lane"; } }

        public void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;
            var follow = args["follow"].AsBool;
            var maxSpeed = args["max_speed"].AsFloat;
            var isLaneChange = args["isLaneChange"].AsBool;
            var api = SimulatorManager.Instance.ApiManager;
            
            if (api.Agents.TryGetValue(uid, out GameObject obj))
            {
                var npc = obj.GetComponent<NPCController>();
                if (npc == null)
                {
                    api.SendError($"Agent '{uid}' is not a NPC agent");
                    return;
                }

                if (follow)
                {
                    npc.SetFollowClosestLane(maxSpeed, isLaneChange);
                }
                else
                {
                    npc.SetManualControl();
                }

                api.SendResult();
            }
            else
            {
                api.SendError($"Agent '{uid}' not found");
            }
        }
    }
}
