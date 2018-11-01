using UnityEngine;
using WebSocketSharp;

[RequireComponent(typeof(KeyboardInputController))]
[RequireComponent(typeof(SteeringWheelInputController))]
public class ApolloAutoMode : MonoBehaviour, Ros.IRosClient
{
    Ros.Bridge Bridge;

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
        if (Keyboard.AccelBrakeInput != 0.0f || Keyboard.SteerInput != 0.0f || Wheel.AccelBrakeInput != 0.0f)
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

    public void OnRosBridgeAvailable(Ros.Bridge bridge)
    {
        Bridge = bridge;
    }

    public void OnRosConnected()
    {
        Bridge.Subscribe<Ros.control_command>(VehicleInputController.APOLLO_CMD_TOPIC, msg =>
        {
            Wheel.autonomousBehavior = SteerWheelAutonomousFeedbackBehavior.OutputOnly;
        });
    }
}
