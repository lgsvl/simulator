/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using Simulator.Bridge;
using Simulator.Utilities;
using Simulator.Sensors.UI;
using System.Collections.Generic;

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
        public abstract void OnVisualize(Visualizer visualizer);
        public abstract void OnVisualizeToggle(bool state);
        public virtual bool CheckVisible(Bounds bounds) => false;
    }
}
