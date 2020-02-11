/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Generic;

namespace Simulator.Bridge
{
    public static class BridgeConfig
    {
        public static Dictionary<Type, IDataConverter> bridgeConverters = new Dictionary<Type, IDataConverter>();
    }
}
