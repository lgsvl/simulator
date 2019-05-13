/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class AxleInfo
{
    public WheelCollider left;
    public WheelCollider right;
    public GameObject leftVisual;
    public GameObject rightVisual;
    public bool motor;
    public bool steering;
    public float brakeBias = 0.5f;

    [System.NonSerialized]
    public WheelHit hitLeft;
    [System.NonSerialized]
    public WheelHit hitRight;
    [System.NonSerialized]
    public bool isGroundedLeft = false;
    [System.NonSerialized]
    public bool isGroundedRight = false;
}

public enum IgnitionStatus { Off, On }
public enum DriveMode { Controlled, Cruise }

public class VehicleController : AgentController
{
    public string vehicleName;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    public List<AxleInfo> axles;
    private int numberOfDrivingWheels;

    public Rigidbody rb { get; private set; }
    public Vector3 centerOfMass = new Vector3 (0f, 0.35f, 0f);
    private Vector3 lastRBPosition;
    public Transform carCenter; // TODO get collider center
    
    [Tooltip("torque at peak of torque curve")]
    private float maxMotorTorque = 450f;

    [Tooltip("torque at max brake")]
    private float maxBrakeTorque = 3000f;

    [Tooltip("steering range is +-maxSteeringAngle")]
    private float maxSteeringAngle = 39.4f;

    [Tooltip("idle rpm")]
    private float minRPM = 800f;

    [Tooltip("max rpm")]
    private float maxRPM = 8299f;

    [Tooltip("gearbox ratios")]
    private float[] gearRatios = new float[] { 4.17f, 3.14f, 2.11f, 1.67f, 1.28f, 1f, 0.84f, 0.67f };
    private float finalDriveRatio = 2.56f;

    [Tooltip("min time between gear changes")]
    private float shiftDelay = 0.7f;

    [Tooltip("time interpolated for gear shift")]
    private float shiftTime = 0.4f;

    [Tooltip("torque curve that gives torque at specific percentage of max RPM")]
    public AnimationCurve rpmCurve;
    [Tooltip("curves controlling whether to shift up at specific rpm, based on throttle position")]
    public AnimationCurve shiftUpCurve;
    [Tooltip("curves controlling whether to shift down at specific rpm, based on throttle position")]
    public AnimationCurve shiftDownCurve;

    //drag coefficients
    private float airDragCoeff = 1.0f;
    private float airDownForceCoeff = 2.0f;
    private float tireDragCoeff = 4.0f;

    [Tooltip("wheel collider damping rate")]
    private float wheelDamping = 1f;

    [Tooltip("autosteer helps the car maintain its heading")]
    [Range(0, 1)]
    private float autoSteerAmount = 0.338f;

    [Tooltip("traction control limits torque based on wheel slip - traction reduced by amount when slip exceeds the tractionControlSlipLimit")]
    [Range(0, 1)]
    private float tractionControlAmount = 0.675f;
    private float tractionControlSlipLimit = 0.8f;

    [Tooltip("how much to smooth out real RPM")]
    private float RPMSmoothness = 20f;

    // combined throttle and brake input
    public float accellInput { get; private set; } = 0f;

    //steering input
    public float steerInput { get; private set; } = 0f;

    //handbrake
    public bool isHandBrake { get; private set; } = false;
    
    public IgnitionStatus ignitionStatus { get; private set; } = IgnitionStatus.On;
    public DriveMode driveMode { get; private set; } = DriveMode.Controlled;
    
    public float MotorWheelsSlip
    {
        get
        {
            float slip = 0f;
            int i = 0;
            foreach (var axle in axles)
            {
                if (axle.motor)
                {
                    i += 2;
                    if (axle.isGroundedLeft)
                        slip += axle.hitLeft.forwardSlip;
                    if (axle.isGroundedRight)
                        slip += axle.hitRight.forwardSlip;
                }
            }
            return slip / i;
        }
    }

    private float cruiseTargetSpeed { get; set; }
    public float currentRPM { get; private set; }
    private float currentSpeed { get; set; } = 0.0f;
    public float currentSpeedMeasured { get; private set; }

    public float cruiseSensitivity { get; private set; } = 1.0f;
    public float cruiseSpeed { get; private set; } = 10f;
    
    private float oldRotation;
    private float tractionControlAdjustedMaxTorque;
    private float currentTorque = 0;

