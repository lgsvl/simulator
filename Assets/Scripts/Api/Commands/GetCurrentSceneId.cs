/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;

namespace Simulator.Api.Commands
{
    class GetCurrentSceneId : ICommand
    {
        public string Name => "simulator/current_scene_id";

        public void Execute(JSONNode args)
        {
            var api = ApiManager.Instance;
            var scene = api.CurrentSceneId;
            if (string.IsNullOrEmpty(scene))
            {
                api.SendResult(this);
            }
            else
            {
                api.SendResult(this, new JSONString(scene));
            }
        }
    }
}
