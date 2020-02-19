/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Shared
{
    public enum SimulationState
    {
        Initial,
        Connecting, // waiting from "init" command
        Connected,  // "init" command is received
        Loading,    // client is loading bundles
        Ready,      // client finished all bundle loading
        Running,    // simulation is running
    }
}