    private const float LOW_SPEED = 5f;
    private const float LARGE_FACING_ANGLE = 50f;

    private float currentGear = 1;
    private int targetGear { get; set; } = 1;
    private int lastGear = 1;
    private bool isShifting = false;
    private float lastShift = 0.0f;
    public bool isReverse { get; private set; }

    private float traction = 0f;
    
    private float odometer { get; set; } = 0.0f;
    private float mileTicker { get; set; }
    private float consumptionTime { get; set; } = 0.0f;
    private float consumptionDistance { get; set; } = 0.0f;
    private float fuelCapacity { get; set; } = 60.0f;
    private float fuelLevel { get; set; } = 60.0f;

    private const float zeroK = 273.15f;
    public float ambientTemperatureK { get; private set; }
    public float engineTemperatureK { get; private set; }
    public bool coolingMalfunction { get; private set; } = false;

    // api do not remove
    private bool sticky = false;
    private float stickySteering;
    private float stickAcceleraton;

    //turn signals
    //public bool leftTurnSignal = false;
    //public bool rightTurnSignal = false;
    //private bool resetTurnSignal = false;

    //windshield wiper speed level
    //public float WiperStatus //Use float here because int doesn't work
    //{
    //    get { return (float)wiperStatus; }
    //    set
    //    {
    //        wiperStatus = (int)System.Math.Round(value);
    //    }
    //}
    //public int wiperStatus = 0; // 0 == off, 1 == low, 2 == mid, 3 == high, 4 == auto ...
    //private int prevWiperStatus = 0;

    //VehicleAnimationManager animationManager;
    //private CarHeadlights headlights;
    //public int headlightMode;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        RecalcDrivingWheels();

        tractionControlAdjustedMaxTorque = maxMotorTorque - (tractionControlAmount * maxMotorTorque);

        axles[0].left.ConfigureVehicleSubsteps(5.0f, 30, 10);
        axles[0].right.ConfigureVehicleSubsteps(5.0f, 30, 10);
        axles[1].left.ConfigureVehicleSubsteps(5.0f, 30, 10);
        axles[1].right.ConfigureVehicleSubsteps(5.0f, 30, 10);

        foreach (var axle in axles)
        {
            axle.left.wheelDampingRate = wheelDamping;
            axle.right.wheelDampingRate = wheelDamping;
        }

        ambientTemperatureK = zeroK + 22.0f; // a pleasant 22° celsius ambient
        engineTemperatureK = zeroK + 22.0f;

