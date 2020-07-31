/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.SceneManagement;
using Simulator.Database;
using Simulator.Api;
using Simulator.Web;
using Simulator.Utilities;
using Simulator.Bridge;
using System.Text;
using System.Linq;
using ICSharpCode.SharpZipLib.Zip;
using Simulator.Network.Shared;
using Simulator.Network.Core.Configs;
using ICSharpCode.SharpZipLib.Core;
using Simulator.FMU;
using Simulator.PointCloud.Trees;
using System.Threading.Tasks;
using Simulator.Database.Services;

namespace Simulator
{
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
        public BridgePlugin Bridge;
        public string Connection;
        public string Sensors;
        public Vector3 Position;
        public Quaternion Rotation;
        public AgentConfig(){}
        public AgentConfig(VehicleData vehicleData)
        {
            Name = vehicleData.Name;
            Connection = vehicleData.bridge != null ? vehicleData.bridge.connectionString : "";
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
            Sensors = Newtonsoft.Json.JsonConvert.SerializeObject(vehicleData.Sensors);

            if (vehicleData.bridge != null && !string.IsNullOrEmpty(vehicleData.bridge.type))
            {
                Bridge = BridgePlugins.Get(vehicleData.bridge.type);
                if (Bridge == null)
                {
                    throw new Exception($"Bridge {vehicleData.bridge.type} not found");
                }
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
        Idle = 0,
        Loading = 1,
        Starting = 2,
        Running = 3,
        Stopping = 4
    }

    public class Loader : MonoBehaviour
    {
        private static string ScenarioEditorSceneName = "ScenarioEditor";
        private static bool IsInScenarioEditor;

        public SimulationNetwork Network { get; } = new SimulationNetwork();
        public SimulatorManager SimulatorManagerPrefab;
        public ApiManager ApiManagerPrefab;
        public TestCaseProcessManager TestCaseProcessManagerPrefab;

        public NetworkSettings NetworkSettings;

        public LoaderUI LoaderUI => FindObjectOfType<LoaderUI>();

        // NOTE: When simulation is not running this reference will be null.
        public SimulationData CurrentSimulation;

        ConcurrentQueue<Action> Actions = new ConcurrentQueue<Action>();
        public string LoaderScene { get; private set; }

        public SimulationConfig SimConfig;

        private TestCaseProcessManager TCManager;

        // Loader object is never destroyed, even between scene reloads
        public static Loader Instance { get; private set; }
        private System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
        private SimulatorStatus status = SimulatorStatus.Idle;

        public bool EditorLoader { get; set; } = false;

        string reportedStatus(SimulatorStatus status)
        {
            switch(status)
            {
                case SimulatorStatus.Idle: return "Idle";
                // WISE does not care about Loading, just Starting
                case SimulatorStatus.Loading:
                case SimulatorStatus.Starting: return "Starting";
                case SimulatorStatus.Running: return "Running";
                case SimulatorStatus.Stopping: return "Stopping";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public SimulatorStatus Status
        {
            get => status;
            set
            {
                Console.WriteLine($"[LOADER] Update simulation status {status} -> {value}");

                var previous = reportedStatus(status);
                var newStatus = reportedStatus(value);

                status = value;

                if (previous == newStatus)
                    return;

                if(ConnectionManager.instance != null)
                    ConnectionManager.instance.UpdateStatus(newStatus, CurrentSimulation.Id);

                if(status == SimulatorStatus.Running)
                    WindowFlasher.Flash();
            }
        }

        private void Start()
        {
            stopWatch.Start();
            var info = Resources.Load<BuildInfo>("BuildInfo");
            SIM.Init(info == null ? "Development" : info.Version);
            SIM.LogSimulation(SIM.Simulation.ApplicationStart);

            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (!EditorLoader)
            {
                RenderLimiter.RenderLimitEnabled();
                LoaderScene = SceneManager.GetActiveScene().name;
                DontDestroyOnLoad(this);
            }
            else
            {
#if UNITY_EDITOR
                SimulationData devSim;
                var devSettings = (Simulator.Editor.DevelopmentSettingsAsset)UnityEditor.AssetDatabase.LoadAssetAtPath("Assets/Resources/Editor/DeveloperSettings.asset", typeof(Simulator.Editor.DevelopmentSettingsAsset));
                if (devSettings != null && devSettings.developerSimulationJson != null)
                    devSim = Newtonsoft.Json.JsonConvert.DeserializeObject<SimulationData>(devSettings.developerSimulationJson);
                else
                    devSim = new SimulationData();

                StartSimulation(devSim);
#endif
            }
        }

        void OnApplicationQuit()
        {
            stopWatch.Stop();
            SIM.LogSimulation(SIM.Simulation.ApplicationExit, value: (long)stopWatch.Elapsed.TotalSeconds);
        }

        private void Update()
        {
            while (Actions.TryDequeue(out var action))
            {
                action();
            }
        }

        public static async void StartSimulation(SimulationData simData)
        {
            Instance.CurrentSimulation = simData;
            CloudAPI api = null;
            if (Instance.Status != SimulatorStatus.Idle)
            {
                Debug.LogWarning("Received start simulation command while Simulator is not idle.");
                return;
            }
            try
            {
#if UNITY_EDITOR
                // downloads still need simulator to be online, but in developer mode we don't have ConnectionManager
                if (ConnectionManager.instance == null)
                {
                    api = new CloudAPI(new Uri(Config.CloudUrl), Config.SimID);
                    var simInfo = CloudAPI.GetInfo();
                    var reader = await api.Connect(simInfo);
                    await api.EnsureConnectSuccess();
                }
#endif
                Instance.Status = SimulatorStatus.Loading;
                Instance.Network.Initialize(Config.SimID, simData.Cluster, Instance.NetworkSettings);
                var downloads = new List<Task>();
                if (simData.ApiOnly == false)
                {
                    if (simData.Map != null)
                    {
                        downloads.Add(DownloadManager.GetAsset(BundleConfig.BundleTypes.Environment, simData.Map.AssetGuid, simData.Map.Name));
                    }

                    foreach (var vehicle in simData.Vehicles.Where(v => !v.Id.EndsWith(".prefab")))
                    {
                        downloads.Add(DownloadManager.GetAsset(BundleConfig.BundleTypes.Vehicle, vehicle.AssetGuid, vehicle.Name));
                    }
                }

                if (ConnectionUI.instance != null)
                {
                    ConnectionUI.instance.SetLinkingButtonActive(false);
                }

                await Task.WhenAll(downloads);

                if (!string.IsNullOrEmpty(simData.Id))
                {
                    SimulationService simService = new SimulationService();
                    simService.AddOrUpdate(simData);
                }

                Debug.Log("All Downloads Complete");

                if (!Instance.Network.IsClusterSimulation)
                    StartAsync(simData);
                else
                    Instance.Network.SetSimulationData(simData);
            }
            catch (Exception ex)
            {
                Debug.Log($"Failed to start '{simData.Name}' simulation");
                Debug.LogException(ex);
                if (ConnectionManager.instance != null)
                {
                    ConnectionManager.instance.UpdateStatus("Error", simData.Id, ex.Message);
                }

                if (SceneManager.GetActiveScene().name != Instance.LoaderScene)
                {
                    Instance.Status = SimulatorStatus.Stopping;
                    SceneManager.LoadScene(Instance.LoaderScene);
                    Instance.Status = SimulatorStatus.Idle;
                }
            }
#if UNITY_EDITOR
            finally
            {
                if (api != null)
                {
                    api.Disconnect();
                }
            }
#endif
        }

        public static void StartAsync(SimulationData simulation)
        {
            Debug.Assert(Instance.Status == SimulatorStatus.Loading);
            Instance.Status = SimulatorStatus.Starting;
            Instance.CurrentSimulation = simulation;

            Instance.Actions.Enqueue(() =>
            {
                    AssetBundle textureBundle = null;
                    AssetBundle mapBundle = null;
                    try
                    {
                        if (Config.Headless && (simulation.Headless))
                        {
                            throw new Exception("Simulator is configured to run in headless mode, only headless simulations are allowed");
                        }

                        if (Instance.LoaderUI != null)
                        {
                            Instance.LoaderUI.SetLoaderUIState(LoaderUI.LoaderUIStateType.PROGRESS);
                        }

                        Instance.SimConfig = new SimulationConfig(simulation);

                        // load environment
                        if (Instance.SimConfig.ApiOnly)
                        {
                            var api = Instantiate(Instance.ApiManagerPrefab);
                            api.name = "ApiManager";

                            Instance.LoaderUI.SetLoaderUIState(LoaderUI.LoaderUIStateType.READY);

                            // Spawn external test case process
                            RunTestCase(simulation.Template);
                        }
                        else if (simulation.Map != null)
                        {
                            var mapData = simulation.Map;
                            var mapBundlePath = WebUtilities.GenerateLocalPath(mapData.AssetGuid, BundleConfig.BundleTypes.Environment);
                            mapBundle = null;
                            textureBundle = null;

                            ZipFile zip = new ZipFile(mapBundlePath);
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

                                if (manifest.attachments != null)
                                {
                                    foreach (string key in manifest.attachments.Keys)
                                    {
                                        if (key.Contains("pointcloud"))
                                        {
                                            if (!Directory.Exists(Path.Combine(Application.persistentDataPath, manifest.assetGuid)))
                                            {
                                                Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, manifest.assetGuid));
                                                FastZip fastZip = new FastZip();
                                                fastZip.ExtractZip(mapBundlePath, Path.Combine(Application.persistentDataPath, manifest.assetGuid), ".*\\.(pcnode|pcindex|pcmesh)$");
                                            }
                                        }
                                    }
                                }

                                if (manifest.assetFormat != BundleConfig.Versions[BundleConfig.BundleTypes.Environment])
                                {
                                    zip.Close();

                                    // TODO: proper exception
                                    throw new ZipException("BundleFormat version mismatch");
                                }

                                if (zip.FindEntry($"{manifest.assetGuid}_environment_textures", false) != -1)
                                {
                                    var texStream = zip.GetInputStream(zip.GetEntry($"{manifest.assetGuid}_environment_textures"));
                                    textureBundle = AssetBundle.LoadFromStream(texStream, 0, 1 << 20);
                                }

                                string platform = SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows ? "windows" : "linux";
                                var mapStream = zip.GetInputStream(zip.GetEntry($"{manifest.assetGuid}_environment_main_{platform}"));
                                mapBundle = AssetBundle.LoadFromStream(mapStream, 0, 1 << 20);

                                if (mapBundle == null)
                                {
                                    throw new Exception($"Failed to load environment from '{mapData.Name}' asset bundle");
                                }

                                textureBundle?.LoadAllAssets();

                                var scenes = mapBundle.GetAllScenePaths();
                                if (scenes.Length != 1)
                                {
                                    throw new Exception($"Unsupported environment in '{mapData.Name}' asset bundle, only 1 scene expected");
                                }

                                var sceneName = Path.GetFileNameWithoutExtension(scenes[0]);
                                Instance.SimConfig.MapName = sceneName;
                                Instance.SimConfig.MapAssetGuid = simulation.Map.AssetGuid;

                                var loader = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
                                loader.completed += op =>
                                {
                                    if (op.isDone)
                                    {
                                        textureBundle?.Unload(false);
                                        mapBundle.Unload(false);
                                        zip.Close();
                                        NodeTreeLoader[] loaders = FindObjectsOfType<NodeTreeLoader>();
                                        foreach (NodeTreeLoader l in loaders)
                                        {
                                            l.UpdateData(Path.Combine(Application.persistentDataPath, manifest.assetGuid, $"pointcloud_{Utilities.Utility.StringToGUID(l.GetDataPath())}".ToString()));
                                        }

                                        SetupScene(simulation);
                                    }
                                };
                            }
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

                        if (ConnectionManager.instance != null)
                        {
                            ConnectionManager.instance.UpdateStatus("Error", simulation.Id, ex.Message);
                        }

                        if (SceneManager.GetActiveScene().name != Instance.LoaderScene)
                        {
                            Instance.Status = SimulatorStatus.Stopping;
                            SceneManager.LoadScene(Instance.LoaderScene);
                            Instance.Status = SimulatorStatus.Idle;
                        }

                        textureBundle?.Unload(false);
                        mapBundle?.Unload(false);
                        AssetBundle.UnloadAllAssetBundles(true);
                        Instance.CurrentSimulation = null;
                        Instance.Network.Deinitialize();
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"Failed to start '{simulation.Name}' simulation");
                        Debug.LogException(ex);

                        if (ConnectionManager.instance != null)
                        {
                            ConnectionManager.instance.UpdateStatus("Error", simulation.Id, ex.Message);
                        }

                        if (SceneManager.GetActiveScene().name != Instance.LoaderScene && ConnectionManager.Status != ConnectionManager.ConnectionStatus.Offline)
                        {
                            Instance.Status = SimulatorStatus.Stopping;
                            SceneManager.LoadScene(Instance.LoaderScene);
                            Instance.Status = SimulatorStatus.Idle;
                        }

                        textureBundle?.Unload(false);
                        mapBundle?.Unload(false);
                        AssetBundle.UnloadAllAssetBundles(true);
                        Instance.CurrentSimulation = null;
                        Instance.Network.Deinitialize();
                    }
            });
        }

        public static void StopAsync()
        {
            //Check if simulation scene was initialized
            if (Instance.Status == SimulatorStatus.Loading)
            {
                Instance.Status = SimulatorStatus.Stopping;
                Instance.Network.Deinitialize();
                Instance.Status = SimulatorStatus.Idle;
                Instance.CurrentSimulation = null;
                return;
            }

            Instance.Actions.Enqueue(() =>
            {
                if (SimulatorManager.InstanceAvailable)
                {
                    SimulatorManager.Instance.AnalysisManager.AnalysisSave();
                }

                Instance.Network.Deinitialize();

                if (Instance.TCManager)
                {
                    Debug.Log("[LOADER] StopAsync: Terminating process");
                    // Don't bother to stop simulation on process exit
                    Instance.TCManager.OnFinished -= Instance.StopSimulationOnTestCaseExit;
                    Instance.TCManager.Terminate();
                }

                using (var db = DatabaseManager.Open())
                {
                    try
                    {
                        if (ConnectionManager.Status != ConnectionManager.ConnectionStatus.Offline)
                        {
                            Instance.Status = SimulatorStatus.Stopping;
                        }

                        if (ApiManager.Instance != null)
                        {
                            SceneManager.MoveGameObjectToScene(ApiManager.Instance.gameObject, SceneManager.GetActiveScene());
                        }
                        SIM.LogSimulation(SIM.Simulation.ApplicationClick, "Exit");
                        var loader = SceneManager.LoadSceneAsync(Instance.LoaderScene);
                        loader.completed += op =>
                        {
                            if (op.isDone)
                            {
                                AssetBundle.UnloadAllAssetBundles(false);
                                Instance.LoaderUI.SetLoaderUIState(LoaderUI.LoaderUIStateType.START);
                                Instance.Status = SimulatorStatus.Idle;
                                Instance.CurrentSimulation = null;
                            }
                        };
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"Failed to stop '{Instance.CurrentSimulation.Name}' simulation");
                        Debug.LogException(ex);
                        Instance.Status = SimulatorStatus.Idle;
                        Instance.CurrentSimulation = null;
                    }
                }
            });
        }

