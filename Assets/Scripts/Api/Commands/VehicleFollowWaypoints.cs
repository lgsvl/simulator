/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using UnityEngine;
using System.Collections.Generic;

namespace Simulator.Api.Commands
{
    class VehicleFollowWaypoints : ICommand
    {
        public string Name => "vehicle/follow_waypoints";

        public void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;
            var waypoints = args["waypoints"].AsArray;
            var loop = args["loop"];
            var api = ApiManager.Instance;

            if (waypoints.Count == 0)
            {
                api.SendError($"Waypoint list is empty");
                return;
            }
            
            if (api.Agents.TryGetValue(uid, out GameObject obj))
            {
                var npc = obj.GetComponent<NPCController>();
                if (npc == null)
                {
                    api.SendError( $"Agent '{uid}' is not a NPC agent");
                    return;
                }

                var wp = new List<DriveWaypoint>();
                for (int i=0; i< waypoints.Count; i++)
                {
                    var deactivate = waypoints[i]["deactivate"];

                    wp.Add(new DriveWaypoint()
                    {
                        Position = waypoints[i]["position"].ReadVector3(),
                        Speed = waypoints[i]["speed"].AsFloat,
                        Angle = waypoints[i]["angle"].ReadVector3(),
                        Idle = waypoints[i]["idle"].AsFloat,
                        Deactivate = deactivate.IsBoolean ? deactivate.AsBool : false,
                        TriggerDistance = waypoints[i]["trigger_distance"].AsFloat
                    }); ;
                }

                var loopValue = loop.IsBoolean ? loop.AsBool : false;
                npc.SetFollowWaypoints(wp, loopValue);
                api.SendResult();
                SIM.LogAPI(SIM.API.FollowWaypoints, "NPC");
            }
            else
            {
                api.SendError($"Agent '{uid}' not found");
            }
        }
    }
}
