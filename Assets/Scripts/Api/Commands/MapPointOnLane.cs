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
    class MapPointOnLane : ICommand
    {
        public string Name { get { return "map/point_on_lane"; } }

        public void Execute(string client, JSONNode args)
        {
            var point = args["point"].ReadVector3();

            Vector3 position;
            Quaternion rotation;
            MapManager.Instance.GetPointOnLane(point, out position, out rotation);

            var j = new JSONObject();
            j.Add("position", position);
            j.Add("rotation", rotation.eulerAngles);

            ApiManager.Instance.SendResult(client, j);
        }
    }
}
