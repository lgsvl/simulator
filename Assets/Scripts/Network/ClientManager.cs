/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Globalization;
using Simulator.Web;
using Simulator.Database;
using PetaPoco;
using ICSharpCode.SharpZipLib.Zip;
using YamlDotNet.Serialization;
using System.Text;
using System.Collections.Generic;
using System.Threading;

namespace Simulator.Network
{
    public class ClientManager : MonoBehaviour, INetEventListener
    {
        State ClientState = State.Initial;

        NetManager Manager;
        NetPacketProcessor Packets = new NetPacketProcessor();

        NetPeer Master;

        ConcurrentQueue<Action> Actions = new ConcurrentQueue<Action>();

        void Awake()
        {
            Packets.RegisterNestedType(SerializationHelpers.SerializeLoadAgent, SerializationHelpers.DeserializeLoadAgent);
            Packets.SubscribeReusable<Commands.Load>(OnLoadCommand);
            Packets.SubscribeReusable<Commands.Run>(OnRunCommand);
            Packets.SubscribeReusable<Commands.EnvironmentState>(OnEnvironmentStateCommand);

            Manager = new NetManager(this);
            Manager.UpdateTime = 1;
            Manager.Start(Constants.Port);

            DontDestroyOnLoad(this);
        }

        void OnApplicationQuit()
        {
            Manager.Stop();
        }

        void Update()
        {
            Manager.PollEvents();

            while (Actions.TryDequeue(out var action))
            {
                action();
            }
        }

        void OnDestroy()
        {
            Manager.Stop();
        }

        public void OnPeerConnected(NetPeer peer)
        {
            Debug.Assert(ClientState == State.Initial);
            Master = peer;

            Debug.Log($"Master {peer.EndPoint} connected");

            var info = new Commands.Info()
            {
                Version = "todo",
                UnityVersion = Application.unityVersion,
                OperatingSystem = SystemInfo.operatingSystemFamily.ToString(),
            };
            Packets.Send(Master, info, DeliveryMethod.ReliableOrdered);

            ClientState = State.Connected;
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Debug.Log($"Peer {peer.EndPoint} disconnected: reason={disconnectInfo.Reason}, error={disconnectInfo.SocketErrorCode}");
            ClientState = State.Initial;
            Master = null;
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            Debug.Log($"Error {socketError} for {endPoint} endpoint");

            // if master != null then raise exceptions
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            Debug.Assert(Master == peer);
            Packets.ReadAllPackets(reader, peer);
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
        }

        public void OnConnectionRequest(ConnectionRequest request)
        {
            Debug.Assert(ClientState == State.Initial);

            if (Master == null)
            {
                request.AcceptIfKey(Constants.ConnectionKey);
            }
            else
            {
                request.Reject();
            }
        }

        void OnLoadCommand(Commands.Load load)
        {
            Debug.Assert(ClientState == State.Connected);
            ClientState = State.Loading;

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
                            Packets.Send(Master, err, DeliveryMethod.ReliableOrdered);
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
                Packets.Send(Master, err, DeliveryMethod.ReliableOrdered);

                Loader.ResetLoaderScene();
            }
        }

        void OnRunCommand(Commands.Run run)
        {
            Debug.Assert(ClientState == State.Ready);
            ClientState = State.Running;

            SimulatorManager.SetTimeScale(1.0f);
        }

        void OnEnvironmentStateCommand(Commands.EnvironmentState state)
        {
            // TODO: this seems backwards to update UI to update actual values

            var ui = SimulatorManager.Instance.UIManager;
            ui.FogSlider.value = state.Fog;
            ui.RainSlider.value = state.Rain;
            ui.WetSlider.value = state.Wet;
            ui.CloudSlider.value = state.Cloud;
            ui.TimeOfDaySlider.value = state.TimeOfDay;
        }

        void DownloadVehicleBundles(Commands.Load load, List<string> bundles, Action finished)
        {
            try
            {
                int count = 0;

                var agents = load.Agents;
                for (int i=0; i<load.Agents.Length; i++)
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
                                    Packets.Send(Master, err, DeliveryMethod.ReliableOrdered);
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
                Packets.Send(Master, err, DeliveryMethod.ReliableOrdered);

                Loader.ResetLoaderScene();
            }
        }

        GameObject[] LoadVehicleBundles(List<string> bundles)
        {
            return bundles.Select(bundle =>
            {
                using (ZipFile zip = new ZipFile(bundle))
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

                    AssetBundle textureBundle = null;

                    if (zip.FindEntry($"{manifest.bundleGuid}_vehicle_textures", true) != -1)
                    {
                        var texStream = zip.GetInputStream(zip.GetEntry($"{manifest.bundleGuid}_vehicle_textures"));
                        textureBundle = AssetBundle.LoadFromStream(texStream, 0, 1 << 20);
                    }

                    string platform = SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows ? "windows" : "linux";
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

        void LoadMapBundle(Commands.Load load, string mapBundlePath)
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
                        int streamSize = (int)entry.Size;
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

                    string platform = SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows ? "windows" : "linux";
                    var mapStream = zip.GetInputStream(zip.GetEntry($"{manifest.bundleGuid}_environment_main_{platform}"));
                    var mapBundle = AssetBundle.LoadFromStream(mapStream, 0, 1 << 20);

                    if (mapBundle == null)
                    {
                        throw new Exception($"Failed to load environment from '{load.MapName}' asset bundle");
                    }

                    textureBundle?.LoadAllAssets();

                    var scenes = mapBundle.GetAllScenePaths();
                    if (scenes.Length != 1)
                    {
                        throw new Exception($"Unsupported environment in '{load.MapName}' asset bundle, only 1 scene expected");
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
                                            config.Bridge = Web.Config.Bridges.Find(bridge => bridge.Name == agent.Bridge);
                                            if (config.Bridge == null)
                                            {
                                                throw new Exception($"Bridge {agent.Bridge} not found");
                                            }
                                        }

                                        return config;

                                    }).ToArray(),
                                };

                                Loader.CreateSimulationManager();

                                Debug.Log($"Client ready to start");

                                var result = new Commands.LoadResult()
                                {
                                    Success = true,
                                };
                                Packets.Send(Master, result, DeliveryMethod.ReliableOrdered);

                                ClientState = State.Ready;
                            }
                            catch (Exception ex)
                            {
                                Debug.LogException(ex);

                                var err = new Commands.LoadResult()
                                {
                                    Success = false,
                                    ErrorMessage = ex.ToString(),
                                };
                                Packets.Send(Master, err, DeliveryMethod.ReliableOrdered);

                                Loader.ResetLoaderScene();
                            }
                        }
                    };
                }
            });
        }
    }
}