        lastRBPosition = rb.position;
    }

    public void Update()
    {
        if (rb.centerOfMass != centerOfMass)
            rb.centerOfMass = centerOfMass;

        if (axles[0].left.wheelDampingRate != wheelDamping)
        {
            foreach (var axle in axles)
            {
                axle.left.wheelDampingRate = wheelDamping;
                axle.right.wheelDampingRate = wheelDamping;
            }
        }

        //update wheel visual component rotations
        foreach (var axle in axles)
        {
            ApplyLocalPositionToVisuals(axle.left, axle.leftVisual);
            ApplyLocalPositionToVisuals(axle.right, axle.rightVisual);
        }
        
        ApplyCruiseControl();

        //UpdateWipers();
    }

    public void FixedUpdate()
    {
        if (sticky)
        {
            steerInput = stickySteering;
            accellInput = stickAcceleraton;
        }

        //air drag (quadratic)
        rb.AddForce(-airDragCoeff * rb.velocity * rb.velocity.magnitude);

        //downforce (quadratic)
        rb.AddForce(-airDownForceCoeff * rb.velocity.sqrMagnitude * transform.up);

        //tire drag (Linear)
        rb.AddForceAtPosition(-tireDragCoeff * rb.velocity, transform.position);

        //calc current gear ratio
        float gearRatio = Mathf.Lerp(gearRatios[Mathf.FloorToInt(currentGear) - 1], gearRatios[Mathf.CeilToInt(currentGear) - 1], currentGear - Mathf.Floor(currentGear));
        if (isReverse)
        {
            gearRatio = -1.0f * gearRatios[0];
            if (driveMode == DriveMode.Cruise)
            {
                ToggleCruiseMode();
            }
        }

        //calc engine RPM from wheel rpm
        float wheelsRPM = (axles[1].right.rpm + axles[1].left.rpm) / 2f;
        if (wheelsRPM < 0)
            wheelsRPM = 0;

        // if the engine is on, the fuel injectors are going to be triggered at minRPM
        // to keep the engine running.  If the engine is OFF, then the engine will eventually
        // go all the way down to 0, because there's nothing keeping it spinning.
        var minPossibleRPM = ignitionStatus == IgnitionStatus.On ? minRPM : 0.0f;
        currentRPM = Mathf.Lerp(currentRPM, minPossibleRPM + (wheelsRPM * finalDriveRatio * gearRatio), Time.fixedDeltaTime * RPMSmoothness);
        // I don't know why, but logging RPM while engine is off and we're not moving, is showing
        // a constant drift between 0.0185 and 0.0192 or so .. so just clamp it down to 0 at that
        // point.
        if (currentRPM < 0.02f)
        {
            currentRPM = 0.0f;
        }

        //find out which wheels are on the ground
        foreach (var axle in axles)
        {
            axle.isGroundedLeft = axle.left.GetGroundHit(out axle.hitLeft);
            axle.isGroundedRight = axle.right.GetGroundHit(out axle.hitRight);
        }

        //convert inputs to torques
        float steer = maxSteeringAngle * steerInput;
        currentTorque = rpmCurve.Evaluate(currentRPM / maxRPM) * gearRatio * finalDriveRatio * tractionControlAdjustedMaxTorque;

        foreach (var axle in axles)
        {
            if (axle.steering)
            {
                axle.left.steerAngle = steer;
                axle.right.steerAngle = steer;
            }
        }

        if (isHandBrake)
        {
            //Make the accellInput negative so that brakes are applied in ApplyTorque()
            accellInput = -1.0f;
        }

        // No autodrive while engine is off.
        if (ignitionStatus == IgnitionStatus.On)
        {
            AutoSteer();
        }

        ApplyTorque();
        TractionControl();

        //shift if need be. No auto shifting while engine is off.
        if (ignitionStatus == IgnitionStatus.On)
        {
            AutoGearBox();
        }

        //record current speed in MPH
        currentSpeed = rb.velocity.magnitude * 2.23693629f;

        float deltaDistance = wheelsRPM / 60.0f * (axles[1].left.radius * 2.0f * Mathf.PI) * Time.fixedDeltaTime;
        odometer += deltaDistance;
        mileTicker += deltaDistance;

        if ((mileTicker * 0.00062137f) > 1)
        {
            mileTicker = 0;
            //AnalyticsManager.Instance?.MileTickEvent();
        }

        /*
        // why does this not work :(
        float currentRPS = currentRPM / 60.0f;

        float accel = Mathf.Max(0.0f, accellInput);
        float angularV = currentRPS * Mathf.PI * 2.0f;

        float power = currentTorque //(rpmCurve.Evaluate(currentRPM / maxRPM) * accel * maxMotorTorque)  //Nm
            * angularV; // Watt

        float energy = power * Time.fixedDeltaTime  // w/s
            / 1000.0f * 3600.0f;                    // kw/h

        print("p:" + power + " e:" + energy);
        //approximation function for
        // https://en.wikipedia.org/wiki/Brake_specific_fuel_consumption#/media/File:Brake_specific_fuel_consumption.svg
        // range ~ 200-400 g/kWh
        float bsfc = (206.0f + Mathf.Sqrt(Mathf.Pow(currentRPM - 2200, 2.0f)
                            + Mathf.Pow((accel - 0.9f) * 10000.0f, 2.0f)) / 80 + currentRPM / 4500); // g/kWh

        float gasolineDensity = 1f / .75f;      // cm^3/g

        float deltaConsumption = bsfc * energy  // g
            * gasolineDensity                   // cm^3
            / 1000.0f;                          // l
        */

        // FIXME fix the more correct method above...
        float deltaConsumption = currentRPM * 0.00000001f
                               + currentTorque * 0.000000001f * Mathf.Max(0.0f, accellInput);

        // if engine is not powered up, or there's zero acceleration, AND we're not idling
        // the engine to keep it on, then the fuel injectors are off, and no fuel is being used
        // idling == non-scientific calculation of "minRPM + 25%".
        if (ignitionStatus != IgnitionStatus.On || (accellInput <= 0.0f && currentRPM > minRPM + (minRPM * 0.25)))
        {
            deltaConsumption = 0.0f;
        }

        consumptionDistance = deltaConsumption / deltaDistance;     // l/m
        consumptionTime = deltaConsumption / Time.fixedDeltaTime;   // l/s

        fuelLevel -= deltaConsumption;

        float engineWeight = 200.0f; // kg
        float energyDensity = 34.2f * 1000.0f * 1000.0f; // J/l fuel
        float specificHeatIron = 448.0f; // J/kg for 1K temperature increase

        engineTemperatureK += (deltaConsumption * energyDensity) / (specificHeatIron * engineWeight);

        float coolFactor = 0.00002f; //ambient exchange
        if (engineTemperatureK > zeroK + 90.0f && !coolingMalfunction)
            coolFactor += 0.00002f + 0.0001f * Mathf.Max(0.0f, currentSpeed); // working temperature reached, start cooling

        engineTemperatureK = Mathf.Lerp(engineTemperatureK, ambientTemperatureK, coolFactor);

        //find current road surface type
        WheelHit hit;
        if (axles[0].left.GetGroundHit(out hit))
        {
            traction = hit.forwardSlip; // ground
            var roadObject = hit.collider.transform.parent == null ? hit.collider.transform : hit.collider.transform.parent;
        }
        else
        {
            traction = 0f; // air
        }

        currentSpeedMeasured = ((rb.position - lastRBPosition) / Time.fixedDeltaTime).magnitude;
        lastRBPosition = rb.position;

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

    public void RecalcDrivingWheels()
    {
        //calculate how many wheels are driving
        numberOfDrivingWheels = axles.Where(a => a.motor).Count() * 2;
    }

    public void GearboxShiftUp()
    {
        if (isReverse)
        {
            isReverse = false;
            return;
        }
        lastGear = Mathf.RoundToInt(currentGear);
        targetGear = lastGear + 1;
        lastShift = Time.time;
        isShifting = true;
    }

    public void GearboxShiftDown()
    {
        if (Mathf.RoundToInt(currentGear) == 1)
        {
            isReverse = true;
            return;
        }

        lastGear = Mathf.RoundToInt(currentGear);
        targetGear = lastGear - 1;
        lastShift = Time.time;
        isShifting = true;
    }

    public void ToggleShift()
    {
        if (isReverse)
        {
            isReverse = false;
        }
        else
        {
            currentGear = 1;
            isReverse = true;
        }
    }

    private void AutoGearBox()
    {
        //check delay so we cant shift up/down too quick
        //FIXME lock gearbox for certain amount of time if user did override
        if (Time.time - lastShift > shiftDelay)
        {
            //shift up
            if (currentRPM / maxRPM > shiftUpCurve.Evaluate(accellInput) && Mathf.RoundToInt(currentGear) < gearRatios.Length)
            {
                //don't shift up if we are just spinning in 1st
                if (Mathf.RoundToInt(currentGear) > 1 || currentSpeed > 15f)
                {
                    GearboxShiftUp();
                }
            }
            //else down
            if (currentRPM / maxRPM < shiftDownCurve.Evaluate(accellInput) && Mathf.RoundToInt(currentGear) > 1)
            {
                GearboxShiftDown();
            }

        }

        if (isShifting)
        {
            float lerpVal = (Time.time - lastShift) / shiftTime;
            currentGear = Mathf.Lerp(lastGear, targetGear, lerpVal);
            if (lerpVal >= 1f)
                isShifting = false;
        }

        //clamp to gear range
        if (currentGear >= gearRatios.Length)
        {
            currentGear = gearRatios.Length - 1;
        }
        else if (currentGear < 1)
        {
            currentGear = 1;
        }
    }

    private void AutoSteer()
    {
        //bail if a wheel isn't on the ground
        foreach (var axle in axles)
        {
            if (axle.isGroundedLeft == false || axle.isGroundedRight == false)
                return;
        }

        var yawRate = oldRotation - transform.eulerAngles.y;

        //don't adjust if the yaw rate is super high
        if (Mathf.Abs(yawRate) < 10f)
            rb.velocity = Quaternion.AngleAxis(yawRate * autoSteerAmount, Vector3.up) * rb.velocity;

        oldRotation = transform.eulerAngles.y;
    }

    private void ApplyTorque()
    {
        // acceleration is ignored when engine is not running, brakes are available still.
        if (accellInput >= 0)
        {
            //motor
            float torquePerWheel = ignitionStatus == IgnitionStatus.On ? accellInput * (currentTorque / numberOfDrivingWheels) : 0f;
            foreach (var axle in axles)
            {
                if (axle.motor)
                {
                    if (axle.isGroundedLeft)
                        axle.left.motorTorque = torquePerWheel;
                    else
                        axle.left.motorTorque = 0f;

                    if (axle.isGroundedRight)
                        axle.right.motorTorque = torquePerWheel;
                    else
                        axle.left.motorTorque = 0f;
                }

                axle.left.brakeTorque = 0f;
                axle.right.brakeTorque = 0f;
            }

        }
        // TODO: to get brake + accelerator working at the same time, modify this area.
        // You'll need to do some work to separate the brake and accel pedal inputs, though.
        // TODO: handBrake should apply full braking to rear axle (possibly all axles), without
        // changing the accelInput
        else
        {
            //brakes
            foreach (var axle in axles)
            {
                var brakeTorque = maxBrakeTorque * accellInput * -1 * axle.brakeBias;
                axle.left.brakeTorque = brakeTorque;
                axle.right.brakeTorque = brakeTorque;
                axle.left.motorTorque = 0f;
                axle.right.motorTorque = 0f;
            }
        }
    }

    private void TractionControl()
    {
        foreach (var axle in axles)
        {
            if (axle.motor)
            {
                if (axle.left.isGrounded)
                    AdjustTractionControlTorque(axle.hitLeft.forwardSlip);

                if (axle.right.isGrounded)
                    AdjustTractionControlTorque(axle.hitRight.forwardSlip);
            }
        }
    }

    private void AdjustTractionControlTorque(float forwardSlip)
    {
        if (forwardSlip >= tractionControlSlipLimit && tractionControlAdjustedMaxTorque >= 0)
        {
            tractionControlAdjustedMaxTorque -= 10 * tractionControlAmount;
            if (tractionControlAdjustedMaxTorque < 0)
                tractionControlAdjustedMaxTorque = 0f;
        }
        else
        {
            tractionControlAdjustedMaxTorque += 10 * tractionControlAmount;
            if (tractionControlAdjustedMaxTorque > maxMotorTorque)
                tractionControlAdjustedMaxTorque = maxMotorTorque;
        }
    }

    public void ToggleIgnition()
    {
        switch (ignitionStatus)
        {
            case IgnitionStatus.Off:
                StartEngine();
                break;
            case IgnitionStatus.On:
                StopEngine();
                break;
        }
    }

    public void StartEngine()
    {
        ignitionStatus = IgnitionStatus.On;
    }

    public void StopEngine()
    {
        ignitionStatus = IgnitionStatus.Off;
    }

    public void ApplyCruiseControl()
    {
        if (driveMode != DriveMode.Cruise) return;

        accellInput = Mathf.Clamp((currentSpeed - cruiseTargetSpeed) * Time.deltaTime * 20f, -1f, 1f); // TODO set cruiseTargetSpeed on toggle what is magic number 20f?
    }

    public void ToggleCruiseMode()
    {
        driveMode = driveMode == DriveMode.Controlled ? DriveMode.Cruise : DriveMode.Controlled;
    }

    public void ToggleHandBrake()
    {
        isHandBrake = !isHandBrake;
    }

    public void SetHandBrake(bool enable)
    {
        isHandBrake = enable;
    }

    public void ForceReset()
    {
        currentGear = 1;
        currentRPM = 0.0f;
        currentSpeed = 0.0f;
        currentTorque = 0.0f;
        accellInput = 0.0f;
    }

    private void ApplyLocalPositionToVisuals(WheelCollider collider, GameObject visual)
    {
        if (visual == null) return;
        
        Vector3 position;
        Quaternion rotation;
        collider.GetWorldPose(out position, out rotation);

        visual.transform.position = position;
        visual.transform.rotation = rotation;
    }

    public override void ResetPosition()
    {
        rb.position = initialPosition;
        rb.rotation = initialRotation;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    public override void ResetSavedPosition(Vector3 pos, Quaternion rot)
    {
        rb.position = pos == Vector3.zero ? initialPosition : pos;
        rb.rotation = rot == Quaternion.identity ? initialRotation : rot;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
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
        if (SimulatorManager.Instance.currentMode != StartModeTypes.API)
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
