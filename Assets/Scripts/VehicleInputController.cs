/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PedalInputController))]
public class VehicleInputController : MonoBehaviour, Comm.BridgeClient
{
    public ROSTargetEnvironment TargetRosEnv;
    static readonly string AUTOWARE_CMD_TOPIC = "/vehicle_cmd";
    public static readonly string APOLLO_CMD_TOPIC = "/apollo/control";
    static readonly string LANEFOLLOWING_CMD_TOPIC = "/lanefollowing/steering_cmd";
    static readonly string SIMULATOR_CMD_TOPIC = "/simulator/control/command";
    
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
    public VehicleController controller { get; private set; }

    KeyboardInputController keyboardInput;
    SteeringWheelInputController steerwheelInput;

    Comm.Bridge Bridge;
    Comm.Writer<Ros.TwistStamped> LgsvlSimulatorCmdWriter;
    public bool underKeyboardControl;
    bool underSteeringWheelControl;
    bool autoBrake;

    uint seq;
    bool isControlEnabled = false;

    void Awake()
    {
        AddUIElement();
        lastAutoUpdate = Time.time;
        controller = GetComponent<VehicleController>();
        input = GetComponent<CarInputController>();
        input[InputEvent.ENABLE_LEFT_TURN_SIGNAL].Press += controller.EnableLeftTurnSignal;
        input[InputEvent.ENABLE_RIGHT_TURN_SIGNAL].Press += controller.EnableRightTurnSignal;
        input[InputEvent.GEARBOX_SHIFT_DOWN].Press += controller.GearboxShiftDown;
        input[InputEvent.GEARBOX_SHIFT_UP].Press += controller.GearboxShiftUp;
        input[InputEvent.TOGGLE_SHIFT].Press += controller.ToggleShift;
        input[InputEvent.ENABLE_HANDBRAKE].Press += controller.ToggleHandBrake;
        input[InputEvent.HEADLIGHT_MODE_CHANGE].Press += controller.ChangeHeadlightMode;
        input[InputEvent.TOGGLE_IGNITION].Press += controller.ToggleIgnition;
        input[InputEvent.TOGGLE_CRUISE_MODE].Press += controller.ToggleCruiseMode;
        input[InputEvent.SET_WIPER_OFF].Release += controller.SetWindshiledWiperLevelOff;
        input[InputEvent.SET_WIPER_AUTO].Release += controller.SetWindshiledWiperLevelAuto;
        input[InputEvent.SET_WIPER_LOW].Release += controller.SetWindshiledWiperLevelLow;
        input[InputEvent.SET_WIPER_MID].Release += controller.SetWindshiledWiperLevelMid;
        input[InputEvent.SET_WIPER_HIGH].Release += controller.SetWindshiledWiperLevelHigh;
        input[InputEvent.TOGGLE_WIPER].Press += controller.IncrementWiperState;

        // CES
        // ???
        //input[InputEvent.ACCEL].Release += controller.ToggleBrakeLightsOff;
        //input[InputEvent.BRAKE].Release += controller.ToggleBrakeLightsOn;
    }

    void OnDestroy()
    {
        input[InputEvent.ENABLE_LEFT_TURN_SIGNAL].Press -= controller.EnableLeftTurnSignal;
        input[InputEvent.ENABLE_RIGHT_TURN_SIGNAL].Press -= controller.EnableRightTurnSignal;
        input[InputEvent.GEARBOX_SHIFT_DOWN].Press -= controller.GearboxShiftDown;
        input[InputEvent.GEARBOX_SHIFT_UP].Press -= controller.GearboxShiftUp;
        input[InputEvent.TOGGLE_SHIFT].Press -= controller.ToggleShift;
        input[InputEvent.ENABLE_HANDBRAKE].Press -= controller.ToggleHandBrake;
        input[InputEvent.HEADLIGHT_MODE_CHANGE].Press -= controller.ChangeHeadlightMode;
        input[InputEvent.TOGGLE_IGNITION].Press -= controller.ToggleIgnition;
        input[InputEvent.TOGGLE_CRUISE_MODE].Press -= controller.ToggleCruiseMode;
        input[InputEvent.SET_WIPER_OFF].Release -= controller.SetWindshiledWiperLevelOff;
        input[InputEvent.SET_WIPER_AUTO].Release -= controller.SetWindshiledWiperLevelAuto;
        input[InputEvent.SET_WIPER_LOW].Release -= controller.SetWindshiledWiperLevelLow;
        input[InputEvent.SET_WIPER_MID].Release -= controller.SetWindshiledWiperLevelMid;
        input[InputEvent.SET_WIPER_HIGH].Release -= controller.SetWindshiledWiperLevelHigh;
        input[InputEvent.TOGGLE_WIPER].Press -= controller.IncrementWiperState;
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
            underKeyboardControl = (keyboardInput != null && (keyboardInput.SteerInput != 0.0f || keyboardInput.isKeyboardAccelBrake));
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

        var hasWorkingSteerwheel = (steerwheelInput != null && SteeringWheelInputController.available);

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

        if (isControlEnabled == true)  // Publish control command for training
        {
            var simControl = new Ros.TwistStamped()
            {
                header = new Ros.Header()
                {
                    stamp = Ros.Time.Now(),
                    seq = seq++,
                    frame_id = "",
                },
                twist = new Ros.Twist()
                {
                    linear = new Ros.Vector3()
                    {
                        x = controller.accellInput,
                        y = 0,
                        z = 0,
                    },
                    angular = new Ros.Vector3()
                    {
                        x = controller.steerInput,
                        y = 0,
                        z = 0,
                    }
                }
            };
            LgsvlSimulatorCmdWriter.Publish(simControl);
        }
    }

