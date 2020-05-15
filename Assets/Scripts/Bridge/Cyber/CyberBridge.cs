/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using Simulator.Bridge.Data;
using Google.Protobuf.Reflection;
using System.IO;
using ProtoBuf;

namespace Simulator.Bridge.Cyber
{
    enum BridgeOp : byte
    {
        RegisterDesc = 1,
        AddReader = 2,
        AddWriter = 3,
        Publish = 4,
    }

    public partial class Bridge : IBridge
    {
        static readonly TimeSpan Timeout = TimeSpan.FromSeconds(1.0);

        Socket Socket;

        Dictionary<string, Tuple<Func<byte[], object>, List<Action<object>>>> Readers
            = new Dictionary<string, Tuple<Func<byte[], object>, List<Action<object>>>>();

        ConcurrentQueue<Action> QueuedActions = new ConcurrentQueue<Action>();

        List<byte[]> Setup = new List<byte[]>();

        byte[] ReadBuffer = new byte[1024 * 1024];
        List<byte> Buffer = new List<byte>();

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

        public void AddReader<T>(string topic, Action<T> callback) where T : class
        {
            var type = typeof(T);

            Func<object, object> converter = null;
            if (type == typeof(Detected3DObjectArray))
            {
                converter = (object msg) => Conversions.ConvertTo(msg as apollo.common.Detection3DArray);
                type = typeof(apollo.common.Detection3DArray);
            }
            else if(type == typeof(Detected2DObjectArray))
            {
                converter = (object msg) => Conversions.ConvertTo(msg as apollo.common.Detection2DArray);
                type = typeof(apollo.common.Detection2DArray);
            }
            else if (type == typeof(VehicleControlData))
            {
                type = typeof(apollo.control.ControlCommand);
                converter = (object msg) => Conversions.ConvertTo(msg as apollo.control.ControlCommand);
            }
            else
            {
                throw new Exception($"Cyber bridge does not support {typeof(T).Name} type");
            }

            var channelBytes = Encoding.ASCII.GetBytes(topic);
            var typeBytes = Encoding.ASCII.GetBytes(type.ToString());

            var bytes = new List<byte>(1024);
            bytes.Add((byte)BridgeOp.AddReader);
            bytes.Add((byte)(channelBytes.Length >> 0));
            bytes.Add((byte)(channelBytes.Length >> 8));
            bytes.Add((byte)(channelBytes.Length >> 16));
            bytes.Add((byte)(channelBytes.Length >> 24));
            bytes.AddRange(channelBytes);
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
                    Readers.Add(topic,
                        Tuple.Create<Func<byte[], object>, List<Action<object>>>(
                            msg =>
                            {
                                using (var stream = new MemoryStream(msg))
                                {
                                    return converter(Serializer.Deserialize(type, stream));
                                }
                            },
                            new List<Action<object>>())
                    );
                }

                Readers[topic].Item2.Add(msg => callback((T)msg));
            }

