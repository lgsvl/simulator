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
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Nancy.Hosting.Self;
using Simulator.Database;
using Simulator.Web;
using Simulator.Web.Modules;
using Web;

namespace Simulator
{
    public class Loader : MonoBehaviour
    {
        private int Port = 8080;
        private string Address;

        private NancyHost Server;

        public Button button;
        public SimulatorManager SimulatorManager;

        // NOTE: When simulation is not running this reference will be null.
        public SimulationModel CurrentSimulation;

        ConcurrentQueue<Action> Actions = new ConcurrentQueue<Action>();
        string LoaderScene;

        // Loader object is never destroyed, even between scene reloads
        public static Loader Instance { get; private set; }

        void Start()
        {
            if (Instance != null)
            {
                InitLoader();
                Destroy(gameObject);
                return;
            }

            DatabaseManager.Init();

            Address = $"http://localhost:{Port}";

            try
            {
                // Bind to all interfaces instead of localhost
                var config = new HostConfiguration { RewriteLocalhost = true };

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

            DontDestroyOnLoad(this);
            Instance = this;
            InitLoader();
        }

        void RestartPendingDownloads()
        {
            using (var db = DatabaseManager.Open())
            {
                foreach (var map in DatabaseManager.PendingMapDownloads())
                {
                    Uri uri = new Uri(map.Url);
                    DownloadManager.AddDownloadToQueue(
                        uri,
                        map.LocalPath,
                        progress =>
                        {
                            Debug.Log($"Map Download at {progress}%");
                            NotificationManager.SendNotification("MapDownload", new { map.Id, progress });
                        },
                        success =>
                        {
                            var updatedModel = db.Single<MapModel>(map.Id);
                            updatedModel.Status = success ? "Valid" : "Invalid";
                            db.Update(updatedModel);
                            NotificationManager.SendNotification("MapDownloadComplete", updatedModel);
                        }
                    );
                }
                
                foreach (var vehicle in DatabaseManager.PendingVehicleDownloads())
                {
                    Uri uri = new Uri(vehicle.Url);
                    DownloadManager.AddDownloadToQueue(
                        uri,
                        vehicle.LocalPath,
                        progress =>
                        {
                            Debug.Log($"Vehicle Download at {progress}%");
                            NotificationManager.SendNotification("VehicleDownload", new { vehicle.Id, progress });
                        },
                        success =>
                        {
                            var updatedModel = db.Single<VehicleModel>(vehicle.Id);
                            updatedModel.Status = success ? "Valid" : "Invalid";
                            db.Update(updatedModel);
                            NotificationManager.SendNotification("VehicleDownloadComplete", updatedModel);
                        }
                    );
                }
            }
        }

        void InitLoader()
        {
            button.onClick.AddListener(Instance.OnButtonClick);
        }

        void OnButtonClick()
        {
            Application.OpenURL(Address + "/");
        }

        void OnApplicationQuit()
        {
            Server?.Stop();
        }

        private void Update()
        {
            Action action;
            while (Actions.TryDequeue(out action))
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
                        simulation.Status = "Starting";
                        NotificationManager.SendNotification("simulation", SimulationResponse.Create(simulation));

                        var config = new SimulationConfig()
                        {
                            Name = simulation.Name,
                            Cluster = db.Single<ClusterModel>(simulation.Cluster).Ips,
                            ApiOnly = simulation.ApiOnly.GetValueOrDefault(),
                            Interactive = simulation.Interactive.GetValueOrDefault(),
                            OffScreen = simulation.Headless.GetValueOrDefault(),
                            TimeOfDay = simulation.TimeOfDay.GetValueOrDefault(),
                            Rain = simulation.Rain.GetValueOrDefault(),
                            Fog = simulation.Fog.GetValueOrDefault(),
                            Wetness = simulation.Wetness.GetValueOrDefault(),
                            Cloudiness = simulation.Cloudiness.GetValueOrDefault(),
                            UseTraffic = simulation.UseTraffic.GetValueOrDefault(),
                            UsePedestrians = simulation.UsePedestrians.GetValueOrDefault(),
                        };

                        // load environment
                        if (config.ApiOnly)
                        {
                            SetupScene(config, simulation);
                        }
                        else
                        {
                            var mapBundlePath = db.Single<MapModel>(simulation.Map).LocalPath;

                            // TODO: make this async
                            mapBundle = AssetBundle.LoadFromFile(mapBundlePath);
                            if (mapBundle == null)
                            {
                                throw new Exception($"Failed to load environment from '{mapBundlePath}' asset bundle");
                            }

                            var scenes = mapBundle.GetAllScenePaths();
                            if (scenes.Length != 1)
                            {
                                throw new Exception($"Unsupported environment in '{mapBundlePath}' asset bundle, only 1 scene expected");
                            }

                            var sceneName = Path.GetFileNameWithoutExtension(scenes[0]);

                            var loader = SceneManager.LoadSceneAsync(sceneName);
                            loader.completed += op =>
                            {
                                if (op.isDone)
                                {
                                    mapBundle.Unload(false);
                                    SetupScene(config, simulation);
                                }
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"Failed to start simulation with {simulation.Id}");
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
                        NotificationManager.SendNotification("simulation", SimulationResponse.Create(simulation));
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
                        NotificationManager.SendNotification("simulation", SimulationResponse.Create(simulation));

                        var loader = SceneManager.LoadSceneAsync(Instance.LoaderScene);
                        loader.completed += op =>
                        {
                            if (op.isDone)
                            {
                                AssetBundle.UnloadAllAssetBundles(true);

                                simulation.Status = "Valid";
                                NotificationManager.SendNotification("simulation", SimulationResponse.Create(simulation));
                                Instance.CurrentSimulation = null;
                            }
                        };
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"Failed to stop simulation with {simulation.Id}");
                        Debug.LogException(ex);

                        // NOTE: In case of failure we have to update Simulation state
                        simulation.Status = "Invalid";
                        db.Update(simulation);

                        // TODO: take ex.Message and append it to response here
                        NotificationManager.SendNotification("simulation", SimulationResponse.Create(simulation));
                    }
                }
            });
        }

        static void SetupScene(SimulationConfig config, SimulationModel simulation)
        {
            using (var db = DatabaseManager.Open())
            {
                try
                {
                    if (simulation.Vehicles == null || simulation.Vehicles.Length == 0)
                    {
                        config.Agents = Array.Empty<AgentConfig>();
                    }
                    else
                    {
                        var agents = new List<AgentConfig>();
                        foreach (var vehicleId in simulation.Vehicles)
                        {
                            var vehicle = db.SingleOrDefault<VehicleModel>(vehicleId.Vehicle);
                            var bundlePath = vehicle.LocalPath;

                            // TODO: make this async
                            var vehicleBundle = AssetBundle.LoadFromFile(bundlePath);
                            if (vehicleBundle == null)
                            {
                                throw new Exception($"Failed to load vehicle from '{bundlePath}' asset bundle");
                            }
                            try
                            {

                                var vehicleAssets = vehicleBundle.GetAllAssetNames();
                                if (vehicleAssets.Length != 1)
                                {
                                    throw new Exception($"Unsupported vehicle in '{bundlePath}' asset bundle, only 1 asset expected");
                                }

                                // TODO: make this async
                                var prefab = vehicleBundle.LoadAsset<GameObject>(vehicleAssets[0]);
                                var agent = new AgentConfig()
                                {
                                    Name = vehicle.Name,
                                    Prefab = prefab,
                                    Sensors = vehicle.Sensors,
                                    Connection = vehicleId.Connection,
                                };
                                if (!string.IsNullOrEmpty(vehicle.BridgeType))
                                {
                                    agent.Bridge = Config.Bridges.Find(bridge => bridge.Name == vehicle.BridgeType);
                                    if (agent.Bridge == null)
                                    {
                                        throw new Exception($"Bridge {vehicle.BridgeType} not found");
                                    }
                                }
                                agents.Add(agent);
                            }
                            finally
                            {
                                vehicleBundle.Unload(false);
                            }
                        }

                        config.Agents = agents.ToArray();
                    }

                    // simulation manager
                    {
                        var sim = Instantiate(Instance.SimulatorManager);
                        sim.name = "SimulatorManager";
                        sim.Config = config;
                        sim.Init();
                    }

                    // ready to go!
                    Instance.CurrentSimulation = simulation;
                    Instance.CurrentSimulation.Status = "Running";
                    NotificationManager.SendNotification("simulation", SimulationResponse.Create(simulation));
                }
                catch (Exception ex)
                {
                    Debug.Log($"Failed to start simulation with {simulation.Id}");
                    Debug.LogException(ex);

                    // NOTE: In case of failure we have to update Simulation state
                    simulation.Status = "Invalid";
                    db.Update(simulation);

                    // TODO: take ex.Message and append it to response here
                    NotificationManager.SendNotification("simulation", SimulationResponse.Create(simulation));

                    if (SceneManager.GetActiveScene().name != Instance.LoaderScene)
                    {
                        SceneManager.LoadScene(Instance.LoaderScene);
                        AssetBundle.UnloadAllAssetBundles(true);
                        Instance.CurrentSimulation = null;
                    }
                }
            }
        }
    }
}
