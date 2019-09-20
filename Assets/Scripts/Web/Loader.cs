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
using Web;
using Simulator.Bridge;
using System.Text;
using System.Security.Cryptography;
using Simulator.Database.Services;
using System.Linq;

namespace Simulator
{
    public class AgentConfig
    {
        public string Name;
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
        public SimulatorManager SimulatorManagerPrefab;
        public ApiManager ApiManagerPrefab;

        public Network.MasterManager Master;
        public Network.ClientManager Client;
        public SimulationModel PendingSimulation;

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
            SIM.Identify();
            RenderLimiter.RenderLimitEnabled();

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
                obj.AddComponent<Network.ClientManager>();
            }

            DatabaseManager.Init();

            try
            {
                var host = Config.WebHost == "*" ? "localhost" : Config.WebHost;
                Address = $"http://{host}:{Config.WebPort}";

                var config = new HostConfiguration { RewriteLocalhost = Config.WebHost == "*" };

                Server = new NancyHost(new UnityBootstrapper(), config, new Uri(Address));
                Server.Start();
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

            DownloadManager.Init();
            RestartPendingDownloads();

            LoaderScene = SceneManager.GetActiveScene().name;
            var version = "Development";
            var info = Resources.Load<BuildInfo>("BuildInfo");
            if (info != null)
                version = info.Version;
            SIM.Init(version);
            SIM.LogSimulation(SIM.Simulation.ApplicationStart);

            DontDestroyOnLoad(this);
            Instance = this;
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
                        success =>
                        {
                            var updatedModel = db.Single<MapModel>(map.Id);
                            updatedModel.Status = success ? "Valid" : "Invalid";
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
                        success =>
                        {
                            string status = success ? "Valid" : "Invalid";
                            vehicles.SetStatusForPath(status, vehicle.LocalPath);
                            vehicles.GetAllMatchingUrl(vehicle.Url).ForEach(v =>
                            {
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
                            TimeOfDay = simulation.TimeOfDay.GetValueOrDefault(DateTime.MinValue.AddHours(12)),
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

                            // TODO: make this async
                            mapBundle = AssetBundle.LoadFromFile(mapBundlePath);
                            if (mapBundle == null)
                            {
                                throw new Exception($"Failed to load environment from '{mapModel.Name}' asset bundle");
                            }

                            var scenes = mapBundle.GetAllScenePaths();
                            if (scenes.Length != 1)
                            {
                                throw new Exception($"Unsupported environment in '{mapModel.Name}' asset bundle, only 1 scene expected");
                            }

                            var sceneName = Path.GetFileNameWithoutExtension(scenes[0]);
                            Instance.SimConfig.MapName = sceneName;

                            var loader = SceneManager.LoadSceneAsync(sceneName);
                            loader.completed += op =>
                            {
                                if (op.isDone)
                                {
                                    mapBundle.Unload(false);
                                    SetupScene(simulation);
                                }
                            };

                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"Failed to start '{simulation.Name}' simulation");
                        Debug.LogException(ex);

                        // NOTE: In case of failure we have to update Simulation state
                        simulation.Status = "Invalid";
                        db.Update(simulation);

                        if (SceneManager.GetActiveScene().name != Instance.LoaderScene)
                        {
                            SceneManager.LoadScene(Instance.LoaderScene);
                        }
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
                            Destroy(ApiManager.Instance.gameObject);
                        }
                        SIM.LogSimulation(SIM.Simulation.ApplicationClick, "Exit");
                        var loader = SceneManager.LoadSceneAsync(Instance.LoaderScene);
                        loader.completed += op =>
                        {
                            if (op.isDone)
                            {
                                AssetBundle.UnloadAllAssetBundles(true);
                                Instance.LoaderUI.SetLoaderUIState(LoaderUI.LoaderUIStateType.START);

                                simulation.Status = "Valid";
                                NotificationManager.SendNotification("simulation", SimulationResponse.Create(simulation), simulation.Owner);
                                Instance.CurrentSimulation = null;
                            }
                        };
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"Failed to stop '{simulation.Name}' simulation");
                        Debug.LogException(ex);

                        // NOTE: In case of failure we have to update Simulation state
                        simulation.Status = "Invalid";
                        db.Update(simulation);

                        // TODO: take ex.Message and append it to response here
                        NotificationManager.SendNotification("simulation", SimulationResponse.Create(simulation), simulation.Owner);
                    }
                }
            });
        }

        static void SetupScene(SimulationModel simulation)
        {
            using (var db = DatabaseManager.Open())
            {
                try
                {
                    foreach (var agentConfig in Instance.SimConfig.Agents)
                    {
                        var bundlePath = agentConfig.AssetBundle;

                        // TODO: make this async
                        var vehicleBundle = AssetBundle.LoadFromFile(bundlePath);
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
                            agentConfig.Prefab = vehicleBundle.LoadAsset<GameObject>(vehicleAssets[0]);
                        }
                        finally
                        {
                            vehicleBundle.Unload(false);
                        }
                    }

                    var sim = CreateSimulationManager();

                    // TODO: connect to cluster instances
                    //if (Instance.SimConfig.Clusters.Length > 0)
                    //{
                    //    SimulatorManager.SetTimeScale(0);

                    //    Instance.PendingSimulation = simulation;

                    //    StartNetworkMaster();
                    //    Instance.Master.AddClients(Instance.SimConfig.Clusters);
                    //}
                    //else
                    {
                        Instance.CurrentSimulation = simulation;

                        // Notify WebUI simulation is running
                        Instance.CurrentSimulation.Status = "Running";
                        NotificationManager.SendNotification("simulation", SimulationResponse.Create(Instance.CurrentSimulation), Instance.CurrentSimulation.Owner);

                        // Flash main window to let user know simulation is ready
                        WindowFlasher.Flash();
                    }
                }
                catch (Exception ex)
                {
                    Debug.Log($"Failed to start '{simulation.Name}' simulation");
                    Debug.LogException(ex);

                    // NOTE: In case of failure we have to update Simulation state
                    simulation.Status = "Invalid";
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
            Instance.Master = obj.AddComponent<Network.MasterManager>();
            Instance.Master.Simulation = Instance.SimConfig;
        }

        public static SimulatorManager CreateSimulationManager()
        {
            var sim = Instantiate(Instance.SimulatorManagerPrefab);
            sim.name = "SimulatorManager";
            sim.Init();
            return sim;
        }
    }
}
