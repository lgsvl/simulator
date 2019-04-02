/**
* Copyright (c) 2019 LG Electronics, Inc.
*
* This software contains code licensed as described in LICENSE.
*
*/

using SimpleJSON;
using UnityEngine;
using System.Collections.Generic;

namespace Api.Commands
{
    class PedestrianWaypoints : ICommand
    {
        public string Name { get { return "pedestrian/follow_waypoints"; } }

        public void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;
            var waypoints = args["waypoints"].AsArray;
            var loop = args["loop"].AsBool;

            if (waypoints.Count == 0)
            {
                ApiManager.Instance.SendError($"Waypoint list is empty");
                return;
            }

            GameObject obj;
            if (ApiManager.Instance.Agents.TryGetValue(uid, out obj))
            {
                var ped = obj.GetComponent<PedestrianComponent>();
                if (ped == null)
                {
                    ApiManager.Instance.SendError($"Agent '{uid}' is not a pedestrian agent");
                    return;
                }

                var wp = new List<WalkWaypoint>();
                for (int i = 0; i < waypoints.Count; i++)
                {
                    wp.Add(new WalkWaypoint()
                    {
                        Position = waypoints[i]["position"].ReadVector3(),
                        Idle = waypoints[i]["idle"].AsFloat,
                    });
                }

                ped.FollowWaypoints(wp, loop);

                ApiManager.Instance.SendResult();
            }
            else
            {
                ApiManager.Instance.SendError($"Agent '{uid}' not found");
            }
        }
    }
}
