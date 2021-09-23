/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using UnityEngine;
using System.Linq;
using Simulator.Utilities;

namespace Simulator.Api.Commands
{
    class GetSpawn : ICommand
    {
        public string Name => "map/spawn/get";

        public void Execute(JSONNode args)
        {
            var spawns = new JSONArray();
            var api = ApiManager.Instance;

            foreach (var spawn in Object.FindObjectsOfType<SpawnInfo>().OrderBy(spawn => spawn.name))
            {
                var position = spawn.transform.position;
                var rotation = spawn.transform.rotation.eulerAngles;
                var destinations = new JSONArray();
                foreach (var dest in spawn.Destinations)
                {
                    try
                    {
                        var DestPosition = dest.transform.position;
                        var DestRotation = dest.transform.rotation.eulerAngles;
                        var d = new JSONObject();
                        d.Add("position", DestPosition);
                        d.Add("rotation", DestRotation);
                        destinations.Add(d);
                    }
                    catch
                    {
                        Debug.LogError("Destination object linked to the spawn point is null or missing data.");
                    }
                }

                var s = new JSONObject();
                s.Add("position", position);
                s.Add("rotation", rotation);
                s.Add("destinations", destinations);
                spawns.Add(s);
            }
            api.SendResult(this, spawns);
        }
    }
}
