/**
* Copyright (c) 2019 LG Electronics, Inc.
*
* This software contains code licensed as described in LICENSE.
*
*/

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using SimpleJSON;

namespace Api.Commands
{
    class LoadScene : ICommand
    {
        public string Name { get { return "simulator/load_scene"; } }

        static IEnumerator LoadMenuAsync(string client, string name)
        {
            var loader = SceneManager.LoadSceneAsync("Menu");

            while (!loader.isDone)
            {
                yield return null;
            }

            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            DoLoad(client, name);
        }

        static void DoLoad(string client, string name)
        {
            Time.timeScale = 0;

            var api = ApiManager.Instance;
            api.Reset();

            NPCManager.Instance?.DespawnAllNPC();

            var menu = Object.FindObjectOfType<MenuManager>();
            menu.LoadScene(name, () =>
            {
                var parkedCars = GameObject.Find("ParkedCarHolder");
                parkedCars?.SetActive(false);

                api.CurrentScene = name;
                api.TimeLimit = 0.0;
                api.FrameLimit = 0;
                api.SendResult(client, JSONNull.CreateOrGet());
            });
        }

        public void Execute(string client, JSONNode args)
        {
            var name = args["scene"].Value;

            var menu = Object.FindObjectOfType<MenuManager>();
            if (menu == null)
            {
                Reset.Run();
                ApiManager.Instance.StartCoroutine(LoadMenuAsync(client, name));
            }
            else
            {
                DoLoad(client, name);
            }
        }
    }
}
