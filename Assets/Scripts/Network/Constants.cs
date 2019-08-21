/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network
{
    enum State
    {
        Initial,
        Connecting, // waiting from "init" command
        Connected,  // init" command is received
        Loading,    // client is loading bundles
        Ready,      // client finished all bundle loading
        Running,    // simulation is running
    }

    public static class Constants
    {
        public const string ConnectionKey = "simulator"; // TODO: this can be unique per run
        public const int Port = 9999;
    };
}
