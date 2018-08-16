/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using System;
using System.Collections.Generic;
using System.Text;
using WebSocketSharp;
using SimpleJSON;
using System.Reflection;
using System.Collections;

namespace Ros
{
    public enum Status
    {
        Disconnected,
        Connecting,
        Connected,
        Disconnecting,
    }

    public class Bridge
    {
        public static bool canConnect = false;

        WebSocket Socket;

        struct Subscription
        {
            public string Topic;
            public Delegate Callback;
        }

        public struct Topic
        {
            public String Name;
            public String Type;
        }

        public List<Topic> TopicSubscriptions = new List<Topic>();
        public List<Topic> TopicPublishers = new List<Topic>();

        List<IRosClient> Publishers = new List<IRosClient>();
        List<Subscription> Subscriptions = new List<Subscription>();
        Queue<Action> QueuedMessages = new Queue<Action>();
        Queue<String> QueuedSends = new Queue<string>();

        public int Version { get; private set; }
        public Status Status { get; private set; }

        public Bridge()
        {
            Status = Status.Disconnected;
        }

        public void Connect(string address, int port, int version)
        {
            Version = version;
            try
            {
                Socket = new WebSocket(String.Format("ws://{0}:{1}", address, port));
                Socket.WaitTime = TimeSpan.FromSeconds(1.0);
                Socket.OnMessage += OnMessage;
                Socket.OnOpen += OnOpen;
                Socket.OnError += OnError;
                Socket.OnClose += OnClose;
                Status = Status.Connecting;
                Socket.ConnectAsync();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e);
            }
        }

        public void Close()
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

        void OnClose(object sender, CloseEventArgs args)
        {
            if (!args.WasClean)
            {
                UnityEngine.Debug.LogError(args.Reason);
            }
            Subscriptions.Clear();
            QueuedMessages.Clear();
            QueuedSends.Clear();
            TopicSubscriptions.Clear();
            TopicPublishers.Clear();
            Status = Status.Disconnected;
        }

        void OnError(object sender, ErrorEventArgs args)
        {
            UnityEngine.Debug.LogError(args.Message);

            if (args.Exception != null)
            {
                UnityEngine.Debug.LogError(args.Exception.ToString());
            }
        }

        void OnOpen(object sender, EventArgs args)
        {
            Status = Status.Connected;
            foreach (var publisher in Publishers)
            {
                publisher.OnRosConnected();
            }
        }

        void OnMessage(object sender, MessageEventArgs args)
        {
            var json = JSONNode.Parse(args.Data);
            string op = json["op"];

            if (op == "publish")
            {
                string topic = json["topic"];

                object data = null;

                foreach (var sub in Subscriptions)
                {
                    if (sub.Topic == topic)
                    {
                        if (data == null)
                        {
                            var msg = json["msg"];
                            data = Unserialize(Version, msg, sub.Callback.Method.GetParameters()[0].ParameterType);
                        }
                        lock (QueuedMessages)
                        {
                            QueuedMessages.Enqueue(() => sub.Callback.DynamicInvoke(data));
                        }
                    }
                }
            }
            else if (op == "set_level")
            {
                // ignore these
            }
            else
            {
                UnityEngine.Debug.Log(String.Format("Unknown RosBridge op {0}", op));
            }
        }

        public void AddPublisher(IRosClient publisher)
        {
            if (Status == Status.Connected)
            {
                publisher.OnRosConnected();
            }
            Publishers.Add(publisher);
        }

        public void AddPublisher<T>(string topic)
        {
            if (Socket.ReadyState != WebSocketState.Open)
            {
                throw new InvalidOperationException("socket not open");
            }

            var type = GetMessageType<T>();

            var sb = new StringBuilder(256);
            sb.Append('{');
            {
                sb.Append("\"op\":\"advertise\",");

                sb.Append("\"topic\":\"");
                sb.Append(topic);
                sb.Append("\",");

                sb.Append("\"type\":\"");
                sb.Append(type);
                sb.Append("\"");
            }
            sb.Append('}');

            TopicPublishers.Add(new Topic()
            {
                Name = topic,
                Type = type,
            });

            //UnityEngine.Debug.Log("Adding publisher " + sb.ToString());
            Socket.SendAsync(sb.ToString(), ok => { });
        }

