/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;

public class VehicleInputController : MonoBehaviour, Ros.IRosClient
{
    static readonly string AUTOWARE_CMD_TOPIC = "/vehicle_cmd";
    
    //public float angularVelocityScaler = 1.0f;
    //public float linearVelocityScaler = 1.0f;

    public float targetLinVel = 0f; //assuming meter per second
    public float targetAngVel = 0f; //assuming radian per second

    public float actualLinVel = 0f; //same unit as target velocity
    public float actualAngVel = 0f; //same unit as target angular velocity

    public float constAccel = 1.0f; //max 1
    public float constSteer = 1.0f; // max 1

    CarInputController input;
    VehicleController controller;

    Ros.Bridge Bridge;
    bool keyboard;

    void Awake()
    {
        controller = GetComponent<VehicleController>();
        input = GetComponent<CarInputController>();
        input[InputEvent.ENABLE_LEFT_TURN_SIGNAL].Press += controller.EnableLeftTurnSignal;
        input[InputEvent.ENABLE_RIGHT_TURN_SIGNAL].Press += controller.EnableRightTurnSignal;
        input[InputEvent.GEARBOX_SHIFT_DOWN].Press += controller.GearboxShiftDown;
        input[InputEvent.GEARBOX_SHIFT_UP].Press += controller.GearboxShiftUp;
        input[InputEvent.ENABLE_HANDBRAKE].Press += controller.EnableHandbrake;
        input[InputEvent.HEADLIGHT_MODE_CHANGE].Press += controller.ChangeHeadlightMode;
        input[InputEvent.TOGGLE_IGNITION].Press += controller.ToggleIgnition;
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
    }

    void Update()
    {
        //grab input values
        float steerInput = input.SteerInput;
        float accelInput = input.AccelBrakeInput;

        if (steerInput != 0.0f || accelInput != 0.0f)
        {
            keyboard = true;

            float k = 0.4f + 0.6f * controller.CurrentSpeed / 30.0f;
            //float steerPow = 1.0f + Mathf.Min(1.0f, k);

            //convert inputs to torques
            controller.steerInput = steerInput; // Mathf.Sign(steerInput) * Mathf.Pow(Mathf.Abs(steerInput), steerPow);
            controller.accellInput = accelInput < 0.0f ? accelInput : accelInput * Mathf.Min(1.0f, k);
        }
        else
        {
            keyboard = false;
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
        if (!keyboard)
        {
            controller.accellInput = 0;
            var linMag = Mathf.Abs(targetLinVel - actualLinVel);
            if (actualLinVel < targetLinVel && !controller.InReverse)
            {
                controller.accellInput = Mathf.Clamp(linMag, 0, constAccel);
            }
            else if(actualLinVel > targetLinVel && !controller.InReverse)
            {
                controller.accellInput = -Mathf.Clamp(linMag, 0, constAccel);
            }
            controller.steerInput = -Mathf.Clamp(targetAngVel * 0.5f, -constSteer, constSteer);
        }
    }

    public void OnRosBridgeAvailable(Ros.Bridge bridge)
    {
        Bridge = bridge;
        bridge.AddPublisher(this);
    }

    public void OnRosConnected()
    {
        Bridge.Subscribe(AUTOWARE_CMD_TOPIC, (System.Action<Ros.VehicleCmd>)((Ros.VehicleCmd msg) =>
        {
            var targetLinear = msg.twist_cmd.twist.linear;
            var targetAngular = msg.twist_cmd.twist.angular;

            if (!keyboard)
            {
                targetAngVel = (float)targetAngular.z;
                targetLinVel = (float)targetLinear.x;
            }
        }));
    }
}
