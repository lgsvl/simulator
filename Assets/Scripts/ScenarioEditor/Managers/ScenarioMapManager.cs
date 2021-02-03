/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Managers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Database;
    using ICSharpCode.SharpZipLib.Zip;
    using Database.Services;
    using UI.Utilities;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Web;

    /// <summary>
    /// Manager for calculating the map's meta-data, loading other maps and caching last loaded map
    /// </summary>
    public class ScenarioMapManager : IScenarioEditorExtension
    {
        /// <summary>
        /// Meta data of the available maps
        /// </summary>
        public class MapMetaData
        {
            /// <summary>
            /// Guid of the map
            /// </summary>
            public readonly string guid;

            /// <summary>
            /// User friendly name of the map
            /// </summary>
            public readonly string name;

            /// <summary>
            /// Guid of the asset loaded within this map
            /// </summary>
            public readonly string assetGuid;

            /// <summary>
            /// Asset model of the downloaded map, null if map is not cached yet
            /// </summary>
            public AssetModel assetModel;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="guid">Guid of the map</param>
            /// <param name="name">User friendly name of the map</param>
            /// <param name="assetGuid">Guid of the asset loaded within this map</param>
            public MapMetaData(string guid, string name, string assetGuid)
            {
                this.guid = guid;
                this.name = name;
                this.assetGuid = assetGuid;
                assetModel = null;
            }
        }

        /// <summary>
        /// Persistence data key for last loaded map
        /// </summary>
        private const string MapPersistenceKey = "Simulator/ScenarioEditor/MapManager/MapName";

        /// <summary>
        /// Currently loaded scene name after loading the map
        /// </summary>
        private string loadedSceneName;

        /// <summary>
        /// Map GUID which is currently being downloaded
        /// </summary>
        private string mapBeingDownloaded;

        /// <summary>
        /// Currently loaded map name
        /// </summary>
        public MapMetaData CurrentMapMetaData { get; private set; }

        /// <summary>
        /// Currently loaded map name
        /// </summary>
        public string CurrentMapName => CurrentMapMetaData?.name;

        /// <summary>
        /// Bounds of currently loaded map
        /// </summary>
        public Bounds CurrentMapBounds { get; private set; }

        /// <summary>
        /// List of meta data with available maps
        /// </summary>
        public List<MapMetaData> AvailableMaps { get; } = new List<MapMetaData>();

        /// <summary>
        /// Handler for snapping positions to the map lanes
        /// </summary>
        public LaneSnappingHandler LaneSnapping { get; } = new LaneSnappingHandler();

        /// <inheritdoc/>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Event invoked when the currently loaded map changes
        /// </summary>
        public event Action<MapMetaData> MapChanged;

        /// <inheritdoc/>
        /// <summary>
        /// Loads and lists all the available maps models from the cloud
        /// </summary>
        public async Task Initialize()
        {
            if (IsInitialized)
                return;
            var library = await ConnectionManager.API.GetLibrary<MapDetailData>();

            var assetService = new AssetService();
            var mapsInDatabase = assetService.List(BundleConfig.BundleTypes.Environment);
            var cachedMaps = mapsInDatabase as AssetModel[] ?? mapsInDatabase.ToArray();

            foreach (var mapDetailData in library)
            {
                var newMap = new MapMetaData(mapDetailData.Id, mapDetailData.Name, mapDetailData.AssetGuid)
                {
                    assetModel = cachedMaps.FirstOrDefault(cachedMap => cachedMap.AssetGuid == mapDetailData.AssetGuid)
                };
                AvailableMaps.Add(newMap);
            }

            IsInitialized = true;
            Debug.Log($"{GetType().Name} scenario editor extension has been initialized.");
        }

        /// <inheritdoc/>
        public void Deinitialize()
        {
            if (!IsInitialized)
                return;
            if (!string.IsNullOrEmpty(mapBeingDownloaded))
            {
                DownloadManager.StopAssetDownload(mapBeingDownloaded);
                mapBeingDownloaded = null;
            }

            UnloadMapAsync();
            IsInitialized = false;
            Debug.Log($"{GetType().Name} scenario editor extension has been deinitialized.");
        }

        /// <summary>
        /// Checks if map with given name is available in the database
        /// </summary>
        /// <param name="name">Map name to check in database</param>
        /// <returns>True if map exists in the database, false otherwise</returns>
        public bool MapExists(string name)
        {
            for (var i = 0; i < AvailableMaps.Count; i++)
            {
                var map = AvailableMaps[i];
                if (map.name == name) return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if map with given name is already downloaded
        /// </summary>
        /// <param name="name">Map name to check in the cache</param>
        /// <returns>True if map is downloaded, false otherwise</returns>
        public bool IsMapDownloaded(string name)
        {
            for (var i = 0; i < AvailableMaps.Count; i++)
            {
                var map = AvailableMaps[i];
                if (map.name == name) return map.assetModel != null;
            }

            return false;
        }

        /// <summary>
        /// Asynchronously loads the map, if map is not available last map will be loaded or any is both are unavailable
        /// </summary>
        /// <param name="mapName">Map name to be loaded, can be null to load last map</param>
        public async Task LoadMapAsync(string mapName = null)
        {
            if (!string.IsNullOrEmpty(CurrentMapName) && CurrentMapName == mapName)
                return;

            var loadingProcess = ScenarioManager.Instance.loadingPanel.AddProgress();
            loadingProcess.Update("Loading scenario map manager.");

            if (!string.IsNullOrEmpty(loadedSceneName))
                UnloadMapAsync();

            await Initialize();

            MapMetaData mapToLoad = null;
            if (!string.IsNullOrEmpty(mapName))
            {
                //Try to load named map
                for (var i = 0; i < AvailableMaps.Count; i++)
                {
                    var map = AvailableMaps[i];
                    if (map.name != mapName) continue;
                    //Download map if it's not available
                    if (map.assetModel == null)
                        await DownloadMap(map, loadingProcess);
                    mapToLoad = map;
                    break;
                }
            }

            if (mapToLoad == null)
            {
                var preferedMapName = PlayerPrefs.GetString(MapPersistenceKey, null);
                //Loads first downloaded map, or downloads first map in AvailableMaps
                for (var i = 0; i < AvailableMaps.Count; i++)
                {
                    var map = AvailableMaps[i];
                    if (map.assetModel == null) continue;
                    //Force prefered map if it is already downloaded
                    if (map.name == preferedMapName)
                    {
                        mapToLoad = map;
                        break;
                    }

                    mapToLoad = map;
                }

                //Download first map if there are no downloaded maps
                if (mapToLoad == null)
                {
                    var map = AvailableMaps[0];
                    await DownloadMap(map, loadingProcess);
                    mapToLoad = map;
                }
            }

            loadingProcess.Update($"Loading map {mapToLoad.name}.");
            await LoadMapAssets(mapToLoad.assetModel, mapToLoad);
            loadingProcess.Update(
                CurrentMapName == mapToLoad.name
                    ? $"Scenario map manager loaded {mapToLoad.name} map."
                    : $"Loaded {mapToLoad.name} map failed.");
            loadingProcess.NotifyCompletion();
        }

        /// <summary>
        /// Downloads selected map asynchronously and updates the loading information
        /// </summary>
        /// <param name="map">Map to download</param>
        /// <param name="loadingProcess">Loading process to update with progress</param>
        /// <returns>Task</returns>
        private async Task DownloadMap(MapMetaData map, LoadingPanel.LoadingProcess loadingProcess)
        {
            mapBeingDownloaded = map.assetGuid;
            var progressUpdate = new Progress<Tuple<string, float>>(p =>
            {
                loadingProcess?.Update($"Downloading {p.Item1} {p.Item2:F}%.");
            });
            map.assetModel =
                await DownloadManager.GetAsset(BundleConfig.BundleTypes.Environment, map.assetGuid, map.name, progressUpdate);
            mapBeingDownloaded = null;
        }

        /// <summary>
        /// Unloads current map asynchronously
        /// </summary>
        public void UnloadMapAsync()
        {
            if (string.IsNullOrEmpty(loadedSceneName))
                return;
            LaneSnapping.Deinitialize();
            SceneManager.UnloadSceneAsync(loadedSceneName);
            loadedSceneName = null;
        }

        /// <summary>
        /// Map assets loading task
        /// </summary>
        /// <param name="map">MapModel to be loaded</param>
        /// <param name="mapMetaData">Map meta data to be loaded</param>
        private async Task LoadMapAssets(AssetModel map, MapMetaData mapMetaData)
        {
            AssetBundle textureBundle = null;
            AssetBundle mapBundle = null;

            ZipFile zip = new ZipFile(map.LocalPath);
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
                    ScenarioManager.Instance.logPanel.EnqueueError(
                        $"Out of date Map AssetBundle: {manifest.assetName}. Please check content website for updated bundle or rebuild the bundle.");
                    return;
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
                    //Debug.LogError($"Failed to load environment from '{map.Name}' asset bundle");
                    return;
                }

                textureBundle?.LoadAllAssets();

                var scenes = mapBundle.GetAllScenePaths();
                if (scenes.Length != 1)
                {
                    //Debug.LogError($"Unsupported environment in '{map.Name}' asset bundle, only 1 scene expected");
                    return;
                }

                var sceneName = Path.GetFileNameWithoutExtension(scenes[0]);

                loadedSceneName = sceneName;
                var loader = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                while (!loader.isDone)
                    await Task.Delay(100);
                var scene = SceneManager.GetSceneByName(sceneName);
                SceneManager.SetActiveScene(scene);
                CurrentMapMetaData = mapMetaData;

                if (Loader.Instance.SimConfig != null)
                    Loader.Instance.SimConfig.MapName = CurrentMapMetaData.name;

                CurrentMapBounds = CalculateMapBounds(scene);
                LaneSnapping.Initialize();
                PlayerPrefs.SetString(MapPersistenceKey, CurrentMapMetaData.name);
                MapChanged?.Invoke(CurrentMapMetaData);
            }
            finally
            {
                textureBundle?.Unload(false);
                mapBundle?.Unload(false);
                zip.Close();
            }
        }

        /// <summary>
        /// Calculates bounds of the given scene
        /// </summary>
        /// <param name="scene">Scene which bounds will be calculated</param>
        /// <returns>Bounds of the given scene</returns>
        private Bounds CalculateMapBounds(Scene scene)
        {
            var gameObjectsOnScene = scene.GetRootGameObjects();
            var b = new Bounds(Vector3.zero, Vector3.zero);
            for (var i = 0; i < gameObjectsOnScene.Length; i++)
            {
                var gameObjectOnScene = gameObjectsOnScene[i];
                foreach (Renderer r in gameObjectOnScene.GetComponentsInChildren<Renderer>())
                {
                    b.Encapsulate(r.bounds);
                }
            }

            //Add margin to the bounds
            b.size += Vector3.one * 10;
            return b;
        }
    }
}