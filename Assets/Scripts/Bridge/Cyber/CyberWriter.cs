/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Text;
using System.Collections.Generic;

namespace Simulator.Bridge.Cyber
{
    class CyberWriter<BridgeType>
    {
        CyberBridgeInstance Instance;
        byte[] Topic;

        public CyberWriter(CyberBridgeInstance instance, string topic)
        {
            Instance = instance;
            Topic = Encoding.ASCII.GetBytes(topic);
        }

        public void Write(BridgeType message, Action completed)
        {
            byte[] msg = CyberSerialization.Serialize(message);

            var data = new List<byte>(1 + 4 + Topic.Length + 4 + msg.Length);
            data.Add((byte)BridgeOp.Publish);
            data.Add((byte)(Topic.Length >> 0));
            data.Add((byte)(Topic.Length >> 8));
            data.Add((byte)(Topic.Length >> 16));
            data.Add((byte)(Topic.Length >> 24));
            data.AddRange(Topic);
            data.Add((byte)(msg.Length >> 0));
            data.Add((byte)(msg.Length >> 8));
            data.Add((byte)(msg.Length >> 16));
            data.Add((byte)(msg.Length >> 24));
            data.AddRange(msg);

            Instance.SendAsync(data.ToArray(), completed);
        }
    }
}
