/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Simulator.Bridge;
using Simulator.Sensors.UI;
using Simulator.Utilities;

namespace Simulator.Sensors
{
    [SensorType("Transform", new System.Type[] { })]
    public class TransformSensor : SensorBase
    {
	    public override SensorDistributionType DistributionType => SensorDistributionType.LowLoad;
	    
        public override void OnBridgeSetup(IBridge bridge)
        {
        }

        public override void OnVisualize(Visualizer visualizer)
        {
        }

        public override void OnVisualizeToggle(bool state)
        {
        }
    }
}