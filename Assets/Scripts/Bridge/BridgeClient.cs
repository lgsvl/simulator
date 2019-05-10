/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using System.Collections.Generic;

namespace Simulator.Bridge
{
    public interface BridgeClient
    {
        void GetSensors(List<Component> sensors);
        void OnBridgeAvailable(BridgeBase bridge);
    }
}
