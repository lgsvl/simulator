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
    using System;
    using Utilities;

    class VehicleFollowWaypoints : ICommand
    {
        public string Name => "vehicle/follow_waypoints";

        public void Execute(JSONNode args)
        {
            var api = ApiManager.Instance;
            var uid = args["uid"].Value;
            var waypoints = args["waypoints"].AsArray;
            // Try parse the path type, set linear as default
            var pathTypeNode = args["waypoints_path_type"];
            WaypointsPathType waypointsPathType;
            if (pathTypeNode == null)
            {
                waypointsPathType = WaypointsPathType.Linear;
            }
            else if (!Enum.TryParse(pathTypeNode, true, out waypointsPathType))
            {
                waypointsPathType = WaypointsPathType.Linear;
                api.SendError(this, $"Could not parse the waypoints path type \"{waypointsPathType}\".");
            }
            var loop = args["loop"];

            if (waypoints.Count == 0)
            {
                api.SendError(this, $"Waypoint list is empty");
                return;
            }
            
            if (api.Agents.TryGetValue(uid, out GameObject obj))
            {
                var npc = obj.GetComponent<NPCController>();
                if (npc == null)
                {
                    api.SendError(this, $"Agent '{uid}' is not a NPC agent");
                    return;
                }

                var wp = new List<DriveWaypoint>();
                for (int i=0; i< waypoints.Count; i++)
                {
                    var deactivate = waypoints[i]["deactivate"];
                    var ts = waypoints[i]["timestamp"];

                    wp.Add(new DriveWaypoint()
                    {
                        Position = waypoints[i]["position"].ReadVector3(),
                        Speed = waypoints[i]["speed"].AsFloat,
                        Angle = waypoints[i]["angle"].ReadVector3(),
                        Idle = waypoints[i]["idle"].AsFloat,
                        Deactivate = deactivate.IsBoolean ? deactivate.AsBool : false,
                        TriggerDistance = waypoints[i]["trigger_distance"].AsFloat,
                        TimeStamp = (ts == null) ? -1 : waypoints[i]["timestamp"].AsFloat,
                        Trigger = WaypointTrigger.DeserializeTrigger(waypoints[i]["trigger"])
                    }); ;
                }

                var loopValue = loop.IsBoolean ? loop.AsBool : false;
                var waypointFollow = npc.SetBehaviour<NPCWaypointBehaviour>();
                waypointFollow.SetFollowWaypoints(wp, loop, waypointsPathType); // TODO use NPCController to init waypoint data
                api.RegisterAgentWithWaypoints(npc.gameObject);
                api.SendResult(this);
            }
            else
            {
                api.SendError(this, $"Agent '{uid}' not found");
            }
        }
    }
    
}
