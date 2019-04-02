/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using System.Linq;
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
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace Comm
{
    namespace Cyber
    {
        public class CyberBridge : Bridge
        {
            Socket Socket;

            Dictionary<string, HashSet<Func<IMessage, byte[], IMessage>>> Readers =
                new Dictionary<string, HashSet<Func<IMessage, byte[], IMessage>>>();
            Queue<Action> QueuedActions = new Queue<Action>();

            byte[] Temp = new byte[1024 * 1024];
            List<byte> Buffer = new List<byte>();

            public TimeSpan Timeout = TimeSpan.FromSeconds(1.0);

            public CyberBridge()
            {
                Status = BridgeStatus.Disconnected;
            }

            public override void Disconnect()
            {
                if (Socket == null)
                {
                    return;
                }

                QueuedActions.Clear();
                Buffer.Clear();
                lock (Readers)
                {
                    Readers.Clear();
                }
                Status = BridgeStatus.Disconnected;

                TopicSubscriptions.Clear();
                TopicPublishers.Clear();

                Socket.Close();
                Socket = null;
            }

            public override void Connect(string address, int port, int version)
            {
                Address = address;
                Port = port;
                Version = version;

                Socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                Socket.ReceiveBufferSize = Temp.Length;
                Socket.SendBufferSize = Temp.Length;
                Socket.ReceiveTimeout = Timeout.Milliseconds;
                Socket.SendTimeout = Timeout.Milliseconds;
                
                Socket.NoDelay = true;
                Status = BridgeStatus.Connecting;
                Socket.BeginConnect(address, port, ar =>
                {
                    try
                    {
                        Socket.EndConnect(ar);
                    }
                    catch (SocketException e)
                    {
                        UnityEngine.Debug.Log($"Error connecting to bridge: {e.Message}");
                        Disconnect();
                        return;
                    }
                    Status = BridgeStatus.Connected;
                    lock (QueuedActions)
                    {
                        QueuedActions.Enqueue(FinishConnecting);
                    }

                    Socket.BeginReceive(Temp, 0, Temp.Length, SocketFlags.Partial, EndRead, null);

                }, null);
            }

            void EndRead(IAsyncResult ar)
            {
                int read;
                try
                {
                    read = Socket.EndReceive(ar);
                }
                catch (SocketException e)
                {
                    UnityEngine.Debug.Log($"Error reading from bridge: {e.Message}");
                    Disconnect();
                    return;
                }

                if (read == 0)
                {
                    UnityEngine.Debug.Log($"Bridge socket closed");
                    Disconnect();
                    return;
                }

                Buffer.AddRange(Temp.Take(read));

                int count = Buffer.Count;

                while (count > 0)
                {
                    byte op = Buffer[0];
                    if (op == (byte)Op.Publish)
                    {
                        try
                        {
                            ReceivePublish();
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                            Disconnect();
                            return;
                        }
                    }
                    else
                    {
                        UnityEngine.Debug.Log($"Unknown operation {op} received, disconnecting");
                        Disconnect();
                        return;
                    }

                    if (count == Buffer.Count)
                    {
                        break;
                    }
                    count = Buffer.Count;
                }

                Socket.BeginReceive(Temp, 0, Temp.Length, SocketFlags.Partial, EndRead, null);
            }

            int Get32le(int offset)
            {
                return Buffer[offset + 0] | (Buffer[offset + 1] << 8) | (Buffer[offset + 2] << 16) | (Buffer[offset + 3] << 24);
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
                return;
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

                var channel = System.Text.Encoding.ASCII.GetString(Buffer.Skip(channel_offset).Take(channel_size).ToArray());

                lock (Readers)
                {
                    if (Readers.ContainsKey(channel))
                    {
                        var message_bytes = Buffer.Skip(message_offset).Take(message_size).ToArray();
                        IMessage message = null;
                        foreach (var reader in Readers[channel])
                        {

                            message = reader(message, message_bytes);
                        }
                    }
                    else
                    {
                        UnityEngine.Debug.Log($"Received message on channel '{channel}' which nobody subscribed");
                    }

                }
                Buffer.RemoveRange(0, offset);
                return true;
            }

            enum Op : byte
            {
                RegisterDesc = 1,
                AddReader = 2,
                AddWriter = 3,
                Publish = 4,
            }

            public override void SendAsync(byte[] data, Action completed = null)
            {
                try
                {
                    Socket.BeginSend(data, 0, data.Length, SocketFlags.None, ar =>
                    {
                        try
                        {
                            Socket.EndSend(ar);
                        }
                        catch (SocketException e)
                        {
                            UnityEngine.Debug.Log($"Error writing to bridge: {e.Message}");
                            Disconnect();
                        }
                        if (completed != null)
                            completed();
                    }, null);
                }
                catch (SocketException e)
                {
                    UnityEngine.Debug.Log($"Error writing to bridge: {e.Message}");
                    Disconnect();
                }
            }

            public override void AddReader<T>(string topic, Action<T> callback)
            {
                lock (Readers)
                {
                    if (!Readers.ContainsKey(topic))
                    {
                        Readers.Add(topic, new HashSet<Func<IMessage, byte[], IMessage>>());

                        var channelb = System.Text.Encoding.ASCII.GetBytes(topic);

                        var descriptor = MessageHelper<T>.Descriptor;
                        var typeb = System.Text.Encoding.ASCII.GetBytes(descriptor.FullName);

                        var data = new List<byte>(128);
                        data.Add((byte)Op.AddReader);
                        data.Add((byte)(channelb.Length >> 0));
                        data.Add((byte)(channelb.Length >> 8));
                        data.Add((byte)(channelb.Length >> 16));
                        data.Add((byte)(channelb.Length >> 24));
                        data.AddRange(channelb);
                        data.Add((byte)(typeb.Length >> 0));
                        data.Add((byte)(typeb.Length >> 8));
                        data.Add((byte)(typeb.Length >> 16));
                        data.Add((byte)(typeb.Length >> 24));
                        data.AddRange(typeb);

                        SendAsync(data.ToArray());
                    }

                    Readers[topic].Add((msg, bytes) =>
                    {
                        if (msg == null)
                        {
                            msg = MessageHelper<T>.Parser.ParseFrom(bytes);
                        }
                        lock (QueuedActions)
                        {
                            QueuedActions.Enqueue(() => callback((T)msg));
                        }
                        return msg;
                    });

                    TopicSubscriptions.Add(new Topic()
                    {
                        Name = topic,
                        Type = typeof(T).ToString(),
                    });
                }
            }

            public override Writer<T> AddWriter<T>(string topic)
            {
                var descriptor = MessageHelper<T>.Descriptor;

                var descriptors = new List<byte[]>();
                GetDescriptors(descriptors, descriptor.File);

                int count = descriptors.Count;

                var data = new List<byte>(4096);
                data.Add((byte)Op.RegisterDesc);
                data.Add((byte)(count >> 0));
                data.Add((byte)(count >> 8));
                data.Add((byte)(count >> 16));
                data.Add((byte)(count >> 24));
                foreach (var s in descriptors)
                {
                    int bytes = s.Length;
                    data.Add((byte)(bytes >> 0));
                    data.Add((byte)(bytes >> 8));
                    data.Add((byte)(bytes >> 16));
                    data.Add((byte)(bytes >> 24));
                    data.AddRange(s);
                }

                var channel = System.Text.Encoding.ASCII.GetBytes(topic);

                var typeb = System.Text.Encoding.ASCII.GetBytes(descriptor.FullName);

                data.Add((byte)Op.AddWriter);
                data.Add((byte)(channel.Length >> 0));
                data.Add((byte)(channel.Length >> 8));
                data.Add((byte)(channel.Length >> 16));
                data.Add((byte)(channel.Length >> 24));
                data.AddRange(channel);
                data.Add((byte)(typeb.Length >> 0));
                data.Add((byte)(typeb.Length >> 8));
                data.Add((byte)(typeb.Length >> 16));
                data.Add((byte)(typeb.Length >> 24));
                data.AddRange(typeb);

                TopicPublishers.Add(new Topic()
                {
                    Name = topic,
                    Type = typeof(T).ToString(),
                });

                SendAsync(data.ToArray());

                return new CyberWriter<T>(this, topic);
            }

            public override void AddService<Args, Result>(string service, Func<Args, Result> callback)
            {
                UnityEngine.Debug.Log("AddService is not implemented in Cyber.");
            }

            void GetDescriptors(List<byte[]> descriptors, FileDescriptor descriptor)
            {
                foreach (var dependency in descriptor.Dependencies)
                {
                    GetDescriptors(descriptors, dependency);
                }
                descriptors.Add(descriptor.SerializedData.ToByteArray());
            }
        }

        class MessageHelper<T>
        {
            public static readonly MessageDescriptor Descriptor =
                typeof(T).GetProperty("Descriptor", BindingFlags.Static | BindingFlags.Public).GetValue(null) as MessageDescriptor;
            public static readonly MessageParser Parser =
                typeof(T).GetProperty("Parser", BindingFlags.Static | BindingFlags.Public).GetValue(null) as MessageParser;
        }
    }
}
