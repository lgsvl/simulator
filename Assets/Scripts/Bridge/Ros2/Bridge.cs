/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Text;
using System.Linq;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using Simulator.Bridge.Data;
using Simulator.Bridge.Ros2.Lgsvl;

namespace Simulator.Bridge.Ros2
{
    enum BridgeOp : byte
    {
        AddSubscriber = 1,
        AddPublisher = 2,
        Publish = 3,
    }

    public partial class Bridge : IBridge
    {
        static readonly TimeSpan Timeout = TimeSpan.FromSeconds(1.0);

        Socket Socket;

        Dictionary<string, IReader> Readers = new Dictionary<string, IReader>();
        ConcurrentQueue<Action> QueuedActions = new ConcurrentQueue<Action>();

        List<byte[]> Setup = new List<byte[]>();

        byte[] ReadBuffer = new byte[1024 * 1024];
        ByteArray Buffer = new ByteArray();

        public Status Status { get; private set; }

        public List<TopicUIData> TopicSubscriptions { get; set; } = new List<TopicUIData>();
        public List<TopicUIData> TopicPublishers { get; set; } = new List<TopicUIData>();

        public Bridge()
        {
            Status = Status.Disconnected;
        }

        public void Connect(string address, int port)
        {
            Socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            Socket.ReceiveBufferSize = ReadBuffer.Length;
            Socket.SendBufferSize = ReadBuffer.Length;
            Socket.ReceiveTimeout = Timeout.Milliseconds;
            Socket.SendTimeout = Timeout.Milliseconds;

            Socket.NoDelay = true;
            Status = Status.Connecting;
            Socket.BeginConnect(address, port, ar =>
            {
                try
                {
                    Socket.EndConnect(ar);
                }
                catch (SocketException ex)
                {
                    Debug.LogException(ex);
                    Disconnect();
                    return;
                }

                lock (Setup)
                {
                    Setup.ForEach(s => SendAsync(s, null));
                    Status = Status.Connected;
                }

                Socket.BeginReceive(ReadBuffer, 0, ReadBuffer.Length, SocketFlags.Partial, OnEndRead, null);
            }, null);
        }

        public void Disconnect()
        {
            if (Socket == null)
            {
                return;
            }

            while (QueuedActions.TryDequeue(out Action action))
            {
            }

            Status = Status.Disconnected;
            Socket.Close();
            Socket = null;
        }

        public void Update()
        {
            Action action;
            while (QueuedActions.TryDequeue(out action))
            {
                action();
            }
        }

        interface IReader
        {
            Type BridgeType { get; }
            Type NativeType { get; }

            void Add(Delegate reader);
            void Read(byte[] data, int offset, int length);
        }

        class Reader<RosType, BridgeData> : IReader
        {
            public Type BridgeType => typeof(BridgeData);
            public Type NativeType => typeof(RosType);

            Func<RosType, BridgeData> Converter;
            ConcurrentQueue<Action> Actions;
            List<Action<BridgeData>> Readers = new List<Action<BridgeData>>();

            public Reader(Func<RosType, BridgeData> converter, ConcurrentQueue<Action> actions)
            {
                Converter = converter;
                Actions = actions;
            }

            public void Add(Delegate reader)
            {
                lock (Readers)
                {
                    Readers.Add(reader as Action<BridgeData>);
                }
            }

            public void Read(byte[] data, int offset, int length)
            {
                var message = Converter(Serialization.Unserialize<RosType>(data, offset, length));
                lock (Readers)
                {
                    foreach (var reader in Readers)
                    {
                        Actions.Enqueue(() => reader(message));
                    }
                }
            }
        }

