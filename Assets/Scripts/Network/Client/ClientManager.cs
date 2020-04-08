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
    using Core.Configs;
    using Core.Connection;
    using Core.Messaging;
    using Core.Messaging.Data;
    using Database;
    using global::Web;
    using ICSharpCode.SharpZipLib.Zip;
    using LiteNetLib.Utils;
    using PetaPoco;
    using Shared;

    using Simulator.Network.Core;

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
        /// Downloads that are currently in progress
        /// </summary>
        private List<string> processedDownloads = new List<string>();

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
        /// Cached current load command, validates if download operation has been overriden
        /// </summary>
        private Commands.Load CurrentLoadCommand { get; set; }

        /// <summary>
        /// Root of the mocked objects
        /// </summary>
        public ClientObjectsRoot ObjectsRoot => objectsRoot;

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
            PacketsProcessor.SubscribeReusable<Commands.Ping>(OnPingCommand);

            SetCollisionBetweenSimulationObjects(false);
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
            SetCollisionBetweenSimulationObjects(true);
            StopConnection();
        }

        /// <summary>
        /// Unity OnDestroy method
        /// </summary>
        private void OnDestroy()
        {
            SetCollisionBetweenSimulationObjects(true);
            StopConnection();
        }

        private void SetCollisionBetweenSimulationObjects(bool collision)
        {
            var agentLayer = LayerMask.NameToLayer("Agent");
            var npcLayer = LayerMask.NameToLayer("NPC");
            var pedestrianLayer = LayerMask.NameToLayer("Pedestrian");
            Physics.IgnoreLayerCollision(agentLayer, agentLayer, !collision);
            Physics.IgnoreLayerCollision(agentLayer, npcLayer, !collision);
            Physics.IgnoreLayerCollision(agentLayer, pedestrianLayer, !collision);
            Physics.IgnoreLayerCollision(npcLayer, npcLayer, !collision);
            Physics.IgnoreLayerCollision(npcLayer, pedestrianLayer, !collision);
            Physics.IgnoreLayerCollision(pedestrianLayer, pedestrianLayer, !collision);
        }

        /// <summary>
        /// Sets network settings for this simulation
        /// </summary>
        /// <param name="networkSettings">Network settings to set</param>
        public void SetSettings(NetworkSettings networkSettings)
        {
            settings = networkSettings;
            if (ObjectsRoot != null)
                ObjectsRoot.SetSettings(settings);
        }

        /// <summary>
        /// Initializes the simulation, adds <see cref="ClientObjectsRoot"/> component to the root game object
        /// </summary>
        /// <param name="rootGameObject">Root game object where new component will be added</param>
        public void InitializeSimulation(GameObject rootGameObject)
        {
            objectsRoot = rootGameObject.AddComponent<ClientObjectsRoot>();
            ObjectsRoot.SetMessagesManager(MessagesManager);
            ObjectsRoot.SetSettings(settings);
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

            Log.Info($"Master {peer.PeerEndPoint} connected.");

            var info = new Commands.Info()
            {
                Version = "todo",
                UnityVersion = Application.unityVersion,
                OperatingSystem = SystemInfo.operatingSystemFamily.ToString(),
            };
            State = SimulationState.Connected;
            if (Loader.Instance.LoaderUI!=null)
                Loader.Instance.LoaderUI.SetLoaderUIState(LoaderUI.LoaderUIStateType.PROGRESS);
            var infoData = PacketsProcessor.Write(info);
            var message = MessagesPool.Instance.GetMessage(infoData.Length);
            message.AddressKey = Key;
            message.Content.PushBytes(infoData);
            message.Type = DistributedMessageType.ReliableOrdered;
            UnicastMessage(peer.PeerEndPoint, message);
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
            Log.Info($"Peer {peer.PeerEndPoint} disconnected.");
        }

        /// <inheritdoc />
        public void UnicastMessage(IPEndPoint endPoint, DistributedMessage distributedMessage)
        {
            MessagesManager.UnicastMessage(endPoint, distributedMessage);
        }

        /// <inheritdoc />
        public void BroadcastMessage(DistributedMessage distributedMessage)
        {
            MessagesManager.BroadcastMessage(distributedMessage);
        }

        /// <inheritdoc />
        void IMessageSender.UnicastInitialMessages(IPEndPoint endPoint)
        {
        }

        /// <inheritdoc />
        public void ReceiveMessage(IPeerManager sender, DistributedMessage distributedMessage)
        {
            Debug.Assert(MasterPeer == null || MasterPeer == sender);
            PacketsProcessor.ReadAllPackets(new NetDataReader(distributedMessage.Content.GetDataCopy()), sender);
        }

        /// <summary>
        /// Method invoked when manager receives load command
        /// </summary>
        /// <param name="load">Received load command</param>
        private void OnLoadCommand(Commands.Load load)
        {
            Debug.Assert(State == SimulationState.Connected);
            CurrentLoadCommand = load;
            State = SimulationState.Loading;

            Log.Info("Preparing simulation");

            try
            {
                //Check if downloading is already being processed, if true this may be a quick rerun of the simulation
                if (processedDownloads.Contains(load.MapUrl))
                    return;
                MapModel map;
                using (var db = DatabaseManager.Open())
                {
                    var sql = Sql.Builder.Where("name = @0", load.MapName);
                    map = db.SingleOrDefault<MapModel>(sql);
                }

                if (map == null)
                {
                    Log.Info($"Downloading {load.MapName} from {load.MapUrl}");

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

                    processedDownloads.Add(map.Name);
                    DownloadManager.AddDownloadToQueue(new Uri(map.Url), map.LocalPath, null, (success, ex) =>
                    {
                        processedDownloads.Remove(map.Name);
                        //Check if downloaded map is still valid in current load command
                        if (CurrentLoadCommand.MapName != map.Name)
                            return;
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
                            var errData = PacketsProcessor.Write(err);
                            var message = MessagesPool.Instance.GetMessage(errData.Length);
                            message.AddressKey = Key;
                            message.Content.PushBytes(errData);
                            message.Type = DistributedMessageType.ReliableOrdered;
                            UnicastMessage(MasterPeer.PeerEndPoint, message);
                        }
                    });
                }
                else
                {
                    Log.Info($"Map {load.MapName} exists");
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
                var errData = PacketsProcessor.Write(err);
                var message = MessagesPool.Instance.GetMessage(errData.Length);
                message.AddressKey = Key;
                message.Content.PushBytes(errData);
                message.Type = DistributedMessageType.ReliableOrdered;
                UnicastMessage(MasterPeer.PeerEndPoint, message);

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
            if (Loader.Instance.LoaderUI != null) Loader.Instance.LoaderUI.DisableUI();
            SceneManager.UnloadSceneAsync(Loader.Instance.LoaderScene);
            State = SimulationState.Running;
        }

        /// <summary>
        /// Method invoked when manager receives stop command
        /// </summary>
        /// <param name="stop">Received stop command</param>
        private void OnStopCommand(Commands.Stop stop)
        {
            if (Loader.Instance.CurrentSimulation != null && State != SimulationState.Initial)
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
        /// Method invoked when manager receives ping command
        /// </summary>
        /// <param name="ping">Ping command</param>
        private void OnPingCommand(Commands.Ping ping)
        {
            var stopData = PacketsProcessor.Write(new Commands.Pong() { Id = ping.Id});
            var message = MessagesPool.Instance.GetMessage(stopData.Length);
            message.AddressKey = Key;
            message.Content.PushBytes(stopData);
            message.Type = DistributedMessageType.Unreliable;
            BroadcastMessage(message);
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
                var agentsToDownload = load.Agents.Length;
                if (agentsToDownload == 0)
                {
                    finished();
                    return;
                }

                for (int i = 0; i < agentsToDownload; i++)
                {
                    //Check if downloading is already being processed, if true this may be a quick rerun of the simulation
                    if (processedDownloads.Contains(agents[i].Name))
                    {
                        Interlocked.Increment(ref count);
                        continue;
                    }
                    VehicleModel vehicleModel;
                    using (var db = DatabaseManager.Open())
                    {
                        var sql = Sql.Builder.Where("name = @0", agents[i].Name);
                        vehicleModel = db.SingleOrDefault<VehicleModel>(sql);
                    }

                    if (vehicleModel == null)
                    {
                        Log.Info($"Downloading {agents[i].Name} from {agents[i].Url}");

                        vehicleModel = new VehicleModel()
                        {
                            Name = agents[i].Name,
                            Url = agents[i].Url,
                            BridgeType = agents[i].Bridge,
                            LocalPath = WebUtilities.GenerateLocalPath("Vehicles"),
                            Sensors = agents[i].Sensors,
                        };
                        bundles.Add(vehicleModel.LocalPath);

                        processedDownloads.Add(vehicleModel.Name);
                        DownloadManager.AddDownloadToQueue(new Uri(vehicleModel.Url), vehicleModel.LocalPath, null,
                            (success, ex) =>
                            {
                                //Check if downloaded vehicle model is still valid in current load command
                                if (CurrentLoadCommand.Agents.All(loadAgent => loadAgent.Name != vehicleModel.Name))
                                    return;
                                processedDownloads.Remove(vehicleModel.Name);
                                if (ex != null)
                                {
                                    var err = new Commands.LoadResult()
                                    {
                                        Success = false,
                                        ErrorMessage = ex.ToString(),
                                    };
                                    var errData = PacketsProcessor.Write(err);
                                    var message = MessagesPool.Instance.GetMessage(errData.Length);
                                    message.AddressKey = Key;
                                    message.Content.PushBytes(errData);
                                    message.Type = DistributedMessageType.ReliableOrdered;
                                    UnicastMessage(MasterPeer.PeerEndPoint, message);
                                    return;
                                }

                                using (var db = DatabaseManager.Open())
                                {
                                    db.Insert(vehicleModel);
                                }

                                if (Interlocked.Increment(ref count) == agentsToDownload)
                                {
                                    finished();
                                }
                            }
                        );
                    }
                    else
                    {
                        Log.Info($"Vehicle {agents[i].Name} exists");

                        bundles.Add(vehicleModel.LocalPath);
                        if (Interlocked.Increment(ref count) == agentsToDownload)
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
                var errData = PacketsProcessor.Write(err);
                var message = MessagesPool.Instance.GetMessage(errData.Length);
                message.AddressKey = Key;
                message.Content.PushBytes(errData);
                message.Type = DistributedMessageType.ReliableOrdered;
                UnicastMessage(MasterPeer.PeerEndPoint, message);

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

                    if (zip.FindEntry($"{manifest.assetGuid}_vehicle_textures", true) != -1)
                    {
                        var texStream = zip.GetInputStream(zip.GetEntry($"{manifest.assetGuid}_vehicle_textures"));
                        textureBundle = AssetBundle.LoadFromStream(texStream, 0, 1 << 20);
                    }

                    string platform = SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows
                        ? "windows"
                        : "linux";
                    var mapStream = zip.GetInputStream(zip.GetEntry($"{manifest.assetGuid}_vehicle_main_{platform}"));
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
                if (MasterPeer == null)
                {
                    Log.Warning("Master peer has disconnected while loading the simulation scene.");
                    Loader.ResetLoaderScene();
                    return;
                }
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

                    if (zip.FindEntry(($"{manifest.assetGuid}_environment_textures"), false) != -1)
                    {
                        var texStream = zip.GetInputStream(zip.GetEntry($"{manifest.assetGuid}_environment_textures"));
                        textureBundle = AssetBundle.LoadFromStream(texStream, 0, 1 << 20);
                    }

                    string platform = SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows
                        ? "windows"
                        : "linux";
                    var mapStream =
                        zip.GetInputStream(zip.GetEntry($"{manifest.assetGuid}_environment_main_{platform}"));
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

                    var loader = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                    loader.completed += op =>
                    {
                        if (op.isDone)
                        {
                            SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
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
                                if (Loader.Instance.SimConfig.ApiOnly)
                                {
                                    var api = Instantiate(Loader.Instance.ApiManagerPrefab);
                                    api.name = "ApiManager";
                                }
                                
                                var simulatorManager = Loader.CreateSimulatorManager();
                                if (load.UseSeed)
                                    simulatorManager.Init(load.Seed);
                                else
                                    simulatorManager.Init();
                                InitializeSimulation(simulatorManager.gameObject);
                                
                                // Notify WebUI simulation is running
                                NotificationManager.SendNotification("simulation",
                                    SimulationResponse.Create(Loader.Instance.CurrentSimulation),
                                    Loader.Instance.CurrentSimulation.Owner);

                                Log.Info($"Client ready to start");

                                var result = new Commands.LoadResult()
                                {
                                    Success = true,
                                };
                                
                                Loader.Instance.LoaderUI.SetLoaderUIState(LoaderUI.LoaderUIStateType.READY);
                                if (MasterPeer == null)
                                {
                                    Log.Warning("Master peer has disconnected while loading the simulation scene.");
                                    Loader.ResetLoaderScene();
                                    return;
                                }
                                var resultData = PacketsProcessor.Write(result);
                                var message = MessagesPool.Instance.GetMessage(resultData.Length);
                                message.AddressKey = Key;
                                message.Content.PushBytes(resultData);
                                message.Type = DistributedMessageType.ReliableOrdered;
                                UnicastMessage(MasterPeer.PeerEndPoint, message);

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
                                var errData = PacketsProcessor.Write(err);
                                var message = MessagesPool.Instance.GetMessage(errData.Length);
                                message.AddressKey = Key;
                                message.Content.PushBytes(errData);
                                message.Type = DistributedMessageType.ReliableOrdered;
                                UnicastMessage(MasterPeer.PeerEndPoint, message);

                                Loader.ResetLoaderScene();
                            }
                        }
                    };
                }
            });
        }
    }
}