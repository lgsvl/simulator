/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Simulator.Api;
using Simulator.Bridge;
using Simulator.Database;
using Simulator.Database.Services;
using Simulator.FMU;
using Simulator.Network.Core.Configs;
using Simulator.Network.Shared;
using Simulator.PointCloud.Trees;
using Simulator.Utilities;
using Simulator.Web;
using UnityEngine;
using UnityEngine.SceneManagement;
using VirtualFileSystem;

namespace Simulator
{
    using System.Reflection;
    using UnityEditor;

    public class AgentConfig
    {
        public string Name;
        public uint GTID;
        public string AssetGuid;
        public string AssetBundle;
        [NonSerialized]
        public GameObject Prefab;
        [NonSerialized]
        public GameObject AgentGO;
        public BridgeData BridgeData;
        public BridgePlugin Bridge;
        public string Connection;
        public SensorData[] Sensors;
        public Vector3 Position;
        public Quaternion Rotation;
        public AgentConfig() { }
        public AgentConfig(VehicleData vehicleData) // TODO refactor this
        {
            Name = vehicleData.Name;
            Connection = vehicleData.Bridge != null ? vehicleData.Bridge.ConnectionString : "";
            AssetGuid = vehicleData.AssetGuid;
#if UNITY_EDITOR
            if (vehicleData.Id.EndsWith(".prefab"))
            {
                AssetBundle = vehicleData.Id;
                AssetGuid = vehicleData.Id;
            }
            else
#endif
            {
                AssetBundle = Web.WebUtilities.GenerateLocalPath(vehicleData.AssetGuid, BundleConfig.BundleTypes.Vehicle);
            }
            Sensors = vehicleData.Sensors;

            BridgeData = vehicleData.Bridge;

            //Load sensors from the configuration if no sensors are set
            if ((Sensors == null || Sensors.Length == 0) && vehicleData.SensorsConfigurations != null && vehicleData.SensorsConfigurations.Length > 0)
            {
                Sensors = vehicleData.SensorsConfigurations[0].Sensors;
            }
        }
    }

    public class SimulationConfig
    {
        public string Name;
        public string MapName;
        public string MapAssetGuid;
        public string[] Clusters;
        public string ClusterName;
        public bool ApiOnly;
        public bool Headless;
        public bool Interactive;
        public DateTime TimeOfDay;
        public float Rain;
        public float Fog;
        public float Wetness;
        public float Cloudiness;
        public float Damage;
        public AgentConfig[] Agents;
        public bool UseTraffic;
        public bool UsePedestrians;
        public int? Seed;

        public string TestReportId;

        public SimulationConfig(SimulationData simulation)
        {
            Name = simulation.Name;
            Clusters = simulation.Cluster != null && simulation.Cluster.Instances.Length > 1 ? simulation.Cluster.Instances.SelectMany(i => i.Ip).ToArray() : new string[] { };
            ClusterName = simulation.Cluster.Name;
            ApiOnly = simulation.ApiOnly;
            Headless = simulation.Headless;
            Interactive = simulation.Interactive;
            TimeOfDay = simulation.TimeOfDay;
            Rain = simulation.Rain;
            Fog = simulation.Fog;
            Wetness = simulation.Wetness;
            Cloudiness = simulation.Cloudiness;
            Damage = simulation.Damage;
            UseTraffic = simulation.UseTraffic;
            UsePedestrians = simulation.UsePedestrians;
            Seed = simulation.Seed;
            if (simulation.Vehicles == null || simulation.Vehicles.Length == 0 || simulation.ApiOnly)
            {
                Agents = Array.Empty<AgentConfig>();
            }
            else
            {
                Agents = simulation.Vehicles.Select(v => new AgentConfig(v)).ToArray();
            }

            TestReportId = simulation.TestReportId;
        }
    }

    public enum SimulatorStatus
    {
        Idle,
        Loading,
        Starting,
        Running,
        Error,
        Stopping
    }

    public class Loader : MonoBehaviour
    {
        private static string ScenarioEditorSceneName = "ScenarioEditor";
        public static bool IsInScenarioEditor { get; private set; }

        public SimulationNetwork Network { get; } = new SimulationNetwork();
        public SimulatorManager SimulatorManagerPrefab;
        public ApiManager ApiManagerPrefab;
        public TestCaseProcessManager TestCaseProcessManagerPrefab;

        public NetworkSettings NetworkSettings;

        public ConnectionUI ConnectionUI => FindObjectOfType<ConnectionUI>();

        // NOTE: When simulation is not running this reference will be null.
        private SimulationData currentSimulation;
        public SimulationData CurrentSimulation
        {
            get { return currentSimulation; }
        }

        ConcurrentQueue<Action> Actions = new ConcurrentQueue<Action>();
        public string LoaderScene { get; private set; }

