/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using PetaPoco;
using SimpleJSON;

namespace Api.Commands
{
    class LoadScene : ICommand
    {
        public string Name { get { return "simulator/load_scene"; } }
        
        static IEnumerator DoLoad(string name)
        {
            using (var db = Simulator.Database.DatabaseManager.Open())
            {
                var sql = Sql.Builder.From("maps").Where("name = @0", name);
                var map = db.Single<Simulator.Database.MapModel>(sql);
                var bundlePath = map.LocalPath;
                var mapBundle = AssetBundle.LoadFromFile(bundlePath);
                var api = SimulatorManager.Instance.ApiManager;

                if (mapBundle == null)
                {
                    api.SendError($"Failed to load map from '{bundlePath}' asset bundle");
                }
                var scenes = mapBundle.GetAllScenePaths();
                if (scenes.Length != 1)
                {
                    api.SendError($"Unsupported environment in '{mapBundle}' asset bundle, only 1 scene expected");
                }

                var sceneName = Path.GetFileNameWithoutExtension(scenes[0]);

                var loader = SceneManager.LoadSceneAsync(sceneName);
                yield return new WaitUntil(() => loader.isDone);
                mapBundle.Unload(false);

                // TODO deactivate environment props if needed
                api.Reset();
                api.CurrentScene = name;
                api.SendResult();
            }
        }

        public void Execute(JSONNode args)
        {
            var api = SimulatorManager.Instance.ApiManager;
            var name = args["scene"].Value;
            api.StartCoroutine(DoLoad(name));
        }
    }
}