        public void AddReader<T>(string topic, Action<T> callback) where T : class
        {
            if (topic.Split('/').Any(x => char.IsDigit(x.FirstOrDefault())))
            {
                throw new ArgumentException($"ROS2 does not allow part topic name start with digit - '{topic}'");
            }

            var type = typeof(T);

            IReader reader;
            if (type == typeof(Detected3DObjectArray))
            {
                type = typeof(Detection3DArray);
                reader = new Reader<Detection3DArray, Detected3DObjectArray>(Conversions.ConvertTo, QueuedActions);
            }
            else if (type == typeof(Detected2DObjectArray))
            {
                type = typeof(Detection2DArray);
                reader = new Reader<Detection2DArray, Detected2DObjectArray>(Conversions.ConvertTo, QueuedActions);
            }
            else if (type == typeof(VehicleControlData))
            {
                type = typeof(VehicleControlDataRos);
                reader = new Reader<VehicleControlDataRos, VehicleControlData>(Conversions.ConvertTo, QueuedActions);
            }
            else if (type == typeof(VehicleStateData))
            {
                type = typeof(VehicleStateDataRos);
                reader = new Reader<VehicleStateDataRos, VehicleStateData>(Conversions.ConvertTo, QueuedActions);
            }
            else
            {
                throw new Exception($"ros2-lgsvl-bridge does not support {typeof(T).Name} type");
            }

            var topicBytes = Encoding.ASCII.GetBytes(topic);
            var messageType = GetMessageType(type);
            var typeBytes = Encoding.ASCII.GetBytes(messageType);

            var bytes = new List<byte>(1024);

            bytes.Add((byte)BridgeOp.AddSubscriber);

            bytes.Add((byte)(topicBytes.Length >> 0));
            bytes.Add((byte)(topicBytes.Length >> 8));
            bytes.Add((byte)(topicBytes.Length >> 16));
            bytes.Add((byte)(topicBytes.Length >> 24));
            bytes.AddRange(topicBytes);

            bytes.Add((byte)(typeBytes.Length >> 0));
            bytes.Add((byte)(typeBytes.Length >> 8));
            bytes.Add((byte)(typeBytes.Length >> 16));
            bytes.Add((byte)(typeBytes.Length >> 24));
            bytes.AddRange(typeBytes);

            var data = bytes.ToArray();
            lock (Setup)
            {
                if (Status == Status.Connected)
                {
                    SendAsync(data, null);
                }
                Setup.Add(data);
            }

            lock (Readers)
            {
                if (!Readers.ContainsKey(topic))
                {
                    Readers.Add(topic, reader);
                }
            }
            Readers[topic].Add(callback);

            TopicSubscriptions.Add(new TopicUIData()
            {
                Topic = topic,
                Type = messageType,
                Frequency = 0f,
            });
        }

        public IWriter<T> AddWriter<T>(string topic) where T : class
        {
            if (topic.Split('/').Any(x => char.IsDigit(x.FirstOrDefault())))
            {
                throw new ArgumentException($"ROS2 does not allow part topic name start with digit - '{topic}'");
            }

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
                type = typeof(Detection3DArray);
                writer = new Writer<Detected3DObjectData, Detection3DArray>(this, topic, Conversions.ConvertFrom) as IWriter<T>;
            }
            else if (type == typeof(Detected2DObjectData))
            {
                type = typeof(Detection2DArray);
                writer = new Writer<Detected2DObjectData, Detection2DArray>(this, topic, Conversions.ConvertFrom) as IWriter<T>;
            }
            else if (type == typeof(SignalDataArray))
            {
                type = typeof(SignalArray);
                writer = new Writer<SignalDataArray, SignalArray>(this, topic, Conversions.ConvertFrom) as IWriter<T>;
            }
            else if (type == typeof(CanBusData))
            {
                type = typeof(CanBusDataRos);
                writer = new Writer<CanBusData, CanBusDataRos>(this, topic, Conversions.ConvertFrom) as IWriter<T>;
            }
            else if (type == typeof(GpsData))
            {
                type = typeof(NavSatFix);
                writer = new Writer<GpsData, NavSatFix>(this, topic, Conversions.ConvertFrom) as IWriter<T>;
            }
            else if (type == typeof(ImuData))
            {
                type = typeof(Imu);
                writer = new Writer<ImuData, Imu>(this, topic, Conversions.ConvertFrom) as IWriter<T>;
            }
            else if (type == typeof(GpsOdometryData))
            {
                type = typeof(Odometry);
                writer = new Writer<GpsOdometryData, Odometry>(this, topic, Conversions.ConvertFrom) as IWriter<T>;
            }
            // else if (type == typeof(VehicleOdometryData))
            // {
            //     type = typeof(VehicleOdometry);
            //     writer = new Writer<VehicleOdometryData, VehicleOdometry>(this, topic, Conversions.ConvertFrom) as IWriter<T>;
            // }
            else if (type == typeof(ClockData))
            {
                type = typeof(Clock);
                writer = new Writer<ClockData, Clock>(this, topic, Conversions.ConvertFrom) as IWriter<T>;
            }
            else if (BridgeConfig.bridgeConverters.ContainsKey(type))
            {
                writer = new Writer<T, object>(this, topic, (BridgeConfig.bridgeConverters[type] as IDataConverter<T>).GetConverter(this)) as IWriter<T>;
                type = (BridgeConfig.bridgeConverters[type] as IDataConverter<T>).GetOutputType(this);
            }
            else
            {
                throw new Exception($"Unsupported message type {type} used for ros2-lgsvl-bridge");
            }

            var topicBytes = Encoding.ASCII.GetBytes(topic);
            var messageType = GetMessageType(type);
            var typeBytes = Encoding.ASCII.GetBytes(messageType);

            var bytes = new List<byte>(4096);

            bytes.Add((byte)BridgeOp.AddPublisher);

