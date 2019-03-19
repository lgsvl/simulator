/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using System;
using System.Collections.Generic;
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
        public Dictionary<string, Component> Sensors = new Dictionary<string, Component>();
        public Dictionary<Component, string> SensorUID = new Dictionary<Component, string>();

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

        class SimulatorService : WebSocketBehavior
        {
            protected override void OnOpen()
            {
                lock (Instance.Clients)
                {
                    Instance.Clients.Add(ID, this);
                }
            }

            protected override void OnClose(CloseEventArgs e)
            {
                lock (Instance.Clients)
                {
                    Instance.Clients.Remove(ID);
                }
            }
            
            protected override void OnMessage(MessageEventArgs e)
            {
                var json = JSONNode.Parse(e.Data);
                var command = json["command"];
                if (Commands.ContainsKey(command))
                {
                    var arguments = json["arguments"];

                    lock (Instance.Actions)
                    {
                        Instance.Actions.Enqueue(new ClientAction
                        {
                            Client = ID,
                            Command = Commands[command],
                            Arguments = arguments,
                        });
                    }
                }
                else
                {
                    var error = new JSONObject();
                    error.Add("error", $"Ignoring unknown command '{command}'");
                    Send(error.ToString());
                }
            }

            public void SendJson(JSONObject json)
            {
                Send(json.ToString());
            }
        }

        struct ClientAction
        {
            public string Client;
            public ICommand Command;
            public JSONNode Arguments;
        }

        Dictionary<string, SimulatorService> Clients = new Dictionary<string, SimulatorService>();
        Queue<ClientAction> Actions = new Queue<ClientAction>();

        public void SendResult(string client, JSONNode data)
        {
            lock (Clients)
            {
                if (Clients.ContainsKey(client))
                {
                    var json = new JSONObject();
                    json.Add("result", data);
                    Clients[client].SendJson(json);
                }
            }
        }

        public void SendError(string client, string message)
        {
            lock (Clients)
            {
                if (Clients.ContainsKey(client))
                {
                    var json = new JSONObject();
                    json.Add("error", new JSONString(message));
                    Clients[client].SendJson(json);
                }
            }
        }

        void Awake()
        {
            if (Instance != null)
            {
                DestroyImmediate(this);
                return;
            }

            var agentManager = ROSAgentManager.Instance;
            agentManager.currentMode = StartModeTypes.API;
            agentManager.Clear();

            Server = new WebSocketServer(Port);
            Server.AddWebSocketService<SimulatorService>("/");
            Server.Start();

            DontDestroyOnLoad(this);
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

        void Update()
        {
            lock (Actions)
            {
                while (Actions.Count != 0)
                {
                    var action = Actions.Dequeue();
                    try
                    {
                        action.Command.Execute(action.Client, action.Arguments);
                    }
                    catch (Exception ex)
                    {
                        SendError(action.Client, ex.Message);
                    }
                }
            }

            if (Time.timeScale != 0.0f)
            {
                CurrentTime += Time.deltaTime;
                CurrentFrame += 1;

                if ((TimeLimit != 0.0f && CurrentTime >= TimeLimit) ||
                    (FrameLimit != 0 && CurrentFrame >= FrameLimit))
                {
                    Time.timeScale = 0.0f;

                    foreach (var client in Clients)
                    {
                        SendResult(client.Key, JSONNull.CreateOrGet());
                    }

                }
            }
        }
    }
}
