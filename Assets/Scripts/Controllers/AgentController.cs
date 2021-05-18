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
    [SerializeField]
    private Transform driverViewTransform;
    
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

    public virtual Transform DriverViewTransform
    {
        get => driverViewTransform;
        set => driverViewTransform = value;
    }

    public virtual List<SensorBase> AgentSensors { get; } = new List<SensorBase>();

    public virtual float AccelInput { get; set; }
    public virtual float SteerInput { get; set; }
    public virtual float BrakeInput { get; set; }

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

    public abstract void ApplyControl(bool sticky, float steering, float acceleration);

    public virtual void DisableControl()
    {
        //Disable rigidbodies physics simulations
        var rigidbodies = GetComponentsInChildren<Rigidbody>();
        foreach (var rb in rigidbodies)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.isKinematic = true;
        }
        
        //Disable articulation bodies physics simulations
        var articulationBodies = gameObject.GetComponentsInChildren<ArticulationBody>();
        foreach (var articulationBody in articulationBodies)
        {
            articulationBody.enabled = false;
        }
        
        //Disable controller and dynamics so they will not perform physics updates
        Enabled = false;
        var vehicleDynamics = gameObject.GetComponent<IVehicleDynamics>() as MonoBehaviour;
        if (vehicleDynamics != null)
            vehicleDynamics.enabled = false;
    }

    protected virtual void SensorsControllerOnSensorsChanged()
    {
        SensorsChanged?.Invoke(this);
    }
}
