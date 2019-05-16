/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;

public enum DriveMode { Controlled, Cruise }

public class VehicleController : AgentController
{
    private VehicleDynamics vehicleDynamics;
    private SimulatorControls controls;

    public string vehicleName;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private float keyboardAccelSensitivity = 3f;
    private float keyboardSteerSensitivity = 0.3f;

    private Vector2 directionInput;

    public float accelInput { get; private set; } = 0f;
    public float steerInput { get; private set; } = 0f;
    
    public DriveMode driveMode { get; private set; } = DriveMode.Controlled;

    // api do not remove
    private bool sticky = false;
    private float stickySteering;
    private float stickAcceleraton;
    
    private void Awake() // TODO start?
    {
        vehicleDynamics = GetComponent<VehicleDynamics>();
        controls = SimulatorManager.Instance.controls;
        controls.Vehicle.Direction.started += ctx => directionInput = ctx.ReadValue<Vector2>();
        controls.Vehicle.Direction.performed += ctx => directionInput = ctx.ReadValue<Vector2>();
        controls.Vehicle.Direction.cancelled += ctx => directionInput = Vector2.zero;
    }

    private void OnEnable()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    public void Update()
    {
        if (sticky)
        {
            steerInput = stickySteering;
            accelInput = stickAcceleraton;
        }
        else
        {
            steerInput = directionInput.x;
            accelInput = directionInput.y;
        }
        //UpdateWipers();
    }

    public void FixedUpdate()
    {
        // apply turn signal logic
        //const float turnSignalTriggerThreshold = 0.2f;
        //const float turnSignalOffThreshold = 0.1f;

        //if (leftTurnSignal)
        //{
        //    if (resetTurnSignal)
        //    {
        //        if (steerInput > -turnSignalOffThreshold)
        //        {
        //            leftTurnSignal = false;
        //        }
        //    }
        //    else
        //    {
        //        if (steerInput < -turnSignalTriggerThreshold)
        //        {
        //            // "click"
        //            resetTurnSignal = true;
        //        }
        //    }
        //}

        //if (rightTurnSignal)
        //{
        //    if (resetTurnSignal)
        //    {
        //        if (steerInput < turnSignalOffThreshold)
        //        {
        //            rightTurnSignal = false;
        //        }
        //    }
        //    else
        //    {
        //        if (steerInput > turnSignalTriggerThreshold)
        //        {
        //            // "click"
        //            resetTurnSignal = true;
        //        }
        //    }
        //}
    }

    private void OnDestroy()
    {
        //AnalyticsManager.Instance?.TotalMileageEvent(Mathf.RoundToInt(odometer * 0.00062137f));
    }
    
    public void ApplyCruiseControl()
    {
        if (driveMode != DriveMode.Cruise) return;

        accelInput = Mathf.Clamp((vehicleDynamics.currentSpeed - vehicleDynamics.cruiseTargetSpeed) * Time.deltaTime * 20f, -1f, 1f); // TODO set cruiseTargetSpeed on toggle what is magic number 20f?
    }

    public void ToggleCruiseMode()
    {
        driveMode = driveMode == DriveMode.Controlled ? DriveMode.Cruise : DriveMode.Controlled;
    }

    public override void ResetPosition()
    {
        if (vehicleDynamics == null) return;

        vehicleDynamics.rb.position = initialPosition;
        vehicleDynamics.rb.rotation = initialRotation;
        vehicleDynamics.rb.angularVelocity = Vector3.zero;
        vehicleDynamics.rb.velocity = Vector3.zero;
    }

    public override void ResetSavedPosition(Vector3 pos, Quaternion rot)
    {
        if (vehicleDynamics == null) return;

        vehicleDynamics.rb.position = pos == Vector3.zero ? initialPosition : pos;
        vehicleDynamics.rb.rotation = rot == Quaternion.identity ? initialRotation : rot;
        vehicleDynamics.rb.velocity = Vector3.zero;
        vehicleDynamics.rb.angularVelocity = Vector3.zero;
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
