/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Bridge
{
    // implement this interface in sensor plugin to be able to
    // communicate with custom types to bridge
    public interface ISensorBridgePlugin
    {
        void Register(IBridgePlugin plugin);
    }
}
