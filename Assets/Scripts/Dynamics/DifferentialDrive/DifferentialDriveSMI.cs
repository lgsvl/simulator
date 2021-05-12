/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
*/

using UnityEngine;

public class DifferentialDriveSMI : MonoBehaviour, IVehicleDynamics
{
    private ArticulationBody AB;
    public Vector3 Velocity => AB.velocity;
    public Vector3 AngularVelocity => AB.angularVelocity;
    public Transform BaseLink { get; set; }
    public float AccellInput { get; set; } = 0f;
    public float SteerInput { get; set; } = 0f;
    public bool HandBrake { get; set; } = false;
    public float CurrentRPM { get; set; } = 0f;
    public float CurrentGear { get; set; } = 1f;
    public bool Reverse { get; set; } = false;
    public float WheelAngle { get; set; } = 0f;
    public float Speed { get; set; } = 0f;

    public float MaxSteeringAngle { get; set; } = 0f;
    public IgnitionStatus CurrentIgnitionStatus { get; set; } = IgnitionStatus.On;

    private RobotController RobotController;

    public float WheelSeparation = 0;
    public float WheelRadius = 0;
    private float DivideWheelRadius = 0.0f;
    public ArticulationBody RightWheel;
    public ArticulationBody LeftWheel;

    public enum VelocityType { LinearAndAngular, LeftAndRight };
    public VelocityType ControlType = VelocityType.LinearAndAngular;

    public float TargetLinearVelocity;
    public float TargetAngularVelocity;

    private Motor LeftMotor = null;
    private Motor RightMotor = null;

    public float P_Gain = 1.0f;
    public float I_Gain = 0.05f;
    public float D_Gain = 0.0f;

    private float LastTheta = 0.0f;
    private Vector3 OdomPose = Vector3.zero;
    private Vector2 OdomVelocity = Vector2.zero;

    private Vector3 ImuInitialRotation = Vector3.zero;
    private Quaternion ImuOrientation = Quaternion.identity;
    private Vector3 ImuAngularVelocity = Vector3.zero;
    private Vector3 ImuLinearAcceleration = Vector3.zero;

    private Vector3 PreviousImuPosition = Vector3.zero;
    private Vector3 PreviousImuRotation = Vector3.zero;
    private Vector3 PreviousLinearVelocity = Vector3.zero;

    private void Awake()
    {
        AB = GetComponent<ArticulationBody>();

        if (RightWheel == null || LeftWheel == null)
        {
            Debug.LogWarning("wheels not set in DifferentialDriveSMI!");
            return;
        }

        if (WheelSeparation == 0)
        {
            WheelSeparation = (RightWheel.transform.position - LeftWheel.transform.position).magnitude;
        }

        if (WheelRadius == 0)
        {
            var capsuleCollider = RightWheel.GetComponentInChildren<CapsuleCollider>();
            var collider = RightWheel.GetComponentInChildren<Collider>();
            if (capsuleCollider != null)
            {
                WheelRadius = capsuleCollider.radius;
            }
            else if (collider != null)
            {
                // FIXME test this branch
                WheelRadius = collider.bounds.extents.x;
            }

            DivideWheelRadius = 1.0f / WheelRadius;
        }

        LeftMotor = LeftWheel.gameObject.AddComponent<Motor>();
        LeftMotor.SetTargetJoint(LeftWheel);
        LeftMotor.SetPID(P_Gain, I_Gain, D_Gain);
        RightMotor = LeftWheel.gameObject.AddComponent<Motor>();
        RightMotor.SetTargetJoint(RightWheel);
        RightMotor.SetPID(P_Gain, I_Gain, D_Gain);

        ImuInitialRotation = transform.rotation.eulerAngles;
    }

    private void FixedUpdate()
    {
        UpdateIMU();
        GetInput();
        UpdateOdom(Time.fixedDeltaTime);
    }

    private void GetInput()
    {
        if (RobotController == null)
        {
            RobotController = GetComponent<RobotController>();
        }

        if (RobotController != null)
        {
            SteerInput = RobotController.SteerInput;
            AccellInput = RobotController.AccelInput;
        }

        TargetAngularVelocity = SteerInput;
        TargetLinearVelocity = AccellInput;
        SetTwistDrive(TargetLinearVelocity, TargetAngularVelocity);
        UpdateMotorFeedback(TargetAngularVelocity);
    }

    public void UpdateMotorFeedback(float linearVelocityLeft, float linearVelocityRight)
    {
        var linearVelocity = (linearVelocityLeft + linearVelocityRight) * 0.5f;
        var angularVelocity = (linearVelocityRight - linearVelocity) / (WheelSeparation * 0.5f);

        UpdateMotorFeedback(angularVelocity);
    }

    public void UpdateMotorFeedback(float angularVelocity)
    {
        LeftMotor.Feedback.SetRotatingTargetVelocity(angularVelocity);
        RightMotor.Feedback.SetRotatingTargetVelocity(angularVelocity);
    }

    public void SetTwistDrive(float linearVelocity, float angularVelocity)
    {
        // m/s, rad/s
        // var linearVelocityLeft = ((2 * linearVelocity) + (angularVelocity * wheelBase)) / (2 * wheelRadius);
        // var linearVelocityRight = ((2 * linearVelocity) + (angularVelocity * wheelBase)) / (2 * wheelRadius);
        var angularCalculation = angularVelocity * WheelSeparation * 0.5f;
        var linearVelocityLeft = linearVelocity - angularCalculation;
        var linearVelocityRight = linearVelocity + angularCalculation;

        SetDifferentialDrive(linearVelocityLeft, linearVelocityRight);
    }
    public void SetDifferentialDrive(float linearVelocityLeft, float linearVelocityRight)
    {
        var angularVelocityLeft = linearVelocityLeft * DivideWheelRadius * Mathf.Rad2Deg;
        var angularVelocityRight = linearVelocityRight * DivideWheelRadius * Mathf.Rad2Deg;

        SetMotorVelocity(angularVelocityLeft, angularVelocityRight);
    }

