/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using System.Collections;

public enum DriveMode { Controlled, Cruise }

public class VehicleController : AgentController
{
    private VehicleDynamics dynamics;
    private VehicleActions actions;
    private SimulatorControls controls;

    private string vehicleName;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    //private float keyboardAccelSensitivity = 3f;
    //private float keyboardSteerSensitivity = 0.3f;

    private Vector2 directionInput;

    public float AccelInput { get; set; } = 0f;
    public float SteerInput { get; set; } = 0f;

    private float turnSignalTriggerThreshold = 0.2f;
    private float turnSignalOffThreshold = 0.1f;
    private bool resetTurnIndicator = false;

    public DriveMode DriveMode { get; private set; } = DriveMode.Controlled;
    
    // api do not remove
    private bool sticky = false;
    private float stickySteering;
    private float stickAcceleraton;
    
    private void Awake()
    {
        vehicleName = transform.root.name;
        dynamics = GetComponent<VehicleDynamics>();
        actions = GetComponent<VehicleActions>();
    }

    private void OnEnable()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    public void Update()
    {
        SetupControls();
        UpdateInput();
        UpdateLights();
    }

    public void FixedUpdate()
    {
        UpdateInputAPI();
    }

    private void SetupControls()
    {
        if (FindObjectOfType<SimulatorManager>() == null)
            return;

        if (controls == null)
        {
            controls = SimulatorManager.Instance.controls;
            controls.Vehicle.Direction.started += ctx => directionInput = ctx.ReadValue<Vector2>();
            controls.Vehicle.Direction.performed += ctx => directionInput = ctx.ReadValue<Vector2>();
            controls.Vehicle.Direction.canceled += ctx => directionInput = Vector2.zero;
            controls.Vehicle.ShiftFirst.performed += ctx => dynamics.ShiftFirstGear();
            controls.Vehicle.ShiftReverse.performed += ctx => dynamics.ShiftReverse();
            controls.Vehicle.ParkingBrake.performed += ctx => dynamics.ToggleHandBrake();
            controls.Vehicle.Ignition.performed += ctx => dynamics.ToggleIgnition();
            controls.Vehicle.HeadLights.performed += ctx => actions.IncrementHeadLightState();
            controls.Vehicle.IndicatorLeft.performed += ctx => actions.LeftTurnSignal = !actions.LeftTurnSignal;
            controls.Vehicle.IndicatorRight.performed += ctx => actions.RightTurnSignal = !actions.RightTurnSignal;
            controls.Vehicle.IndicatorHazard.performed += ctx => actions.HazardLights = !actions.HazardLights;
            controls.Vehicle.FogLights.performed += ctx => actions.FogLights = !actions.FogLights;
            controls.Vehicle.InteriorLight.performed += ctx => actions.InteriorLight = !actions.InteriorLight;
        }
    }

    private void UpdateInput()
    {
        if (!isActive) return;
        SteerInput = directionInput.x;
        AccelInput = directionInput.y;
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

    public void ApplyCruiseControl()
    {
        if (DriveMode != DriveMode.Cruise) return;

        AccelInput = Mathf.Clamp((dynamics.CurrentSpeed - dynamics.CruiseTargetSpeed) * Time.deltaTime * 20f, -1f, 1f); // TODO set cruiseTargetSpeed on toggle what is magic number 20f?
    }

    public void ToggleCruiseMode()
    {
        DriveMode = DriveMode == DriveMode.Controlled ? DriveMode.Cruise : DriveMode.Controlled;
    }

    public override void ResetPosition()
    {
        if (dynamics == null) return;

        dynamics.RB.position = initialPosition;
        dynamics.RB.rotation = initialRotation;
        dynamics.RB.angularVelocity = Vector3.zero;
        dynamics.RB.velocity = Vector3.zero;
    }

    public override void ResetSavedPosition(Vector3 pos, Quaternion rot)
    {
        if (dynamics == null) return;

        dynamics.RB.position = pos == Vector3.zero ? initialPosition : pos;
        dynamics.RB.rotation = rot == Quaternion.identity ? initialRotation : rot;
        dynamics.RB.velocity = Vector3.zero;
        dynamics.RB.angularVelocity = Vector3.zero;
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
