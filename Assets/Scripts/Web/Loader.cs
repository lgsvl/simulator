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
using SimpleJSON;
using Simulator.Network.Shared;
using Simulator.Network.Client;
using Simulator.Network.Core.Configs;
using Simulator.Network.Core.Threading;
using MasterManager = Simulator.Network.Master.MasterManager;
using ICSharpCode.SharpZipLib.Core;
using Simulator.FMU;
using Simulator.PointCloud.Trees;
using Simulator.Database.Services;
using System.Threading.Tasks;

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
        public IBridgeFactory Bridge;
        public string Connection;
        public string Sensors;
        public Vector3 Position;
        public Quaternion Rotation;
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
    }

    public class Loader : MonoBehaviour
    {
        public SimulationNetwork Network { get; } = new SimulationNetwork();
        public SimulatorManager SimulatorManagerPrefab;
        public ApiManager ApiManagerPrefab;

        public NetworkSettings NetworkSettings;

        public LoaderUI LoaderUI => FindObjectOfType<LoaderUI>();

        // NOTE: When simulation is not running this reference will be null.
        public SimulationData CurrentSimulation;

        ConcurrentQueue<Action> Actions = new ConcurrentQueue<Action>();
        public string LoaderScene { get; private set; }

        public SimulationConfig SimConfig;

        // Loader object is never destroyed, even between scene reloads
        public static Loader Instance { get; private set; }
        private System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();

        public bool EditorLoader { get; set; } = false;

        private void Start()
        {
            if (!EditorLoader)
            {
                Init();
            }
            else
            {
                EditorInit();
            }
        }

        private void Init()
        {
            RenderLimiter.RenderLimitEnabled();
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            stopWatch.Start();

            var info = Resources.Load<BuildInfo>("BuildInfo");
            SIM.Init(info == null ? "Development" : info.Version);

            DownloadManager.Init();

            LoaderScene = SceneManager.GetActiveScene().name;
            SIM.LogSimulation(SIM.Simulation.ApplicationStart);

            DontDestroyOnLoad(this);
            Instance = this;
        }

        private void EditorInit()
        {
#if UNITY_EDITOR
            stopWatch.Start();
            var info = Resources.Load<BuildInfo>("BuildInfo");
            SIM.Init(info == null ? "Development" : info.Version);
            SIM.LogSimulation(SIM.Simulation.ApplicationStart);
            Instance = this;

            var sim = Instantiate(Instance.SimulatorManagerPrefab);
            sim.name = "SimulatorManager";
            bool useSeed = false;
            int? seed = null;
            bool enableNPCs = false;
            bool enablePEDs = false;

            var data = UnityEditor.EditorPrefs.GetString("Simulator/DevelopmentSettings");
            if (data != null)
            {
                try
                {
                    var json = JSONNode.Parse(data);

                    useSeed = json["UseSeed"];
                    if (useSeed)
                    {
                        seed = json["Seed"];
                    }

                    enableNPCs = json["EnableNPCs"];
                    enablePEDs = json["EnablePEDs"];
                }
                catch (System.Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            sim.Init(seed);
            sim.AgentManager.SetupDevAgents();
            sim.NPCManager.NPCActive = enableNPCs;
            sim.PedestrianManager.PedestriansActive = enablePEDs;
#endif
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
            Instance.Network.Initialize(Config.SimID, simData.Cluster, Instance.NetworkSettings);
            var downloads = new List<Task>();
            if(simData.ApiOnly == false)
            {
                downloads.Add(DownloadManager.GetAsset(BundleConfig.BundleTypes.Environment, simData.Map.AssetGuid));

                foreach (var vehicle in simData.Vehicles)
                {
                    downloads.Add(DownloadManager.GetAsset(BundleConfig.BundleTypes.Vehicle, vehicle.AssetGuid));
                }
            }

            ConnectionUI.instance.SetLinkingButtonActive(false);
            await Task.WhenAll(downloads);

            Debug.Log("All Downloads Complete");

            if (!Instance.Network.IsClusterSimulation)
                StartAsync(simData);
            else
                Instance.Network.SetSimulationModel(simData);
        }

        public static void StartAsync(SimulationData simulation)
        {
            Debug.Assert(Instance.CurrentSimulation == null);

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
                        
                        Instance.LoaderUI.SetLoaderUIState(LoaderUI.LoaderUIStateType.PROGRESS);
                        Instance.SimConfig = new SimulationConfig()
                        {
                            Name = simulation.Name,
                            Clusters = simulation.Cluster.Instances.Length > 1 ? simulation.Cluster.Instances.SelectMany(i => i.Ip).ToArray() : new string[] { },
                            ClusterName = simulation.Cluster.Name,
                            ApiOnly = simulation.ApiOnly,
                            Headless = simulation.Headless,
                            Interactive = simulation.Interactive,
                            TimeOfDay = simulation.TimeOfDay,
                            Rain = simulation.Rain,
                            Fog = simulation.Fog,
                            Wetness = simulation.Wetness,
                            Cloudiness = simulation.Cloudiness,
                            Damage = simulation.Damage,
                            UseTraffic = simulation.UseTraffic,
                            UsePedestrians = simulation.UsePedestrians,
                            Seed = simulation.Seed,
                        };

                        if (simulation.Vehicles == null || simulation.Vehicles.Length == 0 || simulation.ApiOnly)
                        {
                            Instance.SimConfig.Agents = Array.Empty<AgentConfig>();
                        }
                        else
                        {
                            Instance.SimConfig.Agents = simulation.Vehicles.Select(v =>
                            {
                                var config = new AgentConfig()
                                {
                                    Name = v.Name,
                                    Connection = v.bridge != null ? v.bridge.connectionString : "",
                                    AssetGuid = v.AssetGuid,
                                    AssetBundle = Web.WebUtilities.GenerateLocalPath(v.AssetGuid, BundleConfig.BundleTypes.Vehicle),
                                    Sensors = Newtonsoft.Json.JsonConvert.SerializeObject(v.Sensors),
                                };

                                if (v.bridge != null && !string.IsNullOrEmpty(v.bridge.type))
                                {
                                    config.Bridge = Config.Bridges.Find(bridge => bridge.Name == v.bridge.type);
                                    if (config.Bridge == null)
                                    {
                                        throw new Exception($"Bridge {v.bridge.type} not found");
                                    }
                                }

                                return config;

                            }).ToArray();
                        }

                        // load environment
                        if (Instance.SimConfig.ApiOnly)
                        {
                            var api = Instantiate(Instance.ApiManagerPrefab);
                            api.name = "ApiManager";

                            Instance.CurrentSimulation = simulation;

                            Instance.LoaderUI.SetLoaderUIState(LoaderUI.LoaderUIStateType.READY);
                        }
                        else
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
                    }
                    catch (ZipException ex)
                    {
                        Debug.Log($"Failed to start '{simulation.Name}' simulation");
                        Debug.LogException(ex);

                        if (SceneManager.GetActiveScene().name != Instance.LoaderScene)
                        {
                            ConnectionManager.instance.UpdateStatus("Stopping", simulation.Id);
                            SceneManager.LoadScene(Instance.LoaderScene);
                            ConnectionManager.instance.UpdateStatus("Idle", simulation.Id);
                        }

                        textureBundle?.Unload(false);
                        mapBundle?.Unload(false);
                        AssetBundle.UnloadAllAssetBundles(true);
                        Instance.CurrentSimulation = null;
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"Failed to start '{simulation.Name}' simulation");
                        Debug.LogException(ex);

                        if (SceneManager.GetActiveScene().name != Instance.LoaderScene)
                        {
                            ConnectionManager.instance.UpdateStatus("Stopping", simulation.Id);
                            SceneManager.LoadScene(Instance.LoaderScene);
                            ConnectionManager.instance.UpdateStatus("Idle", simulation.Id);
                        }

                        textureBundle?.Unload(false);
                        mapBundle?.Unload(false);
                        AssetBundle.UnloadAllAssetBundles(true);
                        Instance.CurrentSimulation = null;
                    }
            });
        }

        public static void ResetMaterials()
        {
            // TODO remove hack for editor opaque with alpha clipping 2019.3.3
#if UNITY_EDITOR
            var go = FindObjectsOfType<Renderer>();
            foreach (var renderer in go)
            {
                foreach (var m in renderer.sharedMaterials)
                {
                    m.shader = Shader.Find(m.shader.name);
                }
            }
#endif
        }

        public static void StopAsync()
        {
            if (Instance.CurrentSimulation == null) return;

            Instance.Network.StopConnection();

            Instance.Actions.Enqueue(() =>
            {
                var simulation = Instance.CurrentSimulation;
                using (var db = DatabaseManager.Open())
                {
                    try
                    {
                        ConnectionManager.instance.UpdateStatus("Stopping", simulation.Id);
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
                                
                                Instance.CurrentSimulation = null;
                                ConnectionManager.instance.UpdateStatus("Idle", simulation.Id);
                            }
                        };
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"Failed to stop '{simulation.Name}' simulation");
                        Debug.LogException(ex);
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
                        AssetBundle textureBundle = null;
                        AssetBundle vehicleBundle = null;
                        if (cachedVehicles.ContainsKey(agentConfig.Name))
                        {
                            agentConfig.Prefab = cachedVehicles[agentConfig.Name];
                            continue;
                        }

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

                            var texStream = zip.GetInputStream(zip.GetEntry($"{manifest.assetGuid}_vehicle_textures"));
                            textureBundle = AssetBundle.LoadFromStream(texStream, 0, 1 << 20);

                            string platform = SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows ? "windows" : "linux";
                            var mapStream = zip.GetInputStream(zip.GetEntry($"{manifest.assetGuid}_vehicle_main_{platform}"));

                            vehicleBundle = AssetBundle.LoadFromStream(mapStream, 0, 1 << 20);

                            if (vehicleBundle == null)
                            {
                                throw new Exception($"Failed to load '{agentConfig.Name}' vehicle asset bundle");
                            }

                            try
                            {
                                var vehicleAssets = vehicleBundle.GetAllAssetNames();
                                if (vehicleAssets.Length != 1)
                                {
                                    throw new Exception($"Unsupported '{agentConfig.Name}' vehicle asset bundle, only 1 asset expected");
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

                                agentConfig.Prefab = vehicleBundle.LoadAsset<GameObject>(vehicleAssets[0]);
                                cachedVehicles.Add(agentConfig.Name, agentConfig.Prefab);
                            }
                            finally
                            {
                                textureBundle?.Unload(false);
                                vehicleBundle.Unload(false);
                            }
                        }
                    }

                    var sim = CreateSimulatorManager();
                    if (simulation.Seed != null)
                        sim.Init(simulation.Seed);
                    else
                        sim.Init();

                    Instance.CurrentSimulation = simulation;

                    if (Instance.CurrentSimulation != null)
                    {
                        ConnectionManager.instance.UpdateStatus("Running", Instance.CurrentSimulation.Id);
                    }

                    // Flash main window to let user know simulation is ready
                    WindowFlasher.Flash();
                }
                catch (ZipException ex)
                {
                    Debug.Log($"Failed to start '{simulation.Name}' simulation - out of date asset bundles");
                    Debug.LogException(ex);

                    ResetLoaderScene(simulation);
                }
                catch (Exception ex)
                {
                    Debug.Log($"Failed to start '{simulation.Name}' simulation");
                    Debug.LogException(ex);

                    ResetLoaderScene(simulation);
                }
        }

        public static void ResetLoaderScene(SimulationData simulation)
        {
            if (SceneManager.GetActiveScene().name != Instance.LoaderScene)
            {
                ConnectionManager.instance.UpdateStatus("Stopping", simulation.Id);
                SceneManager.LoadScene(Instance.LoaderScene);
                AssetBundle.UnloadAllAssetBundles(true);
                Instance.CurrentSimulation = null;
                ConnectionManager.instance.UpdateStatus("Idle", simulation.Id);
            }
        }

        static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        static byte[] StringToByteArray(string hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        public static SimulatorManager CreateSimulatorManager()
        {
            var sim = Instantiate(Instance.SimulatorManagerPrefab);
            sim.name = "SimulatorManager";
            Instance.Network.InitializeSimulationScene(sim.gameObject);

            return sim;
        }

        static byte[] GetFile(ZipFile zip, string entryName)
        {
            var entry = zip.GetEntry(entryName);
            int streamSize = (int)entry.Size;
            byte[] buffer = new byte[streamSize];
            zip.GetInputStream(entry).Read(buffer, 0, streamSize);
            return buffer;
        }
    }
}
