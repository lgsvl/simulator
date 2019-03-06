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
    class GetSpawn : ICommand
    {
        public string Name { get { return "simulator/get_spawn"; } }

        public void Execute(string client, JSONNode args)
        {
            var spawns = new JSONArray();
            foreach (var spawn in Object.FindObjectsOfType<SpawnInfo>())
            {
                var position = spawn.transform.position;
                var rotation = spawn.transform.rotation.eulerAngles;

                var s = new JSONObject();
                s.Add("position", position);
                s.Add("rotation", rotation);
                spawns.Add(s);
            }
            ApiManager.Instance.SendResult(client, spawns);
        }
    }
}
