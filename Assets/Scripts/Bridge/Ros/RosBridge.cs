/**
 * Copyright(c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Globalization;
using System.Collections.Generic;
using System.Collections.Concurrent;
using WebSocketSharp;
using SimpleJSON;
using Simulator.Bridge.Data;
using Simulator.Bridge.Ros.Lgsvl;
using Simulator.Bridge.Ros.Autoware;

namespace Simulator.Bridge.Ros
{
    public class Bridge : IBridge
    {
        WebSocket Socket;
        int Version;
        bool Apollo;

        ConcurrentQueue<Action> QueuedActions = new ConcurrentQueue<Action>();

        Dictionary<string, Tuple<Func<JSONNode, object>, List<Action<object>>>> Readers
            = new Dictionary<string, Tuple<Func<JSONNode, object>, List<Action<object>>>>();

        Dictionary<string, Tuple<Type, Type, Func<object, object>>> Services = new Dictionary<string, Tuple<Type, Type, Func<object, object>>>();

        List<string> Setup = new List<string>();

        public Status Status { get; private set; }

        public List<TopicUIData> TopicSubscriptions { get; set; } = new List<TopicUIData>();
        public List<TopicUIData> TopicPublishers { get; set; } = new List<TopicUIData>();

        static Bridge()
        {
            // increase send buffer size for WebSocket C# library
            // FragmentLength is internal filed, that's why reflection is used here
            var f = typeof(WebSocket).GetField("FragmentLength", BindingFlags.Static | BindingFlags.NonPublic);
            f.SetValue(null, 65536 - 8);
        }

        public Bridge(int version, bool apollo = false)
        {
            Version = version;
            Status = Status.Disconnected;
            Apollo = apollo;
        }

        public void Connect(string address, int port)
        {
            try
            {
                Socket = new WebSocket(string.Format("ws://{0}:{1}", address, port));
                Socket.WaitTime = TimeSpan.FromSeconds(1.0);
                Socket.OnMessage += OnMessage;
                Socket.OnOpen += OnOpen;
                Socket.OnError += OnError;
                Socket.OnClose += OnClose;
                Status = Status.Connecting;
                Socket.ConnectAsync();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
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
            TopicSubscriptions.Clear();
            TopicPublishers.Clear();
        }

        public void SendAsync(byte[] data, Action completed, string topic = null)
        {
            if (completed == null)
            {
                Socket.SendAsync(data, ok => { });
            }
            else
            {
                Socket.SendAsync(data, ok => completed());
            }

            if (topic != null)
            {
                var pub = TopicPublishers.Find(x => x.Topic == topic);
                if (pub != null)
                {
                    pub.Count++;
                }
            }
        }

        public void AddReader<T>(string topic, Action<T> callback) where T : class
        {
            var type = typeof(T);

            Func<JSONNode, object> converter;
            if (type == typeof(Detected3DObjectArray))
            {
                type = typeof(Detection3DArray);
                converter = (JSONNode json) => Conversions.ConvertTo((Detection3DArray)Unserialize(json, type));
            }
            else if (type == typeof(Detected2DObjectArray))
            {
                type = typeof(Detection2DArray);
                converter = (JSONNode json) => Conversions.ConvertTo((Detection2DArray)Unserialize(json, type));
            }
            else if (type == typeof(VehicleControlData))
            {
                if (Apollo)
                {
                    type = typeof(Apollo.control_command);
                    converter = (JSONNode json) => Conversions.ConvertTo((Apollo.control_command)Unserialize(json, type));
                }
                else if (Version == 2)
                {
                    // Since there is no mapping acceleration to throttle, VehicleControlCommand is not supported for now.
                    // After supporting it, VehicleControlCommand will replace RawControlCommand.
                    // type = typeof(Autoware.VehicleControlCommand);
                    // converter = (JSONNode json) => Conversions.ConvertTo((Autoware.VehicleControlCommand)Unserialize(json, type));

                    type = typeof(Lgsvl.VehicleControlDataRos);
                    converter = (JSONNode json) => Conversions.ConvertTo((Lgsvl.VehicleControlDataRos)Unserialize(json, type));
                }
                else
                {
                    type = typeof(Autoware.VehicleCmd);
                    converter = (JSONNode json) => Conversions.ConvertTo((Autoware.VehicleCmd)Unserialize(json, type));
                }
            }
            else if (type == typeof(VehicleStateData))
            {
                type = typeof(Lgsvl.VehicleStateDataRos);
                converter = (JSONNode json) => Conversions.ConvertTo((Lgsvl.VehicleStateDataRos)Unserialize(json, type));
            }
            else if (BridgeConfig.bridgeConverters.ContainsKey(type))
            {
                converter = (JSONNode json) => (BridgeConfig.bridgeConverters[type] as IDataConverter<T>).GetConverter(this);
                type = (BridgeConfig.bridgeConverters[type] as IDataConverter<T>).GetOutputType(this);
            }
            else
            {
                converter = (JSONNode json) => Unserialize(json, type);
            }

            var messageType = GetMessageType(type);

            var sb = new StringBuilder(1024);
            sb.Append('{');
            {
                sb.Append("\"op\":\"subscribe\",");

                sb.Append("\"topic\":\"");
                sb.Append(topic);
                sb.Append("\",");

                sb.Append("\"type\":\"");
                sb.Append(messageType);
                sb.Append("\"");
            }
            sb.Append('}');

            TopicSubscriptions.Add(new TopicUIData()
            {
                Topic = topic,
                Type = messageType,
                Frequency = 0f,
            });

            var data = sb.ToString();
            lock (Setup)
            {
                if (Status == Status.Connected)
                {
                    Socket.SendAsync(data, null);
                }
                Setup.Add(data);
            }

            lock (Readers)
            {
                if (!Readers.ContainsKey(topic))
                {
                    Readers.Add(topic,
                        Tuple.Create<Func<JSONNode, object>, List<Action<object>>>(
                            msg => converter(msg),
                            new List<Action<object>>())
                    );
                }
                Readers[topic].Item2.Add(msg => callback((T)msg));
            }
        }

        public IWriter<T> AddWriter<T>(string topic) where T : class
        {
            IWriter<T> writer;

            var type = typeof(T);
            if (type == typeof(ImageData))
            {
                type = typeof(CompressedImage);
                writer = new Writer<ImageData, CompressedImage>(this, topic, Conversions.ConvertFrom) as IWriter<T>;
            }
            else if (type == typeof(PointCloudData))
            {
                type = typeof(PointCloud2);
                writer = new PointCloudWriter(this, topic) as IWriter<T>;
            }
            else if (type == typeof(Detected3DObjectData))
            {
                type = typeof(Lgsvl.Detection3DArray);
                writer = new Writer<Detected3DObjectData, Lgsvl.Detection3DArray>(this, topic, Conversions.ConvertFrom) as IWriter<T>;
            }
            else if (type == typeof(Detected2DObjectData))
            {
                type = typeof(Lgsvl.Detection2DArray);
                writer = new Writer<Detected2DObjectData, Lgsvl.Detection2DArray>(this, topic, Conversions.ConvertFrom) as IWriter<T>;
            }
            else if (type == typeof(SignalDataArray))
            {
                type = typeof(Lgsvl.SignalArray);
                writer = new Writer<SignalDataArray, Lgsvl.SignalArray>(this, topic, Conversions.ConvertFrom) as IWriter<T>;
            }
            else if (type == typeof(DetectedRadarObjectData) && Apollo)
            {
                if (Apollo)
                {
                    type = typeof(Apollo.Drivers.ContiRadar);
                    writer = new Writer<DetectedRadarObjectData, Apollo.Drivers.ContiRadar>(this, topic, Conversions.ConvertFrom) as IWriter<T>;
                }
                else
                {
                    type = typeof(Lgsvl.DetectedRadarObjectArray);
                    writer = new Writer<DetectedRadarObjectData, Lgsvl.DetectedRadarObjectArray>(this, topic, Conversions.ROS2ConvertFrom) as IWriter<T>;
                }
            }
            else if (type == typeof(CanBusData))
            {
                if (Version == 2 && !Apollo)
                {
                    // type = typeof(VehicleStateReport);
                    // writer = new Writer<CanBusData, VehicleStateReport>(this, topic, Conversions.ROS2ReturnAutowareAutoConvertFrom) as IWriter<T>;

                    type = typeof(CanBusDataRos);
                    writer = new Writer<CanBusData, CanBusDataRos>(this, topic, Conversions.ROS2ReturnLgsvlConvertFrom) as IWriter<T>;
                }
                else
                {
                    type = typeof(Apollo.ChassisMsg);
                    writer = new Writer<CanBusData, Apollo.ChassisMsg>(this, topic, Conversions.ConvertFrom) as IWriter<T>;
                }
            }
            else if (type == typeof(GpsData))
            {
                if (Apollo)
                {
                    type = typeof(Apollo.GnssBestPose);
                    writer = new Writer<GpsData, Apollo.GnssBestPose>(this, topic, Conversions.ConvertFrom) as IWriter<T>;
                }
                else if (Version == 2)
                {
                    type = typeof(NavSatFix);
                    writer = new Writer<GpsData, NavSatFix>(this, topic, Conversions.ROS2ConvertFrom) as IWriter<T>;
                }
                else
                {
                    type = typeof(Sentence);
                    writer = new RosNmeaWriter(this, topic) as IWriter<T>;
                }
            }
            else if (type == typeof(ImuData))
            {
                if (Apollo)
                {
                    type = typeof(Apollo.Imu);
                    writer = new Writer<ImuData, Apollo.Imu>(this, topic, Conversions.ApolloConvertFrom) as IWriter<T>;
                }
                else
                {
                    type = typeof(Imu);
                    writer = new Writer<ImuData, Imu>(this, topic, Conversions.ConvertFrom) as IWriter<T>;
                }
            }
            else if (type == typeof(CorrectedImuData))
            {
                type = typeof(Apollo.CorrectedImu);
                writer = new Writer<CorrectedImuData, Apollo.CorrectedImu>(this, topic, Conversions.ApolloConvertFrom) as IWriter<T>;
            }
            else if (type == typeof(GpsOdometryData))
            {
                if (Apollo)
                {
                    type = typeof(Apollo.Gps);
                    writer = new Writer<GpsOdometryData, Apollo.Gps>(this, topic, Conversions.ApolloConvertFrom) as IWriter<T>;
                }
                else
                {
                    type = typeof(Odometry);
                    writer = new Writer<GpsOdometryData, Odometry>(this, topic, Conversions.ConvertFrom) as IWriter<T>;
                }
            }
            else if (type == typeof(VehicleOdometryData))
            {
                type = typeof(VehicleOdometry);
                writer =  new Writer<VehicleOdometryData, VehicleOdometry>(this, topic, Conversions.ROS2ConvertFrom) as IWriter<T>;
            }
            else if (type == typeof(ClockData))
            {
                type = typeof(Ros.Clock);
                writer = new Writer<ClockData, Ros.Clock>(this, topic, Conversions.ConvertFrom) as IWriter<T>;
            }
            else if (BridgeConfig.bridgeConverters.ContainsKey(type))
            {
                writer = new Writer<T, object>(this, topic, (BridgeConfig.bridgeConverters[type] as IDataConverter<T>).GetConverter(this)) as IWriter<T>;
                type = (BridgeConfig.bridgeConverters[type] as IDataConverter<T>).GetOutputType(this);
            }
            else
            {
                throw new Exception($"Unsupported message type {type} used for ROS bridge");
            }

            var messageType = GetMessageType(type);

            var sb = new StringBuilder(1024);
            sb.Append('{');
            {
                sb.Append("\"op\":\"advertise\",");

                sb.Append("\"topic\":\"");
                sb.Append(topic);
                sb.Append("\",");

                sb.Append("\"type\":\"");
                sb.Append(messageType);
                sb.Append("\"");
            }
            sb.Append('}');

            TopicPublishers.Add(new TopicUIData()
            {
                Topic = topic,
                Type = messageType,
                Frequency = 0f,
            });

            var data = sb.ToString();
            lock (Setup)
            {
                if (Status == Status.Connected)
                {
                    Socket.SendAsync(data, null);
                }
                Setup.Add(data);
            }

            return writer;
        }

        public void AddService<Argument, Result>(string topic, Func<Argument, Result> callback)
        {
            var argtype = typeof(Argument);
            var restype = typeof(Result);

            Func<object, object> converter = null;
            if (argtype == typeof(EmptySrv))
            {
                argtype = typeof(Empty);
                converter = (object obj) => Conversions.ConvertTo((Empty)obj);
            }
            else if (argtype == typeof(SetBoolSrv))
            {
                argtype = typeof(SetBool);
                converter = (object obj) => Conversions.ConvertTo((SetBool)obj);
            }

            Func<object, object> convertResult = null;
            if (restype == typeof(EmptySrv))
            {
                restype = typeof(Empty);
                convertResult = (object obj) => Conversions.ConvertFrom((EmptySrv)obj);
            }
            else if (restype == typeof(SetBoolSrv))
            {
                restype = typeof(SetBoolResponse);
                convertResult = (object obj) => Conversions.ConvertFrom((SetBoolSrv)obj);
            }
            else if (restype == typeof(TriggerSrv))
            {
                restype = typeof(Trigger);
                convertResult = (object obj) => Conversions.ConvertFrom((TriggerSrv)obj);
            }

            var argType = GetMessageType(argtype);
            var resType = GetMessageType(restype);

            if (Services.ContainsKey(topic))
            {
                throw new Exception($"Topic {topic} already has service registered");
            }

            var sb = new StringBuilder(1024);
            sb.Append('{');
            {
                sb.Append("\"op\":\"advertise_service\",");

                sb.Append("\"type\":\"");
                sb.Append(argType);
                sb.Append("\",");

                sb.Append("\"service\":\"");
                sb.Append(topic);
                sb.Append("\"");
            }
            sb.Append('}');

            var data = sb.ToString();
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
                Services.Add(topic, Tuple.Create<Type, Type, Func<object, object>>(argtype, restype, (object argObj) => {
                    var argData = (Argument)converter(argObj);
                    var resData = callback(argData);
                    var resObj = convertResult(resData);
                    return resObj;
                }));
            }
        }

        public void Update()
        {
            Action action;
            while (QueuedActions.TryDequeue(out action))
            {
                action();
            }
        }

        void OnClose(object sender, CloseEventArgs args)
        {
            while (QueuedActions.TryDequeue(out Action action))
            {
            }

            Status = Status.Disconnected;
            Socket = null;
            TopicSubscriptions.Clear();
            TopicPublishers.Clear();
        }

        void OnError(object sender, ErrorEventArgs args)
        {
            UnityEngine.Debug.LogError(args.Message);

            if (args.Exception != null)
            {
                UnityEngine.Debug.LogException(args.Exception);
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

                Tuple<Func<JSONNode, object>, List<Action<object>>> readerPair;
                lock (Readers)
                {
                    if (!Readers.TryGetValue(topic, out readerPair))
                    {
                        UnityEngine.Debug.Log($"Received message on topic '{topic}' which nobody subscribed");
                        return;
                    }
                }

                var parse = readerPair.Item1;
                var readers = readerPair.Item2;

                var msg = parse(json["msg"]);

                foreach (var reader in readers)
                {
                    QueuedActions.Enqueue(() => reader(msg));
                }

                if (!string.IsNullOrEmpty(topic))
                {
                    var topicSub = TopicSubscriptions.Find(x => x.Topic == topic);
                    if (topicSub != null)
                    {
                        topicSub.Count++;
                    }
                }
            }
            else if (op == "call_service")
            {
                var service = json["service"];
                var id = json["id"];

                Tuple<Type, Type, Func<object, object>> serviceTuple;

                lock (Services)
                {
                    if (!Services.TryGetValue(service, out serviceTuple))
                    {
                        return;
                    }
                }

                var (argumentType, resultType, convertResult) = serviceTuple;

                var arg = Unserialize(json["args"], argumentType);

                QueuedActions.Enqueue(() =>
                {
                    var result = convertResult.DynamicInvoke(arg);

                    var sb = new StringBuilder(1024);
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
                        Serialize(result, resultType, sb);
                        sb.Append(",");

                        sb.Append("\"result\":true");
                    }
                    sb.Append('}');

                    var data = sb.ToString();
                    Socket.SendAsync(data, null);
                });
            }
            else if (op == "set_level")
            {
                // ignore these
            }
            else
            {
                UnityEngine.Debug.Log($"Unknown operation from rosbridge: {op}");
            }
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

        string GetMessageType(Type type)
        {
            string name;
            if (BuiltinMessageTypes.TryGetValue(type, out name))
            {
                return name;
            }

            object[] attributes = type.GetCustomAttributes(typeof(MessageTypeAttribute), false);
            if (attributes == null || attributes.Length == 0)
            {
                throw new Exception(string.Format("Type {0} does not have {1} attribute", type.Name, typeof(MessageTypeAttribute).Name));
            }

            var attribute = attributes[0] as MessageTypeAttribute;
            return Version == 1 ? attribute.Type : attribute.Type2;
        }

        public void SerializeInternal(object message, Type type, StringBuilder sb)
        {
            if (type.IsNullable())
            {
                type = Nullable.GetUnderlyingType(type);
            }

            if (type == typeof(string))
            {
                var str = message as string;

                sb.Append('"');
                if (!string.IsNullOrEmpty(str))
                {
                    sb.AppendEscaped(str);
                }
                sb.Append('"');
            }
            else if (type.IsEnum)
            {
                var etype = type.GetEnumUnderlyingType();
                SerializeInternal(Convert.ChangeType(message, etype), etype, sb);
            }
            else if (BuiltinMessageTypes.ContainsKey(type))
            {
                if (type == typeof(bool))
                {
                    sb.Append(string.Format(CultureInfo.InvariantCulture, "{0}", message).ToLowerInvariant());
                }
                else
                {
                    sb.Append(string.Format(CultureInfo.InvariantCulture, "{0}", message));
                }
            }
            else if (type == typeof(PartialByteArray))
            {
                var arr = message as PartialByteArray;
                if (Version == 1)
                {
                    sb.Append('"');
                    if (arr.Base64 == null)
                    {
                        sb.Append(Convert.ToBase64String(arr.Array, 0, arr.Length));
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
                if (type.GetElementType() == typeof(byte) && Version == 1)
                {
                    sb.Append('"');
                    sb.Append(Convert.ToBase64String((byte[])message));
                    sb.Append('"');
                }
                else
                {
                    var array = message as Array;
                    var elementType = type.GetElementType();
                    sb.Append('[');
                    for (int i = 0; i < array.Length; i++)
                    {
                        SerializeInternal(array.GetValue(i), elementType, sb);
                        if (i < array.Length - 1)
                        {
                            sb.Append(',');
                        }
                    }
                    sb.Append(']');
                }
            }
            else if (type.IsGenericList())
            {
                var list = message as IList;
                var elementType = type.GetGenericArguments()[0];
                sb.Append('[');
                for (int i = 0; i < list.Count; i++)
                {
                    SerializeInternal(list[i], elementType, sb);
                    if (i < list.Count - 1)
                    {
                        sb.Append(',');
                    }
                }
                sb.Append(']');
            }
            else if (type == typeof(Time))
            {
                var t = message as Time;
                if (Version == 1)
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
                    if (Version == 2 && type == typeof(Header) && field.Name == "seq")
                    {
                        continue;
                    }

                    var fieldType = field.FieldType;
                    var fieldValue = field.GetValue(message);
                    if (fieldValue != null)
                    {
                        sb.Append('"');
                        sb.Append(field.Name);
                        sb.Append('"');
                        sb.Append(':');
                        SerializeInternal(fieldValue, fieldType, sb);
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

        public void Serialize(object message, Type type, StringBuilder sb)
        {
            if (type == typeof(string))
            {
                var str = message as string;
                sb.Append("{");
                sb.Append("\"data\":");
                sb.Append('"');
                sb.AppendEscaped(str);
                sb.Append('"');
                sb.Append('}');
            }
            else if (BuiltinMessageTypes.ContainsKey(type))
            {
                sb.Append("{");
                sb.Append("\"data\":");
                sb.Append(string.Format(CultureInfo.InvariantCulture, "{0}", message));
                sb.Append('}');
            }
            else if (type == typeof(Time))
            {
                sb.Append("{");
                sb.Append("\"data\":");
                SerializeInternal(message, type, sb);
                sb.Append('}');
            }else if (BridgeConfig.bridgeConverters.ContainsKey(type))
            {
                SerializeInternal(message, BridgeConfig.bridgeConverters[type].GetOutputType(this), sb);
            }
            else
            {
                SerializeInternal(message, type, sb);
            }
        }

        object UnserializeInternal(JSONNode node, Type type)
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
            else if (type == typeof(PartialByteArray))
            {
                var nodeArr = node.AsArray;

                if (type.GetElementType() == typeof(byte) && nodeArr == null)
                {
                    var array = Convert.FromBase64String(node.Value);
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
                    return Convert.FromBase64String(node.Value);
                }

                var arr = Array.CreateInstance(type.GetElementType(), node.Count);
                for (int i = 0; i < node.Count; i++)
                {
                    arr.SetValue(UnserializeInternal(nodeArr[i], type.GetElementType()), i);
                }
                return arr;
            }
            else if (type == typeof(Time))
            {
                var nodeObj = node.AsObject;
                var obj = new Time();
                if (Version == 1)
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
                    if (nodeObj[field.Name] != null)
                    {
                        var fieldType = field.FieldType;
                        if (fieldType.IsNullable())
                        {
                            fieldType = Nullable.GetUnderlyingType(fieldType);
                        }
                        var value = UnserializeInternal(nodeObj[field.Name], fieldType);
                        field.SetValue(obj, value);

                    }
                }
                return obj;
            }
        }

        object Unserialize( JSONNode node, Type type)
        {
            if (BuiltinMessageTypes.ContainsKey(type) || type == typeof(Time))
            {
                return UnserializeInternal(node["data"], type);
            }
            return UnserializeInternal(node, type);
        }
    }
}
