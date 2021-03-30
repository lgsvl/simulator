/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using Simulator;
using Simulator.Sensors;

public abstract class AgentController : MonoBehaviour, IAgentController
{
    private ISensorsController sensorsController;
    
    public bool Active { get; set; }
    public bool Enabled
    {
        get => enabled;
        set => enabled = value;
    }

    public GameObject AgentGameObject => gameObject;
    public AgentConfig Config { get; set; }
    public uint GTID { get; set; }
    public virtual Bounds Bounds { get; set; }
    public virtual List<Transform> CinematicCameraTransforms { get; } = new List<Transform>();
    public virtual Transform DriverViewTransform { get; set; }
    public virtual List<SensorBase> AgentSensors { get; } = new List<SensorBase>();

    public ISensorsController AgentSensorsController
    {
        get => sensorsController;
        set
        {
            if (sensorsController == value)
                return;
            if (sensorsController != null)
                sensorsController.SensorsChanged -= SensorsControllerOnSensorsChanged;
            sensorsController = value;
            if (sensorsController != null)
                sensorsController.SensorsChanged += SensorsControllerOnSensorsChanged;
        }
    }
    
    public abstract Vector3 Velocity { get; }
    public abstract Vector3 Acceleration { get; }
    public event Action<IAgentController> SensorsChanged;
    
    #region ITriggerAgent
    Transform ITriggerAgent.AgentTransform => transform;
    float ITriggerAgent.MovementSpeed => Velocity.magnitude;
    #endregion
    
    public abstract void ResetPosition();
    public abstract void ResetSavedPosition(Vector3 pos, Quaternion rot);
    public abstract void Init();

    protected virtual void SensorsControllerOnSensorsChanged()
    {
        SensorsChanged?.Invoke(this);
    }
}