        public SimulationConfig SimConfig;

        private TestCaseProcessManager TCManager;

        // Loader object is never destroyed, even between scene reloads
        public static Loader Instance { get; private set; }
        private System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
        private SimulatorStatus status = SimulatorStatus.Idle;
        private ConcurrentDictionary<Task, string> assetDownloads = new ConcurrentDictionary<Task, string>();

        public bool EditorLoader { get; set; } = false;

        public SentrySdk Sentry;

        string reportedStatus(SimulatorStatus status)
        {
            switch (status)
            {
                case SimulatorStatus.Idle: return "Idle";
                // WISE does not care about Loading, just Starting
                case SimulatorStatus.Loading:
                case SimulatorStatus.Starting: return "Starting";
                case SimulatorStatus.Running: return "Running";
                case SimulatorStatus.Error: return "Error";
                case SimulatorStatus.Stopping: return "Stopping";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void reportStatus(SimulatorStatus value, string message = "")
        {
            Debug.Log($"loader status: {status}->{value} {message}");
            Console.WriteLine($"[LOADER] Update simulation status {status} -> {value}");

            if (value < status && !(status == SimulatorStatus.Stopping && value == SimulatorStatus.Idle))
            {
                throw new Exception($"Attemted to transition simulation status from {Enum.GetName(typeof(SimulatorStatus), value)} to {Enum.GetName(typeof(SimulatorStatus), status)}");
            }

            var previous = reportedStatus(status);
            var newStatus = reportedStatus(value);

            status = value;

            if (value == SimulatorStatus.Error && SimulatorManager.InstanceAvailable)
            {
                SimulatorManager.Instance.AnalysisManager.AddErrorEvent(message);
            }

            if (previous == newStatus)
                return;

            if (ConnectionManager.instance != null &&
                ConnectionManager.Status != ConnectionManager.ConnectionStatus.Offline &&
                CurrentSimulation != null)
            {
                ConnectionManager.instance.UpdateStatus(newStatus, CurrentSimulation.Id, message);
            }

            if (value == SimulatorStatus.Idle)
            {
                currentSimulation = null;
            }

            if (status == SimulatorStatus.Running)
            {
                WindowFlasher.Flash();
            }
        }

        public SimulatorStatus Status
        {
            get => status;
        }

        private void Start()
        {
            SimulatorConsole.Log.WriteLine("Loader Start");
            stopWatch.Start();
            var info = Resources.Load<BuildInfo>("BuildInfo");
            Application.wantsToQuit += CleanupOnExit;

            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            RenderLimiter.RenderLimitEnabled();

            if (!EditorLoader)
            {
                LoaderScene = SceneManager.GetActiveScene().name;
                DontDestroyOnLoad(this);
            }
            else
            {
#if UNITY_EDITOR
                SimulationData devSim;
                var devSettings = (Simulator.Editor.DevelopmentSettingsAsset)UnityEditor.AssetDatabase.LoadAssetAtPath("Assets/Resources/Editor/DeveloperSettings.asset", typeof(Simulator.Editor.DevelopmentSettingsAsset));
                if (devSettings != null && devSettings.developerSimulationJson != null)
                {
                    devSim = Newtonsoft.Json.JsonConvert.DeserializeObject<SimulationData>(devSettings.developerSimulationJson);
                }
                else
                {
                    devSim = new SimulationData();
                }

                StartSimulation(devSim);
#endif
            }

            if (!string.IsNullOrEmpty(info?.SentryDSN))
            {
                Sentry = gameObject.AddComponent<SentrySdk>();
                Sentry.Dsn = info.SentryDSN;
                Sentry.Version = $"{info?.Version}-{info?.GitCommit}";
                Sentry.SendDefaultPii = false;
                Sentry.Debug = false;
                Sentry.AutoGenerateBreadcrumb = false;
                Sentry.Reset();
            }
        }

        void OnApplicationQuit()
        {
            stopWatch.Stop();
        }

        private void Update()
        {
            while (Actions.TryDequeue(out var action))
            {
                action();
            }
        }

        public async void StartSimulation(SimulationData simData)
        {
            CloudAPI API = null;
            if (Instance.Status != SimulatorStatus.Idle)
            {
                Debug.LogWarning($"Received start simulation command while Simulator is not idle. (status: {Instance.Status})");
                return;
            }
            try
            {
#if UNITY_EDITOR
                // downloads still need simulator to be online, but in developer mode we don't have ConnectionManager
                if (ConnectionManager.instance == null)
                {
                    if (string.IsNullOrEmpty(Config.CloudProxy))
                    {
                        API = new CloudAPI(new Uri(Config.CloudUrl), Config.SimID);
                    }
                    else
                    {
                        API = new CloudAPI(new Uri(Config.CloudUrl), Config.SimID, new Uri(Config.CloudProxy));
                    }

                    var simInfo = CloudAPI.GetInfo();
                    var reader = await API.Connect(simInfo);
                    var streamReader = await API.EnsureConnectSuccess();
                    EditorApplication.playModeStateChanged += (PlayModeStateChange state) =>
                    {
                        if (state == PlayModeStateChange.ExitingPlayMode)
                        {
                            streamReader.Close();
                            streamReader.Dispose();
                            API.Disconnect();
                        }
                    };
                }
#endif
                currentSimulation = simData;
                reportStatus(SimulatorStatus.Loading);
                Network.Initialize(Config.SimID, simData.Cluster, NetworkSettings);

                var downloads = new List<Task>();
                if (simData.ApiOnly == false)
                {
                    if (simData.Map != null)
                    {
                        var task = DownloadManager.GetAsset(BundleConfig.BundleTypes.Environment, simData.Map.AssetGuid, simData.Map.Name);
                        downloads.Add(task);
                        assetDownloads.TryAdd(task, simData.Map.AssetGuid);
                    }

                    foreach (var vehicle in simData.Vehicles.Where(v => !v.Id.EndsWith(".prefab")).Select(v => v.AssetGuid).Distinct())
                    {
                        var task = DownloadManager.GetAsset(BundleConfig.BundleTypes.Vehicle, vehicle, simData.Vehicles.First(v => v.AssetGuid == vehicle).Name);
                        downloads.Add(task);
                        assetDownloads.TryAdd(task, vehicle);
                    }

                    List<string> bridgeGUIDs = new List<string>();
                    foreach (var vehicle in simData.Vehicles.Where(v => v.Bridge != null))
                    {
#if UNITY_EDITOR
                        // as of now, developer settings cannot look up bridge assetguid while offline.
                        if (string.IsNullOrEmpty(vehicle.Bridge.AssetGuid))
                        {
                            Debug.Log("missing assetguid on bridge, looking up by id " + vehicle.Bridge.Id);
                            var bridge = await API.GetByIdOrName<PluginDetailData>(vehicle.Bridge.Id);
                            if (bridge != null)
                            {
                                vehicle.Bridge.AssetGuid = bridge.AssetGuid;
                            }
                            else
                            {
                                Debug.LogWarning("bridge assetguid lookup failed.");
                            }
                        }
#endif
                        if (!bridgeGUIDs.Contains(vehicle.Bridge.AssetGuid))
                        {
                            Debug.Log($"adding bridge for vehicle {vehicle.Name}: {vehicle.Bridge.AssetGuid}");
                            bridgeGUIDs.Add(vehicle.Bridge.AssetGuid);
                            var task = DownloadManager.GetAsset(BundleConfig.BundleTypes.Bridge, vehicle.Bridge.AssetGuid, vehicle.Bridge.Name);
                            downloads.Add(task);
                            assetDownloads.TryAdd(task, vehicle.Bridge.AssetGuid);
                        }
                    }

                    List<SensorData> sensorsToDownload = new List<SensorData>();
                    foreach (var data in simData.Vehicles)
                    {
                        foreach (var plugin in data.Sensors)
                        {
#if UNITY_EDITOR
                            if (EditorPrefs.GetBool("Simulator/Developer Debug Mode", false) == true && Config.Sensors.FirstOrDefault(s => s.Name == plugin.Name) != null)
                            {
                                Debug.Log($"Sensor {plugin.Name} is not being downloaded, but used from cache or local sources. (Developer Debug Mode)");
                                continue;
                            }
                            // WISE stopped sending us assetGuids when in edit mode (no simId),
                            // so we have to look up while in play mode.
                            if (plugin.Plugin.AssetGuid == null)
                            {
                                var detail = await API.GetByIdOrName<PluginDetailData>(plugin.Plugin.Id);
                                plugin.Plugin.AssetGuid = detail.AssetGuid;
                            }
#endif
                            if (plugin.Plugin.AssetGuid != null
                                && sensorsToDownload.FirstOrDefault(s => s.Plugin.AssetGuid == plugin.Plugin.AssetGuid) == null
                            )
                            {
                                sensorsToDownload.Add(plugin);
                            }
                        }
                    }

                    foreach (var sensor in sensorsToDownload)
                    {
                        var pluginTask = DownloadManager.GetAsset(BundleConfig.BundleTypes.Sensor, sensor.Plugin.AssetGuid, sensor.Name);
                        downloads.Add(pluginTask);
                        assetDownloads.TryAdd(pluginTask, sensor.Plugin.AssetGuid);
                    }
                }

                if (ConnectionUI.instance != null)
                {
                    ConnectionUI.instance.SetLinkingButtonActive(false);
                    ConnectionUI.instance.SetVSEButtonActive(false);
                }

                await Task.WhenAll(downloads);
                foreach (var download in downloads)
                {
                    assetDownloads.TryRemove(download, out _);
                }

                if (simData.Vehicles != null)
                {
                    foreach (var vehicle in simData.Vehicles)
                    {
                        if (vehicle.Bridge != null)
                        {
                            if (vehicle.Bridge.AssetGuid != null)
                            {
                                var dir = Path.Combine(Config.PersistentDataPath, "Bridges");
                                var vfs = VfsEntry.makeRoot(dir);
                                Config.CheckDir(vfs.GetChild(vehicle.Bridge.AssetGuid), Config.LoadBridgePlugin);
                            }
                        }

                        foreach (var sensor in vehicle.Sensors)
                        {
                            if (sensor.Plugin?.AssetGuid != null) // TODO cleaner check for development mode sensors
                            {
                                var dir = Path.Combine(Config.PersistentDataPath, "Sensors");
                                var vfs = VfsEntry.makeRoot(dir);
                                Config.CheckDir(vfs.GetChild(sensor.Plugin.AssetGuid), Config.LoadSensorPlugin);
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(simData.Id))
                {
                    SimulationService simService = new SimulationService();
                    simService.AddOrUpdate(simData);
                }

                Debug.Log("All Downloads Complete");

                if (Status == SimulatorStatus.Stopping)
                {
                    Debug.Log("Simulation stop requested before simulation started.");
                    return;
                }

                if (!Network.IsClusterSimulation)
                {
                    StartAsync(simData);
                }
                else
                {
                    Network.SetSimulationData(simData);
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"Failed to start '{simData.Name}' simulation");
                Debug.LogException(ex);
                reportStatus(SimulatorStatus.Error, ex.Message);

                if (SceneManager.GetActiveScene().name != Instance.LoaderScene)
                {
                    reportStatus(SimulatorStatus.Stopping);
                    SceneManager.LoadScene(Instance.LoaderScene);
                    reportStatus(SimulatorStatus.Idle);
                }
            }
#if UNITY_EDITOR
            finally
            {
                if (API != null)
                {
                    API.Disconnect();
                }
            }
#endif
        }

        public void StartAsync(SimulationData simulation)
        {
            if (Status != SimulatorStatus.Loading)
            {
                throw new Exception("aborting start, expected Status==Loading, found " + Status);
            }

            currentSimulation = simulation;
            reportStatus(SimulatorStatus.Starting);

            Actions.Enqueue(async () =>
            {
                try
                {
                    if (Config.Headless && (simulation.Headless))
                    {
                        throw new Exception("Simulator is configured to run in headless mode, only headless simulations are allowed");
                    }

                    if (ConnectionUI != null)
                    {
                        ConnectionUI.SetLoaderUIState(ConnectionUI.LoaderUIStateType.PROGRESS);
                    }

                    SimConfig = new SimulationConfig(simulation);

                    // load environment
                    if (SimConfig.ApiOnly)
                    {
                        var API = Instantiate(ApiManagerPrefab);
                        API.name = "ApiManager";

                        ConnectionUI.SetLoaderUIState(ConnectionUI.LoaderUIStateType.READY);

                        // Spawn external test case process
                        RunTestCase(simulation.Template);
                    }
                    else if (simulation.Map != null)
                    {
                        var callback = new Action<bool, string, string>((isDone, sceneName, mapBundlePath) =>
                        {
                            SimConfig.MapName = sceneName;
                            SimConfig.MapAssetGuid = simulation.Map.AssetGuid;
                            if (!isDone) return;
                            var loaders = FindObjectsOfType<NodeTreeLoader>();
                            foreach (var l in loaders)
                                l.UpdateData(mapBundlePath, Utility.StringToGUID(l.GetDataPath()).ToString());

                            SetupScene(simulation);
                        });
                        LoadMap(simulation.Map.AssetGuid, simulation.Map.Name, LoadSceneMode.Single, callback);
                    }
                    else
                    {
                        SetupScene(simulation);
                    }
                }
                catch (ZipException ex)
                {
                    Debug.Log($"Failed to start '{simulation.Name}' simulation");
                    Debug.LogException(ex);

                    reportStatus(SimulatorStatus.Error, ex.Message);

                    if (SceneManager.GetActiveScene().name != LoaderScene)
                    {
                        reportStatus(SimulatorStatus.Stopping);
                        SceneManager.LoadScene(LoaderScene);
                        reportStatus(SimulatorStatus.Idle);
                    }

                    AssetBundle.UnloadAllAssetBundles(true);
                    await Network.Deinitialize();
                }
                catch (Exception ex)
                {
                    Debug.Log($"Failed to start '{simulation.Name}' simulation");
                    Debug.LogException(ex);

                    reportStatus(SimulatorStatus.Error, ex.Message);

                    if (SceneManager.GetActiveScene().name != LoaderScene)
                    {
                        reportStatus(SimulatorStatus.Stopping);
                        SceneManager.LoadScene(LoaderScene);
                    }

                    reportStatus(SimulatorStatus.Idle);
                    await Network.Deinitialize();
                }
            });
        }

        public void StopAsync()
        {
            if (Status == SimulatorStatus.Stopping ||
                Status == SimulatorStatus.Idle)
            {
                return;
            }
            // Check if simulation scene was initialized
            bool wasLoading = Status == SimulatorStatus.Loading;

            reportStatus(SimulatorStatus.Stopping);

            if (Sentry != null)
            {
                Sentry.Reset();
            }

            Actions.Enqueue(async () =>
            {
                if (wasLoading)
                {
                    foreach (var download in assetDownloads)
                    {
                        if (!download.Key.IsCompleted && !download.Key.IsCanceled)
                        {
                            DownloadManager.StopAssetDownload(download.Value);
                        }
                    }

                    assetDownloads.Clear();
                    await Network.Deinitialize();
                    ConnectionUI.SetLoaderUIState(ConnectionUI.LoaderUIStateType.START);
                    reportStatus(SimulatorStatus.Idle);
                    return;
                }

                if (ConnectionManager.Status != ConnectionManager.ConnectionStatus.Offline)
                {
                    Network?.BroadcastStopCommand();
                }

                if (SimulatorManager.InstanceAvailable)
                {
                    try
                    {
                        await SimulatorManager.Instance.AnalysisManager.AnalysisSave();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }

                await Network.Deinitialize();

                if (TCManager)
                {
                    Debug.Log("[LOADER] StopAsync: Terminating process");
                    await TCManager.Terminate();
                }

                using (var db = DatabaseManager.Open())
                {
                    try
                    {
                        if (ApiManager.Instance != null)
                        {
                            SceneManager.MoveGameObjectToScene(ApiManager.Instance.gameObject, SceneManager.GetActiveScene());
                        }

                        var loader = SceneManager.LoadSceneAsync(LoaderScene);
                        loader.completed += op =>
                        {
                            if (op.isDone)
                            {
                                AssetBundle.UnloadAllAssetBundles(false);
                                ConnectionUI.SetLoaderUIState(ConnectionUI.LoaderUIStateType.START);

                                if (Status == SimulatorStatus.Stopping)
                                {
                                    reportStatus(SimulatorStatus.Idle);
                                }
                            }
                        };
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"Failed to stop '{CurrentSimulation.Name}' simulation");
                        Debug.LogException(ex);

                        if (Status == SimulatorStatus.Stopping)
                        {
                            reportStatus(SimulatorStatus.Idle);
                        }
                    }
                }
            });
        }

        public static void LoadMap(string assetGuid, string mapName, LoadSceneMode loadMode, Action<bool, string, string> callback)
        {
            var mapBundlePath = WebUtilities.GenerateLocalPath(assetGuid, BundleConfig.BundleTypes.Environment);
            AssetBundle textureBundle = null;
            AssetBundle mapBundle = null;
            ZipFile zip = new ZipFile(mapBundlePath);
            try
            {
                string manfile;
                ZipEntry entry = zip.GetEntry("manifest.json");
                using (var ms = zip.GetInputStream(entry))
                {
                    int streamSize = (int)entry.Size;
                    byte[] buffer = new byte[streamSize];
                    streamSize = ms.Read(buffer, 0, streamSize);
                    manfile = Encoding.UTF8.GetString(buffer);
                }

                Manifest manifest;

                try
                {
                    manifest = Newtonsoft.Json.JsonConvert.DeserializeObject<Manifest>(manfile);
                }
                catch
                {
                    throw new Exception("Out of date AssetBundle, rebuild or download latest AssetBundle.");
                }

                if (manifest.assetFormat != BundleConfig.Versions[BundleConfig.BundleTypes.Environment])
                {
                    zip.Close();

                    throw new ZipException($"BundleFormat: {manifest.assetName} ({manifest.assetGuid}) is for bundle" +
                                           $" version {manifest.assetFormat}, but currently running simulator supports" +
                                           $" only {BundleConfig.Versions[BundleConfig.BundleTypes.Environment]} bundle" +
                                           $" version");
                }

                if (zip.FindEntry($"{manifest.assetGuid}_environment_textures", false) != -1)
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
                entry = zip.GetEntry($"{manifest.assetGuid}_environment_main_{platform}");
                var mapStream = VirtualFileSystem.VirtualFileSystem.EnsureSeekable(zip.GetInputStream(entry), entry.Size);
                mapBundle = AssetBundle.LoadFromStream(mapStream, 0, 1 << 20);
                mapStream.Close();
                mapStream.Dispose();

                if (mapBundle == null)
                {
                    throw new Exception($"Failed to load environment from '{mapName}' asset bundle");
                }

                if (textureBundle != null)
                    textureBundle.LoadAllAssets();

                var scenes = mapBundle.GetAllScenePaths();
                if (scenes.Length != 1)
                {
                    throw new Exception(
                        $"Unsupported environment in '{mapName}' asset bundle, only 1 scene expected");
                }

                var sceneName = Path.GetFileNameWithoutExtension(scenes[0]);
                var loader = SceneManager.LoadSceneAsync(sceneName, loadMode);
                loader.completed += op =>
                {
                    callback?.Invoke(op.isDone, sceneName, mapBundlePath);
                    zip.Close();
                    if (textureBundle != null)
                        textureBundle.Unload(false);
                    if (mapBundle != null)
                        mapBundle.Unload(false);
                };
            }
            catch (Exception)
            {
                zip.Close();
                if (textureBundle != null)
                    textureBundle.Unload(true);
                if (mapBundle != null)
                    mapBundle.Unload(true);
                throw;
            }
        }

        void SetupScene(SimulationData simulation)
        {
            Dictionary<string, GameObject> cachedVehicles = new Dictionary<string, GameObject>();
            try
            {
                foreach (var agentConfig in SimConfig.Agents)
                {
                    var bundlePath = agentConfig.AssetBundle;

                    if (cachedVehicles.ContainsKey(agentConfig.AssetGuid))
                    {
                        agentConfig.Prefab = cachedVehicles[agentConfig.AssetGuid];
                        continue;
                    }
#if UNITY_EDITOR
                    if (EditorPrefs.GetBool("Simulator/Developer Debug Mode", false) == true && !bundlePath.EndsWith(".prefab"))
                    {
                        string assetName = "";
                        using (ZipFile zip = new ZipFile(bundlePath))
                        {
                            Manifest manifest;
                            ZipEntry entry = zip.GetEntry("manifest.json");
                            using (var ms = zip.GetInputStream(entry))
                            {
                                int streamSize = (int)entry.Size;
                                byte[] buffer = new byte[streamSize];
                                streamSize = ms.Read(buffer, 0, streamSize);

                                try
                                {
                                    manifest = Newtonsoft.Json.JsonConvert.DeserializeObject<Manifest>(Encoding.UTF8.GetString(buffer, 0, streamSize));
                                    assetName = manifest.assetName;
                                }
                                catch
                                {
                                    throw new Exception("Out of date AssetBundle, rebuild or download latest AssetBundle.");
                                }
                            }
                        }

                        string filePath = Path.Combine(BundleConfig.ExternalBase, "Vehicles", assetName, $"{assetName}.prefab");
                        if (File.Exists(filePath))
                        {
                            bundlePath = filePath;
                        }
                    }

                    if (bundlePath.EndsWith(".prefab"))
                    {
                        agentConfig.Prefab = (GameObject)UnityEditor.AssetDatabase.LoadAssetAtPath(bundlePath, typeof(GameObject));
                    }
                    else
#endif
                    {
                        agentConfig.Prefab = LoadVehicleBundle(bundlePath);
                    }
                    cachedVehicles.Add(agentConfig.AssetGuid, agentConfig.Prefab);
                }

                var sim = CreateSimulatorManager();
                sim.Init(simulation.Seed);

                if (Instance.CurrentSimulation != null)
                {
                    reportStatus(SimulatorStatus.Running);
                }

                // Flash main window to let user know simulation is ready
                WindowFlasher.Flash();
            }
            catch (ZipException ex)
            {
                Debug.Log($"Failed to start '{simulation.Name}' simulation - out of date asset bundles");
                Debug.LogException(ex);
                reportStatus(SimulatorStatus.Error, ex.Message);
                ResetLoaderScene(simulation);
            }
            catch (Exception ex)
            {
                Debug.Log($"Failed to start '{simulation.Name}' simulation");
                Debug.LogException(ex);
                reportStatus(SimulatorStatus.Error, ex.Message);
                ResetLoaderScene(simulation);
            }
        }

        public static GameObject LoadVehicleBundle(string bundlePath)
        {
            AssetBundle textureBundle = null;
            AssetBundle vehicleBundle = null;
            using (ZipFile zip = new ZipFile(bundlePath))
            {
                Manifest manifest;
                ZipEntry entry = zip.GetEntry("manifest.json");
                using (var ms = zip.GetInputStream(entry))
                {
                    int streamSize = (int)entry.Size;
                    byte[] buffer = new byte[streamSize];
                    streamSize = ms.Read(buffer, 0, streamSize);

                    try
                    {
                        manifest = Newtonsoft.Json.JsonConvert.DeserializeObject<Manifest>(Encoding.UTF8.GetString(buffer, 0, streamSize));
                    }
                    catch
                    {
                        throw new Exception("Out of date AssetBundle, rebuild or download latest AssetBundle.");
                    }
                }

                if (manifest.assetFormat != BundleConfig.Versions[BundleConfig.BundleTypes.Vehicle])
                {
                    zip.Close();

                    throw new ZipException($"BundleFormat: {manifest.assetName} ({manifest.assetGuid}) is for bundle" +
                                           $" version {manifest.assetFormat}, but currently running simulator supports" +
                                           $" only {BundleConfig.Versions[BundleConfig.BundleTypes.Environment]} bundle" +
                                           $" version");
                }

                if (zip.FindEntry($"{manifest.assetGuid}_vehicle_textures", true) != -1)
                {
                    var texStream = zip.GetInputStream(zip.GetEntry($"{manifest.assetGuid}_vehicle_textures"));
                    textureBundle = AssetBundle.LoadFromStream(texStream, 0, 1 << 20);
                }

                string platform = SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows ? "windows" : "linux";
                var mapStream = zip.GetInputStream(zip.GetEntry($"{manifest.assetGuid}_vehicle_main_{platform}"));

                vehicleBundle = AssetBundle.LoadFromStream(mapStream, 0, 1 << 20);

                if (vehicleBundle == null)
                {
                    throw new Exception($"Failed to load '{manifest.assetName}' vehicle asset bundle");
                }

                try
                {
                    var vehicleAssets = vehicleBundle.GetAllAssetNames();
                    if (vehicleAssets.Length != 1)
                    {
                        throw new Exception($"Unsupported '{manifest.assetName}' vehicle asset bundle, only 1 asset expected");
                    }

                    //Import main assembly
                    var assembly = zip.GetEntry($"{manifest.assetName}.dll");
                    if (assembly != null)
                    {
                        using (var s = zip.GetInputStream(assembly))
                        {
                            byte[] buffer = new byte[s.Length];
                            s.Read(buffer, 0, (int)s.Length);
                            Assembly.Load(buffer);
                        }
                    }

                    if (manifest.fmuName != "")
                    {
                        var fmuDirectory = Path.Combine(Config.PersistentDataPath, manifest.assetName);
                        if (platform == "windows")
                        {
                            var dll = zip.GetEntry($"{manifest.fmuName}_windows.dll");
                            if (dll == null)
                            {
                                throw new ArgumentException($"{manifest.fmuName}.dll not found in Zip {bundlePath}");
                            }

                            using (Stream s = zip.GetInputStream(dll))
                            {
                                byte[] buffer = new byte[4096];
                                Directory.CreateDirectory(fmuDirectory);
                                var path = Path.Combine(Config.PersistentDataPath, manifest.assetName, $"{manifest.fmuName}.dll");
                                using (FileStream streamWriter = File.Create(path))
                                {
                                    StreamUtils.Copy(s, streamWriter, buffer);
                                }
                                vehicleBundle.LoadAsset<GameObject>(vehicleAssets[0]).GetComponent<VehicleFMU>().FMUData.Path = path;
                            }
                        }
                        else
                        {
                            var dll = zip.GetEntry($"{manifest.fmuName}_linux.so");
                            if (dll == null)
                            {
                                throw new ArgumentException($"{manifest.fmuName}.so not found in Zip {bundlePath}");
                            }

                            using (Stream s = zip.GetInputStream(dll))
                            {
                                byte[] buffer = new byte[4096];
                                Directory.CreateDirectory(fmuDirectory);
                                var path = Path.Combine(Config.PersistentDataPath, manifest.assetName, $"{manifest.fmuName}.so");
                                using (FileStream streamWriter = File.Create(path))
                                {
                                    StreamUtils.Copy(s, streamWriter, buffer);
                                }
                                vehicleBundle.LoadAsset<GameObject>(vehicleAssets[0]).GetComponent<VehicleFMU>().FMUData.Path = path;
                            }
                        }

                    }

                    // TODO: make this async
                    if (!AssetBundle.GetAllLoadedAssetBundles().Contains(textureBundle))
                    {
                        textureBundle?.LoadAllAssets();
                    }

                    return vehicleBundle.LoadAsset<GameObject>(vehicleAssets[0]);

                }
                finally
                {
                    textureBundle?.Unload(false);
                    vehicleBundle.Unload(false);
                }
            }

        }

        public void ResetLoaderScene(SimulationData simulation)
        {
            if (SceneManager.GetActiveScene().name != LoaderScene)
            {
                reportStatus(SimulatorStatus.Stopping);

                SceneManager.LoadScene(LoaderScene);
                AssetBundle.UnloadAllAssetBundles(true);
                // changing Status requires CurrentSimulation to be valid
                reportStatus(SimulatorStatus.Idle);
            }
        }

        public async Task EnterScenarioEditor()
        {
            ConnectionUI.SetLoaderUIState(ConnectionUI.LoaderUIStateType.PROGRESS);
            ConnectionUI.SetLinkingButtonActive(false);
            ConnectionUI.SetVSEButtonActive(false);
                
            if (SimulatorManager.InstanceAvailable || ApiManager.Instance)
            {
                ConnectionUI.UpdateStatusText("Cannot enter Scenario Editor during a simulation.");
                Debug.LogWarning("Cannot enter Scenario Editor during a simulation.");
                ExitScenarioEditor();
                return;
            }

            if (ConnectionManager.Status != ConnectionManager.ConnectionStatus.Online)
            {
                ConnectionUI.UpdateStatusText("Cannot enter Scenario Editor when connection is not established.");
                Debug.LogWarning("Cannot enter Scenario Editor when connection is not established.");
                ExitScenarioEditor();
                return;
            }

            var maps = await ConnectionManager.API.GetLibrary<MapDetailData>();
            if (maps.Length == 0)
            {
                ConnectionUI.UpdateStatusText("Scenario Editor requires at least one map added to the library.");
                Debug.LogWarning("Scenario Editor requires at least one map added to the library.");
                ExitScenarioEditor();
                return;
            }

            var egos = await ConnectionManager.API.GetLibrary<VehicleDetailData>();
            if (egos.Length == 0)
            {
                ConnectionUI.UpdateStatusText("Scenario Editor requires at least one ego vehicle added to the library.");
                Debug.LogWarning("Scenario Editor requires at least one ego vehicle added to the library.");
                ExitScenarioEditor();
                return;
            }

            if (!IsInScenarioEditor && !SceneManager.GetSceneByName(ScenarioEditorSceneName).isLoaded)
            {
                IsInScenarioEditor = true;
                SceneManager.LoadScene(ScenarioEditorSceneName);
            }
        }

        public void ExitScenarioEditor()
        {
            IsInScenarioEditor = false;
            if (SceneManager.GetSceneByName(ScenarioEditorSceneName).isLoaded)
            {
                SceneManager.LoadScene(LoaderScene);
            }

            if (ConnectionUI != null)
            {
                ConnectionUI.SetLinkingButtonActive(true);
                ConnectionUI.SetVSEButtonActive(true);
                ConnectionUI.SetLoaderUIState(ConnectionUI.LoaderUIStateType.READY);
            }
        }

        public SimulatorManager CreateSimulatorManager()
        {
            var sim = Instantiate(SimulatorManagerPrefab);
            sim.name = "SimulatorManager";
            Network.InitializeSimulationScene(sim.gameObject);

            return sim;
        }

        public TestCaseProcessManager CreateTestCaseProcessManager()
        {
            var manager = Instantiate(TestCaseProcessManagerPrefab);

            if (manager == null)
            {
                Debug.LogWarning($"[LOADER] Can't Instantiate TestCaseProcessManager");
            }
            else
            {
                manager.name = "TestCaseProcessManager";
            }

            return manager;
        }

        async Task RunTestCase(TemplateData template)
        {
            if (template == null)
            {
                // legacy simulation request -> nothing to do
                Debug.LogWarning("[LOADER] Got legacy Simulation Config request");
                return;
            }

            if (SimulationConfigUtils.IsInternalTemplate(template))
            {
                // No external process is needed
                return;
            }

            if (TCManager == null)
            {
                TCManager = CreateTestCaseProcessManager();
                DontDestroyOnLoad(TCManager);
            }


            try
            {
                var simulationId = CurrentSimulation.Id;
                var volumesPath = SimulationConfigUtils.SaveVolumes(simulationId, template);
                var args = await TCManager.StartProcess(template, volumesPath);
                SimulationConfigUtils.CleanupVolumes(simulationId);
                if (args.Failed)
                {
                    reportStatus(SimulatorStatus.Error, $"Test case exit code: {args.ExitCode}\nerror data: {args.ErrorData}");
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                reportStatus(SimulatorStatus.Error, $"Test case exception: " + e);
            }
            Console.WriteLine($"[LOADER] Stopping simulation on TestCase process exit");
            StopAsync();
        }

        bool CleanupOnExit()
        {
            StopAsync();
            WaitOnStop();
            return status == SimulatorStatus.Idle;
        }

        async void WaitOnStop()
        {
            while (status != SimulatorStatus.Idle)
            {
                await Task.Delay(1000);
            }

            Application.Quit();
        }
    }
}