        public void Publish<T>(string topic, T message, Action completed = null)
        {
            var sb = new StringBuilder(128);
            sb.Append('{');
            {
                sb.Append("\"op\":\"publish\",");

                sb.Append("\"topic\":\"");
                sb.Append(topic);
                sb.Append("\",");

                sb.Append("\"msg\":");
                Serialize(Version, sb, typeof(T), message);
            }
            sb.Append('}');

            var s = sb.ToString();
            //UnityEngine.Debug.Log("Publishing " + s.Substring(0, s.Length > 200 ? 200 : s.Length));

            if (completed == null)
            {
                Socket.SendAsync(s, ok => { });
            }
            else
            {
                Socket.SendAsync(s, ok => completed());
            }
        }

        public void Subscribe<T>(string topic, Action<T> callback)
        {
            if (Socket.ReadyState != WebSocketState.Open)
            {
                throw new InvalidOperationException("socket not open");
            }

            var type = GetMessageType<T>();

            var sb = new StringBuilder(256);
            sb.Append('{');
            {
                sb.Append("\"op\":\"subscribe\",");

                sb.Append("\"topic\":\"");
                sb.Append(topic);
                sb.Append("\",");

                sb.Append("\"type\":\"");
                sb.Append(type);
                sb.Append("\"");
            }
            sb.Append('}');

            Subscriptions.Add(new Subscription()
            {
                Topic = topic,
                Callback = callback,
            });

            TopicSubscriptions.Add(new Topic()
            {
                Name = topic,
                Type = type,
            });

            //UnityEngine.Debug.Log("Adding subscriber " + sb.ToString());
            Socket.SendAsync(sb.ToString(), ok => { });
        }

        public void Update()
        {
            lock (QueuedMessages)
            {
                while (QueuedMessages.Count > 0)
                {
                    QueuedMessages.Dequeue()();
                }
            }
        }

        static readonly Dictionary<Type, string> BuiltinMessageTypes = new Dictionary<Type, string> {
            { typeof(bool), "std_msgs/Bool" },
            { typeof(sbyte), "std_msgs/Int8" },
            { typeof(short), "std_msgs/Int16" },
            { typeof(int), "std_msgs/Int32" },
            { typeof(long), "std_msgs/Int64" },
            { typeof(byte), "std_msgs/UInt8" },
            { typeof(ushort), "std_msgs/UInt16" },
            { typeof(uint), "std_msgs/UInt32" },
            { typeof(ulong), "std_msgs/UInt64" },
            { typeof(float), "std_msgs/Float32" },
            { typeof(double), "std_msgs/Float64" },
            { typeof(string), "std_msgs/String" },
        };

        string GetMessageType<T>()
        {
            string type;
            if (BuiltinMessageTypes.TryGetValue(typeof(T), out type))
            {
                return type;
            }

            object[] attributes = typeof(T).GetCustomAttributes(typeof(MessageTypeAttribute), false);
            if (attributes == null || attributes.Length == 0)
            {
                throw new Exception(String.Format("Type {0} does not have {1} attribute", typeof(T).Name, typeof(MessageTypeAttribute).Name));
            }

            MessageTypeAttribute attribute = (MessageTypeAttribute)attributes[0];
            return Version == 1 ? attribute.Type : attribute.Type2;
        }

