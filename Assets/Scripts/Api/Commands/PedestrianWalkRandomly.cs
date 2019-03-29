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
    class PedestrianWalkRandomly : ICommand
    {
        public string Name { get { return "pedestrian/walk_randomly"; } }

        public void Execute(string client, JSONNode args)
        {
            var uid = args["uid"].Value;
            var enable = args["enable"].AsBool;

            GameObject obj;
            if (ApiManager.Instance.Agents.TryGetValue(uid, out obj))
            {
                var ped = obj.GetComponent<PedestrianComponent>();
                if (ped == null)
                {
                    ApiManager.Instance.SendError(client, $"Agent '{uid}' is not a pedestrian");
                    return;
                }

                ped.WalkRandomly(enable);

                ApiManager.Instance.SendResult(client, JSONNull.CreateOrGet());
            }
            else
            {
                ApiManager.Instance.SendError(client, $"Agent '{uid}' not found");
            }
        }
    }
}
