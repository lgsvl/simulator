/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using UnityEngine;
using Simulator.Map;

namespace Simulator.Api.Commands
{
    class GetNavOrigin : ICommand
    {
        public string Name => "navigation/get_origin";

        public void Execute(JSONNode args)
        {
            var api = ApiManager.Instance;

            var res = new JSONObject();
            var origin = GameObject.FindObjectOfType<NavOrigin>();

            if (origin != null)
            {
                var position = origin.transform.position;
                var rotation = origin.transform.rotation.eulerAngles;
                var offset = new Vector3(origin.OriginX, origin.OriginY, origin.Rotation);

                res.Add("position", position);
                res.Add("rotation", rotation);
                res.Add("offset", offset);
            }

            api.SendResult(this, res);
        }
    }
}
