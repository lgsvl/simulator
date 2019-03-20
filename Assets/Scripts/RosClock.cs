/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using UnityEngine;

public class RosClock : MonoBehaviour, Comm.BridgeClient
{
    Comm.Bridge Bridge;
    Comm.Writer<Ros.Clock> RosClockWriter;

    public string SimulatorTopic = "/clock";

    public void GetSensors(List<Component> sensors)
    {
        sensors.Add(this);
    }

    public void OnBridgeAvailable(Comm.Bridge bridge)
    {
        Debug.Log("rosbridge for /clock available");
        Bridge = bridge;
        //Bridge.AddPublisher(this);
        Bridge.OnConnected += () =>
        {
            RosClockWriter = Bridge.AddWriter<Ros.Clock>(SimulatorTopic);
        };
    }

    void FixedUpdate()
    {
        if (Bridge != null && Bridge.Status == Comm.BridgeStatus.Connected)
        {
            var clock_msg = new Ros.Clock();
            clock_msg.clock = Ros.Time.Now();
            RosClockWriter.Publish(clock_msg);
        }
    }
}
