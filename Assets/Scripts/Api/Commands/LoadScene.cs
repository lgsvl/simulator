/**
* Copyright (c) 2019 LG Electronics, Inc.
*
* This software contains code licensed as described in LICENSE.
*
*/

using SimpleJSON;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Api.Commands
{
    class LoadScene : ICommand
    {
        public string Name { get { return "simulator/load_scene"; } }

        static void DoLoad(string client, string name)
        {
            Time.timeScale = 0;
            ApiManager.Instance.TimeLimit = 0.0;
            ApiManager.Instance.FrameLimit = 0;

            ApiManager.Instance.Agents.Clear();

            var agentManager = ROSAgentManager.Instance;
            agentManager.currentMode = StartModeTypes.API;
            agentManager.Clear();

            var menu = Object.FindObjectOfType<MenuManager>();
            menu.LoadScene(name, () =>
            {
                ApiManager.Instance.CurrentTime = 0.0;
                ApiManager.Instance.CurrentFrame = 0;
                ApiManager.Instance.SendResult(client, JSONNull.CreateOrGet());
            });
        }

        public void Execute(string client, JSONNode args)
        {
            var name = args["scene"].Value;

            var menu = Object.FindObjectOfType<MenuManager>();
            if (menu == null)
            {
                var loader = SceneManager.LoadSceneAsync("Menu");
                loader.completed += op => DoLoad(client, name);
            }
            else
            {
                DoLoad(client, name);
            }
        }
    }
}
