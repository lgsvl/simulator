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

namespace Api.Commands
{
    class GetSpawn : ICommand
    {
        public string Name { get { return "map/spawn/get"; } }

        public void Execute(JSONNode args)
        {
            var spawns = new JSONArray();
            var api = SimulatorManager.Instance.ApiManager;

            foreach (var spawn in Object.FindObjectsOfType<SpawnInfo>().OrderBy(spawn => spawn.name))
            {
                var position = spawn.transform.position;
                var rotation = spawn.transform.rotation.eulerAngles;

                var s = new JSONObject();
                s.Add("position", position);
                s.Add("rotation", rotation);
                spawns.Add(s);
            }
            api.SendResult(spawns);
        }
    }
}
