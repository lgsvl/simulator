/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

 using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;

[RequireComponent(typeof(KeyboardInputController))]
[RequireComponent(typeof(SteeringWheelInputController))]
public class ApolloAutoMode : MonoBehaviour, Comm.BridgeClient
{
    Comm.Bridge Bridge;

    KeyboardInputController Keyboard;
    SteeringWheelInputController Wheel;

    void Start()
    {
        Keyboard = GetComponent<KeyboardInputController>();
        Wheel = GetComponent<SteeringWheelInputController>();

        Wheel.TriggerPress += ev =>
        {
            if (ev == InputEvent.AUTONOMOUS_MODE_OFF)
            {
                StopControl();
            }
        };
    }

    void Update()
    {
        // NOTE: don't use keyboard input to disable control mode for Apollo, use only wheel acceleration
        //if (Keyboard.AccelBrakeInput != 0.0f || Keyboard.SteerInput != 0.0f || Wheel.AccelBrakeInput != 0.0f)
        if (Wheel.AccelBrakeInput != 0.0f)
        {
            StopControl();
        }
    }

    void StopControl()
    {
        int port = 8888; // TODO: get dreamview port
        var ws = new WebSocket($"ws://{Bridge.Address}:{port}/websocket");

        ws.OnOpen += (sender, args) =>
        {
            string[] modules = { "control", "navigation_control" };
            foreach (var module in modules)
            {
                var msg = $"{{ \"type\":\"ExecuteModuleCommand\",\"module\":\"{module}\",\"command\":\"stop\"}}";
                ws.Send(msg);
            }

            Wheel.autonomousBehavior = SteerWheelAutonomousFeedbackBehavior.InputAndOutputNoRoadFeedback;

            ws.Close();
        };

        ws.OnError += (sender, args) =>
        {
            Debug.Log(args.Message);
            if (args.Exception != null)
            {
                Debug.LogError(args.Exception.ToString());
            }
        };

        ws.ConnectAsync();
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
            Bridge.AddReader<Ros.control_command>(VehicleInputController.APOLLO_CMD_TOPIC, msg =>
            {
                Wheel.autonomousBehavior = SteerWheelAutonomousFeedbackBehavior.OutputOnly;
            });
        };
    }
}
