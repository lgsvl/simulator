/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;

public class RosClock : MonoBehaviour, Comm.BridgeClient
{
    Comm.Bridge Bridge;
    Comm.Writer<Ros.Clock> RosClockWriter;
    public string SimulatorTopic = "/clock";
    private volatile bool is_connected = false;

    public void OnBridgeAvailable(Comm.Bridge bridge)
    {
        Debug.Log("rosbridge for /clock available");
        Bridge = bridge;
        //Bridge.AddPublisher(this);
        Bridge.OnConnected += () =>
        {
            RosClockWriter = Bridge.AddWriter<Ros.Clock>(SimulatorTopic);
            is_connected = true;
        };
    }

    // Use this for initialization
    void Start ()
    {

    }
	
	// Update is called once per frame
	void Update ()
    {

    }

    // Update is called in fixed Rate
    void FixedUpdate()
    {
        if (is_connected)
        {
            var clock_msg = new Ros.Clock();
            clock_msg.clock = Ros.Time.Now();
            RosClockWriter.Publish(clock_msg);
        }
    }
}
