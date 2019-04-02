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
    class Raycast : ICommand
    {
        public string Name { get { return "simulator/raycast"; } }

        public void Execute(JSONNode args)
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
                ApiManager.Instance.SendResult(node);
            }
            else
            {
                ApiManager.Instance.SendResult();
            }
        }
    }
}
