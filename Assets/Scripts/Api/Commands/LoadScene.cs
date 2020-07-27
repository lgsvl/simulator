/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Text;
using System.IO;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using SimpleJSON;
using ICSharpCode.SharpZipLib.Zip;
using System.Threading.Tasks;

namespace Simulator.Api.Commands
{
    using Database;
    using Web;

    class LoadScene : IDistributedCommand
    {
        public string Name => "simulator/load_scene";

        private async Task LoadMap(JSONNode args, string mapId, int? seed = null)
        {
            var api = ApiManager.Instance;
            MapDetailData mapData = await ConnectionManager.API.GetByIdOrName<MapDetailData>(mapId);

            var ret = await DownloadManager.GetAsset(BundleConfig.BundleTypes.Environment, mapData.AssetGuid, mapData.Name);
            api.StartCoroutine(LoadMapAssets(this, mapData, ret.LocalPath, seed));
        }

        static IEnumerator LoadMapAssets(LoadScene sourceCommand, MapDetailData map, string localPath, int? seed = null)
        {
            var api = ApiManager.Instance;

            AssetBundle textureBundle = null;
            AssetBundle mapBundle = null;

            ZipFile zip = new ZipFile(localPath);
            try
            {
                Manifest manifest;
                ZipEntry entry = zip.GetEntry("manifest.json");
                using (var ms = zip.GetInputStream(entry))
                {
                    int streamSize = (int) entry.Size;
                    byte[] buffer = new byte[streamSize];
                    streamSize = ms.Read(buffer, 0, streamSize);
                    manifest = Newtonsoft.Json.JsonConvert.DeserializeObject<Manifest>(Encoding.UTF8.GetString(buffer));
                }

                if (manifest.assetFormat != BundleConfig.Versions[BundleConfig.BundleTypes.Environment])
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
                    api.SendError(sourceCommand, $"Failed to load environment from '{map.AssetGuid}' asset bundle '{map.Name}'");
                    api.ActionsSemaphore.Unlock();
                    yield break;
                }

                textureBundle?.LoadAllAssets();

                var scenes = mapBundle.GetAllScenePaths();
                if (scenes.Length != 1)
                {
                    api.SendError(sourceCommand, $"Unsupported environment in '{map.AssetGuid}' asset bundle '{map.Name}', only 1 scene expected");
                    api.ActionsSemaphore.Unlock();
                    yield break;
                }

                var sceneName = Path.GetFileNameWithoutExtension(scenes[0]);

                var loader = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
                yield return new WaitUntil(() => loader.isDone);
                SIM.LogAPI(SIM.API.SimulationLoad, sceneName);

                if (Loader.Instance.SimConfig != null)
                {
                    Loader.Instance.SimConfig.Seed = seed;
                    Loader.Instance.SimConfig.MapName = map.Name;
                    Loader.Instance.SimConfig.MapAssetGuid = map.AssetGuid;
                }

                var sim = Loader.CreateSimulatorManager();
                sim.Init(seed);

                if (Loader.Instance.CurrentSimulation != null && ConnectionManager.Status != ConnectionManager.ConnectionStatus.Offline)
                {
                    Loader.Instance.Status = SimulatorStatus.Running;
                }

            }
            finally
            {
                textureBundle?.Unload(false);
                mapBundle?.Unload(false);
                zip.Close();
            }

            api.Reset();
            api.CurrentScene = map.Name;
            api.ActionsSemaphore.Unlock();
            api.SendResult(sourceCommand);
        }

        public async void Execute(JSONNode args)
        {
            var api = ApiManager.Instance;
            var mapId = args["scene"].Value;
            int? seed = null;
            if (!args["seed"].IsNull)
            {
                seed = args["seed"].AsInt;
            }
            api.ActionsSemaphore.Lock();
            try
            {
                await LoadMap(args, mapId, seed);
            }
            catch(Exception e)
            {
                api.SendError(this, e.Message);
                // only unlock in error case as map loading continues in coroutine after which we unlock
                api.ActionsSemaphore.Unlock();
            }
        }
    }
}
