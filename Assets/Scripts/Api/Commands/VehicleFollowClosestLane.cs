/**
* Copyright (c) 2019 LG Electronics, Inc.
*
* This software contains code licensed as described in LICENSE.
*
*/

using SimpleJSON;
using UnityEngine;

namespace Api.Commands
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

            GameObject obj;
            if (ApiManager.Instance.Agents.TryGetValue(uid, out obj))
            {
                var npc = obj.GetComponent<NPCControllerComponent>();
                if (npc == null)
                {
                    ApiManager.Instance.SendError($"Agent '{uid}' is not a NPC agent");
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

                ApiManager.Instance.SendResult();
            }
            else
            {
                ApiManager.Instance.SendError($"Agent '{uid}' not found");
            }
        }
    }
}
