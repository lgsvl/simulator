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

    public float inputAccel = 0f;
    public float steerAngle = 0f;

    // public float steeringTimeStamp = 0f; // timestamp of the last steering angle
    public float lastTimeStamp = 0f;

    public float throttle { get ; private set; }
    public float brake { get; private set; }

    private float lastUpdate;

    CarInputController input;
    VehicleController controller;

    Ros.Bridge Bridge;
    bool underKeyboardControl;

    void Awake()
    {
        lastUpdate = Time.time;
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
        //grab input values
        float steerInput = input.SteerInput;
        float accelInput = input.AccelBrakeInput;

        if (steerInput != 0.0f || accelInput != 0.0f)
        {
            underKeyboardControl = true;

            float k = 0.4f + 0.6f * controller.CurrentSpeed / 30.0f;
            //float steerPow = 1.0f + Mathf.Min(1.0f, k);

            if (controller.driveMode == DriveMode.Controlled)
            {
                controller.accellInput = accelInput < 0.0f ? accelInput : accelInput * Mathf.Min(1.0f, k);
            }
            //convert inputs to torques
            controller.steerInput = steerInput; // Mathf.Sign(steerInput) * Mathf.Pow(Mathf.Abs(steerInput), steerPow);
        }
        else
        {
            underKeyboardControl = false;
        }

        if (input.HasValidSteeringWheelInput())
        {
            underKeyboardControl = true;
        }

        Vector3 simLinVel = controller.GetComponent<Rigidbody>().velocity;
        Vector3 simAngVel = controller.GetComponent<Rigidbody>().angularVelocity;

        var projectedLinVec = Vector3.Project(simLinVel, transform.forward);
        actualLinVel = projectedLinVec.magnitude * (Vector3.Dot(simLinVel, transform.forward) > 0 ? 1.0f : -1.0f);

        var projectedAngVec = Vector3.Project(simAngVel, transform.up);
        actualAngVel = projectedAngVec.magnitude * (projectedAngVec.y > 0 ? -1.0f : 1.0f);
    }

    void FixedUpdate()
    {
        if (!underKeyboardControl)
        {
            float accellInput = 0.0f;
            float steerInput = 0.0f;
            if (Time.time - lastUpdate < 0.5) {
                accellInput = inputAccel;
                steerInput = steerAngle;
            }
            else {
                accellInput = -1;
                steerInput = 0;
            }

            if (controller.driveMode == DriveMode.Controlled)
            {
                controller.accellInput = accellInput;
            }
            controller.steerInput = steerInput;
        }
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
                lastUpdate = Time.time;
                var targetLinear = (float)msg.twist_cmd.twist.linear.x;
                var targetAngular = (float)msg.twist_cmd.twist.angular.z;

                if (!underKeyboardControl)
                {
                    var linMag = Mathf.Abs(targetLinear - actualLinVel);
                    if (actualLinVel < targetLinear && !controller.InReverse)
                    {
                        inputAccel = Mathf.Clamp(linMag, 0, constAccel);
                    }
                    else if (actualLinVel > targetLinear && !controller.InReverse)
                    {
                        inputAccel = -Mathf.Clamp(linMag, 0, constAccel);
                    }
                    steerAngle = -Mathf.Clamp(targetAngular * 0.5f, -constSteer, constSteer);
                }
            }));
        }
        else if (TargetRosEnv == ROSTargetEnvironment.APOLLO)
        {
            Bridge.Subscribe<Ros.control_command>(APOLLO_CMD_TOPIC, (System.Action<Ros.control_command>)(msg =>
            {
                lastUpdate = Time.time;
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
                    steerAngle = steeringAngle;
                    inputAccel = linearAccel;
                }
            }));
        }
    }
}