        static void SetupScene(SimulationData simulation)
        {
            Dictionary<string, GameObject> cachedVehicles = new Dictionary<string, GameObject>();
            try
            {
                foreach (var agentConfig in Instance.SimConfig.Agents)
                {
                    var bundlePath = agentConfig.AssetBundle;
                    if (cachedVehicles.ContainsKey(agentConfig.AssetGuid))
                    {
                        agentConfig.Prefab = cachedVehicles[agentConfig.AssetGuid];
                        continue;
                    }
#if UNITY_EDITOR
                    if(bundlePath.EndsWith(".prefab"))
                    {
                        agentConfig.Prefab = (GameObject) UnityEditor.AssetDatabase.LoadAssetAtPath(bundlePath, typeof(GameObject));
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

                if (Instance.CurrentSimulation != null && ConnectionManager.Status != ConnectionManager.ConnectionStatus.Offline)
                {
                    Instance.Status = SimulatorStatus.Running;
                }

                // Flash main window to let user know simulation is ready
                WindowFlasher.Flash();
            }
            catch (ZipException ex)
            {
                Debug.Log($"Failed to start '{simulation.Name}' simulation - out of date asset bundles");
                Debug.LogException(ex);
                if (ConnectionManager.instance != null)
                {
                    ConnectionManager.instance.UpdateStatus("Error", simulation.Id, ex.Message);
                }

                ResetLoaderScene(simulation);
            }
            catch (Exception ex)
            {
                Debug.Log($"Failed to start '{simulation.Name}' simulation");
                Debug.LogException(ex);
                if (ConnectionManager.instance != null)
                {
                    ConnectionManager.instance.UpdateStatus("Error", simulation.Id, ex.Message);
                }

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

                    // TODO: proper exception
                    throw new ZipException("BundleFormat version mismatch");
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

                    if (manifest.fmuName != "")
                    {
                        var fmuDirectory = Path.Combine(Application.persistentDataPath, manifest.assetName);
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
                                var path = Path.Combine(Application.persistentDataPath, manifest.assetName, $"{manifest.fmuName}.dll");
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
                                var path = Path.Combine(Application.persistentDataPath, manifest.assetName, $"{manifest.fmuName}.so");
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

        public static void ResetLoaderScene(SimulationData simulation)
        {
            if (SceneManager.GetActiveScene().name != Instance.LoaderScene && ConnectionManager.Status != ConnectionManager.ConnectionStatus.Offline)
            {
                Instance.Status = SimulatorStatus.Stopping;
                SceneManager.LoadScene(Instance.LoaderScene);
                AssetBundle.UnloadAllAssetBundles(true);
                // changing Status requires CurrentSimulation to be valid
                Instance.Status = SimulatorStatus.Idle;
                Instance.CurrentSimulation = null;
            }
        }
	    
        public static async Task EnterScenarioEditor()
        {
            if (SimulatorManager.InstanceAvailable || ApiManager.Instance)
            {
                Debug.LogError("Cannot enter Scenario Editor during a simulation.");
                return;
            }

            if (ConnectionManager.Status != ConnectionManager.ConnectionStatus.Online)
            {
                Debug.LogError("Cannot enter Scenario Editor when connection is not established.");
                return;
            }
            
            var maps = await ConnectionManager.API.GetLibrary<MapDetailData>();
            if (maps.Length == 0)
            {
                Debug.LogError("Scenario Editor requires at least one map added to the library.");
                return;
            }
            
            var egos = await ConnectionManager.API.GetLibrary<VehicleDetailData>();
            if (egos.Length == 0)
            {
                Debug.LogError("Scenario Editor requires at least one ego vehicle added to the library.");
                return;
            }

            if (!IsInScenarioEditor && !SceneManager.GetSceneByName(ScenarioEditorSceneName).isLoaded)
            {
                IsInScenarioEditor = true;
                SceneManager.LoadScene(ScenarioEditorSceneName);
            }
        }

        public static void ExitScenarioEditor()
        {
            IsInScenarioEditor = false;
            if (SceneManager.GetSceneByName(ScenarioEditorSceneName).isLoaded)
                SceneManager.LoadScene(Instance.LoaderScene);
        }

        static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
            {
                hex.AppendFormat("{0:x2}", b);
            }
            return hex.ToString();
        }

        static byte[] StringToByteArray(string hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }

        public static SimulatorManager CreateSimulatorManager()
        {
            var sim = Instantiate(Instance.SimulatorManagerPrefab);
            sim.name = "SimulatorManager";
            Instance.Network.InitializeSimulationScene(sim.gameObject);

            return sim;
        }

        public static TestCaseProcessManager CreateTestCaseProcessManager()
        {
            var manager = Instantiate(Instance.TestCaseProcessManagerPrefab);

            if (manager == null)
            {
                Debug.LogError($"[LOADER] Can't Instantiate TestCaseProcessManager");
            }
            else
            {
                manager.name = "TestCaseProcessManager";
            }

            return manager;
        }

        static void RunTestCase(TemplateData template)
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

            if (Instance.TCManager == null)
            {
                Instance.TCManager = CreateTestCaseProcessManager();
                DontDestroyOnLoad(Instance.TCManager);
            }

            Instance.TCManager.OnFinished += Instance.StopSimulationOnTestCaseExit;
            Instance.TCManager.OnFinished += Instance.RemoveVolumesOnTestCaseExit;

            var environment = new Dictionary<string, string>();

            SimulationConfigUtils.UpdateTestCaseEnvironment(template, environment);
            var volumesPath = SimulationConfigUtils.SaveVolumes(Instance.CurrentSimulation.Id, template);

            if (!Instance.TCManager.StartProcess(template.Alias, environment, volumesPath))
            {
                // TODO Report testcase error result to the cloud
                // Stop simulation (by raising an excepton)
                throw new Exception("Failed to launch TestCase runtime");
            }
        }

        void StopSimulationOnTestCaseExit(TestCaseFinishedArgs e)
        {
            Console.WriteLine($"[LOADER] TestCase process exits: {e.ToString()}");
            // Schedule real action to stop simulation
            Instance.Actions.Enqueue(() =>
            {
                
                if (e.Failed)
                {
                    // TODO TC: Report failed testcase to the cloud
                }

                Console.WriteLine($"[LOADER] Stopping simulation on TestCase process exit");
                Loader.StopAsync();
            });
        }

        void RemoveVolumesOnTestCaseExit(TestCaseFinishedArgs e)
        {
            Instance.Actions.Enqueue(() =>
            {
                Console.WriteLine($"[LOADER] Cleanup volumes on TestCase process exit");
                Instance.TCManager.OnFinished -= Instance.RemoveVolumesOnTestCaseExit;
                SimulationConfigUtils.CleanupVolumes(Instance.CurrentSimulation.Id);
            });
        }
    }
}
