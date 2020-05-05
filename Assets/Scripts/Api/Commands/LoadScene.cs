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
    using System;
    using Database;
    using Web;

    class LoadScene : IDistributedCommand
    {
        public string Name => "simulator/load_scene";

        private void LoadMap(JSONNode args, string name, int? seed = null)
        {
            var api = ApiManager.Instance;
            //Lock executing other API commands while map is being downloaded
            api.ActionsSemaphore.Lock();
            using (var db = Database.DatabaseManager.Open())
            {
                var sql = Sql.Builder.From("maps").Where("name = @0", name);
                var map = db.FirstOrDefault<Database.MapModel>(sql);
                if (map == null)
                {
                    var url = args["url"].Value;
                    //Disable using url on master simulation
                    if (Loader.Instance.Network.IsMaster || string.IsNullOrEmpty(url))
                    {
                        api.SendError(this, $"Environment '{name}' is not available");
                        return;
                    }

                    DownloadMapFromUrl(this, args, name, seed, url);
                    return;
                }
                // Add uid key to arguments, as it will be distributed to the clients' simulations
                if (Loader.Instance.Network.IsMaster)
                    args.Add("url", map.Url);

                api.StartCoroutine(LoadMapAssets(this, map, name, seed));
            }
        }

        static IEnumerator LoadMapAssets(LoadScene sourceCommand, MapModel map, string name, int? seed = null)
        {
            var api = ApiManager.Instance;

            AssetBundle textureBundle = null;
            AssetBundle mapBundle = null;

            ZipFile zip = new ZipFile(map.LocalPath);
            try
            {
                Manifest manifest;
                ZipEntry entry = zip.GetEntry("manifest");
                using (var ms = zip.GetInputStream(entry))
                {
                    int streamSize = (int) entry.Size;
                    byte[] buffer = new byte[streamSize];
                    streamSize = ms.Read(buffer, 0, streamSize);
                    manifest = new Deserializer().Deserialize<Manifest>(Encoding.UTF8.GetString(buffer));
                }

                if (manifest.bundleFormat != BundleConfig.Versions[BundleConfig.BundleTypes.Environment])
                {
                    api.SendError(sourceCommand, 
                        "Out of date Map AssetBundle. Please check content website for updated bundle or rebuild the bundle.");
                    api.ActionsSemaphore.Unlock();
                    yield break;
                }

                if (zip.FindEntry($"{manifest.assetGuid}_environment_textures", true) != -1)
                {
                    var texStream = zip.GetInputStream(zip.GetEntry($"{manifest.assetGuid}_environment_textures"));
                    textureBundle = AssetBundle.LoadFromStream(texStream, 0, 1 << 20);
                }

                string platform = SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows
                    ? "windows"
                    : "linux";
                var mapStream =
                    zip.GetInputStream(zip.GetEntry($"{manifest.assetGuid}_environment_main_{platform}"));
                mapBundle = AssetBundle.LoadFromStream(mapStream, 0, 1 << 20);

                if (mapBundle == null)
                {
                    api.SendError(sourceCommand, $"Failed to load environment from '{map.Name}' asset bundle");
                    api.ActionsSemaphore.Unlock();
                    yield break;
                }

                textureBundle?.LoadAllAssets();

                var scenes = mapBundle.GetAllScenePaths();
                if (scenes.Length != 1)
                {
                    api.SendError(sourceCommand, $"Unsupported environment in '{map.Name}' asset bundle, only 1 scene expected");
                    api.ActionsSemaphore.Unlock();
                    yield break;
                }

                var sceneName = Path.GetFileNameWithoutExtension(scenes[0]);

                var clusters = Loader.Instance.SimConfig?.Clusters;
                var isMasterSimulation = clusters != null && clusters.Length != 0;
                var loadAdditive = isMasterSimulation &&
                                   SceneManager.GetSceneByName(Loader.Instance.LoaderScene).isLoaded;
                var loader = SceneManager.LoadSceneAsync(sceneName,
                    loadAdditive ? LoadSceneMode.Additive : LoadSceneMode.Single);
                yield return new WaitUntil(() => loader.isDone);
                if (loadAdditive)
                    SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
                SIM.LogAPI(SIM.API.SimulationLoad, sceneName);

                if (Loader.Instance.SimConfig != null)
                {
                    Loader.Instance.SimConfig.Seed = seed;
                    Loader.Instance.SimConfig.MapName = name;
                    Loader.Instance.SimConfig.MapUrl = map.Url;
                }

                var sim = Loader.CreateSimulatorManager();
                sim.Init(seed);
                if (isMasterSimulation)
                    Loader.Instance.Network.Master.InitializeSimulation(sim.gameObject);
                else if (Loader.Instance.Network.IsClient)
                    Loader.Instance.Network.Client.InitializeSimulation(sim.gameObject);
            }
            finally
            {
                textureBundle?.Unload(false);
                mapBundle?.Unload(false);
                zip.Close();
            }

            api.Reset();
            api.CurrentScene = name;
            api.ActionsSemaphore.Unlock();
            api.SendResult(sourceCommand);
        }

        private static void DownloadMapFromUrl(LoadScene sourceCommand, JSONNode args, string name, int? seed, string url)
        {
            //Remove url from args, so download won't be retried
            args.Remove("url");
            var localPath = WebUtilities.GenerateLocalPath("Maps");
            DownloadManager.AddDownloadToQueue(new Uri(url), localPath, null, (success, ex) =>
            {
                if (success)
                {
                    var map = new MapModel()
                    {
                        Name = name,
                        Url = url,
                        LocalPath = localPath
                    };

                    using (var db = DatabaseManager.Open())
                    {
                        db.Insert(map);
                    }

                    ApiManager.Instance.StartCoroutine(LoadMapAssets(sourceCommand, map, name, seed));
                }
                else
                {
                    Debug.LogError(
                        $"Vehicle '{name}' is not available. Error occured while downloading from url: {ex}.");
                    ApiManager.Instance.SendError(sourceCommand, $"Vehicle '{name}' is not available");
                    ApiManager.Instance.ActionsSemaphore.Unlock();
                }
            });
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

            LoadMap(args, name, seed);
        }
    }
}
