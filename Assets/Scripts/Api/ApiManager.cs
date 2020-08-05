/**
 * Copyright (c) 2019-2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

using Simulator.Web;
using System.Net;
using System.Collections.Concurrent;
using Simulator.Controllable;
using Simulator.Network.Core.Connection;
using Simulator.Network.Core.Identification;
using Simulator.Network.Core.Messaging;
using Simulator.Network.Core.Messaging.Data;

namespace Simulator.Api
{
    using Network.Core.Threading;

    public class ApiManager : MonoBehaviour, IMessageSender, IMessageReceiver
    {
        private enum MessageType
        {
            Command = 0,
            Result = 1,
            Error = 2
        }
    
        [NonSerialized]
        public string CurrentScene;

        [NonSerialized]
        public double CurrentTime;

        [NonSerialized]
        public int CurrentFrame;

        [NonSerialized]
        public int FrameLimit;

        [NonSerialized]
        public float TimeScale;

        WebSocketServer Server;
        public static Dictionary<string, ICommand> Commands = new Dictionary<string, ICommand>();

        public Dictionary<string, GameObject> CachedVehicles = new Dictionary<string, GameObject>();

        public Dictionary<string, GameObject> Agents = new Dictionary<string, GameObject>();
        public Dictionary<GameObject, string> AgentUID = new Dictionary<GameObject, string>();

        public Dictionary<string, IControllable> Controllables = new Dictionary<string, IControllable>();
        public Dictionary<IControllable, string> ControllablesUID = new Dictionary<IControllable, string>();
        
        private List<GameObject> AgentFollowingWaypoints = new List<GameObject>();

        // events
        public HashSet<GameObject> Collisions = new HashSet<GameObject>();
        public HashSet<GameObject> Waypoints = new HashSet<GameObject>();
        public HashSet<GameObject> StopLine = new HashSet<GameObject>();
        public HashSet<GameObject> LaneChange = new HashSet<GameObject>();
        public List<JSONObject> Events = new List<JSONObject>();
        
        public string Key { get; } = "ApiManager";

        struct ClientAction
        {
            public ICommand Command;
            public JSONNode Arguments;
        }

        SimulatorClient Client;
        ConcurrentQueue<ClientAction> Actions = new ConcurrentQueue<ClientAction>();
        HashSet<string> IgnoredClients = new HashSet<string>();

        int roadLayer;

        public static ApiManager Instance { get; private set; }
        
        /// <summary>
        /// Locking semaphore that disables executing actions while semaphore is locked
        /// </summary>
        public LockingSemaphore ActionsSemaphore { get; } = new LockingSemaphore();

        static ApiManager()
        {
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (typeof(ICommand).IsAssignableFrom(type) && !type.IsAbstract)
                {
                    var cmd = Activator.CreateInstance(type) as ICommand;
                    Commands.Add(cmd.Name, cmd);
                }
            }
        }

        class SimulatorClient : WebSocketBehavior
        {
            
            protected override void OnOpen()
            {
                lock (Instance)
                {
                    if (Instance.Client != null)
                    {
                        Instance.IgnoredClients.Add(ID);
                        Context.WebSocket.Close();
                        return;
                    }
                    Instance.Client = this;
                }
            }

            protected override void OnClose(CloseEventArgs e)
            {
                lock (Instance)
                {
                    if (Instance.IgnoredClients.Contains(ID))
                    {
                        Instance.IgnoredClients.Remove(ID);
                    }
                    else
                    {
                        Instance.Client = null;
                    }
                }
            }

            protected override void OnMessage(MessageEventArgs e)
            {
                var json = JSONNode.Parse(e.Data);
                var command = json["command"].Value;
                if (Commands.ContainsKey(command))
                {
                    var arguments = json["arguments"];

                    Instance.Actions.Enqueue(new ClientAction
                    {
                        Command = Commands[command],
                        Arguments = arguments,
                    });
                }
                else
                {
                    Instance.SendError($"Ignoring unknown command '{command}'");
                }
            }

            public void SendJson(JSONObject json)
            {
                Send(json.ToString());
            }
        }

        public void SendResult(ICommand source, JSONNode data = null)
        {
            if (Loader.Instance.Network.IsClient)
            {
                if (!(source is IDelegatedCommand)) return;

                var dataString = data?.ToString();
                var message = MessagesPool.Instance.GetMessage(4 + BytesStack.GetMaxByteCount(dataString));
                message.AddressKey = Key;
                message.Content.PushString(dataString);
                message.Content.PushEnum<MessageType>((int)MessageType.Result);
                message.Type = DistributedMessageType.ReliableOrdered;
                BroadcastMessage(message);
                return;
            }

            SendResult(data);
        }


        private void SendResult(JSONNode data = null)
        {
            if (data == null)
            {
                data = JSONNull.CreateOrGet();
            }

            var json = new JSONObject();
            json.Add("result", data);

            lock (Instance)
            {
                if (Client != null)
                {
                    Client.SendJson(json);
                }
            }
        }

        public void SendError(ICommand source, string message)
        {
            if (Loader.Instance.Network.IsClient)
            {
                var distributedMessage = MessagesPool.Instance.GetMessage(4 + BytesStack.GetMaxByteCount(message));
                distributedMessage.AddressKey = Key;
                distributedMessage.Content.PushString(message);
                distributedMessage.Content.PushEnum<MessageType>((int)MessageType.Error);
                distributedMessage.Type = DistributedMessageType.ReliableOrdered;
                BroadcastMessage(distributedMessage);
                return;
            }

            SendError(message);
        }
        
        private void SendError(string message)
        {
            if (Loader.Instance.Network.IsClient)
                return;
            var json = new JSONObject();
            json.Add("error", new JSONString(message));

            lock (Instance)
            {
                Client.SendJson(json);
            }
        }

        void Awake()
        {
            roadLayer = LayerMask.NameToLayer("Default");

            DontDestroyOnLoad(gameObject);
            Instance = this;

            IPAddress address;
            if (Config.ApiHost == "*")
            {
                address = IPAddress.Any;
            }
            else if (Config.ApiHost == "localhost")
            {
                address = IPAddress.Loopback;
            }
            else
            {
                var entries = Dns.GetHostEntry(Config.ApiHost);
                if (entries.AddressList.Length == 0)
                {
                    throw new Exception($"Cannot resolve {Config.ApiHost} hostname");
                }
                address = entries.AddressList[0];
            }

            Server = new WebSocketServer(address, Config.ApiPort);
            Server.AddWebSocketService<SimulatorClient>("/");
            Server.KeepClean = false;
            Server.Start();
            SIM.LogAPI(SIM.API.SimulationCreate);
            Loader.Instance.Network.MessagesManager?.RegisterObject(this);
        }

        void OnDestroy()
        {
            if (Server != null)
            {
                Server.Stop();
                Server = null;
            }

            Instance = null;
            SimulatorManager.SetTimeScale(1.0f);
            SIM.LogAPI(SIM.API.SimulationDestroy);
            SIM.APIOnly = false;
            Loader.Instance.Network.MessagesManager?.UnregisterObject(this);
        }

        public void Reset()
        {
            SimulatorManager.Instance.AnalysisManager.AnalysisSave();

            Events.Clear();
            Agents.Clear();
            AgentUID.Clear();
            if (SimulatorManager.InstanceAvailable)
                SimulatorManager.Instance.Sensors.ClearSensorsRegistry();
            Controllables.Clear();
            ControllablesUID.Clear();
            AgentFollowingWaypoints.Clear();

            Collisions.Clear();
            Waypoints.Clear();
            StopLine.Clear();
            LaneChange.Clear();

            StopAllCoroutines();

            var sim = SimulatorManager.Instance;
            sim.AgentManager.Reset();
            sim.NPCManager.Reset();
            sim.PedestrianManager.Reset();
            sim.EnvironmentEffectsManager.Reset();
            sim.ControllableManager.Reset();
            sim.MapManager.Reset();
            sim.CameraManager.Reset();
            sim.UIManager.Reset();

            sim.CurrentFrame = 0;
            sim.GTIDs = 0;
            sim.SignalIDs = 0;

            FrameLimit = 0;
            CurrentTime = 0.0;
            CurrentFrame = 0;

            SimulatorManager.SetTimeScale(0.0f);

            Resources.UnloadUnusedAssets();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.UnloadUnusedAssetsImmediate();
#endif
        }

        public void AddCollision(GameObject obj, GameObject other, Collision collision = null)
        {
            if (!Collisions.Contains(obj) || (collision != null && collision.gameObject.layer == roadLayer))
            {
                return;
            }

            string uid1, uid2;
            if (AgentUID.TryGetValue(obj, out uid1))
            {
                var j = new JSONObject();
                j.Add("type", new JSONString("collision"));
                j.Add("agent", new JSONString(uid1));
                if (AgentUID.TryGetValue(other, out uid2))
                {
                    j.Add("other", new JSONString(uid2));
                }
                else
                {
                    j.Add("other", JSONNull.CreateOrGet());
                }
                if (collision != null)
                {
                    j.Add("contact", collision.contacts[0].point);
                }
                else
                {
                    j.Add("contact", JSONNull.CreateOrGet());
                }

                Events.Add(j);
            }
        }

        public void AddCustom(GameObject obj, string kind, JSONObject context)
        {
            string uid;
            if (AgentUID.TryGetValue(obj, out uid))
            {
                var j = new JSONObject();
                j.Add("agent", uid);
                j.Add("type", "custom");
                j.Add("kind", kind);
                j.Add("context", context);
                Events.Add(j);
            }
        }

        public void AddWaypointReached(GameObject obj, int index)
        {
            if (!Waypoints.Contains(obj))
            {
                return;
            }

            string uid;
            if (AgentUID.TryGetValue(obj, out uid))
            {
                var j = new JSONObject();
                j.Add("type", new JSONString("waypoint_reached"));
                j.Add("agent", new JSONString(uid));
                j.Add("index", new JSONNumber(index));

                Events.Add(j);
            }
        }

        public void RegisterAgentWithWaypoints(GameObject obj)
        {
            if (AgentFollowingWaypoints.Contains(obj))
                return;
            AgentFollowingWaypoints.Add(obj);
        }

        public void AgentTraversedWaypoints(GameObject obj)
        {
            if (!AgentFollowingWaypoints.Contains(obj))
                return;
            AgentFollowingWaypoints.Remove(obj);
            if (AgentFollowingWaypoints.Count == 0)
            {
                var j = new JSONObject();
                j.Add("type", new JSONString("agents_traversed_waypoints"));

                Events.Add(j);
            }
        }

        public void AddStopLine(GameObject obj)
        {
            if (!StopLine.Contains(obj))
            {
                return;
            }

            string uid;
            if (AgentUID.TryGetValue(obj, out uid))
            {
                var j = new JSONObject();
                j.Add("type", new JSONString("stop_line"));
                j.Add("agent", new JSONString(uid));

                Events.Add(j);
            }
        }

        public void AddLaneChange(GameObject obj)
        {
            if (!LaneChange.Contains(obj))
            {
                return;
            }

            string uid;
            if (AgentUID.TryGetValue(obj, out uid))
            {
                var j = new JSONObject();
                j.Add("type", new JSONString("lane_change"));
                j.Add("agent", new JSONString(uid));

                Events.Add(j);
            }
        }

        void Update()
        {
            while (ActionsSemaphore.IsUnlocked && Actions.TryDequeue(out var action))
            {
                try
                {
                    var isMasterSimulation = Loader.Instance.Network.IsMaster;
                    if (action.Command is IDelegatedCommand delegatedCommand && isMasterSimulation)
                    {
                        var endpoint = delegatedCommand.TargetNodeEndPoint(action.Arguments);
                        //If there is a connection to this endpoint forward the command, otherwise execute it locally
                        if (Loader.Instance.Network.Master.IsConnectedToClient(endpoint))
                        {
                            var message = MessagesPool.Instance.GetMessage(
                                4 + 4 *
                                (2 + action.Arguments.Count + action.Command.Name.Length));
                            message.AddressKey = Key;
                            message.Content.PushString(action.Arguments.ToString());
                            message.Content.PushString(action.Command.Name);
                            message.Content.PushEnum<MessageType>((int) MessageType.Command);
                            UnicastMessage(endpoint, message);
                        }
                        else 
                            action.Command.Execute(action.Arguments);
                    }
                    else
                    {
                        action.Command.Execute(action.Arguments);
                        if (action.Command is IDistributedCommand && isMasterSimulation)
                        {
                            var message = MessagesPool.Instance.GetMessage(
                                BytesStack.GetMaxByteCount(action.Arguments) +
                                BytesStack.GetMaxByteCount(action.Command.Name));
                            message.AddressKey = Key;
                            message.Content.PushString(action.Arguments.ToString());
                            message.Content.PushString(action.Command.Name);
                            message.Content.PushEnum<MessageType>((int) MessageType.Command);
                            message.Type = DistributedMessageType.ReliableOrdered;
                            BroadcastMessage(message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);

                    var st = new StackTrace(ex, true);
                    StackFrame frame = null;
                    int i = 0;
                    while (i < st.FrameCount)
                    {
                        frame = st.GetFrame(i++);
                        if (frame.GetFileLineNumber() != 0)
                        {
                            break;
                        }
                        frame = null;
                    }

                    if (frame == null)
                    {
                        SendError(ex.Message);
                    }
                    else
                    {
                        var fname = frame.GetFileName();
                        var line = frame.GetFileLineNumber();
                        SendError($"{ex.Message} at {fname}@{line}");
                    }
                    // If an exception was thrown from an async api handler, make sure
                    // we unlock the semaphore, if not done so already
                    if(ActionsSemaphore.IsLocked) ActionsSemaphore.Unlock();
                }
            }
        }

        void FixedUpdate()
        {
            if (Events.Count != 0)
            {
                var events = new JSONArray();
                for (int i = 0; i < Events.Count; i++)
                {
                    events[i] = Events[i];
                }
                Events.Clear();

                var msg = new JSONObject();
                msg["events"] = events;

                SendResult(msg);

                SimulatorManager.SetTimeScale(0.0f);
                return;
            }
            
            if (ActionsSemaphore.IsUnlocked && Time.timeScale != 0.0f)
            {
                if (FrameLimit != 0 && CurrentFrame >= FrameLimit)
                {
                    SimulatorManager.SetTimeScale(0.0f);
                    SendResult();
                }
                else
                {
                    CurrentTime += Time.fixedDeltaTime;
                    CurrentFrame += 1;

                    if (!CurrentScene.IsNullOrEmpty())
                    {
                        SimulatorManager.Instance.PhysicsUpdate();
                    }
                }
            }
        }
            
        public void ReceiveMessage(IPeerManager sender, DistributedMessage distributedMessage)
        {
            var messageType = distributedMessage.Content.PopEnum<MessageType>();
            switch (messageType)
            {
                case MessageType.Command:
                    var command = distributedMessage.Content.PopString();
                    var arguments = JSONNode.Parse(distributedMessage.Content.PopString());
                    Actions.Enqueue(new ClientAction
                    {
                        Command = Commands[command],
                        Arguments = arguments,
                    });
                    break;
                case MessageType.Result:
                    var resultValue = distributedMessage.Content.PopString();
                    if (resultValue==null)
                        SendResult();
                    else
                    {
                        var result = JSONNode.Parse(resultValue);
                        SendResult(result);
                    }
                    break;
                case MessageType.Error:
                    SendError(distributedMessage.Content.PopString());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void UnicastMessage(IPEndPoint endPoint, DistributedMessage distributedMessage)
        {
            Loader.Instance.Network.MessagesManager?.UnicastMessage(endPoint, distributedMessage);
        }

        public void BroadcastMessage(DistributedMessage distributedMessage)
        {
            Loader.Instance.Network.MessagesManager?.BroadcastMessage(distributedMessage);
        }

        public void UnicastInitialMessages(IPEndPoint endPoint)
        {
            //TODO support reconnection
        }
    }
}
