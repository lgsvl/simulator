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
using System.ComponentModel;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Globalization;

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

        public void OnLoadCommand(Commands.Load load)
        {
            Debug.Assert(ClientState == State.Connected);
            ClientState = State.Loading;

            Debug.Log("Preparing simulation");

            try
            {
                using (var web = new WebClient())
                {
                    var mapPath = Path.Combine(Web.Config.Root, load.Name);

                    if (!File.Exists(mapPath))
                    {
                        Debug.Log($"Downloading {load.Name}");

                        AsyncCompletedEventHandler mapDownloadHandler = null;
                        Action<object, AsyncCompletedEventArgs> mapDownloaded = (sender, args) =>
                        {
                            web.DownloadFileCompleted -= mapDownloadHandler;

                            if (args.Error != null)
                            {
                                Debug.LogException(args.Error);

                                var err = new Commands.LoadResult()
                                {
                                    Success = false,
                                    ErrorMessage = args.Error.ToString(),
                                };
                                Packets.Send(Master, err, DeliveryMethod.ReliableOrdered);
                                return;
                            }

                            LoadMapBundle(load);
                        };

                        mapDownloadHandler = new AsyncCompletedEventHandler(mapDownloaded);
                        web.DownloadFileCompleted += mapDownloadHandler;
                        web.DownloadFileAsync(new Uri($"http://{Master.EndPoint.Address}:8080/download/map/{load.Name}"), mapPath);
                    }
                    else
                    {
                        Debug.Log($"Map {load.Name} exists");
                        LoadMapBundle(load);
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

        public void OnRunCommand(Commands.Run run)
        {
            Debug.Assert(ClientState == State.Ready);
            ClientState = State.Running;

            SimulatorManager.SetTimeScale(1.0f);
        }

        void DownloadVehicleBundles(Commands.Load load)
        {
            try
            {
                using (var web = new WebClient())
                {
                    foreach (var agent in load.Agents)
                    {
                        var vehiclePath = Path.Combine(Web.Config.Root, agent.Name);
                        if (!File.Exists(vehiclePath))
                        {
                            Debug.Log($"Downloading {agent.Name}");

                            web.DownloadFile($"http://{Master.EndPoint.Address}:8080/download/vehicle/{agent.Name}", agent.Name);
                        }
                        else
                        {
                            Debug.Log($"Vehicle {agent.Name} exists");
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

        GameObject[] LoadVehicleBundles(Commands.Load load)
        {
            return load.Agents.Select(agent =>
            {
                var vehiclePath = Path.Combine(Web.Config.Root, agent.Name);

                var vehicleBundle = AssetBundle.LoadFromFile(vehiclePath);
                if (vehicleBundle == null)
                {
                    throw new Exception($"Failed to load '{vehiclePath}' vehicle asset bundle");
                }

                try
                {
                    var vehicleAssets = vehicleBundle.GetAllAssetNames();
                    if (vehicleAssets.Length != 1)
                    {
                        throw new Exception($"Unsupported '{vehiclePath}' vehicle asset bundle, only 1 asset expected");
                    }

                    return vehicleBundle.LoadAsset<GameObject>(vehicleAssets[0]);
                }
                finally
                {
                    vehicleBundle.Unload(false);
                }
            }).ToArray();
        }

        void LoadMapBundle(Commands.Load load)
        {
            DownloadVehicleBundles(load);

            var mapPath = Path.Combine(Web.Config.Root, load.Name);

            var mapBundle = AssetBundle.LoadFromFile(mapPath);
            if (mapBundle == null)
            {
                throw new Exception($"Failed to load environment from '{mapPath}' asset bundle");
            }

            var scenes = mapBundle.GetAllScenePaths();
            if (scenes.Length != 1)
            {
                throw new Exception($"Unsupported environment in '{mapPath}' asset bundle, only 1 scene expected");
            }

            var sceneName = Path.GetFileNameWithoutExtension(scenes[0]);

            var loader = SceneManager.LoadSceneAsync(sceneName);
            loader.completed += op =>
            {
                if (op.isDone)
                {
                    mapBundle.Unload(false);

                    try
                    {
                        var prefabs = LoadVehicleBundles(load);

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
    }
}
