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
    class SetNavOrigin : ICommand
    {
        public string Name => "navigation/set_origin";

        public void Execute(JSONNode args)
        {
            var api = ApiManager.Instance;

            var position = args["transform"]["position"].ReadVector3();
            var rotation = args["transform"]["rotation"].ReadVector3();
            var offset = args["offset"].ReadVector3();

            NavOrigin.SetNavOrigin(position, Quaternion.Euler(rotation), offset);

            api.SendResult(this);
        }
    }
}
