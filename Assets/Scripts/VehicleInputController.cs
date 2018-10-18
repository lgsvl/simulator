/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;

[RequireComponent(typeof(PedalInputController))]
public class VehicleInputController : MonoBehaviour, Ros.IRosClient
{
    public ROSTargetEnvironment TargetRosEnv;
    static readonly string AUTOWARE_CMD_TOPIC = "/vehicle_cmd";
    static readonly string APOLLO_CMD_TOPIC = "/apollo/control";
    
    //public float angularVelocityScaler = 1.0f;
    //public float linearVelocityScaler = 1.0f;

    public float actualLinVel = 0f; //same unit as target velocity
    public float actualAngVel = 0f; //same unit as target angular velocity

    public float constAccel = 1.0f; //max 1
    public float constSteer = 1.0f; // max 1

    public float autoInputAccel = 0f;
    public float autoSteerAngle = 0f;

    // public float steeringTimeStamp = 0f; // timestamp of the last steering angle
    public float lastTimeStamp = 0f;

    public float throttle { get ; private set; }
    public float brake { get; private set; }

    private float lastAutoUpdate;
    public bool selfDriving = false;

    CarInputController input;
    VehicleController controller;

    KeyboardInputController keyboardInput;
    SteeringWheelInputController steerwheelInput;

    Ros.Bridge Bridge;
    bool underKeyboardControl;
    bool underSteeringWheelControl;
    bool autoBrake;

    void Awake()
    {
        lastAutoUpdate = Time.time;
        controller = GetComponent<VehicleController>();
        input = GetComponent<CarInputController>();
        input[InputEvent.ENABLE_LEFT_TURN_SIGNAL].Press += controller.EnableLeftTurnSignal;
        input[InputEvent.ENABLE_RIGHT_TURN_SIGNAL].Press += controller.EnableRightTurnSignal;
        input[InputEvent.GEARBOX_SHIFT_DOWN].Press += controller.GearboxShiftDown;
        input[InputEvent.GEARBOX_SHIFT_UP].Press += controller.GearboxShiftUp;
        input[InputEvent.ENABLE_HANDBRAKE].Press += controller.EnableHandbrake;
        input[InputEvent.HEADLIGHT_MODE_CHANGE].Press += controller.ChangeHeadlightMode;
        input[InputEvent.TOGGLE_IGNITION].Press += controller.ToggleIgnition;
        input[InputEvent.TOGGLE_CRUISE_MODE].Press += controller.ToggleCruiseMode;        
    }    

    void OnDestroy()
    {
        input[InputEvent.ENABLE_LEFT_TURN_SIGNAL].Press -= controller.EnableLeftTurnSignal;
        input[InputEvent.ENABLE_RIGHT_TURN_SIGNAL].Press -= controller.EnableRightTurnSignal;
        input[InputEvent.GEARBOX_SHIFT_DOWN].Press -= controller.GearboxShiftDown;
        input[InputEvent.GEARBOX_SHIFT_UP].Press -= controller.GearboxShiftUp;
        input[InputEvent.ENABLE_HANDBRAKE].Press -= controller.EnableHandbrake;
        input[InputEvent.HEADLIGHT_MODE_CHANGE].Press -= controller.ChangeHeadlightMode;
        input[InputEvent.TOGGLE_IGNITION].Press -= controller.ToggleIgnition;
        input[InputEvent.TOGGLE_CRUISE_MODE].Press -= controller.ToggleCruiseMode;
    }

    void Update()
    {
        if (keyboardInput == null)
        {
            keyboardInput = input.KeyboardInput;
        }
        if (steerwheelInput == null)
        {
            steerwheelInput = input.SteerWheelInput;
        }

        //Update states
        {
            selfDriving = Time.time - lastAutoUpdate < 0.5f;
            underKeyboardControl = (keyboardInput != null && (keyboardInput.SteerInput != 0.0f || keyboardInput.AccelBrakeInput != 0.0f));
            underSteeringWheelControl = input.HasValidSteeringWheelInput();
        }

        Vector3 simLinVel = controller.RB.velocity;
        Vector3 simAngVel = controller.RB.angularVelocity;

        var projectedLinVec = Vector3.Project(simLinVel, transform.forward);
        actualLinVel = projectedLinVec.magnitude * (Vector3.Dot(simLinVel, transform.forward) > 0 ? 1.0f : -1.0f);

        var projectedAngVec = Vector3.Project(simAngVel, transform.up);
        actualAngVel = projectedAngVec.magnitude * (projectedAngVec.y > 0 ? -1.0f : 1.0f);
    }

