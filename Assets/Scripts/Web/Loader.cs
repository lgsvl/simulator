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
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using Nancy.Hosting.Self;
using Simulator.Database;
using Simulator.Api;
using Simulator.Web;
using Simulator.Web.Modules;
using Simulator.Utilities;
using Simulator.Bridge;
using System.Text;
using System.Security.Cryptography;
using Simulator.Database.Services;
using System.Linq;
using Web;
using ICSharpCode.SharpZipLib.Zip;
using YamlDotNet.Serialization;
using System.Net.Http;
using SimpleJSON;
using Simulator.Network.Client;
using Simulator.Network.Core.Shared.Configs;
using Simulator.Network.Core.Shared.Threading;
using MasterManager = Simulator.Network.Master.MasterManager;


namespace Simulator
{
    using Network;

    public class AgentConfig
    {
        public string Name;
        public string Url;
        public string AssetBundle;
        public GameObject Prefab;
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
        public string MapUrl;
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
        public AgentConfig[] Agents;
        public bool UseTraffic;
        public bool UsePedestrians;
        public int? Seed;
    }

    public class Loader : MonoBehaviour
    {
        public string Address { get; private set; }

        private NancyHost Server;
        private MasterManager masterManager;
        private ClientManager clientManager;
        public SimulatorManager SimulatorManagerPrefab;
        public ApiManager ApiManagerPrefab;

        public NetworkSettings NetworkSettings;

        public LoaderUI LoaderUI => FindObjectOfType<LoaderUI>();

        // NOTE: When simulation is not running this reference will be null.
        public SimulationModel CurrentSimulation;

        ConcurrentQueue<Action> Actions = new ConcurrentQueue<Action>();
        string LoaderScene;

        public SimulationConfig SimConfig;

        // Loader object is never destroyed, even between scene reloads
        public static Loader Instance { get; private set; }
        private System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();

        private void Awake()
        {
            stopWatch.Start();
            RenderLimiter.RenderLimitEnabled();

            var info = Resources.Load<BuildInfo>("BuildInfo");
            SIM.Init(info == null ? "Development" : info.Version);

            if (PlayerPrefs.HasKey("Salt"))
            {
                Config.salt = StringToByteArray(PlayerPrefs.GetString("Salt"));
            }
            else
            {
                Config.salt = new byte[8];
                RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
                rng.GetBytes(Config.salt);
                PlayerPrefs.SetString("Salt", ByteArrayToString(Config.salt));
                PlayerPrefs.Save();
            }
        }

        void Start()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            
            if (!Config.RunAsMaster)
            {
                // TODO: change UI and do not run rest of code
                var obj = new GameObject("ClientManager");
                clientManager = obj.AddComponent<ClientManager>();
                clientManager.SetSettings(NetworkSettings);
                obj.AddComponent<MainThreadDispatcher>();
                clientManager.StartConnection();
            }

            DatabaseManager.Init();

            DownloadManager.Init();
            RestartPendingDownloads();

            try
            {
                var host = Config.WebHost == "*" ? "localhost" : Config.WebHost;
                Address = $"http://{host}:{Config.WebPort}";

                var config = new HostConfiguration { RewriteLocalhost = Config.WebHost == "*" };

                Server = new NancyHost(new UnityBootstrapper(), config, new Uri(Address));
                if (!string.IsNullOrEmpty(Config.Username))
                {
                    LoginAsync();
                }
                else
                {
                    Server.Start();
                }
            }
            catch (SocketException ex)
            {
                Debug.LogException(ex);
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                // return non-zero exit code
                Application.Quit(1);
#endif
                return;
            }

            LoaderScene = SceneManager.GetActiveScene().name;
            SIM.LogSimulation(SIM.Simulation.ApplicationStart);

            DontDestroyOnLoad(this);
            Instance = this;
        }