        static void Escape(StringBuilder sb, string text)
        {
            foreach (char c in text)
            {
                switch (c)
                {
                    case '\\': sb.Append('\\'); sb.Append('\\'); break;
                    case '\"': sb.Append('\\'); sb.Append('"'); break;
                    case '\n': sb.Append('\\'); sb.Append('n'); break;
                    case '\r': sb.Append('\\'); sb.Append('r'); break;
                    case '\t': sb.Append('\\'); sb.Append('t'); break;
                    case '\b': sb.Append('\\'); sb.Append('b'); break;
                    case '\f': sb.Append('\\'); sb.Append('f'); break;
                    default: sb.Append(c); break;
                }
            }
        }

        static void SerializeInternal(int version, StringBuilder sb, Type type, object message)
        {
            if (type.IsNullable())
            {
                type = Nullable.GetUnderlyingType(type);
            }

            if (type == typeof(string))
            {
                sb.Append('"');
                if (!string.IsNullOrEmpty((string)message))
                {
                    Escape(sb, message.ToString());
                }
                sb.Append('"');                
            }
            else if (type == typeof(bool))
            {
                sb.Append(message.ToString().ToLower());
            }
            else if (type.IsEnum)
            {
                var etype = type.GetEnumUnderlyingType();
                SerializeInternal(version, sb, etype, Convert.ChangeType(message, etype));
            }
            else if (BuiltinMessageTypes.ContainsKey(type))
            {
                sb.Append(message.ToString());
            }
            else if (type == typeof(PartialByteArray))
            {
                PartialByteArray arr = (PartialByteArray)message;
                if (version == 1)
                {
                    sb.Append('"');
                    sb.Append(System.Convert.ToBase64String(arr.Array, 0, arr.Length));
                    sb.Append('"');
                }
                else
                {
                    sb.Append('[');
                    for (int i = 0; i < arr.Length; i++)
                    {
                        sb.Append(arr.Array[i]);
                        if (i < arr.Length - 1)
                        {
                            sb.Append(',');
                        }
                    }
                    sb.Append(']');
                }
            }
            else if (type.IsArray)
            {
                if (type.GetElementType() == typeof(byte) && version == 1)
                {
                    sb.Append('"');
                    sb.Append(System.Convert.ToBase64String((byte[])message));
                    sb.Append('"');
                }
                else
                {
                    Array arr = (Array)message;
                    sb.Append('[');
                    for (int i = 0; i < arr.Length; i++)
                    {
                        SerializeInternal(version, sb, type.GetElementType(), arr.GetValue(i));
                        if (i < arr.Length - 1)
                        {
                            sb.Append(',');
                        }
                    }
                    sb.Append(']');
                }
            }
            else if (type.IsGenericList())
            {
                IList list = (IList)message;            
                sb.Append('[');
                for (int i = 0; i < list.Count; i++)
                {
                    SerializeInternal(version, sb, list[i].GetType(), list[i]);
                    if (i < list.Count - 1)
                    {
                        sb.Append(',');
                    }
                }
                sb.Append(']');                
            }
            else if (type == typeof(Time))
            {
                Time t = (Time)message;
                if (version == 1)
                {
                    sb.AppendFormat("{{\"data\":{{\"secs\":{0},\"nsecs\":{1}}}}}", (uint)t.secs, (uint)t.nsecs);
                }
                else
                {
                    sb.AppendFormat("{{\"sec\":{0},\"nanosec\":{1}}}", (int)t.secs, (uint)t.nsecs);
                }
            }
            else
            {
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

                sb.Append('{');
                for (int i = 0; i < fields.Length; i++)
                {
                    var field = fields[i];
                    if (version == 2 && type == typeof(Header) && field.Name == "seq")
                    {
                        continue;
                    }

                    if (field.GetValue(message) != null || (field.FieldType.IsNullable() && Attribute.IsDefined(field, typeof(global::Apollo.RequiredAttribute))))
                    {
                        sb.Append('"');
                        sb.Append(field.Name);
                        sb.Append('"');
                        sb.Append(':');

                        SerializeInternal(version, sb, field.FieldType, field.GetValue(message));

                        if (i < fields.Length - 1)
                        {
                            sb.Append(',');
                        }
                    }
                }
                sb.Append('}');
            }
        }

