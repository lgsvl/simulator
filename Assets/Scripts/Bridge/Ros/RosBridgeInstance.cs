/**
 * Copyright(c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
using WebSocketSharp;
using System.CodeDom;

namespace Simulator.Bridge.Ros
{
    public class RosBridgeInstance : IBridgeInstance
    {
        const int DefaultPort = 9090;

        static readonly TimeSpan Timeout = TimeSpan.FromSeconds(1.0);
        WebSocket Socket;

        List<string> Setup = new List<string>();

        Dictionary<string, Action<JSONNode>> Subscribers = new Dictionary<string, Action<JSONNode>>();
        Dictionary<string, Action<JSONNode, Action<object>>> Services = new Dictionary<string, Action<JSONNode, Action<object>>>();

        public Status Status { get; private set; } = Status.Disconnected;

        static RosBridgeInstance()
        {
            // increase send buffer size for WebSocket C# library
            // FragmentLength is internal filed, that's why reflection is used here
            var f = typeof(WebSocket).GetField("FragmentLength", BindingFlags.Static | BindingFlags.NonPublic);
            f.SetValue(null, 65536 - 8);
        }

        public RosBridgeInstance()
        {
            Status = Status.Disconnected;
        }

        public void Connect(string connection)
        {
            var split = connection.Split(new[] { ':' }, 2);

            var address = split[0];
            var port = split.Length == 1 ? DefaultPort : int.Parse(split[1]);

            try
            {
                Socket = new WebSocket($"ws://{address}:{port}");
                Socket.WaitTime = Timeout;
                Socket.OnMessage += OnMessage;
                Socket.OnOpen += OnOpen;
                Socket.OnError += OnError;
                Socket.OnClose += OnClose;
                Status = Status.Connecting;
                Socket.ConnectAsync();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public void Disconnect()
        {
            if (Socket != null)
            {
                if (Socket.ReadyState == WebSocketState.Open)
                {
                    Status = Status.Disconnecting;
                    Socket.CloseAsync();
                }
            }
        }

        public void SendAsync(string data, Action completed)
        {
            if (completed == null)
            {
                Socket.SendAsync(data, null);
            }
            else
            {
                Socket.SendAsync(data, ok => completed());
            }
        }

        public void AddSubscriber<BridgeType>(string topic, Action<JSONNode> callback)
        {
            var messageType = RosUtils.GetMessageType<BridgeType>();

            var j = new JSONObject();
            j.Add("op", "subscribe");
            j.Add("topic", topic);
            j.Add("type", messageType);

            var data = j.ToString();
            lock (Setup)
            {
                if (Status == Status.Connected)
                {
                    Socket.SendAsync(data, null);
                }
                Setup.Add(data);
            }

            lock (Subscribers)
            {
                Subscribers.Add(topic, callback);
            }
        }

        public void AddPublisher<BridgeType>(string topic)
        {
            var messageType = RosUtils.GetMessageType<BridgeType>();

            var j = new JSONObject();
            j.Add("op", "advertise");
            j.Add("topic", topic);
            j.Add("type", messageType);

            var data = j.ToString();
            lock (Setup)
            {
                if (Status == Status.Connected)
                {
                    Socket.SendAsync(data, null);
                }
                Setup.Add(data);
            }
        }

        public void AddService<ArgBridgeType>(string topic, Action<JSONNode, Action<object>> callback)
        {
            var argType = RosUtils.GetMessageType<ArgBridgeType>();

            if (Services.ContainsKey(topic))
            {
                throw new InvalidOperationException($"Service on {topic} topic is already registered");
            }

            var j = new JSONObject();
            j.Add("op", "advertise_service");
            j.Add("type", argType);
            j.Add("service", topic);

            var data = j.ToString();
            lock (Setup)
            {
                if (Status == Status.Connected)
                {
                    Socket.SendAsync(data, null);
                }
                Setup.Add(data);
            }

            lock (Services)
            {
                Services.Add(topic, callback);
            }
        }

        void OnClose(object sender, CloseEventArgs args)
        {
            Status = Status.Disconnected;
            Socket = null;
        }

        void OnError(object sender, ErrorEventArgs args)
        {
            Debug.LogError(args.Message);

            if (args.Exception != null)
            {
                Debug.LogException(args.Exception);
            }
        }

        void OnOpen(object sender, EventArgs args)
        {
            lock (Setup)
            {
                Setup.ForEach(s => Socket.SendAsync(s, null));
                Status = Status.Connected;
            }
        }

        void OnMessage(object sender, MessageEventArgs args)
        {
            var json = JSONNode.Parse(args.Data);
            string op = json["op"];

            if (op == "publish")
            {
                string topic = json["topic"];

                Action<JSONNode> callback;

                lock (Subscribers)
                {
                    Subscribers.TryGetValue(topic, out callback);
                }

                if (callback == null)
                {
                    Debug.LogWarning($"Received message on '{topic}' topic which nobody subscribed");
                }
                else
                {
                    callback(json["msg"]);
                }
            }
            else if (op == "call_service")
            {
                var topic = json["service"];
                var id = json["id"];

                Action<JSONNode, Action<object>> callback;

                lock (Services)
                {
                    Services.TryGetValue(topic, out callback);
                }

                if (callback == null)
                {
                    Debug.LogWarning($"Received service message on '{topic}' topic which nobody serves");
                }
                else
                {
                    callback(json["args"], res =>
                    {
                        var sb = new StringBuilder(1024);
                        sb.Append('{');
                        {
                            sb.Append("\"op\":\"service_response\",");

                            sb.Append("\"id\":");
                            sb.Append(id.ToString());
                            sb.Append(",");

                            sb.Append("\"service\":\"");
                            sb.Append(topic.Value);
                            sb.Append("\",");

                            sb.Append("\"values\":");
                            try
                            {
                                RosSerialization.Serialize(res, sb);
                            }
                            catch (Exception ex)
                            {
                                // explicit logging of exception because this method is often called
                                // from background threads for which Unity does not log exceptions
                                Debug.LogException(ex);
                                throw;
                            }
                            sb.Append(",");

                            sb.Append("\"result\":true");
                        }
                        sb.Append('}');

                        var data = sb.ToString();
                        Socket.SendAsync(data, null);
                    });
                }
            }
            else if (op == "set_level")
            {
                // ignore these
            }
            else
            {
                Debug.LogWarning($"Unknown operation from rosbridge: {op}");
            }
        }
    }
}
