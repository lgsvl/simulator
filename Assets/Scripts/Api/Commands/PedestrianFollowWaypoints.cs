/**
 * Copyright (c) 2019-2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using UnityEngine;
using System.Collections.Generic;

namespace Simulator.Api.Commands
{
    class PedestrianWaypoints : ICommand
    {
        public string Name => "pedestrian/follow_waypoints";

        public void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;
            var waypoints = args["waypoints"].AsArray;
            var loop = args["loop"].AsBool;
            var api = ApiManager.Instance;

            if (waypoints.Count == 0)
            {
                api.SendError(this, $"Waypoint list is empty");
                return;
            }
            
            if (api.Agents.TryGetValue(uid, out GameObject obj))
            {
                var ped = obj.GetComponent<PedestrianController>();
                if (ped == null)
                {
                    api.SendError(this, $"Agent '{uid}' is not a pedestrian agent");
                    return;
                }

                var wp = new List<WalkWaypoint>();
                for (int i = 0; i < waypoints.Count; i++)
                {
                    wp.Add(new WalkWaypoint()
                    {
                        Position = waypoints[i]["position"].ReadVector3(),
                        Speed = waypoints[i]["speed"].AsFloat,
                        Idle = waypoints[i]["idle"].AsFloat,
                        TriggerDistance = waypoints[i]["trigger_distance"].AsFloat,
                        Trigger = DeserializeTrigger(waypoints[i]["trigger"])
                    });
                }

                ped.FollowWaypoints(wp, loop);
                api.RegisterAgentWithWaypoints(ped.gameObject);
                api.SendResult(this);
                SIM.LogAPI(SIM.API.FollowWaypoints, "Pedestrian");
            }
            else
            {
                api.SendError(this, $"Agent '{uid}' not found");
            }
        }

        private WaypointTrigger DeserializeTrigger(JSONNode data)
        {
            if (data == null)
                return null;
            var effectorsNode = data["effectors"].AsArray;
            var trigger = new WaypointTrigger();
            trigger.Effectors = new List<TriggerEffector>();
            for (int i = 0; i < effectorsNode.Count; i++)
            {
                var typeName = effectorsNode[i]["type_name"];
                var newEffector = TriggersManager.GetEffectorOfType(typeName);
                newEffector.DeserializeProperties(effectorsNode[i]["parameters"]);
                trigger.Effectors.Add(newEffector);
            }

            return trigger;

        }
    }
}