        static void Serialize(int version, StringBuilder sb, Type type, object message)
        {
            if (type == typeof(string))
            {
                sb.Append("{");
                sb.Append("\"data\":");
                sb.Append('"');
                Escape(sb, message.ToString());
                sb.Append('"');
                sb.Append('}');
            }
            else if (BuiltinMessageTypes.ContainsKey(type))
            {
                sb.Append("{");
                sb.Append("\"data\":");
                sb.Append(message.ToString());
                sb.Append('}');
            }
            else
            {
                SerializeInternal(version, sb, type, message);
            }
        }

        static object UnserializeInternal(int version, JSONNode node, Type type)
        {
            if (type == typeof(bool))
            {
                return node.AsBool;
            }
            else if (type == typeof(sbyte))
            {
                return short.Parse(node.Value);
            }
            else if (type == typeof(int))
            {
                return int.Parse(node.Value);
            }
            else if (type == typeof(long))
            {
                return long.Parse(node.Value);
            }
            else if (type == typeof(byte))
            {
                return byte.Parse(node.Value);
            }
            else if (type == typeof(ushort))
            {
                return ushort.Parse(node.Value);
            }
            else if (type == typeof(uint))
            {
                return uint.Parse(node.Value);
            }
            else if (type == typeof(ulong))
            {
                return ulong.Parse(node.Value);
            }
            else if (type == typeof(float))
            {
                return node.AsFloat;
            }
            else if (type == typeof(double))
            {
                return node.AsDouble;
            }
            else if (type == typeof(string))
            {
                return node["data"];
            }
            else if (type == typeof(PartialByteArray))
            {
                var nodeArr = node.AsArray;

                if (type.GetElementType() == typeof(byte) && nodeArr == null)
                {
                    var array = System.Convert.FromBase64String(node.Value);
                    return new PartialByteArray()
                    {
                        Array = array,
                        Length = array.Length,
                    };
                }
                else
                {
                    var array = new PartialByteArray()
                    {
                        Array = new byte[node.Count],
                        Length = node.Count,
                    };
                    for (int i = 0; i < node.Count; i++)
                    {
                        array.Array[i] = byte.Parse(nodeArr[i].Value);
                    }
                    return array;
                }
            }
            else if (type.IsArray)
            {
                var nodeArr = node.AsArray;

                if (type.GetElementType() == typeof(byte) && nodeArr == null)
                {
                    return System.Convert.FromBase64String(node.Value);
                }

                var arr = Array.CreateInstance(type.GetElementType(), node.Count);
                for (int i = 0; i < node.Count; i++)
                {
                    arr.SetValue(UnserializeInternal(version, nodeArr[i], type.GetElementType()), i);
                }
                return arr;
            }
            else if (type == typeof(Time))
            {
                var nodeObj = node.AsObject;
                var obj = new Time();
                if (version == 1)
                {
                    obj.secs = uint.Parse(nodeObj["secs"].Value);
                    obj.nsecs = uint.Parse(nodeObj["nsecs"].Value);
                }
                else
                {
                    obj.secs = int.Parse(nodeObj["sec"].Value);
                    obj.nsecs = uint.Parse(nodeObj["nanosec"].Value);
                }
                return obj;
            }
            else
            {
                var nodeObj = node.AsObject;
                var obj = Activator.CreateInstance(type);
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (nodeObj.Contains(field.Name))
                    {
                        field.SetValue(obj, UnserializeInternal(version, nodeObj[field.Name], field.FieldType));
                    }
                }
                return obj;
            }
        }

        static object Unserialize(int version, JSONNode node, Type type)
        {
            if (BuiltinMessageTypes.ContainsKey(type))
            {
                return UnserializeInternal(version, node["data"], type);
            }
            return UnserializeInternal(version, node, type);
        }

    }
}
