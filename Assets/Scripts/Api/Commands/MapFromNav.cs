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
    class MapFromNav : ICommand
    {
        public string Name => "map/from_nav";

        public void Execute(JSONNode args)
        {
            var api = ApiManager.Instance;

            var position = args["position"].ReadVector3();
            var orientation = args["orientation"].ReadQuaternion();

            var nav = NavOrigin.Find();
            if (nav == null)
            {
                api.SendError(this, "NavOrigin not found");
                return;
            }

            var point = nav.FromNavPose(position, orientation);

            var res = new JSONObject();
            res.Add("position", point.position);
            res.Add("rotation", point.rotation.eulerAngles);

            api.SendResult(this, res);
        }
    }
}
