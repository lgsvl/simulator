/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Text;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using PetaPoco;
using SimpleJSON;
using ICSharpCode.SharpZipLib.Zip;
using YamlDotNet.Serialization;
using Simulator.Network.Core.Identification;

namespace Simulator.Api.Commands
{
    class LoadScene : ICommand, IDistributedObject
    {
        public string Name => "simulator/load_scene";

        static IEnumerator DoLoad(string name, int? seed = null)
        {
            var api = ApiManager.Instance;

            using (var db = Database.DatabaseManager.Open())
            {
                var sql = Sql.Builder.From("maps").Where("name = @0", name);
                var map = db.FirstOrDefault<Database.MapModel>(sql);
                if (map == null)
                {
                    api.SendError($"Environment '{name}' is not available");
                    yield break;
                }

                AssetBundle textureBundle = null;
                AssetBundle mapBundle = null;

                ZipFile zip = new ZipFile(map.LocalPath);
                try
                {
                    Manifest manifest;
                    ZipEntry entry = zip.GetEntry("manifest");
                    using (var ms = zip.GetInputStream(entry))
                    {
                        int streamSize = (int)entry.Size;
                        byte[] buffer = new byte[streamSize];
                        streamSize = ms.Read(buffer, 0, streamSize);
                        manifest = new Deserializer().Deserialize<Manifest>(Encoding.UTF8.GetString(buffer));
                    }

                    if (manifest.bundleFormat != BundleConfig.MapBundleFormatVersion)
                    {
                        api.SendError("Out of date Map AssetBundle. Please check content website for updated bundle or rebuild the bundle.");
                        yield break;
                    }

                    if (zip.FindEntry($"{manifest.bundleGuid}_environment_textures", true) != -1)
                    {
                        var texStream = zip.GetInputStream(zip.GetEntry($"{manifest.bundleGuid}_environment_textures"));
                        textureBundle = AssetBundle.LoadFromStream(texStream, 0, 1 << 20);
                    }

                    string platform = SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows ? "windows" : "linux";
                    var mapStream = zip.GetInputStream(zip.GetEntry($"{manifest.bundleGuid}_environment_main_{platform}"));
                    mapBundle = AssetBundle.LoadFromStream(mapStream, 0, 1 << 20);

                    if (mapBundle == null)
                    {
                        api.SendError($"Failed to load environment from '{map.Name}' asset bundle");
                        yield break;
                    }

                    textureBundle?.LoadAllAssets();

                    var scenes = mapBundle.GetAllScenePaths();
                    if (scenes.Length != 1)
                    {
                        api.SendError($"Unsupported environment in '{map.Name}' asset bundle, only 1 scene expected");
                        yield break;
                    }

                    var sceneName = Path.GetFileNameWithoutExtension(scenes[0]);

                    var isMasterSimulation = Loader.Instance.SimConfig.Clusters.Length != 0;
                    var loader = SceneManager.LoadSceneAsync(sceneName, 
                        isMasterSimulation? LoadSceneMode.Additive : LoadSceneMode.Single);
                    yield return new WaitUntil(() => loader.isDone);
                    if (isMasterSimulation)
                        SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
                    SIM.LogAPI(SIM.API.SimulationLoad, sceneName);
                    
                    Loader.Instance.SimConfig.Seed = seed;
                    Loader.Instance.SimConfig.MapName = name;
                    Loader.Instance.SimConfig.MapUrl = map.Url;
                    var sim = Loader.CreateSimulatorManager();
                    sim.Init(seed);
                    if (isMasterSimulation)
                        Loader.Instance.Network.Master.InitializeSimulation(sim.gameObject);
                }
                finally
                {
                    textureBundle?.Unload(false);
                    mapBundle?.Unload(false);
                    zip.Close();
                }

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