    public void GetSensors(List<Component> sensors)
    {
        // this is not a sensor
    }

    public void OnBridgeAvailable(Comm.Bridge bridge)
    {
        Bridge = bridge;
        Bridge.OnConnected += () =>
        {
            if (TargetRosEnv == ROSTargetEnvironment.AUTOWARE)
            {
                Bridge.AddReader(AUTOWARE_CMD_TOPIC, (System.Action<Ros.VehicleCmd>)((Ros.VehicleCmd msg) =>
                {
                    lastAutoUpdate = Time.time;

                    bool pub_ctrl_cmd = false;
                    bool pub_gear_cmd = false;

                    var gearCmd = msg.gear;
                    if (gearCmd != 0) pub_gear_cmd = true;

                    var ctrlCmd_linVel = msg.ctrl_cmd.linear_velocity;
                    var ctrlCmd_linAcc = msg.ctrl_cmd.linear_acceleration;
                    var ctrlCmd_steerAng = msg.ctrl_cmd.steering_angle;

                    if (ctrlCmd_linAcc == 0 && ctrlCmd_linVel == 0 && ctrlCmd_steerAng == 0) pub_ctrl_cmd = false;
                    else pub_ctrl_cmd = true;

                    if (!pub_ctrl_cmd && !pub_gear_cmd)
                    {
                        // using twist_cmd to control ego vehicle
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
                    }
                    else
                    {
                        // using gear and ctrl_cmd to control ego vehicle
                        if (gearCmd == 64)
                        {
                            controller.GearboxShiftDown();
                        }
                        else // Switch to "Drive" for anything but "Reverse"
                        {
                            controller.GearboxShiftUp();
                        }

                        if (!underKeyboardControl)
                        {
                            // ignoring the control linear velocity for now.
                            autoSteerAngle = (float)ctrlCmd_steerAng; // angle should be in degrees
                            autoInputAccel = Mathf.Clamp((float)ctrlCmd_linAcc, -1, 1);
                        }
                    }
                }));
            }
            else if (TargetRosEnv == ROSTargetEnvironment.APOLLO)
            {
                Bridge.AddReader<Ros.control_command>(APOLLO_CMD_TOPIC, (System.Action<Ros.control_command>)(msg =>
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
            else if (TargetRosEnv == ROSTargetEnvironment.LGSVL)
            {
                Bridge.AddReader<Ros.TwistStamped>(LANEFOLLOWING_CMD_TOPIC, (System.Action<Ros.TwistStamped>)(msg =>
                {
                    lastAutoUpdate = Time.time;
                    autoSteerAngle = (float) msg.twist.angular.x;
                }));

                seq = 0;
                LgsvlSimulatorCmdWriter = Bridge.AddWriter<Ros.TwistStamped>(SIMULATOR_CMD_TOPIC);
            }
            else if (TargetRosEnv == ROSTargetEnvironment.APOLLO35)
            {
                Bridge.AddReader<apollo.control.ControlCommand>(APOLLO_CMD_TOPIC, (System.Action<apollo.control.ControlCommand>)(msg =>
                {
                    if (double.IsInfinity(msg.brake) ||  double.IsNaN(msg.brake) ||
                        double.IsInfinity(msg.throttle) ||  double.IsNaN(msg.throttle))
                    {
                        return;
                    }

                    lastAutoUpdate = Time.time;
                    var pedals = GetComponent<PedalInputController>();
                    throttle = pedals.throttleInputCurve.Evaluate((float) msg.throttle / 100);
                    brake = pedals.brakeInputCurve.Evaluate((float) msg.brake / 100);
                    var linearAccel = throttle - brake;

                    var timeStamp = (float) msg.header.timestamp_sec; 
                    
                    var steeringTarget = -((float) msg.steering_target) / 100;
                    var dt = timeStamp - lastTimeStamp;
                    lastTimeStamp = timeStamp;

                    var steeringAngle = controller.steerInput;

                    var sgn = Mathf.Sign(steeringTarget - steeringAngle);
                    var steeringRate = (float) msg.steering_rate * sgn;

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

        };
    }

    private void AddUIElement()
    {
        if (TargetRosEnv == ROSTargetEnvironment.LGSVL)
        {
            var controlCheckbox = GetComponent<UserInterfaceTweakables>().AddCheckbox("PublishControlCommand", "Publish Control Command:", isControlEnabled);
            controlCheckbox.onValueChanged.AddListener(x => isControlEnabled = x);
        }
    }
}
