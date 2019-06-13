/**
* Copyright (c) 2019 LG Electronics, Inc.
*
* This software contains code licensed as described in LICENSE.
*
*/

using SimpleJSON;

namespace Api.Commands
{
    class GetCurrentScene : ICommand
    {
        public string Name { get { return "simulator/current_scene"; } }

        public void Execute(JSONNode args)
        {
            var scene = ApiManager.Instance.CurrentScene;
            if (string.IsNullOrEmpty(scene))
            {
                ApiManager.Instance.SendResult();
            }
            else
            {
                ApiManager.Instance.SendResult(new JSONString(scene));
            }
        }
    }
}
