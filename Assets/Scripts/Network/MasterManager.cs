/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Net;
using System.Linq;
using System.Net.Sockets;
using System.Collections.Concurrent;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;
using System.Collections.Generic;
using System.Globalization;

namespace Simulator.Network
{
    public class MasterManager : MonoBehaviour, INetEventListener
    {
        public static MasterManager Instance { get; private set; }

        enum State
        {
            Initial,
            Connecting, // waiting from "init" command
            Connected,  // init" command is received
            Loading,    // client is loading bundles
            Ready,      // client finished all bundle loading
            Running,    // simulation is running
        }

        class Client
        {
            public NetPeer Peer;
            public State State;
        }

        State MasterState = State.Initial;

        NetManager Manager;
        NetPacketProcessor Packets = new NetPacketProcessor();

        List<Client> Clients = new List<Client>();
        ConcurrentQueue<Action> Actions = new ConcurrentQueue<Action>();

        [NonSerialized]
        public SimulationConfig Simulation;

        void Awake()
        {
            Instance = this;

            Packets.RegisterNestedType(SerializationHelpers.SerializeLoadAgent, SerializationHelpers.DeserializeLoadAgent);
            Packets.SubscribeReusable<Commands.Info, NetPeer>(OnInfoCommand);
            Packets.SubscribeReusable<Commands.LoadResult, NetPeer>(OnLoadResultCommand);

            Manager = new NetManager(this);
            Manager.UpdateTime = 1;
            Manager.Start();
        }

        public void AddClients(string[] addresses)
        {
            Debug.Assert(MasterState == State.Initial);
            foreach (var address in addresses)
            {
                var peer = Manager.Connect(address, Constants.Port, Constants.ConnectionKey);
                Clients.Add(new Client() { Peer = peer, State = State.Initial });
            }
            MasterState = State.Connecting;
        }

        void Update()
        {
            Manager.PollEvents();

            while (Actions.TryDequeue(out var action))
            {
                action();
            }
        }

        void OnApplicationQuit()
        {
            Manager.Stop();
        }

        void OnDestroy()
        {
            Manager.Stop();
            Instance = null;
        }

        public void OnPeerConnected(NetPeer peer)
        {
            Debug.Assert(MasterState == State.Connecting);
            var client = Clients.Find(c => c.Peer == peer);
            Debug.Assert(client != null);
            Debug.Assert(client.State == State.Initial);

            client.State = State.Connecting;
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            // TODO: handle disconnects in various stages of simulation
            Clients.RemoveAll(c => c.Peer == peer);
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            Debug.Log($"Error {socketError} for {endPoint} endpoint");
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
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
        }

        public void SendEnvironmentState(Commands.EnvironmentState state)
        {
            Clients.ForEach(c =>
            {
                Packets.Send(c.Peer, state, DeliveryMethod.ReliableSequenced);
            });
        }

        public void OnInfoCommand(Commands.Info info, NetPeer peer)
        {
            Debug.Assert(MasterState == State.Connecting);
            var client = Clients.Find(c => c.Peer == peer);
            Debug.Assert(client != null);
            Debug.Assert(client.State == State.Connecting);

            Debug.Log($"NET: Client connected from {peer.EndPoint}");

            Debug.Log($"NET: Client version = {info.Version}");
            Debug.Log($"NET: Client Unity version = {info.UnityVersion}");
            Debug.Log($"NET: Client OS = {info.OperatingSystem}");

            client.State = State.Connected;

            var sim = Loader.Instance.SimConfig;

            if (!sim.ApiOnly)
            {
                if (Clients.All(c => c.State == State.Connected))
                {
                    var load = new Commands.Load()
                    {
                        Name = sim.Name,
                        MapName = sim.MapName,
                        MapUrl = sim.MapUrl,
                        ApiOnly = sim.ApiOnly,
                        Headless = sim.Headless,
                        Interactive = sim.Interactive,
                        TimeOfDay = sim.TimeOfDay.ToString("o", CultureInfo.InvariantCulture),
                        Rain = sim.Rain,
                        Fog = sim.Fog,
                        Wetness = sim.Wetness,
                        Cloudiness = sim.Cloudiness,
                        Agents = Simulation.Agents.Select(a => new Commands.LoadAgent()
                        {
                            Name = a.Name,
                            Url = a.Url,
                            Bridge = a.Bridge == null ? string.Empty : a.Bridge.Name,
                            Connection = a.Connection,
                            Sensors = a.Sensors,
                        }).ToArray(),
                        UseTraffic = Simulation.UseTraffic,
                        UsePedestrians = Simulation.UsePedestrians,
                    };

                    foreach (var c in Clients)
                    {
                        Packets.Send(c.Peer, load, DeliveryMethod.ReliableOrdered);
                        c.State = State.Loading;
                    }

                    MasterState = State.Loading;
                }
            }
        }

        public void OnLoadResultCommand(Commands.LoadResult res, NetPeer peer)
        {
            Debug.Assert(MasterState == State.Loading);
            var client = Clients.Find(c => c.Peer == peer);
            Debug.Assert(client != null);
            Debug.Assert(client.State == State.Loading);

            if (res.Success)
            {
                Debug.Log("Client loaded");
            }
            else
            {
                // TODO: stop simulation / cancel loading for other clients
                Debug.LogError($"Client failed to load: {res.ErrorMessage}");

                // TODO: reset all other clients

                Debug.Log($"Failed to start '{Simulation.Name}' simulation");

                // TODO: update simulation status in DB
                // simulation.Status = "Invalid";
                // db.Update(simulation);

                // NotificationManager.SendNotification("simulation", SimulationResponse.Create(simulation), simulation.Owner);

                Loader.ResetLoaderScene();

                Clients.Clear();
                return;
            }

            client.State = State.Ready;

            if (!Loader.Instance.SimConfig.ApiOnly)
            {
                if (Clients.All(c => c.State == State.Ready))
                {
                    Debug.Log("All clients are ready. Resuming time.");

                    var run = new Commands.Run();
                    foreach (var c in Clients)
                    {
                        Packets.Send(c.Peer, run, DeliveryMethod.ReliableOrdered);
                        c.State = State.Running;
                    }

                    MasterState = State.Running;

                    SimulatorManager.SetTimeScale(1.0f);
                }
            }
        }
    }
}
