/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using Simulator.Bridge.Data;
using Simulator.Utilities;

namespace Simulator.Sensors
{
    [SensorType("Color Camera", new[] { typeof(ImageData) })]
    public class ColorCameraSensor : CameraSensorBase
    {
        // ColorCameraSensor is currently empty.
        // If we introduce features which are specific for ColorCameraSensor
        // (e.g. noise), we can add them here.
    }
}
