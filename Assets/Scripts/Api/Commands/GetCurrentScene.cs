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

        public void Execute(string client, JSONNode args)
        {
            var scene = ApiManager.Instance.CurrentScene;
            if (string.IsNullOrEmpty(scene))
            {
                ApiManager.Instance.SendResult(client, JSONNull.CreateOrGet());
            }
            else
            {
                ApiManager.Instance.SendResult(client, new JSONString(scene));
            }
        }
    }
}