    private void SetMotorVelocity(float angularVelocityLeft, float angularVelocityRight)
    {
        var isRotating = Mathf.Sign(angularVelocityLeft) != Mathf.Sign(angularVelocityRight);
        LeftMotor.Feedback.SetMotionRotating(isRotating);
        RightMotor.Feedback.SetMotionRotating(isRotating);
        LeftMotor.SetVelocityTarget(angularVelocityLeft);
        RightMotor.SetVelocityTarget(angularVelocityRight);
    }

    public bool UpdateOdom(float duration)
    {
        var angularVelocityLeft = LeftMotor.GetCurrentVelocity();
        var angularVelocityRight = RightMotor.GetCurrentVelocity();

        // Set reversed value due to different direction
        // Left-handed -> Right-handed direction of rotation
        var odomAngularVelocityLeft = -angularVelocityLeft * Mathf.Deg2Rad;
        var odomAngularVelocityRight = -angularVelocityRight * Mathf.Deg2Rad;

        var yaw = ImuOrientation.eulerAngles.y * Mathf.Deg2Rad;
        CalculateOdometry(duration, odomAngularVelocityLeft, odomAngularVelocityRight, yaw);

        LeftMotor.Feedback.SetRotatingVelocity(OdomVelocity.y);
        RightMotor.Feedback.SetRotatingVelocity(OdomVelocity.y);

        return true;
    }

    public bool ForceReset(Vector3 pos, Quaternion rot)
    {
        AB.TeleportRoot(pos, rot);
        AccellInput = 0f;
        SteerInput = 0f;
        TargetAngularVelocity = 0f;
        TargetLinearVelocity = 0f;
        return true;
    }

    public bool GearboxShiftDown()
    {
        return false;
    }

    public bool GearboxShiftUp()
    {
        return false;
    }

    public bool SetHandBrake(bool state)
    {
        return false;
    }

    public bool ShiftFirstGear()
    {
        return false;
    }

    public bool ShiftReverse()
    {
        return false;
    }

    public bool ShiftReverseAutoGearBox()
    {
        return false;
    }

    public bool ToggleHandBrake()
    {
        return false;
    }

    public bool ToggleIgnition()
    {
        return false;
    }

    public bool ToggleReverse()
    {
        return false;
    }

    public void Reset()
    {
        //    imuSensor.Reset();
        OdomVelocity.Set(0, 0);
        OdomPose.Set(0, 0, 0);
        LastTheta = 0.0f;
    }

    /// <summary>Calculate odometry on this robot</summary>
    /// <remarks>rad per second for `theta`</remarks>
    private void CalculateOdometry(float duration, float angularVelocityLeftWheel, float angularVelocityRightWheel, float theta)
    {
        // circumference of wheel [rad] per step time.
        var wheelCircumLeft = angularVelocityLeftWheel * duration;
        var wheelCircumRight = angularVelocityRightWheel * duration;

        var deltaTheta = theta - LastTheta;

        if (deltaTheta > Mathf.PI)
        {
            deltaTheta -= 2 * Mathf.PI;
        }
        else if (deltaTheta < -Mathf.PI)
        {
            deltaTheta += 2 * Mathf.PI;
        }

        // compute odometric pose
        var poseLinear = WheelRadius * (wheelCircumLeft + wheelCircumRight) * 0.5f;
        var halfDeltaTheta = deltaTheta * 0.5f;
        OdomPose.x += poseLinear * Mathf.Cos(OdomPose.z + halfDeltaTheta);
        OdomPose.y += poseLinear * Mathf.Sin(OdomPose.z + halfDeltaTheta);
        OdomPose.z += deltaTheta;

        if (OdomPose.z > Mathf.PI)
        {
            OdomPose.z -= 2 * Mathf.PI;
        }
        else if (OdomPose.z < -Mathf.PI)
        {
            OdomPose.z += 2 * Mathf.PI;
        }

        // compute odometric instantaneouse velocity
        var v = poseLinear / duration; // v = translational velocity [m/s]
        var w = deltaTheta / duration; // w = rotational velocity [rad/s]

        OdomVelocity.x = v;
        OdomVelocity.y = w;

        LastTheta = theta;
    }

    void UpdateIMU()
    {
        // Caculate orientation and acceleration
        var imuRotation = transform.rotation.eulerAngles - ImuInitialRotation;
        ImuOrientation = Quaternion.Euler(imuRotation.x, imuRotation.y, imuRotation.z);

        ImuAngularVelocity.x = Mathf.DeltaAngle(imuRotation.x, PreviousImuRotation.x) / Time.fixedDeltaTime;
        ImuAngularVelocity.y = Mathf.DeltaAngle(imuRotation.y, PreviousImuRotation.y) / Time.fixedDeltaTime;
        ImuAngularVelocity.z = Mathf.DeltaAngle(imuRotation.z, PreviousImuRotation.z) / Time.fixedDeltaTime;

        var currentPosition = transform.position;
        var currentLinearVelocity = (currentPosition - PreviousImuPosition) / Time.fixedDeltaTime;
        ImuLinearAcceleration = (currentLinearVelocity - PreviousLinearVelocity) / Time.fixedDeltaTime;
        ImuLinearAcceleration.y += (-Physics.gravity.y);

        PreviousImuRotation = imuRotation;
        PreviousImuPosition = currentPosition;
        PreviousLinearVelocity = currentLinearVelocity;
    }
}