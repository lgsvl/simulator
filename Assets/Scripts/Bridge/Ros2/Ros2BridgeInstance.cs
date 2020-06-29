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
using UnityEngine;

namespace Simulator.Bridge.Ros2
{
    enum BridgeOp : byte
    {
        AddSubscriber = 1,
        AddPublisher = 2,
        Publish = 3,
    }

    public partial class Ros2BridgeInstance : IBridgeInstance
    {
        const int DefaultPort = 9090;

        static readonly TimeSpan Timeout = TimeSpan.FromSeconds(1.0);
        Socket Socket;

        List<byte[]> Setup = new List<byte[]>();

        byte[] ReadBuffer = new byte[1024 * 1024];
        ByteArray Buffer = new ByteArray();

        Dictionary<string, Action<ArraySegment<byte>>> Subscribers = new Dictionary<string, Action<ArraySegment<byte>>>();

        public Status Status { get; private set; } = Status.Disconnected;

        public Ros2BridgeInstance()
        {
        }

        public void Connect(string connection)
        {
            var split = connection.Split(new[] { ':' }, 2);

            var address = split[0];
            var port = split.Length == 1 ? DefaultPort : int.Parse(split[1]);

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

            Status = Status.Disconnected;
            Socket.Close();
            Socket = null;
        }

        public void AddPublisher<BridgeType>(string topic)
        {
            if (topic.Split('/').Any(x => char.IsDigit(x.FirstOrDefault())))
            {
                throw new ArgumentException($"ROS2 does not allow part topic name start with digit - '{topic}'");
            }

            var topicBytes = Encoding.ASCII.GetBytes(topic);
            var messageType = Ros2Utils.GetMessageType<BridgeType>();
            var typeBytes = Encoding.ASCII.GetBytes(messageType);

            var bytes = new List<byte>(1 + 4 + topicBytes.Length + 4 + typeBytes.Length);

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

            var data = bytes.ToArray();
            lock (Setup)
            {
                if (Status == Status.Connected)
                {
                    SendAsync(data, null);
                }
                Setup.Add(data);
            }
        }

        public void AddSubscriber<BridgeType>(string topic, Action<ArraySegment<byte>> callback)
        {
            if (topic.Split('/').Any(x => char.IsDigit(x.FirstOrDefault())))
            {
                throw new ArgumentException($"ROS2 does not allow part topic name start with digit - '{topic}'");
            }

            var topicBytes = Encoding.ASCII.GetBytes(topic);
            var messageType = Ros2Utils.GetMessageType<BridgeType>();
            var typeBytes = Encoding.ASCII.GetBytes(messageType);

            var bytes = new List<byte>(1 + 4 + topicBytes.Length + 4 + typeBytes.Length);

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

            lock (Subscribers)
            {
                Subscribers.Add(topic, callback);
            }
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

            Buffer.Append(ReadBuffer, 0, read);

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
                    Debug.LogError($"Unknown ros2-lgsvl-bridge operation {op} received, disconnecting");
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

            int topicSize = Get32le(offset);
            offset += 4;
            if (offset + topicSize > Buffer.Count)
            {
                return false;
            }

            int topicOffset = offset;
            offset += topicSize;

            int messageSize = Get32le(offset);
            offset += 4;
            if (offset + messageSize > Buffer.Count)
            {
                return false;
            }

            int messageOffset = offset;
            offset += messageSize;

            var topic = Encoding.ASCII.GetString(Buffer.Data, topicOffset, topicSize);

            Action<ArraySegment<byte>> callback;

            lock (Subscribers)
            {
                Subscribers.TryGetValue(topic, out callback);
            }

            if (callback == null)
            {
                Debug.LogWarning($"Received message on '{topic}' topic which nobody subscribed to");
            }
            else
            {
                callback(new ArraySegment<byte>(Buffer.Data, messageOffset, messageSize));
            }

            Buffer.RemoveFirst(offset);
            return true;
        }

        public void SendAsync(byte[] data, Action completed)
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
        }
    }
}
