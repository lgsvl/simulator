/**
 * Copyright (c) 2019 LG Electronics, Inc.
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

namespace Simulator.Api
{
    public class ApiManager : MonoBehaviour
    {
        [NonSerialized]
        public string CurrentScene;

        [NonSerialized]
        public double CurrentTime;

        [NonSerialized]
        public int CurrentFrame;

        [NonSerialized]
        public double TimeLimit;

        [NonSerialized]
        public float TimeScale;

        [NonSerialized]
        public float TargetFrameRate;

        [NonSerialized]
        public bool Realtime = true;

        WebSocketServer Server;
        static Dictionary<string, ICommand> Commands = new Dictionary<string, ICommand>();

        public Dictionary<string, GameObject> Agents = new Dictionary<string, GameObject>();
        public Dictionary<GameObject, string> AgentUID = new Dictionary<GameObject, string>();

        public Dictionary<string, Component> Sensors = new Dictionary<string, Component>();
        public Dictionary<Component, string> SensorUID = new Dictionary<Component, string>();

        // events
        public HashSet<GameObject> Collisions = new HashSet<GameObject>();
        public HashSet<GameObject> Waypoints = new HashSet<GameObject>();
        public HashSet<GameObject> StopLine = new HashSet<GameObject>();
        public HashSet<GameObject> LaneChange = new HashSet<GameObject>();
        public List<JSONObject> Events = new List<JSONObject>();

        struct ClientAction
        {
            public ICommand Command;
            public JSONNode Arguments;
        }

        SimulatorClient Client;
        Queue<ClientAction> Actions = new Queue<ClientAction>();
        HashSet<string> IgnoredClients = new HashSet<string>();

        int roadLayer;

        public static ApiManager Instance { get; private set; }

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

                SimulatorManager.Instance.AgentManager.ClearActiveAgents();
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

                    lock (Instance.Actions)
                    {
                        Instance.Actions.Enqueue(new ClientAction
                        {
                            Command = Commands[command],
                            Arguments = arguments,
                        });
                    }
                }
                else
                {
                    lock (Instance)
                    {
                        Instance.SendError($"Ignoring unknown command '{command}'");
                    }
                }
            }

            public void SendJson(JSONObject json)
            {
                Send(json.ToString());
            }
        }

        public void SendResult(JSONNode data = null)
        {
            if (data == null)
            {
                data = JSONNull.CreateOrGet();
            }

            lock (Instance)
            {
                var json = new JSONObject();
                json.Add("result", data);
                Client.SendJson(json);
            }
        }

        public void SendError(string message)
        {
            lock (Instance)
            {
                var json = new JSONObject();
                json.Add("error", new JSONString(message));
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
            Server.Start();
            SIM.LogAPI(SIM.API.SimulationCreate);
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
        }

        public void Reset()
        {
            lock (Events)
            {
                Events.Clear();
            }

            Agents.Clear();
            AgentUID.Clear();
            Sensors.Clear();
            SensorUID.Clear();

            Collisions.Clear();
            Waypoints.Clear();
            StopLine.Clear();
            LaneChange.Clear();

            SimulatorManager.Instance.EnvironmentEffectsManager.Reset();

            TimeLimit = 0.0;
            CurrentTime = 0.0;
            CurrentFrame = 0;

            SimulatorManager.SetTimeScale(0.0f);
        }

        public void AddCollision(GameObject obj, Collision collision)
        {
            if (!Collisions.Contains(obj) || (collision.gameObject.layer == roadLayer))
            {
                return;
            }

            string uid1, uid2;
            if (AgentUID.TryGetValue(obj, out uid1))
            {
                var j = new JSONObject();
                j.Add("type", new JSONString("collision"));
                j.Add("agent", new JSONString(uid1));
                if (AgentUID.TryGetValue(collision.gameObject, out uid2))
                {
                    j.Add("other", new JSONString(uid2));
                }
                else
                {
                    j.Add("other", JSONNull.CreateOrGet());
                }
                j.Add("contact", collision.contacts[0].point);

                lock (Events)
                {
                    Events.Add(j);
                }
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

                lock (Events)
                {
                    Events.Add(j);
                }
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

                lock (Events)
                {
                    Events.Add(j);
                }
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

                lock (Events)
                {
                    Events.Add(j);
                }
            }
        }

        void Update()
        {
            lock (Actions)
            {
                while (Actions.Count != 0)
                {
                    var action = Actions.Dequeue();
                    try
                    {
                        action.Command.Execute(action.Arguments);
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
                    }
                }
            }

            lock (Events)
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
            }

            if (Time.timeScale != 0.0f)
            {
                CurrentTime += Time.deltaTime;
                CurrentFrame += 1;

                if (TimeLimit != 0.0 && CurrentTime >= TimeLimit)
                {
                    SimulatorManager.SetTimeScale(0.0f);
                    SendResult();
                }
            }
        }
    }
}
