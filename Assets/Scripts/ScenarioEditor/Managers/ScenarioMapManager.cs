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
    using System.Linq;
    using System.Threading.Tasks;
    using Data;
    using Database;
    using Database.Services;
    using SimpleJSON;
    using UI.Utilities;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Web;

    /// <summary>
    /// Manager for calculating the map's meta-data, loading other maps and caching last loaded map
    /// </summary>
    public class ScenarioMapManager : MonoBehaviour, IScenarioEditorExtension, ISerializedExtension
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
        /// Currently loaded scene
        /// </summary>
        private Scene? loadedScene;

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
        /// Currently loaded scene
        /// </summary>
        public Scene? LoadedScene => loadedScene;

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

            var previousMap = CurrentMapMetaData;
            if (!string.IsNullOrEmpty(loadedSceneName))
                UnloadMapAsync();

            await Initialize();

            // Try to load the map
            MapMetaData mapToLoad = null;
            try
            {
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
            }
            catch (Exception ex)
            {
                if (previousMap != null)
                {
                    loadingProcess.Update(
                        $"Loading previous map {previousMap.name}.");
                    await LoadMapAssets(previousMap.assetModel, previousMap);
                }

                ScenarioManager.Instance.logPanel.EnqueueError($"Failed to load map {mapName}. Exception: {ex.Message}");
                loadingProcess.NotifyCompletion();
                return;
            }

            loadingProcess.Update(
                CurrentMapName == mapToLoad.name
                    ? $"Scenario map manager loaded {mapToLoad.name} map."
                    : $"Failed loaded the {mapToLoad.name} map.");
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
            ScenarioManager.Instance.ReportAssetDownload(map.assetGuid);
            var progressUpdate = new Progress<Tuple<string, float>>(p =>
            {
                loadingProcess?.Update($"Downloading {p.Item1} {p.Item2:F}%.");
            });
            map.assetModel =
                await DownloadManager.GetAsset(BundleConfig.BundleTypes.Environment, map.assetGuid, map.name,
                    progressUpdate);
            ScenarioManager.Instance.ReportAssetFinishedDownload(map.assetGuid);
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
            loadedScene = null;
        }

        /// <summary>
        /// Map assets loading task
        /// </summary>
        /// <param name="map">Map to be loaded</param>
        /// <param name="mapMetaData">Map meta data to be loaded</param>
        private async Task LoadMapAssets(AssetModel map, MapMetaData mapMetaData)
        {
            var loading = true;
            try
            {
                var callback = new Action<bool, string, string>((isDone, sceneName, mapBundlePath) =>
                {
                    var scene = SceneManager.GetSceneByName(sceneName);
                    SceneManager.SetActiveScene(scene);
                    CurrentMapMetaData = mapMetaData;

                    if (Loader.Instance.SimConfig != null)
                        Loader.Instance.SimConfig.MapName = CurrentMapMetaData.name;

                    DisableGravity(scene);
                    CurrentMapBounds = CalculateMapBounds(scene);
                    LaneSnapping.Initialize();
                    loadedSceneName = sceneName;
                    loadedScene = scene;
                    PlayerPrefs.SetString(MapPersistenceKey, CurrentMapMetaData.name);
                    loading = false;
                    MapChanged?.Invoke(CurrentMapMetaData);
                });
                Loader.LoadMap(map.AssetGuid, map.Name, LoadSceneMode.Additive, callback);
                while (loading)
                    await Task.Delay(100);
            }
            catch (Exception ex)
            {
                ScenarioManager.Instance.logPanel.EnqueueError(ex.Message);
                loading = false;
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
                foreach (var r in gameObjectOnScene.GetComponentsInChildren<Renderer>())
                {
                    b.Encapsulate(r.bounds);
                }
            }

            //Add margin to the bounds
            b.size += Vector3.one * 10;
            return b;
        }

        /// <summary>
        /// Disable gravity for the scene objects
        /// </summary>
        /// <param name="scene">Scene which bounds will be calculated</param>
        private void DisableGravity(Scene scene)
        {
            var gameObjectsOnScene = scene.GetRootGameObjects();
            for (var i = 0; i < gameObjectsOnScene.Length; i++)
            {
                var gameObjectOnScene = gameObjectsOnScene[i];
                foreach (var r in gameObjectOnScene.GetComponentsInChildren<Rigidbody>())
                {
                    r.useGravity = false;
                }
            }
        }

        /// <inheritdoc/>
        public bool Serialize(JSONNode data)
        {
            var mapNode = new JSONObject();
            data.Add("map", mapNode);
            mapNode.Add("id", new JSONString(CurrentMapMetaData.guid));
            mapNode.Add("name", new JSONString(CurrentMapMetaData.name));
            mapNode.Add("parameterType", new JSONString("map"));
            return true;
        }

        /// <inheritdoc/>
        public async Task<bool> Deserialize(JSONNode data)
        {
            var map = data["map"];
            if (map == null)
                return false;
            var mapName = map["name"];
            if (mapName == null)
                return false;
            var mapManager = ScenarioManager.Instance.GetExtension<ScenarioMapManager>();
            if (mapManager.CurrentMapName != mapName)
            {
                if (mapManager.MapExists(mapName))
                {
                    await mapManager.LoadMapAsync(mapName);
                    return true;
                }

                ScenarioManager.Instance.logPanel.EnqueueError(
                    $"Loaded scenario requires map {mapName} which is not available in the database.");
                return false;
            }

            await mapManager.LoadMapAsync(mapName);
            return true;
        }
    }
}