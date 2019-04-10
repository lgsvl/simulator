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

namespace Api
{
    public class ApiManager : MonoBehaviour
    {
        public ushort Port = 8181;
        public static ApiManager Instance { get; private set; }

        [HideInInspector]
        public string CurrentScene;

        [HideInInspector]
        public double CurrentTime;

        [HideInInspector]
        public int CurrentFrame;

        [HideInInspector]
        public double TimeLimit;

        [HideInInspector]
        public int FrameLimit;

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

        int groundLayer;
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

                var agentManager = ROSAgentManager.Instance;
                agentManager.currentMode = StartModeTypes.API;
                agentManager.Clear();
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
            groundLayer = LayerMask.NameToLayer("Ground And Road"); 

            if (Instance != null)
            {
                DestroyImmediate(this);
                return;
            }

            Server = new WebSocketServer(Port);
            Server.AddWebSocketService<SimulatorClient>("/");
            Server.Start();

            DontDestroyOnLoad(gameObject);
            Instance = this;
        }

        void OnDestroy()
        {
            if (Server != null)
            {
                Server.Stop();
                Server = null;
            }
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

            EnvironmentEffectsManager.Instance?.Reset();

            TimeLimit = 0.0;
            FrameLimit = 0;
            Time.timeScale = 0.0f;
            CurrentTime = 0.0;
            CurrentFrame = 0;
        }

        public void AddCollision(GameObject obj, Collision collision)
        {
            if (!Collisions.Contains(obj) || (collision.gameObject.layer == groundLayer))
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
                        var st = new StackTrace(ex, true);
                        var frame = st.GetFrame(0);
                        var fname = frame.GetFileName();
                        var line = frame.GetFileLineNumber();

                        SendError($"{ex.Message} at {fname}@{line}");
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

                    Time.timeScale = 0.0f;
                    return;
                }
            }

            if (Time.timeScale != 0.0f)
            {
                CurrentTime += Time.timeScale * Time.deltaTime;
                CurrentFrame += 1;

                if ((TimeLimit != 0.0 && CurrentTime >= TimeLimit) ||
                    (FrameLimit != 0 && CurrentFrame >= FrameLimit))
                {
                    Time.timeScale = 0.0f;

                    SendResult();
                }
            }
        }
    }
}
