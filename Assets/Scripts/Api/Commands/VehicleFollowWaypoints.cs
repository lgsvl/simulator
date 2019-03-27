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
    class VehicleFollowWaypoints : ICommand
    {
        public string Name { get { return "vehicle/follow_waypoints"; } }

        public void Execute(string client, JSONNode args)
        {
            var uid = args["uid"].Value;
            var waypoints = args["waypoints"].AsArray;
            var loop = args["loop"].AsBool;

            if (waypoints.Count == 0)
            {
                ApiManager.Instance.SendError(client, $"Waypoint list is empty");
                return;
            }

            GameObject obj;
            if (ApiManager.Instance.Agents.TryGetValue(uid, out obj))
            {
                var npc = obj.GetComponent<NPCControllerComponent>();
                if (npc == null)
                {
                    ApiManager.Instance.SendError(client, $"Agent '{uid}' is not a NPC agent");
                    return;
                }

                var wp = new List<Waypoint>();
                for (int i=0; i< waypoints.Count; i++)
                {
                    wp.Add(new Waypoint()
                    {
                        Position = waypoints[i]["position"].ReadVector3(),
                        Speed = waypoints[i]["speed"].AsFloat,
                    });
                    if (i != 0)
                    {
                        UnityEngine.Debug.DrawLine(wp[i - 1].Position, wp[i].Position, Color.yellow, 1000);
                    }
                }

                npc.SetFollowWaypoints(wp, loop);

                ApiManager.Instance.SendResult(client, JSONNull.CreateOrGet());
            }
            else
            {
                ApiManager.Instance.SendError(client, $"Agent '{uid}' not found");
            }
        }
    }
}
