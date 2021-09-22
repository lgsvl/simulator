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
                var previousPosition = npc.transform.position;
                for (int i=0; i< waypoints.Count; i++)
                {
                    var deactivate = waypoints[i]["deactivate"];
                    var ts = waypoints[i]["timestamp"];
                    var position = waypoints[i]["position"].ReadVector3();
                    var angle = waypoints[i].HasKey("angle") ? 
                        waypoints[i]["angle"].ReadVector3() : 
                        Quaternion.LookRotation((position - previousPosition).normalized).eulerAngles;

                    wp.Add(new DriveWaypoint()
                    {
                        Position = position,
                        Speed = waypoints[i]["speed"].AsFloat,
                        Acceleration = waypoints[i]["acceleration"].AsFloat,
                        Angle = angle,
                        Idle = waypoints[i]["idle"].AsFloat,
                        Deactivate = deactivate.IsBoolean ? deactivate.AsBool : false,
                        TriggerDistance = waypoints[i]["trigger_distance"].AsFloat,
                        TimeStamp = (ts == null) ? -1 : waypoints[i]["timestamp"].AsFloat,
                        Trigger = WaypointTrigger.DeserializeTrigger(waypoints[i]["trigger"])
                    });
                    previousPosition = position;
                }

                var loopValue = loop.IsBoolean && loop.AsBool;
                var waypointFollow = npc.SetBehaviour<NPCWaypointBehaviour>();
                waypointFollow.SetFollowWaypoints(wp, loopValue, waypointsPathType); // TODO use NPCController to init waypoint data
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
