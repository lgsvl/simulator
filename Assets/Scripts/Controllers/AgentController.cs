/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using Simulator;
using Simulator.Sensors;

public abstract class AgentController : MonoBehaviour
{
    public abstract void ResetPosition();
    public abstract void ResetSavedPosition(Vector3 pos, Quaternion rot);
    public abstract void Init();

    public bool Active { get; set; }
    public AgentConfig Config { get; set; }
    public virtual SensorsController AgentSensorsController { get; set; }
    public uint GTID { get; set; }
    public abstract Vector3 Velocity { get; }
    public abstract Vector3 Acceleration { get; }

    public List<SensorBase> AgentSensors = new List<SensorBase>();

    public event Action<AgentController> SensorsChanged;

    protected void OnSensorsChanged()
    {
        SensorsChanged?.Invoke(this);
    }
}
