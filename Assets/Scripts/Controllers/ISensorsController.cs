/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using Simulator.Web;

public interface ISensorsController
{
    event Action SensorsChanged;

    void SetupSensors(SensorData[] sensors);

    void RemoveSensor(string name);
}
