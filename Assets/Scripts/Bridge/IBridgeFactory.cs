/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Generic;

namespace Simulator.Bridge
{
    public interface IBridgeFactory
    {
        string Name { get; }
        IEnumerable<Type> SupportedDataTypes { get; }
        IBridge Create();
    }
}
