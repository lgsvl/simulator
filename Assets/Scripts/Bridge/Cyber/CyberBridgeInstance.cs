/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using UnityEngine;

namespace Simulator.Bridge.Cyber
{
    enum BridgeOp : byte
    {
        RegisterDesc = 1,
        AddReader = 2,
        AddWriter = 3,
        Publish = 4,
    }

    public partial class CyberBridgeInstance : IBridgeInstance
    {
        const int DefaultPort = 9090;

        static readonly TimeSpan Timeout = TimeSpan.FromSeconds(1.0);
        Socket Socket;

        List<byte[]> Setup = new List<byte[]>();

        byte[] ReadBuffer = new byte[1024 * 1024];
        ByteArray Buffer = new ByteArray();

        Dictionary<string, Action<ArraySegment<byte>>> Readers = new Dictionary<string, Action<ArraySegment<byte>>>();

        public Status Status { get; private set; } = Status.Disconnected;

        public CyberBridgeInstance()
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
            try
            {
                Dns.BeginGetHostAddresses(address, OnDnsResolved, null);
            }
            catch (Exception ex)
            {
                ConnectionError(ex);
            }

            void OnDnsResolved(IAsyncResult ar)
            {
                try
                {
                    var ipAddresses = Dns.EndGetHostAddresses(ar);
                    Socket.BeginConnect(ipAddresses, port, OnConnectionComplete, null);
                }
                catch (Exception ex)
                {
                    ConnectionError(ex);
                }
            }
        }

        private void OnConnectionComplete(IAsyncResult ar)
        {
            try
            {
                Socket.EndConnect(ar);
                lock (Setup)
                {
                    Setup.ForEach(s => SendAsync(s, null));
                    Status = Status.Connected;
                }

                Socket.BeginReceive(ReadBuffer, 0, ReadBuffer.Length, SocketFlags.Partial, OnEndRead, null);
            }
            catch (SocketException ex)
            {
                ConnectionError(ex);
            }
        }

        private void ConnectionError(Exception ex)
        {
            Debug.LogException(ex);
            Status = Status.UnexpectedlyDisconnected;
            Socket.Close();
            Socket = null;
        }

        public void Disconnect()
        {
            Status = Status.Disconnected;
            if (Socket != null)
            {
                Socket.Close();
                Socket = null;
            }
        }

        public void AddPublisher<BridgeType>(string topic)
        {
            var descriptors = GetDescriptors<BridgeType>();
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
            var typeBytes = Encoding.ASCII.GetBytes(typeof(BridgeType).ToString());

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
            var channelBytes = Encoding.ASCII.GetBytes(topic);
            var typeBytes = Encoding.ASCII.GetBytes(typeof(BridgeType).ToString());

            var bytes = new List<byte>(1 + 4 + channelBytes.Length + 4 + typeBytes.Length);
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
                Readers.Add(topic, callback);
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
                Debug.Log($"CyberBridge socket is closed");
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
                    Debug.LogWarning($"Unknown CyberBridge operation {op} received, disconnecting");
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

            int channelSize = Get32le(offset);
            offset += 4;
            if (offset + channelSize > Buffer.Count)
            {
                return false;
            }

            int channelOffset = offset;
            offset += channelSize;

            int messageSize = Get32le(offset);
            offset += 4;
            if (offset + messageSize > Buffer.Count)
            {
                return false;
            }

            int messageOffset = offset;
            offset += messageSize;

            var channel = Encoding.ASCII.GetString(Buffer.Data, channelOffset, channelSize);

            Action<ArraySegment<byte>> callback;

            lock (Readers)
            {
                Readers.TryGetValue(channel, out callback);
            }

            if (callback == null)
            {
                Debug.LogWarning($"Received message on channel '{channel}' which nobody subscribed to");
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
            // Socket already closed, but worker thread just finished preparing data - ignore
            if (Socket == null)
                return;

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
