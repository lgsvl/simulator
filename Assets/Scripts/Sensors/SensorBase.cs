/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using Simulator.Bridge;
using Simulator.Utilities;

namespace Simulator.Sensors
{
    public abstract class SensorBase : MonoBehaviour
    {
        public string Name;

        [SensorParameter]
        public string Topic;
        [SensorParameter]
        public string Frame;

        public abstract void OnBridgeSetup(IBridge bridge);
    }
}
