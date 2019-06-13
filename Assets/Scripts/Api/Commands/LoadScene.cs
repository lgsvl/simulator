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

        //static IEnumerator LoadMenuAsync(string name)
        //{
        //    var loader = SceneManager.LoadSceneAsync("Menu");

        //    while (!loader.isDone)
        //    {
        //        yield return null;
        //    }

        //    yield return new WaitForEndOfFrame();
        //    yield return new WaitForEndOfFrame();

        //    yield return DoLoad(name);
        //}

        //static IEnumerator DoLoad(string name)
        //{
        //    Time.timeScale = 0;

        //    var api = ApiManager.Instance;
        //    api.Reset();

        //    SimulatorManager.Instance.npcManager.DespawnAllNPC();

        //    bool loaded = false;

        //    var menu = Object.FindObjectOfType<MenuManager>();
        //    if (menu.LoadScene(name, () => loaded = true) == false)
        //    {
        //        api.SendError($"Failed to load {name} scene");
        //        yield return null;
        //    }

        //    yield return new WaitUntil(() => loaded);
        //    yield return new WaitUntil(() => EnvironmentEffectsManager.Instance.InitDone);

        //    var parkedCars = GameObject.Find("ParkedCarHolder");
        //    parkedCars?.SetActive(false);

        //    api.CurrentScene = name;
        //    api.TimeLimit = 0.0;
        //    api.FrameLimit = 0;

        //    RosBridgeConnector.canConnect = true;

        //    api.SendResult();
        //}

        public void Execute(JSONNode args)
        {
            var name = args["scene"].Value;
            Debug.LogError("Load Scene API not implemented");
            
            //RosBridgeConnector.canConnect = false;
            //var menu = Object.FindObjectOfType<MenuManager>();
            //if (menu == null)
            //{
            //    Reset.Run();
            //    ApiManager.Instance.StartCoroutine(LoadMenuAsync(name));
            //}
            //else
            //{
            //    ApiManager.Instance.StartCoroutine(DoLoad(name));
            //}
        }
    }
}