            bytes.Add((byte)(topicBytes.Length >> 0));
            bytes.Add((byte)(topicBytes.Length >> 8));
            bytes.Add((byte)(topicBytes.Length >> 16));
            bytes.Add((byte)(topicBytes.Length >> 24));
            bytes.AddRange(topicBytes);

            bytes.Add((byte)(typeBytes.Length >> 0));
            bytes.Add((byte)(typeBytes.Length >> 8));
            bytes.Add((byte)(typeBytes.Length >> 16));
            bytes.Add((byte)(typeBytes.Length >> 24));
            bytes.AddRange(typeBytes);

            TopicPublishers.Add(new TopicUIData()
            {
                Topic = topic,
                Type = messageType,
                Frequency = 0f,
            });

            var data = bytes.ToArray();
            lock (Setup)
            {
                if (Status == Status.Connected)
                {
                    SendAsync(data, null);
                }
                Setup.Add(data);
            }

            return writer;
        }

        public void AddService<Argument, Result>(string topic, Func<Argument, Result> callback)
        {
            Debug.Log("AddService is not supported by ros2-lgsvl-bridge");
            throw new NotImplementedException();
        }

        void OnEndRead(IAsyncResult ar)
        {
            int read;
            try
            {
                read = Socket.EndReceive(ar);
            }
            catch (SocketException ex)
            {
                Debug.LogException(ex);
                Disconnect();
                return;
            }

            if (read == 0)
            {
                Debug.Log($"ros2-lgsvl-bridge socket is closed");
                Disconnect();
                return;
            }

            Buffer.Apppend(ReadBuffer, 0, read);

            int count = Buffer.Count;

            while (count > 0)
            {
                byte op = Buffer.Data[0];
                if (op == (byte)BridgeOp.Publish)
                {
                    try
                    {
                        ReceivePublish();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                        Disconnect();
                        return;
                    }
                }
                else
                {
                    Debug.Log($"Unknown ros2-lgsvl-bridge operation {op} received, disconnecting");
                    Disconnect();
                    return;
                }

                if (count == Buffer.Count)
                {
                    break;
                }
                count = Buffer.Count;
            }

            Socket.BeginReceive(ReadBuffer, 0, ReadBuffer.Length, SocketFlags.Partial, OnEndRead, null);
        }

        int Get32le(int offset)
        {
            return Buffer.Data[offset + 0] | (Buffer.Data[offset + 1] << 8) | (Buffer.Data[offset + 2] << 16) | (Buffer.Data[offset + 3] << 24);
        }

        bool ReceivePublish()
        {
            if (1 + 2 * 4 > Buffer.Count)
            {
                return false;
            }

            int offset = 1;

            int topic_size = Get32le(offset);
            offset += 4;
            if (offset + topic_size > Buffer.Count)
            {
                return false;
            }

            int topic_offset = offset;
            offset += topic_size;

            int message_size = Get32le(offset);
            offset += 4;
            if (offset + message_size > Buffer.Count)
            {
                return false;
            }

            int message_offset = offset;
            offset += message_size;

            var topic = Encoding.ASCII.GetString(Buffer.Data, topic_offset, topic_size);

            IReader reader;

            lock (Readers)
            {
                Readers.TryGetValue(topic, out reader);
            }

            if (reader != null)
            {
                reader.Read(Buffer.Data, message_offset, message_size);
                if (!string.IsNullOrEmpty(topic))
                {
                    TopicSubscriptions.Find(x => x.Topic == topic).Count++;
                }
            }
            else
            {
                Debug.Log($"Received message on topic '{topic}' which nobody subscribed");
            }

            Buffer.RemoveFirst(offset);
            return true;
        }

        public void SendAsync(byte[] data, Action completed, string topic = null)
        {
            try
            {
                Socket.BeginSend(data, 0, data.Length, SocketFlags.None, ar =>
                {
                    try
                    {
                        Socket.EndSend(ar);
                    }
                    catch (SocketException ex)
                    {
                        Debug.LogException(ex);
                        Disconnect();
                    }
                    completed?.Invoke();
                }, null);
            }
            catch (SocketException ex)
            {
                Debug.LogException(ex);
                Disconnect();
            }

            if (topic != null)
            {
                TopicPublishers.Find(x => x.Topic == topic).Count++;
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

        static string GetMessageType(Type type)
        {
            string name;
            if (BuiltinMessageTypes.TryGetValue(type, out name))
            {
                return name;
            }

            object[] attributes = type.GetCustomAttributes(typeof(MessageTypeAttribute), false);
            if (attributes == null || attributes.Length == 0)
            {
                throw new Exception($"Type {type.Name} does not have {nameof(MessageTypeAttribute)} attribute");
            }

            var attribute = attributes[0] as MessageTypeAttribute;
            return attribute.Type;
        }
    }
}
