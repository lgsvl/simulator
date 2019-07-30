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

namespace Simulator.Api.Commands
{
    class LoadScene : ICommand
    {
        public string Name => "simulator/load_scene";
        
        static IEnumerator DoLoad(string name, int? seed = null)
        {
            var api = ApiManager.Instance;

            using (var db = Database.DatabaseManager.Open())
            {
                var sql = Sql.Builder.From("maps").Where("name = @0", name);
                var map = db.SingleOrDefault<Database.MapModel>(sql);
                if (map == null)
                {
                    if (map == null)
                    {
                        api.SendError($"Environment '{name}' is not available");
                        yield break;
                    }
                }
                var bundlePath = map.LocalPath;

                var mapBundle = AssetBundle.LoadFromFile(bundlePath);
                if (mapBundle == null)
                {
                    api.SendError($"Failed to load environment from '{bundlePath}' asset bundle");
                    yield break;
                }
                try
                {

                    var scenes = mapBundle.GetAllScenePaths();
                    if (scenes.Length != 1)
                    {
                        api.SendError($"Unsupported environment in '{mapBundle}' asset bundle, only 1 scene expected");
                        yield break;
                    }

                    var sceneName = Path.GetFileNameWithoutExtension(scenes[0]);

                    var loader = SceneManager.LoadSceneAsync(sceneName);
                    yield return new WaitUntil(() => loader.isDone);
                }
                finally
                {
                    mapBundle.Unload(false);
                }

                var sim = Object.Instantiate(Loader.Instance.SimulatorManagerPrefab);
                sim.name = "SimulatorManager";
                sim.Init(seed);

                // TODO deactivate environment props if needed
                api.Reset();
                api.CurrentScene = name;
                api.SendResult();
            }
        }

        public void Execute(JSONNode args)
        {
            var api = ApiManager.Instance;
            var name = args["scene"].Value;
            int? seed = null;
            if (!args["seed"].IsNull)
            {
                seed = args["seed"].AsInt;
            }
            api.StartCoroutine(DoLoad(name, seed));
        }
    }
}
