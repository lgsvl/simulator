/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
*/

using UnityEngine;

public class RobotSMI : MonoBehaviour, IVehicleDynamics
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

    public ArticulationBody RightWheel;
    public ArticulationBody LeftWheel;

    private void Awake()
    {
        AB = GetComponent<ArticulationBody>();
    }

    private void FixedUpdate()
    {
        GetInput();
        ApplySteer();
        ApplyTorque();
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

        if (SteerInput == 0 && AccellInput == 0)
        {
            var drive = LeftWheel.yDrive;
            drive.damping = 0f;
            drive.targetVelocity = 0f;
            LeftWheel.yDrive = drive;

            drive = RightWheel.yDrive;
            drive.damping = 0f;
            drive.targetVelocity = 0f;
            RightWheel.yDrive = drive;
        }
    }

    private void ApplySteer()
    {
        if (SteerInput > 0)
        {
            var drive = LeftWheel.yDrive;
            drive.damping = SteerInput;
            drive.targetVelocity = -300f;
            LeftWheel.yDrive = drive;

            drive = RightWheel.yDrive;
            drive.damping = 0f;
            drive.targetVelocity = 0f;
            RightWheel.yDrive = drive;
        }
        else if (SteerInput < 0)
        {
            var drive = LeftWheel.yDrive;
            drive.damping = 0f;
            drive.targetVelocity = 0f;
            LeftWheel.yDrive = drive;

            drive = RightWheel.yDrive;
            drive.damping = -SteerInput;
            drive.targetVelocity = -300f;
            RightWheel.yDrive = drive;
        }
    }

    private void ApplyTorque()
    {
        if (AccellInput > 0)
        {
            var drive = LeftWheel.yDrive;
            drive.damping = AccellInput;
            drive.targetVelocity = -300f;
            LeftWheel.yDrive = drive;

            drive = RightWheel.yDrive;
            drive.damping = AccellInput;
            drive.targetVelocity = -300f;
            RightWheel.yDrive = drive;
        }
        else if (AccellInput < 0)
        {
            var drive = LeftWheel.yDrive;
            drive.damping = AccellInput;
            drive.targetVelocity = 300f;
            LeftWheel.yDrive = drive;

            drive = RightWheel.yDrive;
            drive.damping = AccellInput;
            drive.targetVelocity = 300f;
            RightWheel.yDrive = drive;
        }
    }

    public bool ForceReset(Vector3 pos, Quaternion rot)
    {
        AB.TeleportRoot(pos, rot);
        var drive = new ArticulationDrive()
        {
            damping = 0f,
            targetVelocity = 0f
        };
        LeftWheel.yDrive = drive;
        RightWheel.yDrive = drive;
        AccellInput = 0f;
        SteerInput = 0f;
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
}