            TopicSubscriptions.Add(new TopicUIData()
            {
                Topic = topic,
                Type = type.ToString(),
                Frequency = 0f,
            });
        }

        public IWriter<T> AddWriter<T>(string topic) where T : class
        {
            IWriter<T> writer;

            var type = typeof(T);
            if (type == typeof(ImageData))
            {
                type = typeof(apollo.drivers.CompressedImage);
                writer = new Writer<ImageData, apollo.drivers.CompressedImage>(this, topic, Conversions.ConvertFrom) as IWriter<T>;
            }
            else if (type == typeof(PointCloudData))
            {
                type = typeof(apollo.drivers.PointCloud);
                writer = new Writer<PointCloudData, apollo.drivers.PointCloud>(this, topic, Conversions.ConvertFrom) as IWriter<T>;
            }
            else if (type == typeof(Detected3DObjectData))
            {
                type = typeof(apollo.common.Detection3DArray);
                writer = new Writer<Detected3DObjectData, apollo.common.Detection3DArray>(this, topic, Conversions.ConvertFrom) as IWriter<T>;
            }
            else if(type == typeof(Detected2DObjectData))
            {
                type = typeof(apollo.common.Detection2DArray);
                writer = new Writer<Detected2DObjectData, apollo.common.Detection2DArray>(this, topic, Conversions.ConvertFrom) as IWriter<T>;
            }
            else if (type == typeof(DetectedRadarObjectData))
            {
                type = typeof(apollo.drivers.ContiRadar);
                writer = new Writer<DetectedRadarObjectData, apollo.drivers.ContiRadar>(this, topic, Conversions.ConvertFrom) as IWriter<T>;
            }
            else if (type == typeof(CanBusData))
            {
                type = typeof(apollo.canbus.Chassis);
                writer = new Writer<CanBusData, apollo.canbus.Chassis>(this, topic, Conversions.ConvertFrom) as IWriter<T>;
            }
            else if (type == typeof(GpsData))
            {
                type = typeof(apollo.drivers.gnss.GnssBestPose);
                writer = new Writer<GpsData, apollo.drivers.gnss.GnssBestPose>(this, topic, Conversions.ConvertFrom) as IWriter<T>;
            }
            else if (type == typeof(GpsOdometryData))
            {
                type = typeof(apollo.localization.Gps);
                writer = new Writer<GpsOdometryData, apollo.localization.Gps>(this, topic, Conversions.ConvertFrom) as IWriter<T>;
            }
            else if (type == typeof(GpsInsData))
            {
                type = typeof(apollo.drivers.gnss.InsStat);
                writer = new Writer<GpsInsData, apollo.drivers.gnss.InsStat>(this, topic, Conversions.ConvertFrom) as IWriter<T>;
            }
            else if (type == typeof(ImuData))
            {
                type = typeof(apollo.drivers.gnss.Imu);
                writer = new Writer<ImuData, apollo.drivers.gnss.Imu>(this, topic, Conversions.ConvertFrom) as IWriter<T>;
            }
            else if (type == typeof(CorrectedImuData))
            {
                type = typeof(apollo.localization.CorrectedImu);
                writer = new Writer<CorrectedImuData, apollo.localization.CorrectedImu>(this, topic, Conversions.ConvertFrom) as IWriter<T>;
            }
            else if (BridgeConfig.bridgeConverters.ContainsKey(type))
            {
                writer = new Writer<T, object>(this, topic, (BridgeConfig.bridgeConverters[type] as IDataConverter<T>).GetConverter(this)) as IWriter<T>;
                type = (BridgeConfig.bridgeConverters[type] as IDataConverter<T>).GetOutputType(this);
            }
            else
            {
                throw new Exception($"Unsupported message type {type} used for CyberRT bridge");
            }

            var descriptorName = NameByMsgType[type.ToString()];
            var descriptor = DescriptorByName[descriptorName].Item2;

            var descriptors = new List<byte[]>();
            GetDescriptors(descriptors, descriptor);

            int count = descriptors.Count;

            var bytes = new List<byte>(4096);
            bytes.Add((byte)BridgeOp.RegisterDesc);
            bytes.Add((byte)(count >> 0));
            bytes.Add((byte)(count >> 8));
            bytes.Add((byte)(count >> 16));
            bytes.Add((byte)(count >> 24));
            foreach (var desc in descriptors)
            {
                int length = desc.Length;
                bytes.Add((byte)(length >> 0));
                bytes.Add((byte)(length >> 8));
                bytes.Add((byte)(length >> 16));
                bytes.Add((byte)(length >> 24));
                bytes.AddRange(desc);
            }

            var channelBytes = Encoding.ASCII.GetBytes(topic);
            var typeBytes = Encoding.ASCII.GetBytes(type.ToString());

            bytes.Add((byte)BridgeOp.AddWriter);
            bytes.Add((byte)(channelBytes.Length >> 0));
            bytes.Add((byte)(channelBytes.Length >> 8));
            bytes.Add((byte)(channelBytes.Length >> 16));
            bytes.Add((byte)(channelBytes.Length >> 24));
            bytes.AddRange(channelBytes);
            bytes.Add((byte)(typeBytes.Length >> 0));
            bytes.Add((byte)(typeBytes.Length >> 8));
            bytes.Add((byte)(typeBytes.Length >> 16));
            bytes.Add((byte)(typeBytes.Length >> 24));
            bytes.AddRange(typeBytes);

            TopicPublishers.Add(new TopicUIData()
            {
                Topic = topic,
                Type = type.ToString(),
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
            Debug.Log("AddService is not supported by CyberBridge");
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
                Debug.Log($"CyberBridge socket is closed");
                Disconnect();
                return;
            }

            Buffer.AddRange(ReadBuffer.Take(read));

            int count = Buffer.Count;

            while (count > 0)
            {
                byte op = Buffer[0];
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
                    Debug.Log($"Unknown CyberBridge operation {op} received, disconnecting");
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
            return Buffer[offset + 0] | (Buffer[offset + 1] << 8) | (Buffer[offset + 2] << 16) | (Buffer[offset + 3] << 24);
        }

        bool ReceivePublish()
        {
            if (1 + 2 * 4 > Buffer.Count)
            {
                return false;
            }

            int offset = 1;

            int channel_size = Get32le(offset);
            offset += 4;
            if (offset + channel_size > Buffer.Count)
            {
                return false;
            }

            int channel_offset = offset;
            offset += channel_size;

            int message_size = Get32le(offset);
            offset += 4;
            if (offset + message_size > Buffer.Count)
            {
                return false;
            }

            int message_offset = offset;
            offset += message_size;

            var channel = Encoding.ASCII.GetString(Buffer.Skip(channel_offset).Take(channel_size).ToArray());

            if (Readers.TryGetValue(channel, out var readersPair))
            {
                var parser = readersPair.Item1;
                var readers = readersPair.Item2;

                var bytes = Buffer.Skip(message_offset).Take(message_size).ToArray();
                var message = parser(bytes);

                foreach (var reader in readers)
                {
                    QueuedActions.Enqueue(() => reader(message));
                }

                if (!string.IsNullOrEmpty(channel))
                {
                    TopicSubscriptions.Find(x => x.Topic == channel).Count++;
                }
            }
            else
            {
                Debug.Log($"Received message on channel '{channel}' which nobody subscribed");
            }

            Buffer.RemoveRange(0, offset);
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

        static void GetDescriptors(List<byte[]> descriptors, FileDescriptorProto descriptor)
        {
            foreach (var dependency in descriptor.Dependencies)
            {
                var desc = DescriptorByName[dependency].Item2;
                GetDescriptors(descriptors, desc);
            }
            var bytes = DescriptorByName[descriptor.Name].Item1;
            descriptors.Add(bytes);
        }
    }
}
