/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Client
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
    using Core.Client;
    using Core.Shared.Configs;
    using Core.Shared.Connection;
    using Core.Shared.Messaging;
    using Core.Shared.Messaging.Data;
    using Database;
    using global::Web;
    using ICSharpCode.SharpZipLib.Zip;
    using LiteNetLib.Utils;
    using PetaPoco;
    using Shared;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Web;
    using Web.Modules;
    using YamlDotNet.Serialization;

    /// <summary>
    /// Simulation network client manager
    /// </summary>
    public class ClientManager : MonoBehaviour, IMessageSender, IMessageReceiver
    {
        /// <summary>
        /// Network settings for this simulation
        /// </summary>
        private NetworkSettings settings;

        /// <summary>
        /// Root of the mocked objects
        /// </summary>
        private ClientObjectsRoot objectsRoot;

        /// <summary>
        /// Current state of the simulation
        /// </summary>
        private SimulationState State { get; set; } = SimulationState.Initial;

        /// <inheritdoc />
        public string Key { get; } = "SimulationManager";

        /// <summary>
        /// Packets processor used for objects deserialization
        /// </summary>
        private NetPacketProcessor PacketsProcessor { get; } = new NetPacketProcessor();

        /// <summary>
        /// Messages manager for incoming and outgoing messages via connection manager
        /// </summary>
        public MessagesManager MessagesManager { get; }

        /// <summary>
        /// Connection manager for this server simulation
        /// </summary>
        public LiteNetLibClient ConnectionManager { get; } = new LiteNetLibClient();

        /// <summary>
        /// Cached connection manager to the master peer
        /// </summary>
        private IPeerManager MasterPeer { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ClientManager()
        {
            MessagesManager = new MessagesManager(ConnectionManager);
        }

        /// <summary>
        /// Unity Awake method
        /// </summary>
        private void Awake()
        {
            PacketsProcessor.RegisterNestedType(SerializationHelpers.SerializeLoadAgent,
                SerializationHelpers.DeserializeLoadAgent);
            PacketsProcessor.SubscribeReusable<Commands.Load>(OnLoadCommand);
            PacketsProcessor.SubscribeReusable<Commands.Run>(OnRunCommand);
            PacketsProcessor.SubscribeReusable<Commands.Stop>(OnStopCommand);
            PacketsProcessor.SubscribeReusable<Commands.EnvironmentState>(OnEnvironmentStateCommand);
        }

        /// <summary>
        /// Unity LateUpdate method
        /// </summary>
        private void LateUpdate()
        {
            ConnectionManager.PoolEvents();
        }

        /// <summary>
        /// Unity OnApplicationQuit method
        /// </summary>
        private void OnApplicationQuit()
        {
            StopConnection();
        }

        /// <summary>
        /// Unity OnDestroy method
        /// </summary>
        private void OnDestroy()
        {
            StopConnection();
        }

        /// <summary>
        /// Sets network settings for this simulation
        /// </summary>
        /// <param name="networkSettings">Network settings to set</param>
        public void SetSettings(NetworkSettings networkSettings)
        {
            settings = networkSettings;
            if (objectsRoot != null)
                objectsRoot.SetSettings(settings);
        }

        /// <summary>
        /// Start the connection listening for incoming packets
        /// </summary>
        public void StartConnection()
        {
            if (settings == null)
                throw new NullReferenceException("Set network settings before starting the connection.");
            MessagesManager.RegisterObject(this);
            ConnectionManager.Start(settings.ConnectionPort);
            ConnectionManager.PeerConnected += OnPeerConnected;
            ConnectionManager.PeerDisconnected += OnPeerDisconnected;
        }

        /// <summary>
        /// Stop the connection
        /// </summary>
        public void StopConnection()
        {
            State = SimulationState.Initial;
            ConnectionManager.PeerConnected -= OnPeerConnected;
            ConnectionManager.PeerDisconnected -= OnPeerDisconnected;
            ConnectionManager.Stop();
            MessagesManager.UnregisterObject(this);
        }

        /// <summary>
        /// Method invoked when new peer connects
        /// </summary>
        /// <param name="peer">Peer that has connected</param>
        public void OnPeerConnected(IPeerManager peer)
        {
            Debug.Assert(State == SimulationState.Initial);
            MasterPeer = peer;

            Debug.Log($"Master {peer.PeerEndPoint} connected.");

            var info = new Commands.Info()
            {
                Version = "todo",
                UnityVersion = Application.unityVersion,
                OperatingSystem = SystemInfo.operatingSystemFamily.ToString(),
            };
            State = SimulationState.Connected;
            UnicastMessage(peer.PeerEndPoint, new Message(Key, new BytesStack(PacketsProcessor.Write(info), false),
                MessageType.ReliableOrdered));
        }

        /// <summary>
        /// Method invoked when peer disconnects
        /// </summary>
        /// <param name="peer">Peer that has disconnected</param>
        public void OnPeerDisconnected(IPeerManager peer)
        {
            MasterPeer = null;
            OnStopCommand(new Commands.Stop());
            MessagesManager.RevokeIdentifiers();
            Debug.Log($"Peer {peer.PeerEndPoint} disconnected.");
        }

        /// <inheritdoc />
        public void UnicastMessage(IPEndPoint endPoint, Message message)
        {
            MessagesManager.UnicastMessage(endPoint, message);
        }

        /// <inheritdoc />
        public void BroadcastMessage(Message message)
        {
            MessagesManager.BroadcastMessage(message);
        }

        /// <inheritdoc />
        void IMessageSender.UnicastInitialMessages(IPEndPoint endPoint)
        {
        }

        /// <inheritdoc />
        public void ReceiveMessage(IPeerManager sender, Message message)
        {
            Debug.Assert(MasterPeer == null || MasterPeer == sender);
            PacketsProcessor.ReadAllPackets(new NetDataReader(message.Content.GetDataCopy()), sender);
        }

        /// <summary>
        /// Method invoked when manager receives load command
        /// </summary>
        /// <param name="load">Received load command</param>
        private void OnLoadCommand(Commands.Load load)
        {
            Debug.Assert(State == SimulationState.Connected);
            State = SimulationState.Loading;

            Debug.Log("Preparing simulation");

            try
            {
                MapModel map;
                using (var db = DatabaseManager.Open())
                {
                    var sql = Sql.Builder.Where("url = @0", load.MapUrl);
                    map = db.SingleOrDefault<MapModel>(sql);
                }

                if (map == null)
                {
                    Debug.Log($"Downloading {load.MapName} from {load.MapUrl}");

                    map = new MapModel()
                    {
                        Name = load.MapName,
                        Url = load.MapUrl,
                        LocalPath = WebUtilities.GenerateLocalPath("Maps"),
                    };

                    using (var db = DatabaseManager.Open())
                    {
                        db.Insert(map);
                    }

                    DownloadManager.AddDownloadToQueue(new Uri(map.Url), map.LocalPath, null, (success, ex) =>
                    {
                        if (ex != null)
                        {
                            map.Error = ex.Message;
                            using (var db = DatabaseManager.Open())
                            {
                                db.Update(map);
                            }

                            Debug.LogException(ex);
                        }

                        if (success)
                        {
                            LoadMapBundle(load, map.LocalPath);
                        }
                        else
                        {
                            var err = new Commands.LoadResult()
                            {
                                Success = false,
                                ErrorMessage = ex.ToString(),
                            };
                            UnicastMessage(MasterPeer.PeerEndPoint, new Message(Key,
                                new BytesStack(PacketsProcessor.Write(err), false),
                                MessageType.ReliableOrdered));
                            return;
                        }
                    });
                }
                else
                {
                    Debug.Log($"Map {load.MapName} exists");
                    LoadMapBundle(load, map.LocalPath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);

                var err = new Commands.LoadResult()
                {
                    Success = false,
                    ErrorMessage = ex.ToString(),
                };
                UnicastMessage(MasterPeer.PeerEndPoint, new Message(Key,
                    new BytesStack(PacketsProcessor.Write(err), false),
                    MessageType.ReliableOrdered));

                Loader.ResetLoaderScene();
            }
        }

        /// <summary>
        /// Method invoked when manager receives run command
        /// </summary>
        /// <param name="run">Received run command</param>
        private void OnRunCommand(Commands.Run run)
        {
            Debug.Assert(State == SimulationState.Ready);
            State = SimulationState.Running;
        }

        /// <summary>
        /// Method invoked when manager receives stop command
        /// </summary>
        /// <param name="stop">Received stop command</param>
        private void OnStopCommand(Commands.Stop stop)
        {
            if (State != SimulationState.Running) return;
            Loader.StopAsync();
            State = SimulationState.Initial;
        }

        /// <summary>
        /// Method invoked when manager receives command updating environment state
        /// </summary>
        /// <param name="state">Environment state update command</param>
        private void OnEnvironmentStateCommand(Commands.EnvironmentState state)
        {
            // TODO: this seems backwards to update UI to update actual values

            var ui = SimulatorManager.Instance.UIManager;
            ui.FogSlider.value = state.Fog;
            ui.RainSlider.value = state.Rain;
            ui.WetSlider.value = state.Wet;
            ui.CloudSlider.value = state.Cloud;
            ui.TimeOfDaySlider.value = state.TimeOfDay;
        }

        /// <summary>
        /// Download required for the simulation vehicle bundles from the server
        /// </summary>
        /// <param name="load">Load command from the server</param>
        /// <param name="bundles">Paths where bundles will be saved</param>
        /// <param name="finished">Callback invoked when downloading is completed</param>
        private void DownloadVehicleBundles(Commands.Load load, List<string> bundles, Action finished)
        {
            try
            {
                int count = 0;

                var agents = load.Agents;
                for (int i = 0; i < load.Agents.Length; i++)
                {
                    VehicleModel vehicleModel;
                    using (var db = DatabaseManager.Open())
                    {
                        var sql = Sql.Builder.Where("url = @0", agents[i].Url);
                        vehicleModel = db.SingleOrDefault<VehicleModel>(sql);
                    }

                    if (vehicleModel == null)
                    {
                        Debug.Log($"Downloading {agents[i].Name} from {agents[i].Url}");

                        vehicleModel = new VehicleModel()
                        {
                            Name = agents[i].Name,
                            Url = agents[i].Url,
                            BridgeType = agents[i].Bridge,
                            LocalPath = WebUtilities.GenerateLocalPath("Vehicles"),
                            Sensors = agents[i].Sensors,
                        };
                        bundles.Add(vehicleModel.LocalPath);

                        DownloadManager.AddDownloadToQueue(new Uri(vehicleModel.Url), vehicleModel.LocalPath, null,
                            (success, ex) =>
                            {
                                if (ex != null)
                                {
                                    var err = new Commands.LoadResult()
                                    {
                                        Success = false,
                                        ErrorMessage = ex.ToString(),
                                    };
                                    UnicastMessage(MasterPeer.PeerEndPoint, new Message(Key,
                                        new BytesStack(PacketsProcessor.Write(err), false),
                                        MessageType.ReliableOrdered));
                                    return;
                                }

                                using (var db = DatabaseManager.Open())
                                {
                                    db.Insert(vehicleModel);
                                }

                                if (Interlocked.Increment(ref count) == bundles.Count)
                                {
                                    finished();
                                }
                            }
                        );
                    }
                    else
                    {
                        Debug.Log($"Vehicle {agents[i].Name} exists");

                        bundles.Add(vehicleModel.LocalPath);
                        if (Interlocked.Increment(ref count) == bundles.Count)
                        {
                            finished();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);

                var err = new Commands.LoadResult()
                {
                    Success = false,
                    ErrorMessage = ex.ToString(),
                };
                UnicastMessage(MasterPeer.PeerEndPoint, new Message(Key,
                    new BytesStack(PacketsProcessor.Write(err), false),
                    MessageType.ReliableOrdered));

                Loader.ResetLoaderScene();
            }
        }

        /// <summary>
        /// Load vehicle bundles and return vehicles prefabs
        /// </summary>
        /// <param name="bundles">Bundles required to be loaded</param>
        /// <returns>Loaded vehicles prefabs</returns>
        /// <exception cref="Exception">Could not load vehicle from the asset bundle</exception>
        private GameObject[] LoadVehicleBundles(List<string> bundles)
        {
            return bundles.Select(bundle =>
            {
                using (ZipFile zip = new ZipFile(bundle))
                {
                    Manifest manifest;
                    ZipEntry entry = zip.GetEntry("manifest");
                    using (var ms = zip.GetInputStream(entry))
                    {
                        int streamSize = (int) entry.Size;
                        byte[] buffer = new byte[streamSize];
                        streamSize = ms.Read(buffer, 0, streamSize);
                        manifest = new Deserializer().Deserialize<Manifest>(
                            Encoding.UTF8.GetString(buffer, 0, streamSize));
                    }

                    AssetBundle textureBundle = null;

                    if (zip.FindEntry($"{manifest.bundleGuid}_vehicle_textures", true) != -1)
                    {
                        var texStream = zip.GetInputStream(zip.GetEntry($"{manifest.bundleGuid}_vehicle_textures"));
                        textureBundle = AssetBundle.LoadFromStream(texStream, 0, 1 << 20);
                    }

                    string platform = SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows
                        ? "windows"
                        : "linux";
                    var mapStream = zip.GetInputStream(zip.GetEntry($"{manifest.bundleGuid}_vehicle_main_{platform}"));
                    var vehicleBundle = AssetBundle.LoadFromStream(mapStream, 0, 1 << 20);

                    if (vehicleBundle == null)
                    {
                        throw new Exception($"Failed to load '{bundle}' vehicle asset bundle");
                    }

                    try
                    {
                        var vehicleAssets = vehicleBundle.GetAllAssetNames();
                        if (vehicleAssets.Length != 1)
                        {
                            throw new Exception($"Unsupported '{bundle}' vehicle asset bundle, only 1 asset expected");
                        }

                        textureBundle?.LoadAllAssets();

                        return vehicleBundle.LoadAsset<GameObject>(vehicleAssets[0]);
                    }
                    finally
                    {
                        textureBundle?.Unload(false);
                        vehicleBundle.Unload(false);
                    }
                }
            }).ToArray();
        }

        /// <summary>
        /// Creates simulation model corresponding to loaded simulation config
        /// </summary>
        /// <param name="config">Loaded simulation config from the master</param>
        /// <returns>Simulation model corresponding to loaded simulation config</returns>
        private SimulationModel CreateSimulationModel(SimulationConfig config)
        {
            //TODO Remove method and modify the Loader.StopAsync()
            var model = new SimulationModel();
            model.Cloudiness = config.Cloudiness;
            //model.Cluster
            //model.Error
            model.Fog = config.Fog;
            model.Headless = config.Headless;
            //model.Id
            model.Interactive = false;
            //model.Map
            model.Name = config.Name;
            //model.Owner
            model.Rain = config.Rain;
            model.Seed = config.Seed;
            model.Status = "Starting";
            //model.Vehicles
            model.Wetness = config.Wetness;
            model.ApiOnly = config.ApiOnly;
            //model.UseBicyclists
            model.UsePedestrians = false;
            model.UseTraffic = false;
            model.TimeOfDay = config.TimeOfDay;
            return model;
        }

        /// <summary>
        /// Download required for the simulation vehicle bundles from the server
        /// </summary>
        /// <param name="load">Load command from the server</param>
        /// <param name="mapBundlePath">Path where the map bundle will be saved</param>
        private void LoadMapBundle(Commands.Load load, string mapBundlePath)
        {
            var vehicleBundles = new List<string>();
            DownloadVehicleBundles(load, vehicleBundles, () =>
            {
                var zip = new ZipFile(mapBundlePath);
                {
                    string manfile;
                    ZipEntry entry = zip.GetEntry("manifest");
                    using (var ms = zip.GetInputStream(entry))
                    {
                        int streamSize = (int) entry.Size;
                        byte[] buffer = new byte[streamSize];
                        streamSize = ms.Read(buffer, 0, streamSize);
                        manfile = Encoding.UTF8.GetString(buffer);
                    }

                    Manifest manifest = new Deserializer().Deserialize<Manifest>(manfile);

                    AssetBundle textureBundle = null;

                    if (zip.FindEntry(($"{manifest.bundleGuid}_environment_textures"), false) != -1)
                    {
                        var texStream = zip.GetInputStream(zip.GetEntry($"{manifest.bundleGuid}_environment_textures"));
                        textureBundle = AssetBundle.LoadFromStream(texStream, 0, 1 << 20);
                    }

                    string platform = SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows
                        ? "windows"
                        : "linux";
                    var mapStream =
                        zip.GetInputStream(zip.GetEntry($"{manifest.bundleGuid}_environment_main_{platform}"));
                    var mapBundle = AssetBundle.LoadFromStream(mapStream, 0, 1 << 20);

                    if (mapBundle == null)
                    {
                        throw new Exception($"Failed to load environment from '{load.MapName}' asset bundle");
                    }

                    textureBundle?.LoadAllAssets();

                    var scenes = mapBundle.GetAllScenePaths();
                    if (scenes.Length != 1)
                    {
                        throw new Exception(
                            $"Unsupported environment in '{load.MapName}' asset bundle, only 1 scene expected");
                    }

                    var sceneName = Path.GetFileNameWithoutExtension(scenes[0]);

                    var loader = SceneManager.LoadSceneAsync(sceneName);
                    loader.completed += op =>
                    {
                        if (op.isDone)
                        {
                            textureBundle?.Unload(false);
                            mapBundle.Unload(false);
                            zip.Close();

                            try
                            {
                                var prefabs = LoadVehicleBundles(vehicleBundles);

                                Loader.Instance.SimConfig = new SimulationConfig()
                                {
                                    Name = load.Name,
                                    ApiOnly = load.ApiOnly,
                                    Headless = load.Headless,
                                    Interactive = load.Interactive,
                                    TimeOfDay = DateTime.ParseExact(load.TimeOfDay, "o", CultureInfo.InvariantCulture),
                                    Rain = load.Rain,
                                    Fog = load.Fog,
                                    Wetness = load.Wetness,
                                    Cloudiness = load.Cloudiness,
                                    UseTraffic = load.UseTraffic,
                                    UsePedestrians = load.UsePedestrians,
                                    Agents = load.Agents.Zip(prefabs, (agent, prefab) =>
                                    {
                                        var config = new AgentConfig()
                                        {
                                            Name = agent.Name,
                                            Prefab = prefab,
                                            Connection = agent.Connection,
                                            Sensors = agent.Sensors,
                                        };

                                        if (!string.IsNullOrEmpty(agent.Bridge))
                                        {
                                            config.Bridge =
                                                Web.Config.Bridges.Find(bridge => bridge.Name == agent.Bridge);
                                            if (config.Bridge == null)
                                            {
                                                throw new Exception($"Bridge {agent.Bridge} not found");
                                            }
                                        }

                                        return config;
                                    }).ToArray(),
                                };

                                Loader.Instance.CurrentSimulation = CreateSimulationModel(Loader.Instance.SimConfig);
                                Loader.Instance.CurrentSimulation.Status = "Running";
                                Loader.CreateSimulationManager();
                                objectsRoot = SimulatorManager.Instance.gameObject.AddComponent<ClientObjectsRoot>();
                                objectsRoot.SetMessagesManager(MessagesManager);
                                objectsRoot.SetSettings(settings);

                                // Notify WebUI simulation is running
                                NotificationManager.SendNotification("simulation",
                                    SimulationResponse.Create(Loader.Instance.CurrentSimulation),
                                    Loader.Instance.CurrentSimulation.Owner);

                                Debug.Log($"Client ready to start");

                                var result = new Commands.LoadResult()
                                {
                                    Success = true,
                                };
                                UnicastMessage(MasterPeer.PeerEndPoint, new Message(Key,
                                    new BytesStack(PacketsProcessor.Write(result), false),
                                    MessageType.ReliableOrdered));

                                State = SimulationState.Ready;
                            }
                            catch (Exception ex)
                            {
                                Debug.LogException(ex);

                                var err = new Commands.LoadResult()
                                {
                                    Success = false,
                                    ErrorMessage = ex.ToString(),
                                };
                                UnicastMessage(MasterPeer.PeerEndPoint, new Message(Key,
                                    new BytesStack(PacketsProcessor.Write(err), false),
                                    MessageType.ReliableOrdered));

                                Loader.ResetLoaderScene();
                            }
                        }
                    };
                }
            });
        }
    }
}