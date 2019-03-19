using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using WebSocketSharp;
using SimpleJSON;
using System.Reflection;
using System.Collections;
using static Apollo.Utils;

using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;

namespace Comm
{
    namespace Ros
    {
        public class RosBridge : Bridge
        {
            WebSocket Socket;

            struct Subscription
            {
                public string Topic;
                public Delegate Callback;
            }

            List<BridgeClient> Publishers = new List<BridgeClient>();
            List<Subscription> Subscriptions = new List<Subscription>();
            Dictionary<string, Delegate> Services = new Dictionary<string, Delegate>();
            Queue<Action> QueuedActions = new Queue<Action>();
            Queue<string> QueuedSends = new Queue<string>();
            
            [AttributeUsage(AttributeTargets.Struct)]
            class MessageTypeAttribute : Attribute
            {
                public string Type { get; private set; }
                public string Type2 { get; private set; }

                public MessageTypeAttribute(string type)
                {
                    Type = Type2 = type;
                }

                public MessageTypeAttribute(string type, string type2)
                {
                    Type = type;
                    Type2 = type2;
                }
            }

            public override void SendAsync(byte[] data, Action completed = null)
            {
                //UnityEngine.Debug.Log("Publishing " + data.Substring(0, data.Length > 200 ? 200 : data.Length));

                if (completed == null)
                {
                    Socket.SendAsync(data, ok => { });
                }
                else
                {
                    Socket.SendAsync(data, ok => completed());
                }
            }

            public override void AddReader<T>(string topic, Action<T> callback)
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

                // UnityEngine.Debug.Log("Adding subscriber " + sb.ToString());
                Socket.SendAsync(sb.ToString(), ok => { });
            }

            public override Writer<T> AddWriter<T>(string topic)
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

                // UnityEngine.Debug.Log("Adding publisher " + sb.ToString());
                Socket.SendAsync(sb.ToString(), ok => { });

                return new RosWriter<T>(this, topic);
            }

            static RosBridge()
            {
                // incrase send buffer size for WebSocket C# library
                // FragmentLength is internal filed, that's why reflection is used here
                var f = typeof(WebSocket).GetField("FragmentLength", BindingFlags.Static | BindingFlags.NonPublic);
                f.SetValue(null, 65536 - 8);
            }

            public RosBridge()
            {
                Status = BridgeStatus.Disconnected;
            }

            public override void Connect(string address, int port, int version)
            {
                Address = address;
                Port = port;
                Version = version;
                try
                {
                    Socket = new WebSocket(String.Format("ws://{0}:{1}", address, port));
                    Socket.WaitTime = TimeSpan.FromSeconds(1.0);
                    Socket.OnMessage += OnMessage;
                    Socket.OnOpen += OnOpen;
                    Socket.OnError += OnError;
                    Socket.OnClose += OnClose;
                    Status = BridgeStatus.Connecting;
                    Socket.ConnectAsync();
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError(e);
                }
            }

            public override void Disconnect()
            {
                Close();
            }

            public override void Update()
            {
                lock (QueuedActions)
                {
                    while (QueuedActions.Count > 0)
                    {
                        QueuedActions.Dequeue()();
                    }
                }
            }

            public void Close()
            {
                if (Socket != null)
                {
                    if (Socket.ReadyState == WebSocketState.Open)
                    {
                        Status = BridgeStatus.Disconnecting;
                        Socket.CloseAsync();
                    }
                }
            }


