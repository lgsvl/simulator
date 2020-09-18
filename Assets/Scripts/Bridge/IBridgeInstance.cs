/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Bridge
{
    public enum Status
    {
        Disconnected,
        UnexpectedlyDisconnected,
        Connecting,
        Connected,
        Disconnecting,
    }

    public interface IBridgeInstance
    {
        // return current status of bridge (connected, disconnected, ... etc)
        // this will be queried every frame to update bridge status
        // bridge can change it any time to Disconnected when connection drops, or
        // for any other reason (e.g. error happens)
        Status Status { get; }

        // start new connection to bridge
        // this method should be non-blocking and perform connection in background
        // Status should be changed to Connecting (or Connected)
        void Connect(string connection);

        // disconnect currenct connection (if connected)
        // this method should be non-blocking and perform disconnection in background
        // Status should be changed to Disconnecting (or Disconnected)
        void Disconnect();
    }
}