        async void LoginAsync()
        {
            try
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Accept", "application/json");

                var postData = new[] {  new KeyValuePair<string, string>("username", Config.Username),
                                    new KeyValuePair<string, string>("password", Config.Password),
                                    new KeyValuePair<string, string>("returnUrl", Config.WebHost + Config.WebPort)};
                var postForm = new FormUrlEncodedContent(postData);
                var postResponse = await client.PostAsync(Config.CloudUrl + "/users/signin", postForm);
                var postContent = await postResponse.Content.ReadAsStringAsync();

                var postJson = JSONNode.Parse(postContent);

                var putData = new[] { new KeyValuePair<string, string>("token", postJson["token"].Value) };
                var formContent = new FormUrlEncodedContent(putData);

                var response = await client.PutAsync(Config.CloudUrl + "/users/token", formContent);
                var content = await response.Content.ReadAsStringAsync();

                var json = JSONNode.Parse(content);
                UserModel userModel = new UserModel()
                {
                    Username = json["username"].Value,
                    SecretKey = json["secretKey"].Value,
                    Settings = json["settings"].Value
                };

                if (string.IsNullOrEmpty(userModel.Username))
                {
                    throw new Exception("Invalid Login: incorrect login info or account does not exist");
                }

                UserService userService = new UserService();
                userService.AddOrUpdate(userModel);
                SIM.InitUser(userModel.Username);

                var guid = Guid.NewGuid();
                UserMapper.RegisterUserSession(guid, userModel.Username);
                Config.SessionGUID = guid.ToString();

                Server.Start();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                // return non-zero exit code
                Application.Quit(1);
#endif
                return;
            }
        }

        void RestartPendingDownloads()
        {
            using (var db = DatabaseManager.Open())
            {
                foreach (var map in DatabaseManager.PendingMapDownloads())
                {
                    SIM.LogWeb(SIM.Web.MapDownloadStart, map.Name);
                    Uri uri = new Uri(map.Url);
                    DownloadManager.AddDownloadToQueue(
                        uri,
                        map.LocalPath,
                        progress =>
                        {
                            Debug.Log($"Map Download at {progress}%");
                            NotificationManager.SendNotification("MapDownload", new { map.Id, progress }, map.Owner);
                        },
                        (success, ex) =>
                        {
                            var updatedModel = db.Single<MapModel>(map.Id);
                            bool passesValidation = false;
                            if (success)
                            {
                                passesValidation = Validation.BeValidAssetBundle(map.LocalPath);
                                if (!passesValidation)
                                {
                                    updatedModel.Error = "You must specify a valid AssetBundle";
                                }
                            }

                            updatedModel.Status = passesValidation ? "Valid" : "Invalid";

                            if (ex != null)
                            {
                                updatedModel.Error = ex.Message;
                            }

                            db.Update(updatedModel);
                            NotificationManager.SendNotification("MapDownloadComplete", updatedModel, map.Owner);

                            SIM.LogWeb(SIM.Web.MapDownloadFinish, map.Name);
                        }
                    );
                }

                var added = new HashSet<Uri>();
                var vehicles = new VehicleService();

                foreach (var vehicle in DatabaseManager.PendingVehicleDownloads())
                {
                    Uri uri = new Uri(vehicle.Url);
                    if (added.Contains(uri))
                    {
                        continue;
                    }
                    added.Add(uri);

                    SIM.LogWeb(SIM.Web.VehicleDownloadStart, vehicle.Name);
                    DownloadManager.AddDownloadToQueue(
                        uri,
                        vehicle.LocalPath,
                        progress =>
                        {
                            Debug.Log($"Vehicle Download at {progress}%");
                            NotificationManager.SendNotification("VehicleDownload", new { vehicle.Id, progress }, vehicle.Owner);
                        },
                        (success, ex) =>
                        {
                            bool passesValidation = success && Validation.BeValidAssetBundle(vehicle.LocalPath);

                            string status = passesValidation ? "Valid" : "Invalid";
                            vehicles.SetStatusForPath(status, vehicle.LocalPath);
                            vehicles.GetAllMatchingUrl(vehicle.Url).ForEach(v =>
                            {
                                if (!passesValidation)
                                {
                                    v.Error = "You must specify a valid AssetBundle";
                                }

                                if (ex != null)
                                {
                                    v.Error = ex.Message;
                                }

                                NotificationManager.SendNotification("VehicleDownloadComplete", v, v.Owner);
                                SIM.LogWeb(SIM.Web.VehicleDownloadFinish, vehicle.Name);
                            });
                        }
                    );
                }
            }
        }

        void OnApplicationQuit()
        {
            Server?.Stop();
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

        public static void StartAsync(SimulationModel simulation)
        {
            Debug.Assert(Instance.CurrentSimulation == null);

            Instance.Actions.Enqueue(() =>
            {
                using (var db = DatabaseManager.Open())
                {
                    AssetBundle textureBundle = null;
                    AssetBundle mapBundle = null;
                    try
                    {
                        if (Config.Headless && (simulation.Headless.HasValue && !simulation.Headless.Value))
                        {
                            throw new Exception("Simulator is configured to run in headless mode, only headless simulations are allowed");
                        }

                        simulation.Status = "Starting";
                        NotificationManager.SendNotification("simulation", SimulationResponse.Create(simulation), simulation.Owner);
                        Instance.LoaderUI.SetLoaderUIState(LoaderUI.LoaderUIStateType.PROGRESS);

                        Instance.SimConfig = new SimulationConfig()
                        {
                            Name = simulation.Name,
                            Clusters = db.Single<ClusterModel>(simulation.Cluster).Ips.Split(',').Where(c => c != "127.0.0.1").ToArray(),
                            ClusterName = db.Single<ClusterModel>(simulation.Cluster).Name,
                            ApiOnly = simulation.ApiOnly.GetValueOrDefault(),
                            Headless = simulation.Headless.GetValueOrDefault(),
                            Interactive = simulation.Interactive.GetValueOrDefault(),
                            TimeOfDay = simulation.TimeOfDay.GetValueOrDefault(new DateTime(1980, 3, 24, 12, 0, 0)),
                            Rain = simulation.Rain.GetValueOrDefault(),
                            Fog = simulation.Fog.GetValueOrDefault(),
                            Wetness = simulation.Wetness.GetValueOrDefault(),
                            Cloudiness = simulation.Cloudiness.GetValueOrDefault(),
                            UseTraffic = simulation.UseTraffic.GetValueOrDefault(),
                            UsePedestrians = simulation.UsePedestrians.GetValueOrDefault(),
                            Seed = simulation.Seed,
                        };

                        if (simulation.Vehicles == null || simulation.Vehicles.Length == 0 || simulation.ApiOnly.GetValueOrDefault())
                        {
                            Instance.SimConfig.Agents = Array.Empty<AgentConfig>();
                        }
                        else
                        {
                            Instance.SimConfig.Agents = simulation.Vehicles.Select(v =>
                            {
                                var vehicle = db.SingleOrDefault<VehicleModel>(v.Vehicle);

                                var config = new AgentConfig()
                                {
                                    Name = vehicle.Name,
                                    Url = vehicle.Url,
                                    AssetBundle = vehicle.LocalPath,
                                    Connection = v.Connection,
                                    Sensors = vehicle.Sensors,
                                };

                                if (!string.IsNullOrEmpty(vehicle.BridgeType))
                                {
                                    config.Bridge = Config.Bridges.Find(bridge => bridge.Name == vehicle.BridgeType);
                                    if (config.Bridge == null)
                                    {
                                        throw new Exception($"Bridge {vehicle.BridgeType} not found");
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

                            // ready to go!
                            Instance.CurrentSimulation.Status = "Running";
                            NotificationManager.SendNotification("simulation", SimulationResponse.Create(simulation), simulation.Owner);

                            Instance.LoaderUI.SetLoaderUIState(LoaderUI.LoaderUIStateType.READY);
                        }
                        else
                        {
                            var mapModel = db.Single<MapModel>(simulation.Map);
                            var mapBundlePath = mapModel.LocalPath;
                            mapBundle = null;
                            textureBundle = null;

                            ZipFile zip = new ZipFile(mapBundlePath);
                            {
                                string manfile;
                                ZipEntry entry = zip.GetEntry("manifest");
                                using (var ms = zip.GetInputStream(entry))
                                {
                                    int streamSize = (int)entry.Size;
                                    byte[] buffer = new byte[streamSize];
                                    streamSize = ms.Read(buffer, 0, streamSize);
                                    manfile = Encoding.UTF8.GetString(buffer);
                                }

                                Manifest manifest = new Deserializer().Deserialize<Manifest>(manfile);

                                if (manifest.bundleFormat != BundleConfig.MapBundleFormatVersion)
                                {
                                    zip.Close();

                                    // TODO: proper exception
                                    throw new ZipException("BundleFormat version mismatch");
                                }

                                if (zip.FindEntry($"{manifest.bundleGuid}_environment_textures", false) != -1)
                                {
                                    var texStream = zip.GetInputStream(zip.GetEntry($"{manifest.bundleGuid}_environment_textures"));
                                    textureBundle = AssetBundle.LoadFromStream(texStream, 0, 1 << 20);
                                }

                                string platform = SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows ? "windows" : "linux";
                                var mapStream = zip.GetInputStream(zip.GetEntry($"{manifest.bundleGuid}_environment_main_{platform}"));
                                mapBundle = AssetBundle.LoadFromStream(mapStream, 0, 1 << 20);

                                if (mapBundle == null)
                                {
                                    throw new Exception($"Failed to load environment from '{mapModel.Name}' asset bundle");
                                }

                                textureBundle?.LoadAllAssets();

                                var scenes = mapBundle.GetAllScenePaths();
                                if (scenes.Length != 1)
                                {
                                    throw new Exception($"Unsupported environment in '{mapModel.Name}' asset bundle, only 1 scene expected");
                                }

                                var sceneName = Path.GetFileNameWithoutExtension(scenes[0]);
                                Instance.SimConfig.MapName = sceneName;
                                Instance.SimConfig.MapUrl = mapModel.Url;

                                var loader = SceneManager.LoadSceneAsync(sceneName);
                                loader.completed += op =>
                                {
                                    if (op.isDone)
                                    {
                                        textureBundle?.Unload(false);
                                        mapBundle.Unload(false);
                                        zip.Close();
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

                        // NOTE: In case of failure we have to update Simulation state
                        simulation.Status = "Invalid";
                        simulation.Error = "Out of date Map AssetBundle. Please check content website for updated bundle or rebuild the bundle.";
                        db.Update(simulation);

                        if (SceneManager.GetActiveScene().name != Instance.LoaderScene)
                        {
                            SceneManager.LoadScene(Instance.LoaderScene);
                        }

                        textureBundle?.Unload(false);
                        mapBundle?.Unload(false);
                        AssetBundle.UnloadAllAssetBundles(true);
                        Instance.CurrentSimulation = null;

                        // TODO: take ex.Message and append it to response here
                        NotificationManager.SendNotification("simulation", SimulationResponse.Create(simulation), simulation.Owner);
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"Failed to start '{simulation.Name}' simulation");
                        Debug.LogException(ex);

                        // NOTE: In case of failure we have to update Simulation state
                        simulation.Status = "Invalid";
                        simulation.Error = ex.Message;
                        db.Update(simulation);

                        if (SceneManager.GetActiveScene().name != Instance.LoaderScene)
                        {
                            SceneManager.LoadScene(Instance.LoaderScene);
                        }

                        textureBundle?.Unload(false);
                        mapBundle?.Unload(false);
                        AssetBundle.UnloadAllAssetBundles(true);
                        Instance.CurrentSimulation = null;

                        // TODO: take ex.Message and append it to response here
                        NotificationManager.SendNotification("simulation", SimulationResponse.Create(simulation), simulation.Owner);
                    }
                }
            });
        }

        public static void StopAsync()
        {
            Debug.Assert(Instance.CurrentSimulation != null);

            if (Instance.masterManager != null)
                Instance.masterManager.BroadcastSimulationStop();

            Instance.Actions.Enqueue(() =>
            {
                var simulation = Instance.CurrentSimulation;
                using (var db = DatabaseManager.Open())
                {
                    try
                    {
                        simulation.Status = "Stopping";
                        NotificationManager.SendNotification("simulation", SimulationResponse.Create(simulation), simulation.Owner);

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

                                simulation.Status = "Valid";
                                NotificationManager.SendNotification("simulation", SimulationResponse.Create(simulation), simulation.Owner);
                                Instance.CurrentSimulation = null;
                                if (Instance.masterManager != null)
                                    Instance.masterManager.StopConnection();
                            }
                        };
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"Failed to stop '{simulation.Name}' simulation");
                        Debug.LogException(ex);

                        // NOTE: In case of failure we have to update Simulation state
                        simulation.Status = "Invalid";
                        simulation.Error = ex.Message;
                        db.Update(simulation);

                        // TODO: take ex.Message and append it to response here
                        NotificationManager.SendNotification("simulation", SimulationResponse.Create(simulation), simulation.Owner);
                    }
                }
            });
        }

        static void SetupScene(SimulationModel simulation)
        {
            Dictionary<string, GameObject> cachedVehicles = new Dictionary<string, GameObject>();

            using (var db = DatabaseManager.Open())
            {
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
                            ZipEntry entry = zip.GetEntry("manifest");
                            using (var ms = zip.GetInputStream(entry))
                            {
                                int streamSize = (int)entry.Size;
                                byte[] buffer = new byte[streamSize];
                                streamSize = ms.Read(buffer, 0, streamSize);
                                manifest = new Deserializer().Deserialize<Manifest>(Encoding.UTF8.GetString(buffer, 0, streamSize));
                            }

                            if (manifest.bundleFormat != BundleConfig.VehicleBundleFormatVersion)
                            {
                                zip.Close();

                                // TODO: proper exception
                                throw new ZipException("BundleFormat version mismatch");
                            }

                            var texStream = zip.GetInputStream(zip.GetEntry($"{manifest.bundleGuid}_vehicle_textures"));
                            textureBundle = AssetBundle.LoadFromStream(texStream, 0, 1 << 20);

                            string platform = SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows ? "windows" : "linux";
                            var mapStream = zip.GetInputStream(zip.GetEntry($"{manifest.bundleGuid}_vehicle_main_{platform}"));

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

                    var sim = CreateSimulationManager();

                    Instance.CurrentSimulation = simulation;
                    if (Instance.SimConfig.Clusters.Length == 0)
                    {
                        // Notify WebUI simulation is running
                        Instance.CurrentSimulation.Status = "Running";
                        NotificationManager.SendNotification("simulation", SimulationResponse.Create(Instance.CurrentSimulation), Instance.CurrentSimulation.Owner);

                        // Flash main window to let user know simulation is ready
                        WindowFlasher.Flash();
                    }
                }
                catch (ZipException ex)
                {
                    Debug.Log($"Failed to start '{simulation.Name}' simulation - out of date asset bundles");
                    Debug.LogException(ex);

                    // NOTE: In case of failure we have to update Simulation state
                    simulation.Status = "Invalid";
                    simulation.Error = "Out of date Vehicle AssetBundle. Please check content website for updated bundle or rebuild the bundle.";
                    db.Update(simulation);

                    // TODO: take ex.Message and append it to response here
                    NotificationManager.SendNotification("simulation", SimulationResponse.Create(simulation), simulation.Owner);

                    ResetLoaderScene();
                }
                catch (Exception ex)
                {
                    Debug.Log($"Failed to start '{simulation.Name}' simulation");
                    Debug.LogException(ex);

                    // NOTE: In case of failure we have to update Simulation state
                    simulation.Status = "Invalid";
                    simulation.Error = ex.Message;
                    db.Update(simulation);

                    // TODO: take ex.Message and append it to response here
                    NotificationManager.SendNotification("simulation", SimulationResponse.Create(simulation), simulation.Owner);

                    ResetLoaderScene();
                }
            }
        }

        public static void ResetLoaderScene()
        {
            if (SceneManager.GetActiveScene().name != Instance.LoaderScene)
            {
                SceneManager.LoadScene(Instance.LoaderScene);
                AssetBundle.UnloadAllAssetBundles(true);
                Instance.CurrentSimulation = null;
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

        static void StartNetworkMaster()
        {
            var obj = new GameObject("NetworkMaster");
            var masterManager = obj.AddComponent<MasterManager>();
            obj.AddComponent<MainThreadDispatcher>();
            Instance.masterManager = masterManager;
            SimulatorManager.Instance.Network.Master = Instance.masterManager;
            SimulatorManager.Instance.Network.MessagesManager = Instance.masterManager.MessagesManager;
            masterManager.SetSettings(Instance.NetworkSettings);
            masterManager.Simulation = Instance.SimConfig;
            masterManager.StartConnection();
            masterManager.ConnectToClients();
        }

        public static SimulatorManager CreateSimulationManager()
        {
            var sim = Instantiate(Instance.SimulatorManagerPrefab);
            sim.name = "SimulatorManager";
            
            //Initialize network fields
            if (Instance.SimConfig.Clusters == null)
            {
                if (Config.RunAsMaster)
                    sim.Network.Initialize(SimulationNetwork.ClusterNodeType.NotClusterNode, Instance.NetworkSettings);
                else
                {
                    sim.Network.Initialize(SimulationNetwork.ClusterNodeType.Client, Instance.NetworkSettings);
                    sim.Network.Client = Instance.clientManager;
                    sim.Network.MessagesManager = Instance.clientManager.MessagesManager;
                }
            }
            else if (Instance.SimConfig.Clusters.Length == 0)
                sim.Network.Initialize(SimulationNetwork.ClusterNodeType.NotClusterNode, Instance.NetworkSettings);
            else if (Config.RunAsMaster)
            {
                sim.Network.Initialize(SimulationNetwork.ClusterNodeType.Master, Instance.NetworkSettings);
                StartNetworkMaster();
            }

            //Initialize Simulator Manager
            sim.Init();
            
            return sim;
        }
    }
}
