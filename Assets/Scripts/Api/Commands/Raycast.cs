/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using UnityEngine;

namespace Simulator.Api.Commands
{
    class Raycast : ICommand
    {
        public string Name => "simulator/raycast";

        public void Execute(JSONNode argsArray)
        {
            var api = ApiManager.Instance;
            var results = new JSONArray();

            var arr = argsArray.AsArray;

            foreach (var args in arr.Children)
            {
                var origin = args["origin"].ReadVector3();
                var direction = args["direction"].ReadVector3();
                var layer_mask = args["layer_mask"].AsInt;
                var max_distance = args["max_distance"].AsFloat;
                
                RaycastHit hit;
                if (Physics.Raycast(origin, direction, out hit, max_distance, layer_mask))
                {
                    var node = new JSONObject();
                    node.Add("distance", new JSONNumber(hit.distance));
                    node.Add("point", hit.point);
                    node.Add("normal", hit.normal);
                    results.Add(node);
                }
                else
                {
                    results.Add(JSONNull.CreateOrGet());
                }
            }
            api.SendResult(this, results);
        }
    }
}
