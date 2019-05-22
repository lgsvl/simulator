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

    public float accelInput { get; private set; } = 0f;
    public float steerInput { get; private set; } = 0f;

    private float turnSignalTriggerThreshold = 0.2f;
    private float turnSignalOffThreshold = 0.1f;
    private bool isResetTurnIndicator = false;

    public DriveMode driveMode { get; private set; } = DriveMode.Controlled;

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
        //UpdateWipers();
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
            controls.Vehicle.HeadLights.performed += ctx => actions.SetHeadLights();
            controls.Vehicle.IndicatorLeft.performed += ctx => actions.ToggleIndicatorLeftLights();
            controls.Vehicle.IndicatorRight.performed += ctx => actions.ToggleIndicatorRightLights();
            controls.Vehicle.IndicatorHazard.performed += ctx => actions.ToggleIndicatorHazardLights();
            controls.Vehicle.FogLights.performed += ctx => actions.ToggleFogLights();
            controls.Vehicle.InteriorLight.performed += ctx => actions.ToggleInteriorLight();
        }
    }

    private void UpdateInput()
    {
        steerInput = directionInput.x;
        accelInput = directionInput.y;
    }

    private void UpdateInputAPI()
    {
        if (!sticky) return;
        
        steerInput = stickySteering;
        accelInput = stickAcceleraton;
    }

    private void UpdateLights()
    {
        // brakes
        if (accelInput < 0)
            actions.SetBrakeLights(true);
        else
            actions.SetBrakeLights(false);

        // reverse
        actions.SetIndicatorReverseLights(dynamics.isReverse);

        // turn indicator reset on turn
        if (actions.isIndicatorLeft)
        {
            if (isResetTurnIndicator)
            {
                if (steerInput > -turnSignalOffThreshold)
                    actions.isIndicatorLeft = isResetTurnIndicator = false;
                
            }
            else
            {
                if (steerInput < -turnSignalTriggerThreshold)
                    isResetTurnIndicator = true;
            }
        }

        if (actions.isIndicatorRight)
        {
            if (isResetTurnIndicator)
            {
                if (steerInput < turnSignalOffThreshold)
                    actions.isIndicatorRight = isResetTurnIndicator = false;
            }
            else
            {
                if (steerInput > turnSignalTriggerThreshold)
                    isResetTurnIndicator = true;
            }
        }
    }

    private void OnDestroy()
    {
        //AnalyticsManager.Instance?.TotalMileageEvent(Mathf.RoundToInt(odometer * 0.00062137f));
    }
    
    public void ApplyCruiseControl()
    {
        if (driveMode != DriveMode.Cruise) return;

        accelInput = Mathf.Clamp((dynamics.currentSpeed - dynamics.cruiseTargetSpeed) * Time.deltaTime * 20f, -1f, 1f); // TODO set cruiseTargetSpeed on toggle what is magic number 20f?
    }

    public void ToggleCruiseMode()
    {
        driveMode = driveMode == DriveMode.Controlled ? DriveMode.Cruise : DriveMode.Controlled;
    }

    public override void ResetPosition()
    {
        if (dynamics == null) return;

        dynamics.rb.position = initialPosition;
        dynamics.rb.rotation = initialRotation;
        dynamics.rb.angularVelocity = Vector3.zero;
        dynamics.rb.velocity = Vector3.zero;
    }

    public override void ResetSavedPosition(Vector3 pos, Quaternion rot)
    {
        if (dynamics == null) return;

        dynamics.rb.position = pos == Vector3.zero ? initialPosition : pos;
        dynamics.rb.rotation = rot == Quaternion.identity ? initialRotation : rot;
        dynamics.rb.velocity = Vector3.zero;
        dynamics.rb.angularVelocity = Vector3.zero;
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
    
    //public void SetWindshiledWiperLevelOff()
    //{
    //    prevWiperStatus = wiperStatus;
    //    wiperStatus = 0;
    //    // dash ui
    //    ChangeDashState(DashStateTypes.Wiper, wiperStatus);
    //}

    //public void SetWindshiledWiperLevelAuto()
    //{
    //    if (ignitionStatus == IgnitionStatus.Off) return;

    //    prevWiperStatus = wiperStatus;
    //    wiperStatus = 4;
    //}

    //public void SetWindshiledWiperLevelLow()
    //{
    //    if (ignitionStatus == IgnitionStatus.Off) return;

    //    prevWiperStatus = wiperStatus;
    //    wiperStatus = 1;
    //    // dash ui
    //    ChangeDashState(DashStateTypes.Wiper, wiperStatus);
    //}

    //public void SetWindshiledWiperLevelMid()
    //{
    //    if (ignitionStatus == IgnitionStatus.Off) return;

    //    prevWiperStatus = wiperStatus;
    //    wiperStatus = 2;
    //    // dash ui
    //    ChangeDashState(DashStateTypes.Wiper, wiperStatus);
    //}

    //public void SetWindshiledWiperLevelHigh()
    //{
    //    if (ignitionStatus == IgnitionStatus.Off) return;

    //    prevWiperStatus = wiperStatus;
    //    wiperStatus = 3;
    //    // dash ui
    //    ChangeDashState(DashStateTypes.Wiper, wiperStatus);
    //}

    //public void IncrementWiperState()
    //{
    //    if (ignitionStatus == IgnitionStatus.Off) return;

    //    prevWiperStatus = wiperStatus;
    //    wiperStatus = wiperStatus < 3 ? wiperStatus + 1 : 0;
    //    // dash ui
    //    ChangeDashState(DashStateTypes.Wiper, wiperStatus);
    //}

    //public void UpdateWipersAuto()
    //{
    //    if (ignitionStatus == IgnitionStatus.Off) return;

    //    prevWiperStatus = wiperStatus;
    //    animationManager.PlayWiperAnim(0); //temp placeholder code, it should be a auto wiper logic here instead of turning off 
    //}

    //public void UpdateWipersLevel()
    //{
    //    if (prevWiperStatus != wiperStatus)
    //    {
    //        prevWiperStatus = wiperStatus;
    //        if (wiperStatus != 4)
    //        {
    //            animationManager.PlayWiperAnim(wiperStatus);
    //        }
    //    }
    //}

    //void UpdateWipers()
    //{
    //    if (animationManager == null)
    //    {
    //        return;
    //    }

    //    if (animationManager.CanWiperSwitchLevel())
    //    {
    //        if (wiperStatus == 4)
    //        {
    //            UpdateWipersAuto();
    //        }
    //        else
    //        {
    //            UpdateWipersLevel();
    //        }
    //    }
    //}

    //public void EnableLeftTurnSignal()
    //{
    //    if (leftTurnSignal)
    //    {
    //        leftTurnSignal = false;
    //    }
    //    else
    //    {
    //        rightTurnSignal = false;
    //        leftTurnSignal = true;
    //        resetTurnSignal = false;
    //    }
    //}

    //public void EnableRightTurnSignal()
    //{
    //    if (rightTurnSignal)
    //    {
    //        rightTurnSignal = false;
    //    }
    //    else
    //    {
    //        rightTurnSignal = true;
    //        leftTurnSignal = false;
    //        resetTurnSignal = false;
    //    }
    //}

    //public void DisbleTurnSignals()
    //{
    //    leftTurnSignal = false;
    //    rightTurnSignal = false;
    //}

    //public void ChangeHeadlightMode()
    //{
    //    if (ignitionStatus == IgnitionStatus.Off) return;

    //    headlightMode = headlightMode < 2 ? headlightMode + 1 : 0;
    //    headlights.SetMode((LightMode)headlightMode);
    //    headlights.Headlights = headlightMode == 0 ? false : true;
    //}

    //public void ForceHeadlightsOn()
    //{
    //    headlights.Headlights = true;
    //    headlightMode = 1;
    //}

    //public void ForceHeadlightsOff()
    //{
    //    headlights.Headlights = false;
    //    headlightMode = 0;
    //}
}
