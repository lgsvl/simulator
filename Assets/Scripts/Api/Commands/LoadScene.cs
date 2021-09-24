/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
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
using Simulator.Web;

namespace Simulator.Api.Commands
{
    class LoadScene : IDistributedCommand, ILockingCommand
    {
        public string Name => "simulator/load_scene";

        public string LockingGuid { get; set; }

        public float StartRealtime { get; set; }

        public event Action<ILockingCommand> Executed;

        private async Task LoadMap(JSONNode args, string userMapId, int? seed = null)
        {
            var api = ApiManager.Instance;
            MapDetailData mapData = await ConnectionManager.API.GetByIdOrName<MapDetailData>(userMapId);
            var ret = await DownloadManager.GetAsset(BundleConfig.BundleTypes.Environment, mapData.AssetGuid, mapData.Name);
            api.StartCoroutine(LoadMapAssets(this, mapData, ret.LocalPath, userMapId, seed));
        }

        static IEnumerator LoadMapAssets(LoadScene sourceCommand, MapDetailData map, string localPath, string userMapId, int? seed = null)
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
                    int streamSize = (int)entry.Size;
                    byte[] buffer = new byte[streamSize];
                    streamSize = ms.Read(buffer, 0, streamSize);
                    manifest = Newtonsoft.Json.JsonConvert.DeserializeObject<Manifest>(Encoding.UTF8.GetString(buffer));
                }

                if (manifest.assetFormat != BundleConfig.Versions[BundleConfig.BundleTypes.Environment])
                {
                    zip.Close();
                    api.SendError(sourceCommand,
                        "Out of date Map AssetBundle. Please check content website for updated bundle or rebuild the bundle.");
                    sourceCommand.Executed?.Invoke(sourceCommand);
                    yield break;
                }

                if (zip.FindEntry($"{manifest.assetGuid}_environment_textures", true) != -1)
                {
                    entry = zip.GetEntry($"{manifest.assetGuid}_environment_textures");
                    var texStream = VirtualFileSystem.VirtualFileSystem.EnsureSeekable(zip.GetInputStream(entry), entry.Size);
                    textureBundle = AssetBundle.LoadFromStream(texStream, 0, 1 << 20);
                    texStream.Close();
                    texStream.Dispose();
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
                    sourceCommand.Executed?.Invoke(sourceCommand);
                    yield break;
                }

                textureBundle?.LoadAllAssets();

                var scenes = mapBundle.GetAllScenePaths();
                if (scenes.Length != 1)
                {
                    api.SendError(sourceCommand, $"Unsupported environment in '{map.AssetGuid}' asset bundle '{map.Name}', only 1 scene expected");
                    sourceCommand.Executed?.Invoke(sourceCommand);
                    yield break;
                }

                var sceneName = Path.GetFileNameWithoutExtension(scenes[0]);

                var loader = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
                yield return new WaitUntil(() => loader.isDone);

                if (Loader.Instance.SimConfig != null)
                {
                    Loader.Instance.SimConfig.Seed = seed;
                    Loader.Instance.SimConfig.MapName = map.Name;
                    Loader.Instance.SimConfig.MapAssetGuid = map.AssetGuid;
                }

                var sim = Loader.Instance.CreateSimulatorManager();
                sim.Init(seed);

                if (Loader.Instance.CurrentSimulation != null)
                {
                    Loader.Instance.reportStatus(SimulatorStatus.Running);
                }

            }
            finally
            {
                textureBundle?.Unload(false);
                mapBundle?.Unload(false);
                zip.Close();
            }

            var resetTask = api.Reset();
            while (!resetTask.IsCompleted)
                yield return null;
            api.CurrentSceneId = map.Id;
            api.CurrentSceneName = map.Name;
            api.CurrentScene = userMapId;
            sourceCommand.Executed?.Invoke(sourceCommand);
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
            try
            {
                await LoadMap(args, mapId, seed);
            }
            catch (Exception e)
            {
                api.SendError(this, e.Message);
                // only unlock in error case as map loading continues in coroutine after which we unlock
                Executed?.Invoke(this);
            }
        }
    }
}