            void OnClose(object sender, CloseEventArgs args)
            {
                //if (!args.WasClean)
                //{
                //    UnityEngine.Debug.LogError(args.Reason);
                //}
                Subscriptions.Clear();
                Services.Clear();
                QueuedActions.Clear();
                QueuedSends.Clear();
                TopicSubscriptions.Clear();
                TopicPublishers.Clear();
                Status = BridgeStatus.Disconnected;
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
                Status = BridgeStatus.Connected;
                FinishConnecting();
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
                            lock (QueuedActions)
                            {
                                QueuedActions.Enqueue(() => sub.Callback.DynamicInvoke(data));
                            }
                        }
                    }
                }
                else if (op == "call_service")
                {
                    var service = json["service"];
                    var id = json["id"];
                    if (!Services.ContainsKey(service))
                    {
                        return;
                    }

                    var callback = Services[service];

                    var argType = callback.Method.GetParameters()[0].ParameterType;
                    var retType = callback.Method.ReturnType;

                    var arg = Unserialize(Version, json["args"], argType);

                    lock (QueuedActions)
                    {
                        QueuedActions.Enqueue(() =>
                        {
                            var ret = callback.DynamicInvoke(arg);

                            var sb = new StringBuilder(128);
                            sb.Append('{');
                            {
                                sb.Append("\"op\":\"service_response\",");

                                sb.Append("\"id\":");
                                sb.Append(id.ToString());
                                sb.Append(",");

                                sb.Append("\"service\":\"");
                                sb.Append(service.Value);
                                sb.Append("\",");

                                sb.Append("\"values\":");
                                Serialize(Version, sb, retType, ret);
                                sb.Append(",");

                                sb.Append("\"result\":true");
                            }
                            sb.Append('}');

                            var s = sb.ToString();
                            Socket.SendAsync(s, ok => { });
                        });
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

            public override void AddService<Args, Result>(string service, Func<Args, Result> callback)
            {
                if (Socket.ReadyState != WebSocketState.Open)
                {
                    throw new InvalidOperationException("socket not open");
                }

                var type = GetMessageType<Args>();
                GetMessageType<Result>();

                var sb = new StringBuilder(256);
                sb.Append('{');
                {
                    sb.Append("\"op\":\"advertise_service\",");

                    sb.Append("\"type\":\"");
                    sb.Append(type);
                    sb.Append("\",");

                    sb.Append("\"service\":\"");
                    sb.Append(service);
                    sb.Append("\"");
                }
                sb.Append('}');

                Services.Add(service, callback);
                Socket.SendAsync(sb.ToString(), ok => { });
            }

            static bool CheckBasicType(Type type)
            {
                if (type.IsNullable())
                {
                    type = Nullable.GetUnderlyingType(type);
                }

                if (BuiltinMessageTypes.ContainsKey(type))
                {
                    return true;
                }
                if (type == typeof(string))
                {
                    return true;
                }
                if (type.IsEnum)
                {
                    return true;
                }
                return false;
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

                object[] attributes = typeof(T).GetCustomAttributes(typeof(global::Ros.MessageTypeAttribute), false);
                if (attributes == null || attributes.Length == 0)
                {
                    throw new Exception(String.Format("Type {0} does not have {1} attribute", typeof(T).Name, typeof(global::Ros.MessageTypeAttribute).Name));
                }

                global::Ros.MessageTypeAttribute attribute = (global::Ros.MessageTypeAttribute)attributes[0];
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

            public static void SerializeInternal(int version, StringBuilder sb, Type type, object message, string keyName = "")
            {
                if (type.IsNullable())
                {
                    type = Nullable.GetUnderlyingType(type);
                }
                if (message == null)
                {
                    message = type.TypeDefaultValue(); //only underlying value type will be given a default value
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
                else if (type.IsEnum)
                {
                    var etype = type.GetEnumUnderlyingType();
                    SerializeInternal(version, sb, etype, Convert.ChangeType(message, etype));
                }
                else if (BuiltinMessageTypes.ContainsKey(type))
                {
                    if (type == typeof(bool))
                    {
                        sb.Append(message.ToString().ToLower());
                    }
                    else
                    {
                        sb.Append(message.ToString());
                    }
                }
                else if (type == typeof(global::Ros.PartialByteArray))
                {
                    var  arr = (global::Ros.PartialByteArray)message;
                    if (version == 1)
                    {
                        sb.Append('"');
                        if (arr.Base64 == null)
                        {
                            sb.Append(System.Convert.ToBase64String(arr.Array, 0, arr.Length));
                        }
                        else
                        {
                            sb.Append(arr.Base64);
                        }
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
                else if (type == typeof(global::Ros.Time))
                {
                    global::Ros.Time t = (global::Ros.Time)message;
                    if (version == 1)
                    {
                        sb.AppendFormat("{{\"secs\":{0},\"nsecs\":{1}}}", (uint)t.secs, (uint)t.nsecs);
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
                        if (version == 2 && type == typeof(global::Ros.Header) && field.Name == "seq")
                        {
                            continue;
                        }

                        var fieldType = field.FieldType;
                        var fieldValue = field.GetValue(message);

                        if (fieldValue != null && typeof(IOneOf).IsAssignableFrom(fieldType)) //only when it is a OneOf field
                        {
                            var oneof = fieldValue as IOneOf;
                            if (oneof != null) //only when this is a non-null OneOf
                            {
                                var oneInfo = oneof.GetOne();
                                if (oneInfo.Value != null) //only hwne at least one subfield assgined
                                {
                                    var oneFieldName = oneInfo.Key;
                                    var oneFieldValue = oneInfo.Value;
                                    var oneFieldType = oneInfo.Value.GetType();

                                    sb.Append('"');
                                    sb.Append(oneFieldName);
                                    sb.Append('"');
                                    sb.Append(':');
                                    SerializeInternal(version, sb, oneFieldType, oneFieldValue);
                                    sb.Append(',');
                                }
                            }
                        }
                        else if (fieldValue != null || (fieldType.IsNullable() && Attribute.IsDefined(field, typeof(global::Apollo.RequiredAttribute))))
                        {
                            sb.Append('"');
                            sb.Append(field.Name);
                            sb.Append('"');
                            sb.Append(':');
                            SerializeInternal(version, sb, fieldType, fieldValue);
                            sb.Append(',');
                        }
                    }

                    if (sb[sb.Length - 1] == ',')
                    {
                        sb.Remove(sb.Length - 1, 1);
                    }

                    sb.Append('}');
                }
            }

            static public void Serialize(int version, StringBuilder sb, Type type, object message)
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
                else if (type == typeof(global::Ros.Time))
                {
                    sb.Append("{");
                    sb.Append("\"data\":");
                    SerializeInternal(version, sb, type, message);
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
                    return node.Value;
                }
                else if (type == typeof(global::Ros.PartialByteArray))
                {
                    var nodeArr = node.AsArray;

                    if (type.GetElementType() == typeof(byte) && nodeArr == null)
                    {
                        var array = System.Convert.FromBase64String(node.Value);
                        return new global::Ros.PartialByteArray()
                        {
                            Array = array,
                            Length = array.Length,
                        };
                    }
                    else
                    {
                        var array = new global::Ros.PartialByteArray()
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
                else if (type == typeof(global::Ros.Time))
                {
                    var nodeObj = node.AsObject;
                    var obj = new global::Ros.Time();
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
                else if (type.IsEnum)
                {
                    var value = node.AsInt;
                    var obj = Enum.ToObject(type, value);
                    return obj;
                }
                else
                {
                    var nodeObj = node.AsObject;
                    var obj = Activator.CreateInstance(type);
                    foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                    {
                        // UnityEngine.Debug.Log(nodeObj.ToString());
                        if (nodeObj[field.Name] != null)
                        {
                            var fieldType = field.FieldType;
                            if (fieldType.IsNullable())
                            {
                                fieldType = Nullable.GetUnderlyingType(fieldType);
                            }
                            var value = UnserializeInternal(version, nodeObj[field.Name], fieldType);
                            field.SetValue(obj, value);

                        }
                    }
                    return obj;
                }
            }

            static object Unserialize(int version, JSONNode node, Type type)
            {
                if (BuiltinMessageTypes.ContainsKey(type) || type == typeof(global::Ros.Time))
                {
                    return UnserializeInternal(version, node["data"], type);
                }
                return UnserializeInternal(version, node, type);
            }
        }
    }
}