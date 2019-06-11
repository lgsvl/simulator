/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using System.Collections.Generic;
using Simulator.Sensors;

public class VehicleController : AgentController
{
    private VehicleDynamics dynamics;
    private VehicleActions actions;
    private ManualControlSensor manual;

    private List<IVehicleInputs> inputs = new List<IVehicleInputs>();

    private string vehicleName;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    public Vector2 DirectionInput { get; set; } = Vector2.zero;
    public float AccelInput { get; set; } = 0f;
    public float SteerInput { get; set; } = 0f;

    private float turnSignalTriggerThreshold = 0.2f;
    private float turnSignalOffThreshold = 0.1f;
    private bool resetTurnIndicator = false;
    
    // api do not remove
    private bool sticky = false;
    private float stickySteering;
    private float stickAcceleraton;

    private void OnEnable()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    public void Update()
    {
        UpdateInput();
        UpdateLights();
    }

    public void FixedUpdate()
    {
        UpdateInputAPI();
    }

    public override void Init()
    {
        vehicleName = transform.root.name;
        dynamics = GetComponent<VehicleDynamics>();
        actions = GetComponent<VehicleActions>();
        manual = GetComponentInChildren<ManualControlSensor>();
        inputs.AddRange(GetComponentsInChildren<IVehicleInputs>());
    }

    private void UpdateInput()
    {
        if (!isActive || sticky) return;

        SteerInput = AccelInput = 0f;
        
        // get all inputs
        foreach (var input in inputs)
        {
            SteerInput += input.SteerInput;
            AccelInput += input.AccelInput;
        }

        // clamp if over
        SteerInput = Mathf.Clamp(SteerInput, -1f, 1f);
        AccelInput = Mathf.Clamp(AccelInput, -1f, 1f);
    }

    private void UpdateInputAPI()
    {
        if (!sticky) return;
        
        SteerInput = stickySteering;
        AccelInput = stickAcceleraton;
    }

    private void UpdateLights()
    {
        if (!isActive) return;
        // brakes
        if (AccelInput < 0)
            actions.BrakeLights = true;
        else
            actions.BrakeLights = false;

        // reverse
        actions.ReverseLights = dynamics.Reverse;

        // turn indicator reset on turn
        if (actions.LeftTurnSignal)
        {
            if (resetTurnIndicator)
            {
                if (SteerInput > -turnSignalOffThreshold)
                    actions.LeftTurnSignal = resetTurnIndicator = false;
                
            }
            else
            {
                if (SteerInput < -turnSignalTriggerThreshold)
                    resetTurnIndicator = true;
            }
        }

        if (actions.RightTurnSignal)
        {
            if (resetTurnIndicator)
            {
                if (SteerInput < turnSignalOffThreshold)
                    actions.RightTurnSignal = resetTurnIndicator = false;
            }
            else
            {
                if (SteerInput > turnSignalTriggerThreshold)
                    resetTurnIndicator = true;
            }
        }
    }

    private void OnDestroy()
    {
        //AnalyticsManager.Instance?.TotalMileageEvent(Mathf.RoundToInt(odometer * 0.00062137f));
    }
    
    public override void ResetPosition()
    {
        if (dynamics == null) return;

        dynamics.ForceReset(initialPosition, initialRotation);
    }

    public override void ResetSavedPosition(Vector3 pos, Quaternion rot)
    {
        if (dynamics == null) return;

        dynamics.ForceReset(pos, rot);
    }

    // api
    public void ApplyControl(bool sticky, float steering, float acceleration)
    {
        this.sticky = sticky;
        stickySteering = steering;
        stickAcceleraton = acceleration;
    }

    public void ResetStickyControl()
    {
        sticky = false;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (SimulatorManager.Instance.Config == null || SimulatorManager.Instance.Config.ApiOnly)
            return;
       
        //Api.ApiManager.Instance.AddCollision(gameObject, collision);
    }
}