    void FixedUpdate()
    {
        float steerInput = input.SteerInput;
        float accelInput = input.AccelBrakeInput;

        var hasWorkingSteerwheel = (steerwheelInput != null && steerwheelInput.available);

        if (!selfDriving || underKeyboardControl) //manual control or keyboard-interrupted self driving
        {
            //grab input values
            if (!selfDriving)
            {
                steerInput = input.SteerInput;
                accelInput = input.AccelBrakeInput;
            }
            else if (underKeyboardControl)
            {
                steerInput = keyboardInput.SteerInput;
                accelInput = keyboardInput.AccelBrakeInput;
            }


            if (underKeyboardControl || underSteeringWheelControl)
            {
                float k = 0.4f + 0.6f * controller.CurrentSpeed / 30.0f;
                accelInput = accelInput < 0.0f ? accelInput : accelInput * Mathf.Min(1.0f, k);
            }
            else
            {
                accelInput = -1;
                steerInput = 0;
            }

            if (hasWorkingSteerwheel)
            {
                steerwheelInput.SetAutonomousForce(0);
            }
        }
        else if (selfDriving && !underKeyboardControl) //autonomous control(uninterrupted)
        {
            if (hasWorkingSteerwheel)
            {
                var diff = Mathf.Abs(autoSteerAngle - steerInput);
                if (autoSteerAngle < steerInput) // need to steer left
                {
                    steerwheelInput.SetAutonomousForce((int)(diff * 10000));
                }
                else
                {
                    steerwheelInput.SetAutonomousForce((int)(-diff * 10000));
                }
            }

            //use autonomous command values
            if (!hasWorkingSteerwheel || steerwheelInput.autonomousBehavior == SteerWheelAutonomousFeedbackBehavior.OutputOnly || steerwheelInput.autonomousBehavior == SteerWheelAutonomousFeedbackBehavior.None)
            {
                accelInput = autoInputAccel;
                steerInput = autoSteerAngle;
            }
            else
            {
                //purpose of this is to use steering wheel as input even when in self-driving state
                accelInput += autoInputAccel;
                steerInput += autoSteerAngle;
            }                 
        }

        if (controller.driveMode == DriveMode.Controlled)
        {
            controller.accellInput = accelInput;
        }
        controller.steerInput = steerInput;
    }

    public void OnRosBridgeAvailable(Ros.Bridge bridge)
    {
        Bridge = bridge;
        bridge.AddPublisher(this);
    }

    public void OnRosConnected()
    {
        if (TargetRosEnv == ROSTargetEnvironment.AUTOWARE)
        {
            Bridge.Subscribe(AUTOWARE_CMD_TOPIC, (System.Action<Ros.VehicleCmd>)((Ros.VehicleCmd msg) =>
            {
                lastAutoUpdate = Time.time;
                var targetLinear = (float)msg.twist_cmd.twist.linear.x;
                var targetAngular = (float)msg.twist_cmd.twist.angular.z;

                if (!underKeyboardControl)
                {
                    var linMag = Mathf.Abs(targetLinear - actualLinVel);
                    if (actualLinVel < targetLinear && !controller.InReverse)
                    {
                        autoInputAccel = Mathf.Clamp(linMag, 0, constAccel);
                    }
                    else if (actualLinVel > targetLinear && !controller.InReverse)
                    {
                        autoInputAccel = -Mathf.Clamp(linMag, 0, constAccel);
                    }
                    autoSteerAngle = -Mathf.Clamp(targetAngular * 0.5f, -constSteer, constSteer);
                }
            }));
        }
        else if (TargetRosEnv == ROSTargetEnvironment.APOLLO)
        {
            Bridge.Subscribe<Ros.control_command>(APOLLO_CMD_TOPIC, (System.Action<Ros.control_command>)(msg =>
            {
                lastAutoUpdate = Time.time;
                var pedals = GetComponent<PedalInputController>();
                throttle = pedals.throttleInputCurve.Evaluate((float) msg.throttle/100);
                brake = pedals.brakeInputCurve.Evaluate((float) msg.brake/100);
                var linearAccel = throttle - brake;

                var timeStamp = (float) msg.header.timestamp_sec; 
                
                var steeringTarget = -((float) msg.steering_target) / 100;
                var dt = timeStamp - lastTimeStamp;
                lastTimeStamp = timeStamp;

                var steeringAngle = controller.steerInput;

                var sgn = Mathf.Sign(steeringTarget - steeringAngle);
                var steeringRate = (float) msg.steering_rate* sgn;

                steeringAngle += steeringRate* dt;

                // to prevent oversteering
                if (sgn != steeringTarget - steeringAngle) steeringAngle = steeringTarget;

                if (!underKeyboardControl)
                {
                    autoSteerAngle = steeringAngle;
                    autoInputAccel = linearAccel;
                }
            }));
        }
    }
}
